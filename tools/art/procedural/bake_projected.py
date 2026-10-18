"""Hunyuan white mesh + concept image -> textured GLB (Tripo-like: front on +X)."""
import bpy, sys, math, bmesh
a = sys.argv[sys.argv.index('--')+1:]
src, front, side, out, tris = a[0], a[1], a[2], a[3], int(a[4])
top = a[5] if len(a) > 5 else side  # roof: a calm plate texture reads better from the RTS camera than projected wall detail
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=src)
ms = [o for o in bpy.context.scene.objects if o.type == 'MESH']
bpy.ops.object.select_all(action='DESELECT')
for o in ms: o.select_set(True)
bpy.context.view_layer.objects.active = ms[0]
if len(ms) > 1: bpy.ops.object.join()
ob = bpy.context.view_layer.objects.active
bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
n0 = sum(len(p.vertices) - 2 for p in ob.data.polygons)
if n0 > tris:
    m = ob.modifiers.new('dec', 'DECIMATE'); m.ratio = tris / n0
    bpy.ops.object.modifier_apply(modifier=m.name)
print('tris', n0, '->', sum(len(p.vertices) - 2 for p in ob.data.polygons))
for p in ob.data.polygons: p.use_smooth = True
ob.data.materials.clear()
# UVs
bpy.ops.object.mode_set(mode='EDIT'); bpy.ops.mesh.select_all(action='SELECT')
bpy.ops.uv.smart_project(angle_limit=math.radians(60), island_margin=0.004)
bpy.ops.object.mode_set(mode='OBJECT')
# projection material
mat = bpy.data.materials.new('proj'); mat.use_nodes = True; nt = mat.node_tree; nt.nodes.clear()
N = nt.nodes.new; L = nt.links.new
tc = N('ShaderNodeTexCoord'); sep = N('ShaderNodeSeparateXYZ'); L(tc.outputs['Generated'], sep.inputs[0])
nrm = N('ShaderNodeSeparateXYZ'); L(tc.outputs['Normal'], nrm.inputs[0])
def img(path):
    t = N('ShaderNodeTexImage'); t.image = bpy.data.images.load(path); t.extension = 'EXTEND'; return t
def uv(u, v, flip=False):
    c = N('ShaderNodeCombineXYZ')
    if flip:
        m = N('ShaderNodeMath'); m.operation = 'SUBTRACT'; m.inputs[0].default_value = 1.0; L(u, m.inputs[1]); L(m.outputs[0], c.inputs[0])
    else: L(u, c.inputs[0])
    L(v, c.inputs[1]); return c
X, Y, Z = sep.outputs[0], sep.outputs[1], sep.outputs[2]
tf = img(front); L(uv(X, Z).outputs[0], tf.inputs[0])            # front/back (Y faces)
ts = img(side); L(uv(Y, Z).outputs[0], ts.inputs[0])             # sides (X faces)
tt = img(top); L(uv(X, Y).outputs[0], tt.inputs[0])             # top/bottom (Z faces)
def w(comp):
    ab = N('ShaderNodeMath'); ab.operation = 'ABSOLUTE'; L(comp, ab.inputs[0])
    pw = N('ShaderNodeMath'); pw.operation = 'POWER'; L(ab.outputs[0], pw.inputs[0]); pw.inputs[1].default_value = 4.0
    return pw.outputs[0]
wx, wy, wz = w(nrm.outputs[0]), w(nrm.outputs[1]), w(nrm.outputs[2])
s1 = N('ShaderNodeMath'); s1.operation = 'ADD'; L(wx, s1.inputs[0]); L(wy, s1.inputs[1])
s2 = N('ShaderNodeMath'); s2.operation = 'ADD'; L(s1.outputs[0], s2.inputs[0]); L(wz, s2.inputs[1])
def scaled(col, wt):
    d = N('ShaderNodeMath'); d.operation = 'DIVIDE'; L(wt, d.inputs[0]); L(s2.outputs[0], d.inputs[1])
    mm = N('ShaderNodeMix'); mm.data_type = 'RGBA'; mm.blend_type = 'MULTIPLY'; mm.inputs['Factor'].default_value = 1.0
    L(col, mm.inputs['A']); L(d.outputs[0], mm.inputs['B']); return mm.outputs['Result']
def add(a_, b_):
    mm = N('ShaderNodeMix'); mm.data_type = 'RGBA'; mm.blend_type = 'ADD'; mm.inputs['Factor'].default_value = 1.0
    L(a_, mm.inputs['A']); L(b_, mm.inputs['B']); return mm.outputs['Result']
col = add(add(scaled(tf.outputs['Color'], wy), scaled(ts.outputs['Color'], wx)), scaled(tt.outputs['Color'], wz))
em = N('ShaderNodeEmission'); L(col, em.inputs['Color']); outn = N('ShaderNodeOutputMaterial'); L(em.outputs[0], outn.inputs['Surface'])
bake = bpy.data.images.new('baked', 2048, 2048); bt = N('ShaderNodeTexImage'); bt.image = bake; nt.nodes.active = bt
ob.data.materials.append(mat)
sc = bpy.context.scene; sc.render.engine = 'CYCLES'; sc.cycles.samples = 1; sc.cycles.device = 'CPU'
sc.render.bake.margin = 8
bpy.ops.object.bake(type='EMIT')
bake.filepath_raw = out.replace('.glb', '_BC.png'); bake.file_format = 'PNG'; bake.save()
# final material
m2 = bpy.data.materials.new('M_baked'); m2.use_nodes = True
bsdf = next(n for n in m2.node_tree.nodes if n.type == 'BSDF_PRINCIPLED')
t2 = m2.node_tree.nodes.new('ShaderNodeTexImage'); t2.image = bake
m2.node_tree.links.new(t2.outputs['Color'], bsdf.inputs['Base Color'])
ob.data.materials.clear(); ob.data.materials.append(m2)
# front is -Y in Hunyuan output; Tripo convention (what glb_to_unity_fbx expects) is +X
ob.rotation_euler = (0, 0, math.radians(90)); bpy.ops.object.transform_apply(rotation=True)
bpy.ops.export_scene.gltf(filepath=out, export_format='GLB', use_selection=False)
print('WROTE', out)
