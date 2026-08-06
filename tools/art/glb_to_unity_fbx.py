"""GLB -> Unity-FBX Aufbereitung fuer den Hashkrieg-Art-Strang.

Reproduziert den in docs/assets/AssetImport_Tripo_2026-08-06.md Abschnitt 2
dokumentierten Batch, der beim ersten Import (34 Assets, 2026-08-05) noch von
Hand lief und dabei kein Skript hinterlassen hat. Jeder Schritt hier ist die
ausgeschriebene Form einer Zeile jener Tabelle - wer die Tabelle aendert, aendert
dieses Skript mit, und umgekehrt.

Aufruf (Blender 5.2.0 LTS headless):

    /Applications/Blender.app/Contents/MacOS/Blender --background --python \\
        tools/art/glb_to_unity_fbx.py -- \\
        --input "/pfad/modell.glb" \\
        --out-fbx "Assets/_Project/Art/Buildings/Alliance/HQ/SM_BLDG_Alliance_HQ.fbx" \\
        --out-texture "Assets/_Project/Art/Buildings/Alliance/HQ/T_BLDG_Alliance_HQ_BC.png" \\
        --fit footprint --size 12.0 --texture-size 2048 \\
        --report "/pfad/convert_report_entry.json"

Die Vorgaben je Asset-Klasse stehen NICHT hier, sondern kommen als Argumente:
Zielmass aus Buildings.md Paragraph 215 (Footprint-Zellen x 3,0 m) bzw. der
Einheitentabelle, LOD-Verhaeltnisse und Texturgroessen aus
docs/tech/AssetBudget.md Abschnitt 1 und 2.
"""

import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector

# LOD-Kette wie beim Erstimport: LOD1 = 40 %, LOD2 = 12 % der Quelldreiecke.
DEFAULT_LOD_RATIOS = (0.40, 0.12)

# Nadelform-Erkennung (AssetImport Abschnitt 2, Zeile "Splitter entfernt").
SLIVER_MAX_TRIS = 8
SLIVER_MIN_LENGTH_OF_DIAGONAL = 0.15
SLIVER_MAX_SECOND_AXIS_OF_LONGEST = 0.15


def parse_args(argv):
    if "--" in argv:
        argv = argv[argv.index("--") + 1:]
    else:
        argv = []
    p = argparse.ArgumentParser(prog="glb_to_unity_fbx")
    p.add_argument("--input", required=True)
    p.add_argument("--out-fbx", required=True)
    p.add_argument("--out-texture", required=True)
    p.add_argument("--fit", choices=("footprint", "longest"), required=True,
                   help="footprint: groesste horizontale Kante auf --size; longest: laengste Kante ueberhaupt")
    p.add_argument("--size", type=float, required=True, help="Zielmass in Metern")
    p.add_argument("--texture-size", type=int, default=2048)
    p.add_argument("--yaw", type=float, default=-90.0,
                   help="Yaw-Korrektur in Grad; Tripo liefert die Vorderseite auf +X, Ziel ist +Z")
    p.add_argument("--lod-ratios", type=float, nargs=2, default=list(DEFAULT_LOD_RATIOS))
    p.add_argument("--budgets", type=int, nargs=3, required=True,
                   help="Dreiecksbudget LOD0 LOD1 LOD2 aus docs/tech/AssetBudget.md Abschnitt 1")
    p.add_argument("--report", default="")
    return p.parse_args(argv)


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def import_glb(path):
    bpy.ops.import_scene.gltf(filepath=path)
    meshes = [o for o in bpy.context.scene.objects if o.type == "MESH"]
    if not meshes:
        raise SystemExit(f"[glb_to_unity_fbx] Keine Mesh-Objekte in {path}")
    return meshes


def join_meshes(meshes):
    bpy.ops.object.select_all(action="DESELECT")
    for o in meshes:
        o.select_set(True)
    bpy.context.view_layer.objects.active = meshes[0]
    if len(meshes) > 1:
        bpy.ops.object.join()
    obj = bpy.context.view_layer.objects.active
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return obj


def triangle_count(obj):
    return sum(len(p.vertices) - 2 for p in obj.data.polygons)


def bbox_dims(obj):
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    xs = [c.x for c in corners]
    ys = [c.y for c in corners]
    zs = [c.z for c in corners]
    return Vector((max(xs) - min(xs), max(ys) - min(ys), max(zs) - min(zs)))


