"""Legion heavy generator: stacked turbine blocks, twin corrugated stacks, fan racks, ladder, catwalk. Front = -Y."""
import bpy, sys, math, os
sys.path.insert(0, os.path.dirname(__file__)); from kit import *
out = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
box((0, 0.1, 0.06), (3.0, 2.8, 0.12))                           # plinth
box((-0.35, 0.1, 0.85), (2.1, 2.4, 1.5))                        # main generator hall
box((-0.35, 0.1, 1.72), (1.7, 2.1, 0.3))                        # roof step
box((-0.35, -1.12, 0.62), (0.75, 0.08, 1.0)); box((-0.35, -1.17, 1.18), (0.95, 0.06, 0.12))   # door + lintel
box((-0.35, -1.13, 1.42), (1.1, 0.08, 0.3))                     # glowing grille panel
for x in (-1.1, 0.4):                                           # plate seams
    box((x, -1.12, 0.85), (0.06, 0.06, 1.5))
# server/fan rack tower on the right
box((1.15, -0.1, 0.75), (0.75, 1.9, 1.3)); box((1.15, 0.0, 1.55), (0.6, 1.4, 0.35))
for z in (0.45, 0.95):
    fan(1.0, -1.08, z, 0.36); fan(1.32, -1.08, z, 0.36)
box((1.15, -1.1, 1.55), (0.5, 0.08, 0.28))                      # control panel
# twin chimneys with rings
for x in (-0.85, -0.25):
    cyl((x, 0.35, 2.55), 0.26, 1.6, v=20)
    for z in (1.95, 2.35, 2.8, 3.25): ring((x, 0.35, z), 0.27, 0.035)
    cyl((x, 0.35, 3.4), 0.3, 0.14, v=20)
# catwalk + railing on the roof edge
box((-0.35, -0.95, 1.9), (2.1, 0.45, 0.05))
for x in (-1.35, -0.6, 0.15, 0.65): box((x, -1.15, 2.15), (0.035, 0.035, 0.5))
box((-0.35, -1.15, 2.38), (2.1, 0.035, 0.035))
ladder(0.78, -1.05, 0.12, 2.2, axis='x')
# cable looms and pipes
pipe([(-1.4, -1.15, 0.2), (-1.42, -1.2, 0.9), (-1.2, -1.2, 1.5), (-0.9, -0.6, 1.95)], 0.04)
pipe([(0.75, -0.7, 1.0), (0.6, -0.9, 1.4), (0.2, -0.6, 1.88)], 0.05)
pipe([(-1.45, 0.6, 0.3), (-1.6, 0.5, 0.8), (-1.45, 0.4, 1.3)], 0.06)
# fuel drums at the side
for (x, y) in [(-1.6, -0.6), (-1.6, -0.15)]:
    cyl((x, y, 0.4), 0.2, 0.56, v=14); ring((x, y, 0.3), 0.205, 0.02); ring((x, y, 0.55), 0.205, 0.02)
cyl((0.9, 1.0, 2.0), 0.02, 1.0, v=6)                            # antenna
finish(out)
