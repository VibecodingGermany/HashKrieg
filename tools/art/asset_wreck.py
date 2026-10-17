"""Wrack-Variante eines Assets fuer tools/art/asset_pass.py (D-115).

Aus den bereits getrennten Teilen (Hull, Turret, Gun, Wheel_*, Arm_*, Spin_*,
Leg_*, Pad ...) einer LOD-Stufe entsteht ein einzelnes, statisches Mesh:

* Fahrzeuge: Turm aus dem Ring gesprengt und verdreht, Rohr haengt, ein Rad
  fehlt, Wanne sackt schief ab, Dellen.
* Infanterie: liegt auf dem Ruecken.
* Gebaeude: oberhalb einer gezackten Bruchkante weggebrochen, bewegliche Teile
  (Schuessel, Turm) liegen daneben am Boden, Truemmer im Footprint.

Die Textur ist eine verkohlte Fassung der gegradeten Basisfarbe, ohne Leuchten.
Geschrieben werden ``SM_<stem>_Wreck.fbx`` (ein Objekt ``SM_<stem>_Wreck`` unter
``Root``, gleiche Achsen wie das Haupt-FBX) und ``T_<stem>_Wreck_BC.png``.
Alle Zufallswerte haengen am Asset-Namen: zwei Laeufe ergeben dasselbe Wrack.
"""

import math
import os
import random
import zlib

import bmesh
import bpy
import numpy as np
from mathutils import Matrix, Vector, noise


def _rng(stem):
    return random.Random(zlib.crc32(stem.encode()))


def _dup(obj, name):
    o = obj.copy()
    o.data = obj.data.copy()
    o.name = name
    bpy.context.scene.collection.objects.link(o)
    o.parent = None
    o.matrix_world = obj.matrix_world.copy()
    return o


def _bake_world(o):
    """Weltmatrix ins Mesh backen, damit spaetere Drehungen um Weltpunkte gehen."""
    o.data.transform(o.matrix_world)
    o.matrix_world = Matrix.Identity(4)


def _rotate_about(o, pivot, axis, deg):
    m = Matrix.Translation(pivot) @ Matrix.Rotation(math.radians(deg), 4, axis) @ Matrix.Translation(-pivot)
    o.data.transform(m)


def _translate(o, v):
    o.data.transform(Matrix.Translation(v))


