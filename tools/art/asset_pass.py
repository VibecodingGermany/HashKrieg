"""Asset-Pass fuer den Hashkrieg-Art-Strang: macht aus einem konvertierten Tripo-FBX
ein spielfertiges, animierbares Asset.

Aufruf (Blender 5.2 LTS headless), ein Asset pro Lauf:

    /Applications/Blender.app/Contents/MacOS/Blender --background --python \\
        tools/art/asset_pass.py -- --asset UNIT_Alliance_BattleTank \\
        --src ../Hashkrieg_Assets/3d/unity_ready --dst Assets/_Project/Art \\
        [--preview <dir>] [--no-export]

Quelle ist IMMER das unveraenderte Konvertat unter ``--src`` (Hashkrieg_Assets,
ausserhalb von Git); geschrieben wird nur nach ``--dst``. Ein zweiter Lauf
ergibt also dasselbe Ergebnis und ueberschreibt keine Eingabe.

Was je Asset passiert, steht in ``tools/art/asset_recipes.json``:

1. ``yaw_fix``      Feinkorrektur der Blickrichtung (Grad, um Blender-Z).
2. ``drop_boxes``   Faces in normierten Boxen loeschen (schwebende Splitter).
3. ``smooth_angle`` Normalen nach Kantenwinkel glaetten + gewichtete Normalen:
                    Tripo liefert facettierte Flaechen, die wie zerknittertes
                    Papier schattieren.
4. Textur           AO und konvexe Kanten mit Cycles backen, Basisfarbe auf die
                    D-072-Fraktionspalette heben, Leuchtakzente als Emission
                    ausschneiden. Schreibt ``T_*_BC``, ``T_*_MSK`` (R Metallic,
                    G AO, B TeamMask, A Smoothness, ArtAssetStandard 5.1) und
                    ``T_*_EM``.
5. ``parts``        Mesh in animierbare Teile trennen, Pivots setzen und als
                    Knoten-Hierarchie exportieren. Unity findet die Knoten ueber
                    ihre Namen (UnitRigAnimator):

                        Root
                        +- Body            Federung (Nicken/Wippen)
                           +- <Mesh>_Hull_LODn
                           +- Turret       Gier zum Ziel        (kind turret)
                           |  +- Gun       Rueckstoss/Elevation  (kind gun)
                           +- Spin_*       Dauerrotation um Hoch (kind spin)
                           +- Wheel_*      Rollen um Querachse   (kind wheel)
                           +- Arm_*        Pendeln um Querachse  (kind arm)

Koordinaten in den Rezepten sind auf die LOD0-Bounding-Box normiert:
x 0..1 links->rechts, y 0..1 vorne->hinten (vorne = Blender -Y = Unity +Z),
z 0..1 unten->oben. Pivots ebenso, oder ``auto``.
"""

import argparse
import json
import math
import os
import sys

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import rig_tank_hierarchy as tank  # noqa: E402  (find_ring_z, find_gun_y, gun_axis)

RECIPES = os.path.join(os.path.dirname(os.path.abspath(__file__)), "asset_recipes.json")

# D-072-Paletten (sRGB). Rampen fuer die Gradient-Map, dunkel -> hell.
PALETTES = {
    "Alliance": [(0.00, "#0b1016"), (0.28, "#24384e"), (0.55, "#6f7d8a"), (0.78, "#a9b3bc"), (1.00, "#e6ecf1")],
    "Legion":   [(0.00, "#140c08"), (0.30, "#3e1b12"), (0.55, "#7A3524"), (0.78, "#a0612c"), (1.00, "#e2c08a")],
}
ACCENT = {"Alliance": "#4FD8FF", "Legion": "#ff8a2a"}


# ---------------------------------------------------------------- Hilfen

def hex_rgb(h):
    h = h.lstrip("#")
    return np.array([int(h[i:i + 2], 16) / 255.0 for i in (0, 2, 4)], dtype=np.float32)


def parse_args():
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    p = argparse.ArgumentParser(prog="asset_pass")
    p.add_argument("--asset", required=True, help="z. B. UNIT_Alliance_BattleTank")
    p.add_argument("--src", required=True, help="Wurzel der unveraenderten Konvertate (unity_ready)")
    p.add_argument("--dst", required=True, help="Wurzel der Spiel-Art (Assets/_Project/Art)")
    p.add_argument("--preview", default="", help="Ordner fuer Kontrollbilder")
    p.add_argument("--no-export", action="store_true")
    return p.parse_args(argv)


def load_recipe(asset):
    with open(RECIPES) as f:
        all_recipes = json.load(f)
    defaults = all_recipes.get("_defaults", {})
    if asset not in all_recipes:
        raise SystemExit(f"[asset_pass] kein Rezept fuer {asset} in {RECIPES}")
    r = dict(defaults)
    r.update(all_recipes[asset])
    r["texture"] = {**defaults.get("texture", {}), **all_recipes[asset].get("texture", {})}
    return r