def remove_slivers(obj):
    """Entfernt nadelfoermige Splitter, die die Generatoren regelmaessig anhaengen.

    Regel woertlich aus AssetImport Abschnitt 2: lose Inseln bis acht Dreiecke,
    die laenger als 15 Prozent der Modelldiagonale sind und deren zweitgroesste
    Achse hoechstens 15 Prozent ihrer laengsten misst.
    """
    diagonal = bbox_dims(obj).length
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.separate(type="LOOSE")
    bpy.ops.object.mode_set(mode="OBJECT")

    parts = [o for o in bpy.context.selected_objects if o.type == "MESH"]
    removed = 0
    keep = []
    for part in parts:
        tris = triangle_count(part)
        dims = sorted(bbox_dims(part), reverse=True)
        is_needle = (
            tris <= SLIVER_MAX_TRIS
            and diagonal > 0
            and dims[0] > SLIVER_MIN_LENGTH_OF_DIAGONAL * diagonal
            and dims[1] <= SLIVER_MAX_SECOND_AXIS_OF_LONGEST * dims[0]
        )
        if is_needle:
            bpy.data.objects.remove(part, do_unlink=True)
            removed += 1
        else:
            keep.append(part)

    if not keep:
        raise SystemExit("[glb_to_unity_fbx] Splitterfilter hat das gesamte Modell entfernt")
    merged = join_meshes(keep)
    return merged, removed


def scale_to_target(obj, fit, size):
    dims = bbox_dims(obj)
    # Blender ist Z-up: die horizontale Ebene ist X/Y, die Hoehe ist Z.
    reference = max(dims.x, dims.y) if fit == "footprint" else max(dims)
    if reference <= 0:
        raise SystemExit("[glb_to_unity_fbx] Modell hat keine Ausdehnung")
    factor = size / reference
    obj.scale = (factor, factor, factor)
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return factor


def apply_yaw(obj, degrees):
    if abs(degrees) < 1e-6:
        return
    obj.rotation_euler = (0.0, 0.0, math.radians(degrees))
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=False)


def origin_to_ground_center(obj):
    """Origin auf den Bodenmittelpunkt: X/Y zentriert, Z-Minimum auf null."""
    corners = [obj.matrix_world @ Vector(c) for c in obj.bound_box]
    min_x = min(c.x for c in corners)
    max_x = max(c.x for c in corners)
    min_y = min(c.y for c in corners)
    max_y = max(c.y for c in corners)
    min_z = min(c.z for c in corners)
    offset = Vector(((min_x + max_x) * 0.5, (min_y + max_y) * 0.5, min_z))
    for v in obj.data.vertices:
        v.co -= offset
    obj.location = (0.0, 0.0, 0.0)
    obj.data.update()


def decimate_to(obj, target_tris):
    """Dezimiert ein Objekt auf eine Dreieckszahl. Kein Ueberschreiten des Budgets.

    Decimate arbeitet ueber ein Verhaeltnis, nicht ueber eine Zielzahl, und trifft
    sie nur ungefaehr. Deshalb wird nachkorrigiert, solange das Ergebnis ueber dem
    Ziel liegt - ein Budget ist eine Obergrenze, keine Naeherung.
    """
    current = triangle_count(obj)
    for _ in range(6):
        if current <= target_tris:
            return current
        modifier = obj.modifiers.new(name="Decimate", type="DECIMATE")
        modifier.ratio = max(0.0005, (target_tris / current) * 0.98)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=modifier.name)
        current = triangle_count(obj)
    return current


def build_lod_chain(obj, base_name, ratios, budgets):
    """LOD0 bleibt das Original, sofern es ins Budget passt; sonst wird es hineingezwungen.

    Die Zielzahl je Stufe ist das Minimum aus dem Verhaeltnis zur Quelle und dem
    Budget aus AssetBudget.md Abschnitt 1. Beim Erstimport lagen alle Quellen weit
    unter Budget, dort war das Verhaeltnis allein bindend - bei hochaufgeloesten
    Generatormodellen ist es das Budget.

    Unity erkennt die Kette ueber die Objektnamen mit Suffix _LOD0/_LOD1/_LOD2
    im selben FBX (ArtAssetStandard Abschnitt 3).
    """
    source_tris = triangle_count(obj)
    obj.name = f"{base_name}_LOD0"
    obj.data.name = obj.name

    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    lod0_tris = decimate_to(obj, budgets[0])
    chain = [(0, obj, lod0_tris, budgets[0])]

    for index, ratio in enumerate(ratios, start=1):
        copy = obj.copy()
        copy.data = obj.data.copy()
        copy.name = f"{base_name}_LOD{index}"
        copy.data.name = copy.name
        bpy.context.scene.collection.objects.link(copy)

        target = min(int(source_tris * ratio), budgets[index])
        tris = decimate_to(copy, target)
        chain.append((index, copy, tris, budgets[index]))

    return chain


