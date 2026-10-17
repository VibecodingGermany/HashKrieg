#!/usr/bin/env bash
# Baut die vier prozeduralen Legion-Gebaeude neu: Geometrie -> Projektion/Bake -> Konvertat -> Asset-Pass.
# Aufruf aus dem Repo-Root: tools/art/procedural/build_all.sh <arbeitsordner> [Gebaeude ...]
set -euo pipefail
W="$1"; shift || true
B=/Applications/Blender.app/Contents/MacOS/Blender
P=tools/art/procedural
ASSETS=../Hashkrieg_Assets
U=$ASSETS/3d/unity_ready/Buildings/Legion
I=$ASSETS/img
PY=${PYTHON:-python3}
mkdir -p "$W"
build() {  # name script concept size [crop]
  local n=$1 script=$2 concept=$3 size=$4; shift 4
  $PY $P/prep_textures.py "$concept" "$W/$n" "$@"
  $B -b --python $P/$script -- "$W/proc_$n.glb" | grep -E "^tris"
  $B -b --python $P/bake_projected.py -- "$W/proc_$n.glb" "$W/${n}_front.png" "$W/${n}_side.png" "$W/tex_$n.glb" 60000 "$W/${n}_top.png" | grep WROTE
  $B -b --python tools/art/glb_to_unity_fbx.py -- --input "$W/tex_$n.glb" \
     --out-fbx "$U/$n/SM_BLDG_Legion_$n.fbx" --out-texture "$U/$n/T_BLDG_Legion_${n}_BC.png" \
     --fit footprint --size "$size" --texture-size 2048 --budgets 20000 8000 2000 --keep-slivers \
     --report "$W/report_$n.json" | grep -o '"dims_unity_xyz_m": \[[^]]*\]'
  $B -b --python tools/art/asset_pass.py -- --asset "BLDG_Legion_$n" --src $ASSETS/3d/unity_ready \
     --dst Assets/_Project/Art --preview "$W/prev" | grep -E "Textur"
}
ALL=("$@")
sel() { [ ${#ALL[@]} -eq 0 ] && return 0; for x in "${ALL[@]}"; do [ "$x" = "$1" ] && return 0; done; return 1; }
sel Power          && build Power          build_power.py    $I/v2test/legion_building_Power.png             9.0
sel VehicleFactory && build VehicleFactory build_factory.py  $I/v2test/legion_building_VehicleFactory_b.png  12.0
sel ResearchLab    && build ResearchLab    build_lab.py      $I/v2test/legion_building_ResearchLab_b.png     9.0 160 20 935 980
sel Barracks       && build Barracks       build_barracks.py $I/legion_building_Barracks.png                 9.0