def lod_objects():
    meshes = [o for o in bpy.data.objects if o.type == "MESH"]
    return sorted(meshes, key=lambda o: o.name[-1] if o.name[-4:-1] == "LOD" else "9")


def world_bbox(objs):
    pts = [o.matrix_world @ Vector(c) for o in objs for c in o.bound_box]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def denorm(bb, n):
    mn, mx = bb
    return Vector((mn.x + (mx.x - mn.x) * n[0], mn.y + (mx.y - mn.y) * n[1], mn.z + (mx.z - mn.z) * n[2]))


def in_box(bb, box):
    if box and isinstance(box[0], (list, tuple)):  # Liste von Boxen = Vereinigung
        preds = [in_box(bb, b) for b in box]
        return lambda c: any(p(c) for p in preds)
    lo = denorm(bb, (box[0], box[2], box[4]))
    hi = denorm(bb, (box[1], box[3], box[5]))
    return lambda c: lo.x <= c.x <= hi.x and lo.y <= c.y <= hi.y and lo.z <= c.z <= hi.z


def activate(o):
    bpy.ops.object.select_all(action="DESELECT")
    o.select_set(True)
    bpy.context.view_layer.objects.active = o


def edit_faces(obj):
    """Edit-Mode im Face-Modus mit leerer Auswahl - sonst wandern beim Trennen
    noch markierte Vertices aus frueheren Operationen mit."""
    activate(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_mode(type="FACE")
    bpy.ops.mesh.select_all(action="DESELECT")
    return bmesh.from_edit_mesh(obj.data)


def face_islands(bm):
    seen, out = set(), []
    for f in bm.faces:
        if f.index in seen:
            continue
        isl, stack = [], [f]
        while stack:
            x = stack.pop()
            if x.index in seen:
                continue
            seen.add(x.index)
            isl.append(x)
            for e in x.edges:
                stack.extend(l for l in e.link_faces if l.index not in seen)
        out.append(isl)
    return out


def split_faces(obj, pred, newname, islands=0.0):
    """Faces mit pred(Weltzentrum) in ein neues Objekt trennen; None wenn keine.

    ``islands`` > 0 waehlt ganze zusammenhaengende Inseln, von deren Faces
    mindestens dieser Anteil pred erfuellt - fuer Raeder, deren Radkasten-
    Kante geometrisch im Radkreis liegt, aber zur Karosserie-Insel gehoert.
    """
    bm = edit_faces(obj)
    mw = obj.matrix_world
    hit = 0
    if islands > 0:
        bm.faces.ensure_lookup_table()
        for isl in face_islands(bm):
            inside = sum(1 for f in isl if pred(mw @ f.calc_center_median()))
            take = inside >= islands * len(isl)
            for f in isl:
                f.select = take
            hit += len(isl) if take else 0
    else:
        for f in bm.faces:
            f.select = bool(pred(mw @ f.calc_center_median()))
            hit += f.select
    bm.select_flush_mode()
    bmesh.update_edit_mesh(obj.data)
    new = None
    if hit and hit < len(bm.faces):
        before = set(bpy.data.objects)
        bpy.ops.mesh.separate(type="SELECTED")
        new = next(iter(set(bpy.data.objects) - before))
        new.name = newname
    bpy.ops.object.mode_set(mode="OBJECT")
    return new


def delete_faces(obj, pred):
    bm = edit_faces(obj)
    mw = obj.matrix_world
    doomed = [f for f in bm.faces if pred(mw @ f.calc_center_median())]
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    return len(doomed)


# ---------------------------------------------------------------- Geometrie

def drop_needles(o, diag, max_faces=6, min_len=0.22, max_width=0.04):
    """Loose Splitter: Inseln mit hoechstens ``max_faces`` Faces, die laenger als
    ``min_len`` x Diagonale und duenner als ``max_width`` x Laenge sind.

    Der Konverter filtert nur Inseln bis acht Dreiecke ueber 15 % Diagonale;
    einzelne Nadeln ueberleben, wenn sie an Nachbarn haengen. Sie kosten mehr als
    Optik: UnitViewManager skaliert ueber die Renderer-Bounds, ein 6-m-Spiess
    laesst die ganze Einheit schrumpfen.
    """
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.faces.ensure_lookup_table()
    seen, doomed = set(), []
    for f in bm.faces:
        if f.index in seen:
            continue
        isl, stack = [], [f]
        while stack:
            x = stack.pop()
            if x.index in seen:
                continue
            seen.add(x.index)
            isl.append(x)
            for e in x.edges:
                stack.extend(l for l in e.link_faces if l.index not in seen)
        if len(isl) > max_faces:
            continue
        pts = [v.co for g in isl for v in g.verts]
        d = sorted((max(p[i] for p in pts) - min(p[i] for p in pts)) for i in range(3))
        if d[2] > min_len * diag and d[1] < max_width * d[2]:
            doomed.extend(isl)
    if doomed:
        bmesh.ops.delete(bm, geom=list({g.index: g for g in doomed}.values()), context="FACES")
        bm.to_mesh(o.data)
    bm.free()
    return len(doomed)


def move_faces(obj, pred, offset):
    bm = edit_faces(obj)
    mw = obj.matrix_world
    verts = {v for f in bm.faces if pred(mw @ f.calc_center_median()) for v in f.verts}
    for v in verts:
        v.co += offset
    bmesh.update_edit_mesh(obj.data)
    bpy.ops.object.mode_set(mode="OBJECT")
    return len(verts)

def apply_yaw(objs, deg):
    if not deg:
        return
    rot = Matrix.Rotation(math.radians(deg), 4, "Z")
    for o in objs:
        o.matrix_world = rot @ o.matrix_world
        activate(o)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def smooth_normals(o, angle_deg, merge_dist):
    """Facettierte Tripo-Flaechen glaetten, harte Kanten behalten.

    Merge-by-distance verbindet die an UV-Naehten doppelten Vertices, damit die
    Glaettung ueber die Naht reicht (UVs liegen an den Loops und bleiben). Die
    gewichteten Normalen halten grosse Flaechen plan - der Hard-Surface-Look,
    den ein RTS aus 40 m Kamerahoehe noch lesen kann.
    """
    if not angle_deg:
        return
    activate(o)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.remove_doubles(threshold=merge_dist)
    bpy.ops.object.mode_set(mode="OBJECT")
    bpy.ops.object.shade_smooth_by_angle(angle=math.radians(angle_deg), keep_sharp_edges=True)
    m = o.modifiers.new("wn", "WEIGHTED_NORMAL")
    m.keep_sharp = True
    m.weight = 50
    # Smooth-by-Angle ist in 5.x ein Geometry-Nodes-Modifier; beide anwenden,
    # damit LOD-Splits und Export dieselben Normalen sehen.
    for mod in list(o.modifiers):
        bpy.ops.object.modifier_apply(modifier=mod.name)


# ---------------------------------------------------------------- Textur

def ensure_cycles(samples):
    sc = bpy.context.scene
    sc.render.engine = "CYCLES"
    sc.cycles.device = "CPU"
    sc.cycles.samples = samples
    sc.render.bake.margin = 16


def image_np(img):
    a = np.empty(img.size[0] * img.size[1] * 4, dtype=np.float32)
    img.pixels.foreach_get(a)
    return a.reshape(img.size[1], img.size[0], 4)


def np_image(name, arr, colorspace):
    h, w = arr.shape[:2]
    img = bpy.data.images.new(name, w, h, alpha=True)
    img.colorspace_settings.name = colorspace
    img.pixels.foreach_set(np.ascontiguousarray(arr, dtype=np.float32).ravel())
    return img


def bake(o, kind, size, build_nodes):
    """Backt in ein neues Bild; build_nodes(tree) verdrahtet das Material."""
    mat = bpy.data.materials.new(f"bake_{kind}")
    tree = mat.node_tree
    for n in list(tree.nodes):
        if n.type != "OUTPUT_MATERIAL":
            tree.nodes.remove(n)
    build_nodes(tree)
    img = bpy.data.images.new(f"bake_{kind}", size, size, alpha=False)
    img.colorspace_settings.name = "Non-Color"
    tn = tree.nodes.new("ShaderNodeTexImage")
    tn.image = img
    tree.nodes.active = tn
    o.data.materials.clear()
    o.data.materials.append(mat)
    activate(o)
    bpy.ops.object.bake(type=kind)
    return image_np(img)[..., 0].copy()


def bake_ao(o, size, distance):
    def nodes(t):
        out = next(n for n in t.nodes if n.type == "OUTPUT_MATERIAL")
        bs = t.nodes.new("ShaderNodeBsdfDiffuse")
        t.links.new(bs.outputs[0], out.inputs[0])
    bpy.context.scene.world = bpy.context.scene.world or bpy.data.worlds.new("w")
    bpy.context.scene.world.light_settings.distance = distance
    return bake(o, "AO", size, nodes)


def bake_edges(o, size, radius):
    """Konvexe Kanten: Abweichung der Bevel-Normale von der wahren Normale,
    nur wo Pointiness > 0.5 (konvex). Konkave Kanten macht schon die AO dunkel."""
    def nodes(t):
        out = next(n for n in t.nodes if n.type == "OUTPUT_MATERIAL")
        bev = t.nodes.new("ShaderNodeBevel"); bev.inputs["Radius"].default_value = radius
        bev.samples = 12
        geo = t.nodes.new("ShaderNodeNewGeometry")
        dot = t.nodes.new("ShaderNodeVectorMath"); dot.operation = "DOT_PRODUCT"
        t.links.new(bev.outputs[0], dot.inputs[0]); t.links.new(geo.outputs["True Normal"], dot.inputs[1])
        inv = t.nodes.new("ShaderNodeMath"); inv.operation = "SUBTRACT"; inv.inputs[0].default_value = 1.0
        t.links.new(dot.outputs["Value"], inv.inputs[1])
        gain = t.nodes.new("ShaderNodeMath"); gain.operation = "MULTIPLY"; gain.inputs[1].default_value = 6.0
        gain.use_clamp = True
        t.links.new(inv.outputs[0], gain.inputs[0])
        cvx = t.nodes.new("ShaderNodeMath"); cvx.operation = "GREATER_THAN"; cvx.inputs[1].default_value = 0.5
        t.links.new(geo.outputs["Pointiness"], cvx.inputs[0])
        m = t.nodes.new("ShaderNodeMath"); m.operation = "MULTIPLY"
        t.links.new(gain.outputs[0], m.inputs[0]); t.links.new(cvx.outputs[0], m.inputs[1])
        em = t.nodes.new("ShaderNodeEmission")
        t.links.new(m.outputs[0], em.inputs["Color"])
        t.links.new(em.outputs[0], out.inputs[0])
    return bake(o, "EMIT", size, nodes)


def gradient_map(lum, stops):
    xs = np.array([s for s, _ in stops], dtype=np.float32)
    cols = np.stack([hex_rgb(c) for _, c in stops])
    return np.stack([np.interp(lum, xs, cols[:, i]) for i in range(3)], axis=-1)


def rgb_to_hsv(rgb):
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    mx = rgb.max(-1); mn = rgb.min(-1); d = mx - mn
    h = np.zeros_like(mx)
    m = d > 1e-5
    rm = m & (mx == r); gm = m & (mx == g) & ~rm; bm = m & ~rm & ~gm
    h[rm] = ((g - b)[rm] / d[rm]) % 6
    h[gm] = (b - r)[gm] / d[gm] + 2
    h[bm] = (r - g)[bm] / d[bm] + 4
    s = np.where(mx > 1e-5, d / np.maximum(mx, 1e-5), 0)
    return h * 60.0, s, mx


def grade(bc, ao, edge, faction, t):
    """Neue Basisfarbe + Masken. bc in sRGB-Werten (wie gespeichert).

    1. Belichtung: Luminanz auf ``target_mean`` anheben (Gain gedeckelt), mit
       weicher Reinhard-Schulter statt harter Perzentil-Spreizung - letztere
       trieb sehr dunkle Texturen ins Lachsrosa.
    2. Saettigung je Fraktion: die Allianz zieht von Petrol Richtung Stahlgrau.
    3. Gradient-Map auf die D-072-Palette, gemischt mit dem angehobenen Original
       (Tripo-Details wie Kabel, Rost, Leuchten leben von der Eigenfarbe).
    4. Fremde Farbtoene (gesaettigt, ausserhalb des Fraktionsbands: Aetherium-
       Kristalle, Warnlichter) bleiben im Original-Farbton.
    5. Leuchtakzente als Emission ausschneiden.
    """
    W = np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    rgb = bc[..., :3]
    lum = rgb @ W
    mean = float(lum.mean())
    gain = min(t["max_gain"], max(1.0, t["target_mean"] / max(mean, 1e-3)))
    L = lum * gain
    wp = t["white_point"]
    Ls = L * (1 + L / (wp * wp)) / (1 + L)
    scale = np.where(lum > 1e-4, Ls / np.maximum(lum, 1e-4), 0)[..., None]
    raw_lift = np.clip(rgb * scale, 0, 1)
    g = (raw_lift @ W)[..., None]
    lifted = np.clip(g + (raw_lift - g) * t["saturation_by_faction"][faction], 0, 1)

    h, s, v = rgb_to_hsv(rgb)
    lo, hi = t["hue_band_by_faction"][faction]
    in_band = ((h - lo) % 360) <= ((hi - lo) % 360)
    foreign = ((~in_band) & (s > 0.35) & (v > 0.15)).astype(np.float32)[..., None]

    # Zwei Masken: "paint" faerbt Zierstreifen in der Akzentfarbe ein, "glow"
    # (strenger) leuchtet zusaetzlich. Eine einzige weite Maske liess ganze
    # hellblaue Paneele gluehen; eine einzige enge verlor die Zierstreifen.
    acc = t.get("accent") or t["accent_by_faction"][faction]
    hue_ok = ((h - acc["hue"][0]) % 360) <= ((acc["hue"][1] - acc["hue"][0]) % 360)
    def mask(sat, val):
        m = (hue_ok & (s >= sat) & (v >= val)).astype(np.float32)
        # sauber aufweiten, damit die Emission an UV-Raendern nicht franst
        m = np.maximum(m, np.roll(m, 1, 0))
        return np.maximum(m, np.roll(m, 1, 1))
    paint = mask(acc["sat"], acc["val"])
    accent = mask(acc.get("glow_sat", acc["sat"]), acc.get("glow_val", acc["val"]))

    painted = gradient_map(np.clip(Ls, 0, 1), PALETTES[faction])
    mix = t["palette_mix_by_faction"][faction]
    base = painted * mix + lifted * (1 - mix)
    base = base * (1 - foreign) + raw_lift * foreign

    occl = 1.0 - t["ao_strength"] * (1.0 - ao)
    base = base * occl[..., None]
    base = base + edge[..., None] * t["edge_highlight"] * (1.0 - base)

    acc_col = hex_rgb(ACCENT[faction])
    base = base * (1 - paint[..., None]) + (acc_col * 0.85) * paint[..., None]
    # Unter dem Leuchtakzent bleibt die Grundfarbe dunkel: Die Emission traegt
    # den Farbton. Hell darunter addiert sich mit der Emission zu Weiss.
    base = base * (1 - accent[..., None]) + (acc_col * 0.35) * accent[..., None]
    base = np.clip(base, 0, 1)

    em = np.zeros_like(bc)
    em[..., :3] = acc_col * accent[..., None]
    em[..., 3] = 1

    msk = np.zeros_like(bc)
    msk[..., 0] = t["metallic"] * (1 - accent)                       # R Metallic
    msk[..., 1] = 1.0 - 0.4 * (1.0 - ao)                              # G AO (gedaempft: Basisfarbe traegt sie schon)
    msk[..., 2] = 0.0                                                 # B TeamMask (folgt mit NovaUnit)
    msk[..., 3] = np.clip(t["smoothness"] + 0.25 * edge, 0, 1)        # A Smoothness

    out = np.empty_like(bc)
    out[..., :3] = base
    out[..., 3] = 1
    stats = {"lum_in_mean": round(mean, 4), "gain": round(gain, 3),
             "lum_out_mean": round(float((base @ W).mean()), 4),
             "paint_share": round(float(paint.mean()), 4), "accent_share": round(float(accent.mean()), 4),
             "foreign_share": round(float(foreign.mean()), 4)}
    return out, msk, em, stats


def texture_pass(lod0, tex_path, faction, t, out_dir, stem):
    src = bpy.data.images.load(tex_path)
    size = src.size[0]
    bc = image_np(src).copy()
    # Geometrie-AO und Kanten nur auf Wunsch: Auf Tripo-Meshes ueberlappen
    # sich ~1000 Scherben, jeder AO-Strahl trifft die Nachbarschale, und ganze
    # Flaechen backen schwarz. Die gemalte Textur traegt ihre AO schon.
    ao = np.ones(bc.shape[:2], dtype=np.float32)
    edge = np.zeros(bc.shape[:2], dtype=np.float32)
    if t["ao_strength"] > 0 or t["edge_highlight"] > 0:
        ensure_cycles(t["bake_samples"])
        diag = (Vector(world_bbox([lod0])[1]) - Vector(world_bbox([lod0])[0])).length
        if t["ao_strength"] > 0:
            ao = bake_ao(lod0, size, diag * t["ao_distance"])
        if t["edge_highlight"] > 0:
            edge = bake_edges(lod0, size, diag * t["edge_radius"])
    new_bc, msk, em, stats = grade(bc, ao, edge, faction, t)
    paths = {}
    for suffix, arr, cs in (("BC", new_bc, "sRGB"), ("MSK", msk, "Non-Color"), ("EM", em, "sRGB")):
        img = np_image(f"{stem}_{suffix}", arr, cs)
        p = os.path.join(out_dir, f"T_{stem}_{suffix}.png")
        img.filepath_raw = p
        img.file_format = "PNG"
        img.save()
        paths[suffix] = p
    lod0.data.materials.clear()
    return paths, stats, new_bc


# ---------------------------------------------------------------- Teile

def split_parts(lods, recipe, bb):
    """Liefert {lodname: {partname: obj}} plus Pivots {partname: Vector}."""
    parts_cfg = recipe.get("parts", [])
    result = {o.name[-4:]: {"Hull": o} for o in lods}
    pivots, parents, kinds = {"Body": Vector((0, 0, 0))}, {}, {}
    lod0 = lods[0]

    for cfg in parts_cfg:
        name, kind = cfg["name"], cfg["kind"]
        src = cfg.get("from", "Hull")
        parents[name] = cfg.get("parent", "Body")
        kinds[name] = kind
        if kind == "turret":
            ring_z = cfg.get("ring_z")
            if ring_z is None:
                ring_z, _ = tank.find_ring_z(result["LOD0"][src])
            elif cfg.get("ring_z_norm"):
                ring_z = denorm(bb, (0, 0, ring_z)).z
            box = in_box(bb, cfg["box"]) if "box" in cfg else (lambda c: True)
            pred = lambda c, rz=ring_z, bx=box: c.z > rz and bx(c)
        elif kind == "wheel":
            ext = bb[1] - bb[0]
            ay = bb[0].y + ext.y * cfg["axle"][0]
            az = bb[0].z + ext.z * cfg["axle"][1]
            rw = ext.z * cfg["r"]
            x0 = bb[0].x + ext.x * cfg["x"][0]
            x1 = bb[0].x + ext.x * cfg["x"][1]
            pred = lambda c, ay=ay, az=az, rw=rw, x0=x0, x1=x1: x0 <= c.x <= x1 and math.hypot(c.y - ay, c.z - az) <= rw
            cfg = dict(cfg, _axle=(ay, az))
        elif kind == "gun" and "box" in cfg:
            pred = in_box(bb, cfg["box"])
        elif kind == "gun":
            turret0 = result["LOD0"][src]
            g = tank.gun_axis(turret0)
            gun_y = cfg.get("gun_y") or tank.find_gun_y(turret0)
            if gun_y is None or g is None:
                print(f"  {name}: kein Rohr erkannt, uebersprungen"); continue
            rmax = g["r0"] * cfg.get("radius_factor", 2.0)
            pred = lambda c, gy=gun_y, g=g, r=rmax: c.y < gy and math.hypot(c.x - g["ax"], c.z - g["az"]) <= r
            cfg = dict(cfg, _gun=(gun_y, g))
        else:
            pred = in_box(bb, cfg["box"])

        for lodname, d in result.items():
            if src not in d:
                continue
            piece = split_faces(d[src], pred, f"__{name}_{lodname}", cfg.get("islands", 0.0))
            if piece is not None:
                d[name] = piece

        if name not in result["LOD0"]:
            print(f"  {name}: keine Faces getroffen")
            continue
        pivots[name] = part_pivot(result["LOD0"][name], cfg, bb)
        print(f"  {name} ({kind}): LOD0 {len(result['LOD0'][name].data.polygons)} Faces, Pivot {tuple(round(v, 3) for v in pivots[name])}")
    return result, pivots, parents, kinds


def part_pivot(obj, cfg, bb):
    pts = [obj.matrix_world @ v.co for v in obj.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    piv = cfg.get("pivot", "auto")
    if isinstance(piv, list):
        return denorm(bb, piv)
    kind = cfg["kind"]
    if kind == "turret":
        band = [p for p in pts if p.z < mn.z + 0.35]
        ref = band if len(band) > 20 else pts
        return Vector(((min(p.x for p in ref) + max(p.x for p in ref)) / 2,
                       (min(p.y for p in ref) + max(p.y for p in ref)) / 2, mn.z))
    if kind == "gun" and "_gun" in cfg:
        gun_y, g = cfg["_gun"]
        return Vector((g["ax"], mx.y, g["az"]))
    if kind == "gun":  # per Box: Schildzapfen am hinteren Ende, mittig im Rohrquerschnitt
        rear = [p for p in pts if p.y > mx.y - 0.25 * (mx.y - mn.y)]
        return Vector((sum(p.x for p in rear) / len(rear), mx.y, sum(p.z for p in rear) / len(rear)))
    if kind == "wheel":
        ay, az = cfg["_axle"]
        return Vector(((mn.x + mx.x) / 2, ay, az))
    if kind == "spin":
        return Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, mn.z))
    if kind == "arm":
        return Vector(((mn.x + mx.x) / 2, mx.y, mn.z))
    return (mn + mx) / 2  # wheel und Rest: Mittelpunkt


