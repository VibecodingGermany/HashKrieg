"""Segmentiert ein Panzer-FBX in Hull / Turret / Gun und baut die Pivot-Hierarchie.

Blender-Weltachsen nach FBX-Import (Unity-Konvention der Repo-Pipeline):
  +Z hoch, -Y = Fahrzeug-Vorwaerts (== Unity +Z)
"""
import bpy, bmesh, sys, os, json, math
from mathutils import Vector

def parse():
    """rig_tank_hierarchy.py -- <fbx> <outdir> <tag> [--preview] [--ring-z Z] [--gun-y Y]

    --ring-z / --gun-y ueberschreiben die automatische Erkennung. Die Heuristik
    sitzt bei den beiden BattleTanks, aber sie misst Geometrie und keine Absicht;
    bei einem neuen Fahrzeug erst --preview fahren und das Ergebnis ansehen.
    """
    a = sys.argv[sys.argv.index("--")+1:]
    d = {"fbx": a[0], "out": a[1], "tag": a[2], "preview": "--preview" in a,
         "ring_z": None, "gun_y": None}
    for key, name in (("ring_z", "--ring-z"), ("gun_y", "--gun-y")):
        if name in a:
            d[key] = float(a[a.index(name) + 1])
    return d

def world_polys(o):
    mw = o.matrix_world
    return [(mw @ p.center, p.index) for p in o.data.polygons]

def find_ring_z(o, lo_frac=0.30, hi_frac=0.80, slices=160,
                min_width_frac=0.35, min_share=0.05):
    """Turmring = groesster Breitensprung, der oberhalb noch einen echten Turm
    stehen laesst. Ohne diese Bedingung schnappt die Erkennung auf Kuppel oder
    Antenne (schmal, aber ein noch groesserer Sprung)."""
    pts = [o.matrix_world @ v.co for v in o.data.vertices]
    zmin, zmax = min(p.z for p in pts), max(p.z for p in pts)
    h = zmax - zmin
    w_max = max(p.x for p in pts) - min(p.x for p in pts)
    n = len(pts)
    band = h * 0.12
    cands = []
    for i in range(slices):
        z = zmin + h * i / slices
        if not (zmin + h*lo_frac <= z <= zmin + h*hi_frac):
            continue
        above = [p for p in pts if z <= p.z < z + band]
        below = [p for p in pts if z - band <= p.z < z]
        if not above or not below:
            continue
        w_above = max(p.x for p in above) - min(p.x for p in above)
        w_below = max(p.x for p in below) - min(p.x for p in below)
        share = sum(1 for p in pts if p.z > z) / n
        if w_above < w_max * min_width_frac or share < min_share:
            continue                      # Kuppel/Antenne, kein Turm
        cands.append((w_below - w_above, z))
    if not cands:
        return None, 0.0
    top = max(d for d, _ in cands)
    # Der Turmring ist die UNTERSTE Stufe dieser Groessenordnung. Ohne diese
    # Regel gewinnt bei Legion der Aufbau darueber (Sprung 1.75 vs 1.70 m).
    near = [(d, z) for d, z in cands if d >= top * 0.60]
    d, z = min(near, key=lambda t: t[1])
    return z, d

def gun_axis(turret_obj, tip_depth=0.30):
    """Rohrachse aus den vordersten Faces des Turms (Fahrzeug-Vorwaerts = -Y)."""
    mw = turret_obj.matrix_world
    V = turret_obj.data.vertices
    faces = [((mw @ p.center), [mw @ V[i].co for i in p.vertices])
             for p in turret_obj.data.polygons]
    if not faces:
        return None
    ymin = min(c.y for c, _ in faces)
    tip = [(c, vs) for c, vs in faces if c.y < ymin + tip_depth]
    ax = sum(c.x for c, _ in tip) / len(tip)
    az = sum(c.z for c, _ in tip) / len(tip)
    r0 = max(math.hypot(v.x - ax, v.z - az) for _, vs in tip for v in vs)
    return {"faces": faces, "ymin": ymin, "ax": ax, "az": az, "r0": r0}

def find_gun_y(turret_obj, grow=1.6, step=0.10, max_len=3.0):
    """Rohrende = erste Y-Scheibe, in der der Radius um die Rohrachse aufgeht.

    Radial statt per Bounding-Box: eine Y-Scheibe erfasst sonst auch Geometrie
    an den Turmflanken (bei Legion Griffe und Kaesten), und die Breite springt
    dann schon an der Muendung. Radial bleibt der Wert ueber das ganze Rohr
    konstant und geht erst an der Blende auf.
    """
    g = gun_axis(turret_obj)
    if g is None:
        return None
    thr = g["r0"] * grow
    y = g["ymin"]
    while y < g["ymin"] + max_len:
        near = [vs for c, vs in g["faces"]
                if y <= c.y < y + step
                and math.hypot(c.x - g["ax"], c.z - g["az"]) <= 2.5 * g["r0"]]
        mr = max((math.hypot(v.x - g["ax"], v.z - g["az"]) for vs in near for v in vs),
                 default=0.0)
        if mr > thr:
            return None if y <= g["ymin"] + step else y
        y += step
    return None