def find_base_color_image():
    """Findet die BaseColor-Textur ueber den Materialgraphen, nicht ueber die Dateigroesse.

    Ein GLB kann BaseColor, Normal und Metallic/Roughness mitbringen. Die groesste
    Datei zu nehmen ist ein Muenzwurf - eine faelschlich als Albedo verwendete
    Normal Map faerbt das ganze Gebaeude blauviolett. Deshalb wird der Link auf den
    Base-Color-Eingang des Principled BSDF verfolgt; erst wenn der fehlt, faellt die
    Erkennung auf die groesste Textur zurueck und sagt das auch.
    """
    for material in bpy.data.materials:
        if not material.use_nodes:
            continue
        for node in material.node_tree.nodes:
            if node.type != "BSDF_PRINCIPLED":
                continue
            socket = node.inputs.get("Base Color")
            if socket is None or not socket.is_linked:
                continue
            source = socket.links[0].from_node
            # Ueber einen zwischengeschalteten Mix-/Farbknoten hinweg suchen.
            for _ in range(4):
                if source.type == "TEX_IMAGE":
                    if source.image is not None and source.image.size[0] > 0:
                        return source.image, "material-graph"
                    break
                linked = [i for i in source.inputs if i.is_linked]
                if not linked:
                    break
                source = linked[0].links[0].from_node

    images = [i for i in bpy.data.images if i.type == "IMAGE" and i.size[0] > 0]
    if not images:
        return None, "none"
    images.sort(key=lambda i: i.size[0] * i.size[1], reverse=True)
    return images[0], "largest-fallback"


def export_base_color(out_path, size):
    """Speichert die BaseColor-Textur auf die Zielgroesse skaliert (ArtAssetStandard Abschnitt 4.2)."""
    image, how = find_base_color_image()
    if image is None:
        return None
    if how == "largest-fallback":
        print("[glb_to_unity_fbx] WARNUNG: kein Base-Color-Link gefunden, groesste Textur verwendet")

    original = tuple(image.size)
    if image.size[0] != size or image.size[1] != size:
        image.scale(size, size)
    image.filepath_raw = out_path
    image.file_format = "PNG"
    image.save()
    return {
        "file": os.path.basename(out_path),
        "size": [size, size],
        "source_size": list(original),
        "detected_via": how,
    }


def export_fbx(path):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        global_scale=1.0,
        apply_scale_options="FBX_SCALE_NONE",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        use_mesh_modifiers=True,
        mesh_smooth_type="FACE",
        path_mode="STRIP",
    )


def main():
    args = parse_args(sys.argv)
    base_name = os.path.splitext(os.path.basename(args.out_fbx))[0]

    reset_scene()
    meshes = import_glb(args.input)
    obj = join_meshes(meshes)
    source_tris = triangle_count(obj)

    obj, slivers_removed = remove_slivers(obj)
    factor = scale_to_target(obj, args.fit, args.size)
    apply_yaw(obj, args.yaw)
    origin_to_ground_center(obj)

    dims = bbox_dims(obj)
    chain = build_lod_chain(obj, base_name, args.lod_ratios, args.budgets)
    texture = export_base_color(args.out_texture, args.texture_size)
    export_fbx(args.out_fbx)

    record = {
        "glb": os.path.basename(args.input),
        "fbx": args.out_fbx,
        "src_tris": source_tris,
        "lods": [{"lod": i, "tris": t, "budget": b, "ok": t <= b} for i, _, t, b in chain],
        # Blender ist Z-up, Unity Y-up: die Hoehe wandert von Z nach Y.
        "dims_blender_xyz_m": [round(dims.x, 3), round(dims.y, 3), round(dims.z, 3)],
        "dims_unity_xyz_m": [round(dims.x, 3), round(dims.z, 3), round(dims.y, 3)],
        "scale_factor": round(factor, 6),
        "yaw_offset_deg": args.yaw,
        "slivers_removed": slivers_removed,
        "texture": texture,
    }
    print("[glb_to_unity_fbx] " + json.dumps(record))
    if args.report:
        os.makedirs(os.path.dirname(args.report), exist_ok=True)
        with open(args.report, "w", encoding="utf-8") as f:
            json.dump(record, f, indent=1, ensure_ascii=False)


if __name__ == "__main__":
    main()