def squash(result, pivots, bb, cfg):
    """Staucht alles unterhalb ``below`` (normiert) auf ``factor`` und rueckt
    den Rest nach. Fuer Tuerme, die im Spiel mehrere Gebaeudehoehen messen
    (Allianz-Radar: 20 m Saeule unter 6 m Schuessel). Laeuft nach dem Teilen,
    damit die Rezept-Boxen weiter die Original-Bounding-Box meinen."""
    cut = bb[0].z + (bb[1].z - bb[0].z) * cfg["below"]
    k = cfg["factor"]

    def f(z):
        return bb[0].z + (z - bb[0].z) * k if z < cut else z - (cut - bb[0].z) * (1 - k)

    for d in result.values():
        for obj in d.values():
            mw = obj.matrix_world
            inv = mw.inverted()
            for v in obj.data.vertices:
                w = mw @ v.co
                w.z = f(w.z)
                v.co = inv @ w
            obj.data.update()
    for name, p in pivots.items():
        p.z = f(p.z)
    print(f"  squash unter z={cut:.2f} auf {k:.0%}")


def pick_texel(bc, tone, radius=6):
    """UV einer ruhigen, ungesaettigten Texturstelle im Helligkeitsband ``tone``.

    Der Sockel bekommt kein eigenes Material (ein Material je Asset); alle
    seine UVs zeigen auf einen Texel, dessen Umgebung auch in den Mipmaps
    einfarbig bleibt.
    """
    rgb = bc[..., :3]
    lum = rgb @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    sat = rgb.max(-1) - rgb.min(-1)

    def box(a):
        c = np.cumsum(np.cumsum(np.pad(a, ((1, 0), (1, 0))), 0), 1)
        k = 2 * radius + 1
        return (c[k:, k:] - c[:-k, k:] - c[k:, :-k] + c[:-k, :-k]) / (k * k)

    mean, mean2 = box(lum), box(lum * lum)
    var = mean2 - mean * mean
    sat_m = box(sat)
    score = var * 50 + sat_m + np.where((mean >= tone[0]) & (mean <= tone[1]), 0, 10)
    y, x = np.unravel_index(np.argmin(score), score.shape)
    y, x = y + radius, x + radius
    h, w = lum.shape
    return (x + 0.5) / w, (y + 0.5) / h, float(mean[y - radius, x - radius])


