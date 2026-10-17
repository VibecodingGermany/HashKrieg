"""Procedural Legion war lab: welded shipping-container stack. Front = -Y (Hunyuan convention used by bake_projected)."""
import bpy, bmesh, math, sys
from mathutils import Vector, Matrix
out = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
parts = []
def box(c, s, rot=0.0):
    bpy.ops.mesh.primitive_cube_add(size=1, location=c, rotation=(0, 0, rot)); o = bpy.context.object
    o.scale = s; parts.append(o); return o
def cyl(c, r, d, rot=(0, 0, 0), v=16):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=d, location=c, rotation=rot, vertices=v); o = bpy.context.object; parts.append(o); return o
def container(cx, cy, cz, L, W, H, axis='x'):
    # body + corrugation ribs + corner posts
    if axis == 'x': box((cx, cy, cz + H/2), (L, W, H))
    else: box((cx, cy, cz + H/2), (W, L, H))
    n = int(L / 0.28)
    for i in range(n):
        t = -L/2 + (i + 0.5) * L / n
        if axis == 'x':
            box((cx + t, cy - W/2 - 0.02, cz + H/2), (0.09, 0.05, H*0.86))
            box((cx + t, cy + W/2 + 0.02, cz + H/2), (0.09, 0.05, H*0.86))
        else:
            box((cx - W/2 - 0.02, cy + t, cz + H/2), (0.05, 0.09, H*0.86))
            box((cx + W/2 + 0.02, cy + t, cz + H/2), (0.05, 0.09, H*0.86))
    hx, hy = (L/2, W/2) if axis == 'x' else (W/2, L/2)
    for sx in (-1, 1):
        for sy in (-1, 1):
            box((cx + sx*hx, cy + sy*hy, cz + H/2), (0.16, 0.16, H + 0.04))
    # top/bottom rails
    for zz in (cz + 0.04, cz + H - 0.04):
        box((cx, cy - hy, zz), (2*hx + 0.1, 0.14, 0.12)); box((cx, cy + hy, zz), (2*hx + 0.1, 0.14, 0.12))
        box((cx - hx, cy, zz), (0.14, 2*hy + 0.1, 0.12)); box((cx + hx, cy, zz), (0.14, 2*hy + 0.1, 0.12))
# footprint ~ 3 x 3 m normalised; scaled later by the converter
H = 1.15
container(0.0, 0.0, 0.0, 3.0, 2.4, H, 'x')            # ground container, long side facing front
container(-0.15, 0.1, H, 2.6, 2.2, H, 'y')            # second, rotated 90 deg
container(0.55, -0.05, 2*H, 1.6, 1.4, H*0.8, 'x')     # small top box (server room)
# front door + frame
box((0.75, -1.24, 0.55), (0.6, 0.06, 1.0)); box((0.75, -1.27, 1.08), (0.72, 0.06, 0.08))
# fan banks on the front
for (x, z) in [(-0.85, 0.42), (-0.85, 0.9), (-0.25, 0.42)]:
    box((x, -1.24, z), (0.5, 0.06, 0.42))
    cyl((x, -1.29, z), 0.17, 0.04, rot=(math.pi/2, 0, 0), v=20)
    for k in range(4):
        b = box((x, -1.31, z), (0.3, 0.012, 0.05)); b.rotation_euler = (0, k*math.pi/4, 0)
# vents on upper container front
for x in (-0.9, -0.2, 0.5):
    box((x, -1.02, H + 0.75), (0.5, 0.06, 0.22))
# monitors cluster (upper right front)
for (x, z) in [(0.6, H + 0.55), (0.95, H + 0.5), (0.78, H + 0.25)]:
    box((x, -1.0, z), (0.26, 0.12, 0.2))
# scaffold walkway along the right side at mid height
box((1.62, -0.1, H + 0.02), (0.5, 2.3, 0.06))
for y in (-1.15, -0.4, 0.35, 1.0):
    box((1.85, y, H/2), (0.06, 0.06, H)); box((1.85, y, H + 0.35), (0.04, 0.04, 0.7))
box((1.85, -0.1, H + 0.7), (0.04, 2.3, 0.04)); box((1.85, -0.1, H + 0.4), (0.03, 2.3, 0.03))
# exterior ladder on the left side to the roof
for sx in (-0.18, 0.18):
    box((-1.56, -0.6 + sx, H*1.4), (0.04, 0.04, H*2.8))
for k in range(14):
    box((-1.56, -0.6, 0.2 + k*0.22), (0.04, 0.36, 0.03))
# roof: antenna cluster + dish
top = 2*H + H*0.8
for (x, y, h) in [(0.1, 0.2, 1.2), (0.35, -0.3, 0.9), (0.9, 0.3, 1.5), (-0.6, 0.6, 0.8)]:
    cyl((x, y, top + h/2), 0.025, h, v=8); cyl((x, y, top + h*0.75), 0.008, 0.35, rot=(0, math.pi/2, 0), v=6)
cyl((0.9, 0.3, top + 0.06), 0.08, 0.12, v=10)
bpy.ops.mesh.primitive_uv_sphere_add(radius=0.42, location=(-0.6, -0.1, top + 0.55), segments=24, ring_count=12)
d = bpy.context.object; parts.append(d)
bpy.ops.object.mode_set(mode='EDIT'); bm = bmesh.from_edit_mesh(d.data)
bmesh.ops.delete(bm, geom=[v for v in bm.verts if v.co.z > -0.24], context='VERTS'); bmesh.update_edit_mesh(d.data)
bpy.ops.object.mode_set(mode='OBJECT')
d.rotation_euler = (math.radians(-60), 0, math.radians(20))
cyl((-0.6, -0.1, top + 0.25), 0.05, 0.5, v=8)
# cable looms down the front left
for i, x in enumerate((-1.3, -1.2, 0.3)):
    c = cyl((x, -1.27, H), 0.03, 2*H, v=8)
# join, bevel-ish smoothing via weighted normals
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.ops.object.join(); ob = bpy.context.object
bev = ob.modifiers.new('bev', 'BEVEL'); bev.width = 0.015; bev.segments = 1; bev.limit_method = 'ANGLE'
bpy.ops.object.modifier_apply(modifier=bev.name)
ob.name = 'lab'
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB')
print('tris', sum(len(p.vertices)-2 for p in ob.data.polygons))
