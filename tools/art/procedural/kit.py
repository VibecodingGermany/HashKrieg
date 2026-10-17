import bpy, bmesh, math
parts = []
def box(c, s, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cube_add(size=1, location=c, rotation=rot); o = bpy.context.object; o.scale = s; parts.append(o); return o
def cyl(c, r, d, rot=(0, 0, 0), v=16):
    bpy.ops.mesh.primitive_cylinder_add(radius=r, depth=d, location=c, rotation=rot, vertices=v); o = bpy.context.object; parts.append(o); return o
def ring(c, r, minor, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_torus_add(location=c, rotation=rot, major_radius=r, minor_radius=minor, major_segments=20, minor_segments=6)
    parts.append(bpy.context.object); return bpy.context.object
def quonset(cx, cy, r, L, ribs=10, z0=0.0, ends=True):
    bm = bmesh.new(); seg = 20
    rg = lambda y: [bm.verts.new((cx + r*math.cos(math.pi*i/seg), y, z0 + r*math.sin(math.pi*i/seg))) for i in range(seg+1)]
    a, b = rg(cy - L/2), rg(cy + L/2)
    for i in range(seg): bm.faces.new((a[i], a[i+1], b[i+1], b[i]))
    if ends: bm.faces.new(list(reversed(a))); bm.faces.new(b)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
    me = bpy.data.meshes.new('q'); bm.to_mesh(me); o = bpy.data.objects.new('q', me); bpy.context.scene.collection.objects.link(o)
    sol = o.modifiers.new('s', 'SOLIDIFY'); sol.thickness = 0.06
    bpy.context.view_layer.objects.active = o; bpy.ops.object.modifier_apply(modifier='s'); parts.append(o)
    for k in range(ribs + 1):
        y = cy - L/2 + k * L / ribs
        bpy.ops.mesh.primitive_torus_add(location=(cx, y, z0), rotation=(math.pi/2, 0, 0), major_radius=r + 0.03, minor_radius=0.035, major_segments=24, minor_segments=6)
        t = bpy.context.object
        bpy.ops.object.mode_set(mode='EDIT'); tb = bmesh.from_edit_mesh(t.data)
        bmesh.ops.delete(tb, geom=[v for v in tb.verts if v.co.y < -0.02], context='VERTS')
        bmesh.update_edit_mesh(t.data); bpy.ops.object.mode_set(mode='OBJECT'); parts.append(t)
def fan(x, y, z, size=0.42, facing=-1):
    box((x, y, z), (size*1.15, 0.06, size))
    cyl((x, y + facing*0.05, z), size*0.4, 0.04, rot=(math.pi/2, 0, 0), v=20)
    for k in range(4):
        box((x, y + facing*0.07, z), (size*0.72, 0.012, 0.05), rot=(0, k*math.pi/4, 0))
def ladder(x, y, z0, h, w=0.36, axis='y'):
    for s in (-w/2, w/2):
        box((x + (s if axis == 'x' else 0), y + (s if axis == 'y' else 0), z0 + h/2), (0.04, 0.04, h))
    for k in range(int(h / 0.22)):
        box((x, y, z0 + 0.15 + k*0.22), (w if axis == 'x' else 0.04, w if axis == 'y' else 0.04, 0.03))
def pipe(points, r=0.035):
    bpy.ops.curve.primitive_bezier_curve_add(); c = bpy.context.object; sp = c.data.splines[0]
    sp.bezier_points.add(len(points) - 2)
    for bp, p in zip(sp.bezier_points, points):
        bp.co = p; bp.handle_left_type = bp.handle_right_type = 'AUTO'
    c.data.bevel_depth = r; c.data.bevel_resolution = 2
    bpy.ops.object.convert(target='MESH'); parts.append(bpy.context.object)
def finish(out, smooth=False):
    bpy.ops.object.select_all(action='DESELECT')
    for o in parts: o.select_set(True)
    bpy.context.view_layer.objects.active = parts[0]
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    bpy.ops.object.join(); ob = bpy.context.object
    for p in ob.data.polygons: p.use_smooth = smooth
    bpy.ops.export_scene.gltf(filepath=out, export_format='GLB')
    print('tris', sum(len(p.vertices)-2 for p in ob.data.polygons))