def add_pad(result, pivots, cfg, bc):
    """Flacher Betonsockel unter einem Turm, der den Gebaeude-Footprint fuellt.

    UnitViewManager skaliert jedes Gebaeude auf die Breite seines 3x3-Footprints.
    Ein schlanker Turm wurde dadurch vier Soldaten hoch. Der Sockel ist so breit,
    dass der Turm nach der Skalierung ``height_ratio`` x Footprint misst; der Turm
    wird um die Sockeldicke angehoben, Pivots wandern mit.
    """
    objs0 = list(result["LOD0"].values())
    mn, mx = world_bbox(objs0)
    height = mx.z - mn.z
    side = max(height / cfg["height_ratio"], (mx.x - mn.x) * 1.05, (mx.y - mn.y) * 1.05)
    thick = side * cfg.get("thickness", 0.035)
    hull = result["LOD0"]["Hull"]
    base = [hull.matrix_world @ v.co for v in hull.data.vertices]
    base = [p for p in base if p.z < mn.z + height * 0.15] or base
    cx = (min(p.x for p in base) + max(p.x for p in base)) / 2
    cy = (min(p.y for p in base) + max(p.y for p in base)) / 2
    u, v, tone = pick_texel(bc, cfg.get("tone", [0.18, 0.34]))

    lift = Vector((0, 0, thick))
    for d in result.values():
        for obj in d.values():
            obj.matrix_world = Matrix.Translation(lift) @ obj.matrix_world
    for p in pivots.values():
        p.z += thick

    for lodname, d in result.items():
        me = bpy.data.meshes.new(f"pad_{lodname}")
        bm = bmesh.new()
        bmesh.ops.create_cube(bm, size=1.0)
        bmesh.ops.scale(bm, vec=Vector((side, side, thick)), verts=bm.verts)
        bmesh.ops.translate(bm, vec=Vector((cx, cy, mn.z + thick / 2)), verts=bm.verts)
        top = [e for e in bm.edges if all(vv.co.z > mn.z + thick * 0.9 for vv in e.verts)]
        bmesh.ops.bevel(bm, geom=top, offset=thick * 0.6, segments=1, affect="EDGES")
        uv = bm.loops.layers.uv.new("UVMap")
        for f in bm.faces:
            for loop in f.loops:
                loop[uv].uv = (u, v)
        bm.to_mesh(me)
        bm.free()
        obj = bpy.data.objects.new(f"__Pad_{lodname}", me)
        bpy.context.scene.collection.objects.link(obj)
        d["Pad"] = obj
    print(f"  Sockel {side:.2f} m breit, {thick:.2f} m dick, Ton {tone:.2f} bei UV ({u:.3f}, {v:.3f})")