def split_by(obj, pred, newname):
    """Trennt Faces, fuer die pred(world_center) True ist, in ein neues Objekt."""
    me = obj.data
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode='EDIT')
    bm = bmesh.from_edit_mesh(me)
    bm.faces.ensure_lookup_table()
    mw = obj.matrix_world
    for f in bm.faces:
        f.select = pred(mw @ f.calc_center_median())
    bmesh.update_edit_mesh(me)
    n_before = len(bpy.data.objects)
    if any(f.select for f in bm.faces):
        bpy.ops.mesh.separate(type='SELECTED')
    bpy.ops.object.mode_set(mode='OBJECT')
    if len(bpy.data.objects) == n_before:
        return None
    new = [o for o in bpy.context.selected_objects if o is not obj][-1]
    new.name = newname
    return new

def main():
    A = parse()
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.fbx(filepath=A["fbx"])
    lods = sorted([o for o in bpy.data.objects if o.type == 'MESH'], key=lambda o: o.name)
    base = lods[0]

    if A["ring_z"] is not None:
        ring_z, drop = A["ring_z"], 0.0
    else:
        ring_z, drop = find_ring_z(base)
    if ring_z is None:
        print(f"[{A['tag']}] ABBRUCH: kein Turmring gefunden, --ring-z setzen")
        return
    report = {"tag": A["tag"], "ring_z": round(ring_z, 4), "ring_drop_m": round(drop, 3), "lods": {}}
    print(f"[{A['tag']}] Turmring bei z={ring_z:.3f} m (Breitensprung {drop:.2f} m)")

    parts = {}
    gun_ref = None      # Schnittwerte einmal aus LOD0, sonst schneidet jede
                        # LOD-Stufe an anderer Stelle und die Silhouetten driften
    for lod in lods:
        bpy.ops.object.select_all(action='DESELECT')
        turret = split_by(lod, lambda c: c.z > ring_z, f"__turret_{lod.name}")
        if turret is None:
            print(f"  {lod.name}: kein Turm abtrennbar"); continue
        if gun_ref is None:
            gun_y = A["gun_y"] if A["gun_y"] is not None else find_gun_y(turret)
            gun_ref = (gun_y, gun_axis(turret)) if gun_y is not None else (None, None)
        gun_y, g = gun_ref
        gun = None
        if gun_y is not None:
            rmax = g["r0"] * 2.0
            bpy.ops.object.select_all(action='DESELECT')
            gun = split_by(turret, lambda c, gy=gun_y, g=g, rmax=rmax:
                           c.y < gy and math.hypot(c.x - g["ax"], c.z - g["az"]) <= rmax,
                           f"__gun_{lod.name}")
        lodname = lod.name.split("_")[-1]
        parts[lodname] = {"hull": lod, "turret": turret, "gun": gun, "gun_y": gun_y}
        report["lods"][lodname] = {
            "hull_tris": len(lod.data.polygons),
            "turret_tris": len(turret.data.polygons),
            "gun_tris": len(gun.data.polygons) if gun else 0,
            "gun_cut_y": round(gun_y, 4) if gun_y is not None else None,
        }
        print(f"  {lodname}: Hull={len(lod.data.polygons)}  Turm={len(turret.data.polygons)}"
              f"  Rohr={len(gun.data.polygons) if gun else 0}  (gun_y={gun_y})")

    # ---- Pivots aus LOD0 ableiten
    p0 = parts.get("LOD0")
    tp = [p0["turret"].matrix_world @ v.co for v in p0["turret"].data.vertices]
    # Drehachse = Mitte des Turmrings, nicht Mitte der Turm-Bounding-Box:
    # letztere wird vom nach vorn stehenden Rohr aus der Mitte gezogen.
    band = [p for p in tp if p.z < ring_z + 0.35]
    ref = band if len(band) > 20 else tp
    turret_pivot = Vector(((min(p.x for p in ref) + max(p.x for p in ref)) / 2,
                           (min(p.y for p in ref) + max(p.y for p in ref)) / 2,
                           ring_z))
    gun_pivot = None
    if p0["gun"] and gun_ref[1] is not None:
        g = gun_ref[1]
        gp = [p0["gun"].matrix_world @ v.co for v in p0["gun"].data.vertices]
        # Schildzapfen: hinteres Ende des Rohrstuecks, auf der Rohrachse
        gun_pivot = Vector((g["ax"], max(p.y for p in gp), g["az"]))
    report["turret_pivot_blender"] = [round(v, 4) for v in turret_pivot]
    report["turret_pivot_unity"] = [round(turret_pivot.x, 4), round(turret_pivot.z, 4),
                                    round(-turret_pivot.y, 4)]
    if gun_pivot:
        report["gun_pivot_blender"] = [round(v, 4) for v in gun_pivot]
        report["gun_pivot_unity"] = [round(gun_pivot.x, 4), round(gun_pivot.z, 4),
                                     round(-gun_pivot.y, 4)]

    if A["preview"]:
        # Preview: Hull grau, Turm orange, Rohr rot
        cols = {"hull": (0.55, 0.57, 0.60, 1), "turret": (0.90, 0.45, 0.10, 1), "gun": (0.85, 0.10, 0.10, 1)}
        for role, rgba in cols.items():
            m = bpy.data.materials.new(f"col_{role}"); m.use_nodes = False; m.diffuse_color = rgba
            for lodname, d in parts.items():
                if lodname != "LOD0" or d[role] is None: continue
                d[role].data.materials.clear(); d[role].data.materials.append(m)
        for lodname, d in parts.items():
            if lodname == "LOD0": continue
            for role in ("hull", "turret", "gun"):
                if d[role]: bpy.data.objects.remove(d[role], do_unlink=True)
        render_preview(A["out"], A["tag"], turret_pivot, gun_pivot)
    else:
        build_hierarchy(A, parts, turret_pivot, gun_pivot)

    with open(os.path.join(A["out"], f"{A['tag']}_rig.json"), "w") as f:
        json.dump(report, f, indent=2)
    print("REPORT " + json.dumps(report))

