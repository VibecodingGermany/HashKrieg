"""Orthografische Layout-Ansichten eines Konvertats, als Vorlage fuer die
Schnittboxen in ``asset_recipes.json``.

    Blender -b --python tools/art/asset_layout.py -- <fbx> <out_dir> [texture.png]

Schreibt ``<stem>_top.png``, ``_side.png``, ``_front.png`` und ``<stem>_layout.json``.
Jede Ansicht ist exakt auf die LOD0-Bounding-Box beschnitten, das Bild bildet die
normierten Rezept-Koordinaten also linear ab:

    top    x -> rechts, y -> oben   (unten im Bild = vorne, y 0)
    side   y -> rechts, z -> oben   (Blick von +X, links im Bild = vorne)
    front  x -> rechts, z -> oben   (Blick von vorne)

``tools/art/layout_grid.py`` legt das Raster darueber.
"""
import json
import math
import os
import sys

import bpy
from mathutils import Vector

a = sys.argv[sys.argv.index("--") + 1:]
fbx, out = a[0], a[1]
tex = a[2] if len(a) > 2 else ""
os.makedirs(out, exist_ok=True)
stem = os.path.basename(fbx)[3:-4]

bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.fbx(filepath=fbx)
meshes = [o for o in bpy.data.objects if o.type == "MESH"]
lod0 = [o for o in meshes if o.name.endswith("LOD0")] or meshes[:1]
for o in meshes:
    if o not in lod0:
        bpy.data.objects.remove(o, do_unlink=True)

mat = bpy.data.materials.new("m")
if tex and os.path.exists(tex):
    tree = mat.node_tree
    bsdf = next(n for n in tree.nodes if n.type == "BSDF_PRINCIPLED")
    tn = tree.nodes.new("ShaderNodeTexImage")
    tn.image = bpy.data.images.load(tex)
    tree.links.new(tn.outputs[0], bsdf.inputs[0])
for o in lod0:
    o.data.materials.clear()
    o.data.materials.append(mat)

pts = [o.matrix_world @ v.co for o in lod0 for v in o.data.vertices]
mn = Vector((min(p.x for p in pts), min(p.y for p in pts), min(p.z for p in pts)))
mx = Vector((max(p.x for p in pts), max(p.y for p in pts), max(p.z for p in pts)))
ext = mx - mn
ctr = (mn + mx) / 2

sc = bpy.context.scene
sc.render.engine = "BLENDER_WORKBENCH"
sc.display.shading.light = "STUDIO"
sc.display.shading.color_type = "TEXTURE" if tex else "OBJECT"
sc.display.shading.show_cavity = True
sc.render.film_transparent = False

cd = bpy.data.cameras.new("c")
cd.type = "ORTHO"
cam = bpy.data.objects.new("c", cd)
sc.collection.objects.link(cam)
sc.camera = cam

LONG = 900
views = {
    # name: (horizontal extent, vertical extent, location, euler)
    "top": (ext.x, ext.y, ctr + Vector((0, 0, ext.z * 3 + 1)), (0, 0, 0)),
    "side": (ext.y, ext.z, ctr + Vector((ext.x * 3 + 1, 0, 0)), (math.pi / 2, 0, math.pi / 2)),
    "front": (ext.x, ext.z, ctr + Vector((0, -(ext.y * 3 + 1), 0)), (math.pi / 2, 0, 0)),
}
for name, (h, v, loc, rot) in views.items():
    scale = LONG / max(h, v)
    sc.render.resolution_x = max(16, round(h * scale))
    sc.render.resolution_y = max(16, round(v * scale))
    cd.ortho_scale = max(h, v)
    cam.location = loc
    cam.rotation_euler = rot
    sc.render.filepath = os.path.join(out, f"{stem}_{name}.png")
    bpy.ops.render.render(write_still=True)

with open(os.path.join(out, f"{stem}_layout.json"), "w") as f:
    json.dump({"min": list(mn), "max": list(mx), "extent": list(ext)}, f, indent=1)
print("LAYOUT", stem, [round(x, 3) for x in ext])