# ---------------------------------------------------------------- Export

def build_and_export(result, pivots, parents, kinds, stem, fbx_out):
    sc = bpy.context.scene
    nodes = {}

    def empty(name, loc):
        e = bpy.data.objects.new(name, None)
        sc.collection.objects.link(e)
        e.location = loc
        e.empty_display_size = 0.4
        nodes[name] = e
        bpy.context.view_layer.update()  # sonst ist matrix_world noch Identitaet
        return e

    def attach(child, parent):
        bpy.context.view_layer.update()
        world = child.matrix_world.copy()
        child.parent = parent
        child.matrix_parent_inverse = Matrix.Identity(4)
        child.matrix_world = world

    root = empty("Root", Vector((0, 0, 0)))
    body = empty("Body", Vector((0, 0, 0)))
    attach(body, root)
    pending = [n for n in pivots if n != "Body"]
    while pending:  # Eltern vor Kindern anlegen
        for n in list(pending):
            par = parents.get(n, "Body")
            if par in nodes:
                attach(empty(n, pivots[n]), nodes[par])
                pending.remove(n)
            elif par not in pivots:
                parents[n] = "Body"

    for lodname, d in sorted(result.items()):
        for part, obj in d.items():
            target = "Body" if part == "Hull" else part
            if target not in nodes:
                target = "Body"
            obj.name = f"SM_{stem}_{part}_{lodname}"
            obj.data.name = obj.name
            # Mesh-Ursprung auf den Knoten legen, damit die lokale Transform
            # des Meshes Null ist und nur der Knoten animiert.
            pivot_world = nodes[target].matrix_world.translation
            activate(obj)
            sc.cursor.location = pivot_world
            bpy.ops.object.origin_set(type="ORIGIN_CURSOR")
            attach(obj, nodes[target])

    os.makedirs(os.path.dirname(fbx_out), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=fbx_out, use_selection=True, object_types={"EMPTY", "MESH"},
        apply_unit_scale=True, global_scale=1.0, apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=True, mesh_smooth_type="OFF", path_mode="STRIP",
        add_leaf_bones=False)
    print("EXPORTED", fbx_out)