def render_preview(out, tag, tpivot, gpivot):
    import mathutils
    sc = bpy.context.scene
    pts = [o.matrix_world @ v.co for o in bpy.data.objects if o.type == 'MESH' for v in o.data.vertices]
    mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
    mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
    ctr = (mn+mx)/2; size = max(mx-mn)
    sc.render.engine = 'BLENDER_WORKBENCH'
    sc.render.resolution_x = sc.render.resolution_y = 640
    sc.display.shading.light = 'STUDIO'; sc.display.shading.color_type = 'MATERIAL'
    cd = bpy.data.cameras.new("c"); cd.type = 'ORTHO'; cd.ortho_scale = size*1.2
    cam = bpy.data.objects.new("c", cd); sc.collection.objects.link(cam); sc.camera = cam
    for name, d, rot in (("side", Vector((1,0,0)), (math.radians(90),0,math.radians(90))),
                         ("iso", Vector((1,-1,0.6)).normalized(), (math.radians(66),0,math.radians(48)))):
        cam.location = ctr + d*size*3; cam.rotation_euler = rot
        sc.render.filepath = os.path.join(out, f"{tag}_split_{name}.png")
        bpy.ops.render.render(write_still=True)
        print("RENDERED", sc.render.filepath)

def build_hierarchy(A, parts, tpivot, gpivot):
    from mathutils import Matrix
    sc = bpy.context.scene

    def empty(name, world):
        e = bpy.data.objects.new(name, None)
        sc.collection.objects.link(e)
        e.location = world
        e.empty_display_size = 0.5
        return e

    def attach(child, parent, parent_world):
        """Lokale Transform explizit setzen.

        matrix_parent_inverse ueberlebt den FBX-Export nicht: Blender behaelt die
        Weltposition, der Exporter schreibt aber die rohe location als lokale
        Transform. Kinder wandern dann um den Pivot-Offset nach oben.
        """
        child_world = child.location.copy()          # noch ohne Parent == Weltlage
        child.parent = parent
        child.matrix_parent_inverse = Matrix.Identity(4)
        child.location = child_world - parent_world

    zero = Vector((0.0, 0.0, 0.0))
    root = empty("Tank_Root", zero)
    hull_p = empty("Hull", zero)
    turret_p = empty("Turret", tpivot)
    gun_p = empty("Gun", gpivot) if gpivot is not None else None

    attach(hull_p, root, zero)
    attach(turret_p, root, zero)
    if gun_p:
        attach(gun_p, turret_p, tpivot)

    base = os.path.basename(A["fbx"]).replace(".fbx", "")
    for lodname, d in sorted(parts.items()):
        d["hull"].name = f"{base}_Hull_{lodname}"
        attach(d["hull"], hull_p, zero)
        d["turret"].name = f"{base}_Turret_{lodname}"
        attach(d["turret"], turret_p, tpivot)
        if d["gun"]:
            d["gun"].name = f"{base}_Gun_{lodname}"
            attach(d["gun"], gun_p or turret_p, gpivot if gun_p else tpivot)

    os.makedirs(A["out"], exist_ok=True)
    fbx_out = os.path.join(A["out"], f"{base}_Rigged.fbx")
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(filepath=fbx_out, use_selection=True, apply_unit_scale=True,
                             apply_scale_options='FBX_SCALE_NONE', object_types={'EMPTY', 'MESH'},
                             global_scale=1.0, use_mesh_modifiers=True,
                             mesh_smooth_type='FACE', axis_forward='-Z', axis_up='Y',
                             bake_space_transform=False, path_mode='STRIP')
    print("EXPORTED", fbx_out)
    glb_out = os.path.join(A["out"], f"{base}_Rigged.glb")
    bpy.ops.export_scene.gltf(filepath=glb_out, export_format='GLB', use_selection=True)
    print("EXPORTED", glb_out)

if __name__ == "__main__":
    main()