def _bbox(objs):
    pts = [v.co for o in objs for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    return mn, mx


def _dent(o, amp, scale, seed):
    """Weiche Verbiegung durch ein raeumlich glattes Vektorfeld.

    Nicht entlang der Normalen: Tripo-Meshes bestehen aus hunderten loser
    Schalen, deren Normalen in alle Richtungen zeigen - dort reisst eine
    Normalen-Verschiebung jede Naht auf. Ein glattes Feld bewegt benachbarte
    Schalen gemeinsam und verbiegt das Ganze wie Blech.
    """
    off = Vector((seed * 7.1, seed * 3.3, seed * 1.7))
    for v in o.data.vertices:
        v.co += noise.noise_vector(v.co * scale + off) * amp


def _break_top(o, cut_z, jag, seed):
    """Alle Faces oberhalb einer gezackten Bruchkante entfernen."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    off = Vector((seed * 2.3, seed * 5.9, 0))
    doomed = []
    for f in bm.faces:
        c = f.calc_center_median()
        h = cut_z + noise.noise(Vector((c.x * 0.9, c.y * 0.9, 0)) + off) * jag
        # nach der hoechsten Ecke, nicht nach dem Mittelpunkt: grosse
        # Dreiecke ragen sonst als lange Spiesse ueber die Bruchkante
        if max(v.co.z for v in f.verts) > h:
            doomed.append(f)
    removed = len(doomed)
    bmesh.ops.delete(bm, geom=doomed, context="FACES")
    bm.to_mesh(o.data)
    bm.free()
    return removed


def _islands(o):
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.faces.ensure_lookup_table()
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
            isl.append(x.index)
            for e in x.edges:
                stack.extend(l for l in e.link_faces if l.index not in seen)
        out.append(isl)
    bm.free()
    return out


def _rubble(src, count, mn, mx, ground, rng, name):
    """Kleine Inseln des abgebrochenen Oberteils als Truemmer am Boden verteilen."""
    if src is None or not src.data.polygons:
        return None
    bmx = bmesh.new()
    bmx.from_mesh(src.data)
    bmx.faces.ensure_lookup_table()
    limit = (mx - mn).length * 0.14

    def small(i):
        pts = [v.co for fi in i for v in bmx.faces[fi].verts]
        d = max(max(p[k] for p in pts) - min(p[k] for p in pts) for k in range(3))
        return d <= limit

    isl = [i for i in _islands(src) if 4 <= len(i) <= 120 and small(i)]
    bmx.free()
    rng.shuffle(isl)
    pick = set(f for i in isl[:count] for f in i)
    if not pick:
        return None
    bm = bmesh.new()
    bm.from_mesh(src.data)
    bm.faces.ensure_lookup_table()
    keep = [f for f in bm.faces if f.index not in pick]
    bmesh.ops.delete(bm, geom=keep, context="FACES")
    me = bpy.data.meshes.new(name)
    bm.to_mesh(me)
    bm.free()
    deb = bpy.data.objects.new(name, me)
    bpy.context.scene.collection.objects.link(deb)
    # jede Insel einzeln umwerfen und auf den Boden legen
    for isl_faces in _islands(deb):
        bmv = bmesh.new()
        bmv.from_mesh(deb.data)
        bmv.faces.ensure_lookup_table()
        verts = {v for fi in isl_faces for v in bmv.faces[fi].verts}
        c = sum((v.co for v in verts), Vector()) / len(verts)
        target = Vector((rng.uniform(mn.x, mx.x), rng.uniform(mn.y, mx.y), ground))
        # flach liegend: freie Drehung um die Hochachse, wenig Kippen
        rot = (Matrix.Rotation(rng.uniform(0, 2 * math.pi), 3, "Z")
               @ Matrix.Rotation(rng.uniform(-0.35, 0.35), 3, "X"))
        for v in verts:
            v.co = rot @ (v.co - c) + target
        low = min(v.co.z for v in verts)
        for v in verts:
            v.co.z += ground - low
        bmv.to_mesh(deb.data)
        bmv.free()
    return deb


def _is_needle(o, diag):
    mn, mx = _bbox([o])
    d = sorted(mx - mn)
    return d[2] > 0.08 * diag and d[1] < 0.25 * d[2]


def _drop_needles(o, diag, max_faces=80, min_len=0.1, max_width=0.1):
    """Lange, duenne Inseln entfernen: Reste angeschnittener Flaechen."""
    bm = bmesh.new()
    bm.from_mesh(o.data)
    bm.faces.ensure_lookup_table()
    doomed = []
    for isl in _islands(o):
        if len(isl) > max_faces:
            continue
        pts = [v.co for fi in isl for v in bm.faces[fi].verts]
        d = sorted(max(p[k] for p in pts) - min(p[k] for p in pts) for k in range(3))
        if d[2] > min_len * diag and d[1] < max_width * d[2]:
            doomed.extend(bm.faces[fi] for fi in isl)
    if doomed:
        bmesh.ops.delete(bm, geom=doomed, context="FACES")
        bm.to_mesh(o.data)
    bm.free()
    return len(doomed)


def char_texture(graded, stem):
    """Verkohlte Basisfarbe: entsaettigt, stark abgedunkelt, fleckiger Russ."""
    rgb = graded[..., :3]
    h, w = rgb.shape[:2]
    lum = rgb @ np.array([0.2126, 0.7152, 0.0722], dtype=np.float32)
    gray = np.repeat(lum[..., None], 3, -1)
    desat = rgb * 0.3 + gray * 0.7
    rs = np.random.RandomState(zlib.crc32(stem.encode()) & 0x7FFFFFFF)
    # Russflecken: grobes Rauschen, hochskaliert und weichgezeichnet
    coarse = rs.rand(h // 32 + 2, w // 32 + 2).astype(np.float32)
    yy = np.linspace(0, coarse.shape[0] - 1.001, h)
    xx = np.linspace(0, coarse.shape[1] - 1.001, w)
    y0, x0 = yy.astype(int), xx.astype(int)
    fy, fx = (yy - y0)[:, None], (xx - x0)[None, :]
    c00 = coarse[y0][:, x0]; c01 = coarse[y0][:, x0 + 1]
    c10 = coarse[y0 + 1][:, x0]; c11 = coarse[y0 + 1][:, x0 + 1]
    blot = (c00 * (1 - fx) + c01 * fx) * (1 - fy) + (c10 * (1 - fx) + c11 * fx) * fy
    fine = rs.rand(h, w).astype(np.float32)
    soot = np.clip(blot * 0.8 + fine * 0.2, 0, 1)
    dark = 0.24 + 0.32 * soot
    out = np.empty_like(graded)
    burnt = desat * dark[..., None]
    # ein Hauch Glutrost in hellen, russarmen Partien
    rust = np.array([0.32, 0.14, 0.06], dtype=np.float32)
    burnt = burnt + rust * (np.clip(lum - 0.35, 0, 1) * (1 - soot))[..., None] * 0.6
    out[..., :3] = np.clip(burnt, 0, 1)
    out[..., 3] = 1
    return out


def build_wreck(result, pivots, kinds, recipe, stem, category, graded, out_dir, lod="LOD1", preview_png=""):
    """Erzeugt Wrack-Mesh und -Textur und exportiert sie. Liefert den FBX-Pfad."""
    rng = _rng(stem)
    seed = (zlib.crc32(stem.encode()) % 997) / 31.0
    parts = result.get(lod) or result["LOD0"]
    objs = {}
    for name, obj in parts.items():
        d = _dup(obj, f"__wreck_{name}")
        _bake_world(d)
        objs[name] = d

    mn, mx = _bbox(list(objs.values()))
    ext = mx - mn
    ground = mn.z
    up, right, fwd = Vector((0, 0, 1)), Vector((1, 0, 0)), Vector((0, -1, 0))
    cfg = recipe.get("wreck", {})
    infantry = category == "unit" and any(n.startswith("Leg_") for n in objs)
    extra = []

    if infantry:
        # nach hinten umfallen (Vorwaerts = -Y), leicht zur Seite gedreht
        pivot = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, ground))
        side = rng.choice((-1, 1))
        for o in objs.values():
            _rotate_about(o, pivot, right, -86.0)
            _rotate_about(o, pivot, up, side * rng.uniform(15, 50))
    elif category == "unit":
        for name, o in objs.items():
            kind = kinds.get(name, "body")
            p = pivots.get(name, Vector())
            if kind == "turret":
                yaw = rng.uniform(25, 70) * rng.choice((-1, 1))
                _rotate_about(o, p, up, yaw)
                _rotate_about(o, p, right, rng.uniform(-9, 9))
                _rotate_about(o, p, fwd, rng.uniform(-8, 8))
                _translate(o, Vector((rng.uniform(-0.06, 0.06) * ext.x, rng.uniform(-0.05, 0.05) * ext.y, -0.03 * ext.z)))
                # das Rohr haengt am Turm: dieselbe Drehung, dann Senkung
                for gname, go in objs.items():
                    if kinds.get(gname) == "gun":
                        _rotate_about(go, p, up, yaw)
                        _rotate_about(go, pivots[gname], right, 14.0)
            elif kind == "arm":
                _rotate_about(o, p, right, rng.uniform(18, 35))
            elif kind == "wheel" and rng.random() < 0.35:
                _rotate_about(o, p, fwd, rng.uniform(25, 60))
                _translate(o, Vector((0, 0, -0.04 * ext.z)))
        # Wanne sackt schief ab (alles ausser abgefallenen Teilen)
        tilt_axis = Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized()
        base_pivot = Vector(((mn.x + mx.x) / 2, (mn.y + mx.y) / 2, ground))
        for o in objs.values():
            _rotate_about(o, base_pivot, tilt_axis, rng.uniform(3, 6))
            _translate(o, Vector((0, 0, -0.05 * ext.z)))
    else:  # Gebaeude
        cut = ground + ext.z * cfg.get("cut", rng.uniform(0.42, 0.58))
        jag = ext.z * 0.12
        top_parts = []
        for name, o in list(objs.items()):
            kind = kinds.get(name, "body")
            if kind in ("spin", "scan") and _is_needle(o, ext.length):
                # duenne Antennen nicht umlegen: sie laegen als Spiess quer im Bild
                bpy.data.objects.remove(o, do_unlink=True)
                del objs[name]
            elif kind in ("spin", "scan", "turret", "gun"):
                # abgesprengt: neben dem Gebaeude auf den Boden legen
                p = pivots.get(name, Vector())
                lo = min(v.co.z for v in o.data.vertices)
                _rotate_about(o, p, Vector((rng.uniform(-1, 1), rng.uniform(-1, 1), 0)).normalized(), rng.uniform(55, 100))
                lo2 = min(v.co.z for v in o.data.vertices)
                c = sum((v.co for v in o.data.vertices), Vector()) / len(o.data.vertices)
                ang = rng.uniform(0, 2 * math.pi)
                dest = Vector(((mn.x + mx.x) / 2 + math.cos(ang) * ext.x * 0.32,
                               (mn.y + mx.y) / 2 + math.sin(ang) * ext.y * 0.32, 0))
                _translate(o, Vector((dest.x - c.x, dest.y - c.y, ground - lo2)))
            elif kind != "pad":
                top = _dup(o, f"__top_{name}")
                n = _break_top(o, cut, jag, seed)
                # Gegenstueck behalten: nur das Oberteil als Truemmerquelle
                bmt = bmesh.new()
                bmt.from_mesh(top.data)
                keep = [f for f in bmt.faces if f.calc_center_median().z <= cut + jag]
                bmesh.ops.delete(bmt, geom=keep, context="FACES")
                bmt.to_mesh(top.data)
                bmt.free()
                top_parts.append(top)
        for i, t in enumerate(top_parts):
            deb = _rubble(t, cfg.get("rubble", 14) // max(1, len(top_parts)), mn, mx, ground, rng, f"__rubble_{i}")
            bpy.data.objects.remove(t, do_unlink=True)
            if deb is not None:
                extra.append(deb)

    for o in list(objs.values()) + extra:
        _dent(o, ext.length * 0.012, 1.2 / max(ext.length, 1e-3), seed)

    # zusammenfuegen
    all_objs = [o for o in list(objs.values()) + extra if o.data.polygons]
    bpy.ops.object.select_all(action="DESELECT")
    for o in all_objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = all_objs[0]
    bpy.ops.object.join()
    wreck = bpy.context.view_layer.objects.active
    removed = _drop_needles(wreck, ext.length)
    if removed:
        print(f"  Wrack-Splitter: {removed} Faces entfernt")
    wreck.name = f"SM_{stem}_Wreck"
    wreck.data.name = wreck.name
    # Bodenkontakt: tiefster Punkt auf den urspruenglichen Boden
    low = min(v.co.z for v in wreck.data.vertices)
    wreck.data.transform(Matrix.Translation(Vector((0, 0, ground - low))))
    wreck.data.materials.clear()

    # Textur
    tex = char_texture(graded, stem)
    img = bpy.data.images.new(f"{stem}_Wreck_BC", tex.shape[1], tex.shape[0], alpha=True)
    img.colorspace_settings.name = "sRGB"
    img.pixels.foreach_set(np.ascontiguousarray(tex, dtype=np.float32).ravel())
    tex_path = os.path.join(out_dir, f"T_{stem}_Wreck_BC.png")
    img.filepath_raw = tex_path
    img.file_format = "PNG"
    img.save()

    # Export: gleiche Achsen und Root-Knoten wie das Haupt-FBX
    sc = bpy.context.scene
    root = bpy.data.objects.new("Root", None)
    sc.collection.objects.link(root)
    wreck.parent = root
    fbx_path = os.path.join(out_dir, f"SM_{stem}_Wreck.fbx")
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    wreck.select_set(True)
    bpy.ops.export_scene.fbx(
        filepath=fbx_path, use_selection=True, object_types={"EMPTY", "MESH"},
        apply_unit_scale=True, global_scale=1.0, apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z", axis_up="Y", bake_space_transform=False,
        use_mesh_modifiers=True, mesh_smooth_type="OFF", path_mode="STRIP",
        add_leaf_bones=False)
    tris = sum(len(p.vertices) - 2 for p in wreck.data.polygons)
    if preview_png:
        _preview(wreck, img, preview_png)
    bpy.data.objects.remove(wreck, do_unlink=True)
    bpy.data.objects.remove(root, do_unlink=True)
    print(f"  Wrack: {tris} Dreiecke -> {os.path.basename(fbx_path)}")
    return fbx_path, wreck_preview_bounds(mn, mx)


def _preview(obj, img, out_png):
    """Kontrollbild: Wrack mit verkohlter Textur, schraeg von vorne rechts."""
    sc = bpy.context.scene
    hidden = [o for o in sc.objects if o is not obj and o.type == "MESH" and not o.hide_render]
    for o in hidden:
        o.hide_render = True
    mat = bpy.data.materials.new("wreck_prev")
    tree = mat.node_tree
    bsdf = next(n for n in tree.nodes if n.type == "BSDF_PRINCIPLED")
    tn = tree.nodes.new("ShaderNodeTexImage")
    tn.image = img
    tree.links.new(tn.outputs[0], bsdf.inputs[0])
    obj.data.materials.append(mat)
    sc.render.engine = "BLENDER_WORKBENCH"
    sc.display.shading.light = "STUDIO"
    sc.display.shading.color_type = "TEXTURE"
    sc.render.resolution_x, sc.render.resolution_y = 600, 600
    mn, mx = _bbox([obj])
    ctr, size = (mn + mx) / 2, max(mx - mn)
    cd = bpy.data.cameras.new("wc"); cd.type = "ORTHO"; cd.ortho_scale = size * 1.6
    cam = bpy.data.objects.new("wc", cd); sc.collection.objects.link(cam); sc.camera = cam
    loc = ctr + Vector((size * 2.2, -size * 2.2, size * 1.8))
    cam.location = loc
    cam.rotation_euler = (ctr - loc).to_track_quat("-Z", "Y").to_euler()
    sc.render.filepath = out_png
    bpy.ops.render.render(write_still=True)
    obj.data.materials.clear()
    for o in hidden:
        o.hide_render = False
    bpy.data.objects.remove(cam, do_unlink=True)


def wreck_preview_bounds(mn, mx):
    return [list(mn), list(mx)]