def render_preview(result, out_png, tex_paths):
    sc = bpy.context.scene
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "MATERIAL"
    sc.render.resolution_x, sc.render.resolution_y = 1200, 600
    objs = list(result["LOD0"].values())
    for o in bpy.data.objects:
        if o.type == "MESH" and o not in objs:
            o.hide_render = True
    cols = [(0.6, 0.62, 0.65, 1), (0.95, 0.5, 0.1, 1), (0.9, 0.1, 0.1, 1), (0.2, 0.7, 0.3, 1),
            (0.2, 0.45, 0.95, 1), (0.85, 0.2, 0.85, 1), (0.95, 0.9, 0.2, 1)]
    for i, (part, o) in enumerate(result["LOD0"].items()):
        m = bpy.data.materials.new(f"prev_{part}")
        m.diffuse_color = cols[i % len(cols)]
        o.data.materials.clear(); o.data.materials.append(m)
    mn, mx = world_bbox(objs)
    ctr, size = (mn + mx) / 2, max(mx - mn)
    cd = bpy.data.cameras.new("pc"); cd.type = "ORTHO"; cd.ortho_scale = size * 2.4
    cam = bpy.data.objects.new("pc", cd); sc.collection.objects.link(cam); sc.camera = cam
    loc = ctr + Vector((size * 2.2, -size * 2.2, size * 1.8))
    cam.location = loc
    cam.rotation_euler = (ctr - loc).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = out_png
    bpy.ops.render.render(write_still=True)


