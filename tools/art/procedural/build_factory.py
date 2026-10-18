"""Legion vehicle assembly hangar: wide quonset hall, big roll-up bay door, roof gantry crane, side stair, fan banks. Front = -Y."""
import bpy, sys, math, os
sys.path.insert(0, os.path.dirname(__file__)); from kit import *
out = sys.argv[sys.argv.index('--')+1]
bpy.ops.wm.read_factory_settings(use_empty=True)
box((0, 0.1, 0.06), (3.2, 3.6, 0.12))                           # apron
box((-0.1, 0.25, 0.55), (2.5, 3.0, 1.0))                        # straight walls
quonset(-0.1, 0.25, 1.25, 3.0, 12, z0=1.0, ends=True)           # arched roof
box((-0.1, -1.27, 0.5), (2.5, 0.08, 1.0))                       # front wall (arch end cap above)
box((-0.1, -1.33, 0.75), (1.4, 0.06, 1.4))                      # roll-up door
for k in range(9): box((-0.1, -1.37, 0.12 + k*0.155), (1.38, 0.025, 0.04))
box((-0.1, -1.36, 1.5), (1.6, 0.08, 0.1))                       # door header (glow strip)
for x in (-0.86, 0.66): box((x, -1.34, 0.78), (0.1, 0.1, 1.55))
fan(-1.0, -1.33, 1.65, 0.36); fan(-0.55, -1.33, 1.65, 0.36)    # fan bank above-left
box((0.75, -1.33, 1.7), (0.5, 0.08, 0.35))                      # panel
# gantry crane on the roof ridge
for y in (-0.9, 0.0):
    box((-0.1, y, 2.45), (0.1, 0.1, 0.5))
box((-0.1, -0.45, 2.72), (0.12, 1.3, 0.1)); box((0.45, -0.9, 2.72), (1.1, 0.1, 0.1))
cyl((0.9, -0.9, 2.45), 0.012, 0.5, v=6); ring((0.9, -0.9, 2.18), 0.06, 0.018, rot=(math.pi/2, 0, 0))
# external stair to a foreman platform on the right
box((1.45, -0.85, 1.25), (0.5, 0.6, 0.05))
for (x, y) in [(1.25, -1.1), (1.65, -1.1), (1.65, -0.6)]: box((x, y, 0.65), (0.05, 0.05, 1.3))
for k in range(9):
    box((1.45, 0.75 - k*0.17, 0.15 + k*0.125), (0.42, 0.16, 0.03))
box((1.68, 0.05, 0.75), (0.03, 1.5, 0.03), rot=(math.radians(36), 0, 0))
box((1.68, -0.85, 1.55), (0.03, 0.6, 0.03))
# hanging inspection lamps
for x in (-0.6, 0.4): cyl((x, -1.45, 1.85), 0.07, 0.08, v=10)
# side wall vents and cables
for y in (-0.6, 0.3, 1.2): box((-1.37, y, 0.6), (0.06, 0.4, 0.25))
pipe([(-1.38, -1.0, 0.2), (-1.42, -0.6, 1.2), (-1.0, -0.3, 2.1)], 0.04)
pipe([(1.2, 1.4, 0.2), (1.25, 1.2, 0.9), (1.0, 0.8, 1.9)], 0.05)
finish(out)
