"""Procedural Legion recruit camp: quonset huts, canvas awning, watch platform. Front = -Y."""
import bpy, bmesh, math, sys
out = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
parts = []
def box(c, s, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=c, rotation=rot); o = bpy.context.object; o.scale = s; parts.append(o); return o
def cyl(c, r, d, rot=(0, 0, 0), v=12):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=d, location=c, rotation=rot, vertices=v); o = bpy.context.object; parts.append(o); return o
def quonset(cx, cy, r, L, ribs=10):
    """Half-cylinder hut along Y, centred at (cx, cy), with end walls and corrugation ribs."""
    bm = bmesh.new(); seg = 20
    ring = lambda y: [bm.verts.new((cx + r*math.cos(math.pi*i/seg), y, r*math.sin(math.pi*i/seg))) for i in range(seg+1)]
    a, b = ring(cy - L/2), ring(cy + L/2)
    for i in range(seg):
        bm.faces.new((a[i], a[i+1], b[i+1], b[i]))
    bm.faces.new(list(reversed(a))); bm.faces.new(b)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new('q'); bm.to_mesh(me); o = bpy.data.objects.new('q', me); bpy.context.scene.collection.objects.link(o)
    sol = o.modifiers.new('s', 'SOLIDIFY'); sol.thickness = 0.06
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='s'); parts.append(o)
    for k in range(ribs + 1):
        y = cy - L/2 + k * L / ribs
        bpy.ops.mesh.primitive_torus_add(location=(cx, y, 0), rotation=(math.pi/2, 0, 0), major_radius=r + 0.03,
                                         minor_radius=0.035, major_segments=24, minor_segments=6)
        t = bpy.context.object
        bpy.ops.object.mode_set(mode='EDIT'); tb = bmesh.from_edit_mesh(t.data)
        bmesh.ops.delete(tb, geom=[v for v in tb.verts if v.co.y < -0.02], context='VERTS')  # keep upper half (local y = world z)
        bmesh.update_edit_mesh(t.data); bpy.ops.object.mode_set(mode='OBJECT'); parts.append(t)
# concrete-ish base slab
box((0, 0.1, 0.05), (3.0, 2.9, 0.1))
quonset(-0.35, 0.25, 1.05, 2.6)       # main hut (front facing -Y)
quonset(1.05, 0.55, 0.62, 2.0, 8)     # low side hut
# front wall of main hut: door opening frame + canvas awning
box((-0.35, -1.06, 0.5), (0.7, 0.05, 1.0))                     # dark door panel
box((-0.35, -1.12, 1.04), (0.9, 0.08, 0.08))                   # lintel
aw = box((-0.35, -1.45, 1.02), (1.25, 0.8, 0.03), rot=(math.radians(-18), 0, 0))  # sloped canvas
for x in (-0.95, 0.25):
    box((x, -1.8, 0.5), (0.05, 0.05, 1.0))                     # awning poles
# window/vent boxes on the main front
for x in (-1.05, 0.35):
    box((x, -1.06, 0.62), (0.3, 0.06, 0.2))
# watch platform on the right front: 4 legs, deck, rail, roof
px, py, h = 1.15, -0.75, 1.75
for dx in (-0.35, 0.35):
    for dy in (-0.35, 0.35):
        box((px + dx, py + dy, (h + 0.66)/2), (0.07, 0.07, h + 0.66))
box((px, py, h), (0.85, 0.85, 0.07))
for dy in (-0.42, 0.42):
    box((px, py + dy, h + 0.28), (0.85, 0.04, 0.04))
for dx in (-0.42, 0.42):
    box((px + dx, py, h + 0.28), (0.04, 0.85, 0.04))
box((px, py, h + 0.68), (1.05, 1.05, 0.05), rot=(0, math.radians(6), 0))   # tin roof
# cross braces
for s in (-1, 1):
    box((px, py - 0.35, h*0.45), (0.04, 0.04, 1.2), rot=(0, s*math.radians(28), 0))
# ladder to the platform
for dx in (-0.14, 0.14):
    box((px + dx, py - 0.55, h/2), (0.04, 0.04, h), rot=(math.radians(-10), 0, 0))
for k in range(8):
    z = 0.15 + k * (h - 0.25) / 7
    box((px, py - 0.55 + (z - h/2) * math.tan(math.radians(10)), z), (0.3, 0.035, 0.035))
# cable loops between huts and a generator box
box((1.15, 1.25, 0.3), (0.5, 0.4, 0.5)); cyl((1.15, 1.25, 0.7), 0.06, 0.4, v=10)
for i in range(3):
    bpy.ops.curve.primitive_bezier_curve_add(location=(0, 0, 0)); c = bpy.context.object
    sp = c.data.splines[0]; p0, p1 = sp.bezier_points
    p0.co = (-0.6 + i*0.3, 0.9, 1.0); p1.co = (1.1, 1.2, 0.55)
    p0.handle_right = (0.0 + i*0.2, 1.3, 0.4); p1.handle_left = (0.8, 1.2, 0.2)
    p0.handle_left = p0.co; p1.handle_right = p1.co
    c.data.bevel_depth = 0.025; c.data.bevel_resolution = 1
    bpy.ops.object.convert(target='MESH'); parts.append(bpy.context.object)
# antenna on the main hut
cyl((-0.9, 0.9, 1.4), 0.02, 1.2, v=6)
bpy.ops.object.select_all(action='DESELECT')
for o in parts: o.select_set(True)
bpy.context.view_layer.objects.active = parts[0]
bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
bpy.ops.object.join(); ob = bpy.context.object
for p in ob.data.polygons: p.use_smooth = False
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB')
print('tris', sum(len(p.vertices)-2 for p in ob.data.polygons))