# ---------------------------------------------------------------- main

def main():
    a = parse_args()
    r = load_recipe(a.asset)
    stem = a.asset  # z. B. UNIT_Alliance_BattleTank
    rel = r["path"]
    src_fbx = os.path.join(a.src, rel, f"SM_{stem}.fbx")
    src_tex = os.path.join(a.src, rel, f"T_{stem}_BC.png")
    out_dir = os.path.join(a.dst, rel)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=src_fbx)
    lods = lod_objects()
    for o in lods:  # FBX-Import liefert 90 Grad X und Skala 0.01; alles in Weltmass backen
        activate(o)
        bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
        o.data.materials.clear()
    print(f"[{stem}] {len(lods)} LODs: {[o.name for o in lods]}")

    apply_yaw(lods, r.get("yaw_fix", 0))
    # Rezept-Koordinaten beziehen sich auf die Bounding-Box VOR jeder
    # Reparatur - genau die, die asset_layout.py rendert.
    bb = world_bbox([lods[0]])
    diag = (bb[1] - bb[0]).length
    if r.get("drop_needles", True):
        n = sum(drop_needles(o, diag) for o in lods)
        if n:
            print(f"  Splitter: {n} Faces entfernt")
    for box in r.get("drop_boxes", []):
        n = sum(delete_faces(o, in_box(bb, box)) for o in lods)
        print(f"  drop_box {box}: {n} Faces entfernt")
    ext = bb[1] - bb[0]
    for mv in r.get("move_boxes", []):
        off = Vector((ext.x * mv["offset"][0], ext.y * mv["offset"][1], ext.z * mv["offset"][2]))
        n = sum(move_faces(o, in_box(bb, mv["box"]), off) for o in lods)
        print(f"  move_box {mv['box']} um {tuple(round(v, 2) for v in off)}: {n} Vertices")
    # Tripo trennt an jeder UV-Naht (~1000 Scherben je Modell, oft mm bis cm
    # auseinander); 0,2 % der Diagonale schliesst die meisten Naehte, ohne
    # Details zu verschmelzen.
    merge = (bb[1] - bb[0]).length * r.get("merge_frac", 0.002)
    for o in lods:
        smooth_normals(o, r.get("smooth_angle", 0), merge)

    tex_paths, stats, graded = {}, {}, None
    if r["texture"].get("enabled", True) and not a.no_export:
        os.makedirs(out_dir, exist_ok=True)
        tex_paths, stats, graded = texture_pass(lods[0], src_tex, r["faction"], r["texture"], out_dir, stem)
        print(f"  Textur: {json.dumps(stats)}")

    result, pivots, parents, kinds = split_parts(lods, r, bb)

    if r.get("squash"):
        squash(result, pivots, bb, r["squash"])
    if r.get("pad"):
        if graded is None:
            graded = image_np(bpy.data.images.load(src_tex)).copy()
        add_pad(result, pivots, r["pad"], graded)

    if a.preview:
        os.makedirs(a.preview, exist_ok=True)
        render_preview(result, os.path.join(a.preview, f"{stem}_parts.png"), tex_paths)
    if not a.no_export:
        build_and_export(result, pivots, parents, kinds, stem, os.path.join(out_dir, f"SM_{stem}.fbx"))
    report = {"asset": stem, "texture": stats,
              "parts": {n: {"kind": kinds.get(n, "body"), "pivot": [round(v, 4) for v in p]} for n, p in pivots.items()}}
    print("REPORT " + json.dumps(report))


if __name__ == "__main__":
    main()
