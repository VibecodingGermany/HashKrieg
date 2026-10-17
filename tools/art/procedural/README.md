# Prozedurale Gebäude (Legion)

Kraftwerk, Fahrzeugfabrik, Forschungslabor und Kaserne der Legion waren als
Tripo-Modelle unbrauchbar (drei davon als Panzer generiert, die Kaserne
zerfallen). Diese Skripte bauen sie in Blender 5.2 headless nach den
Konzeptbildern neu; die Textur wird aus dem Konzeptbild projiziert und gebacken.

Ablauf je Gebäude (Beispiel Kraftwerk, `<S>` = Arbeitsordner):

```bash
B=/Applications/Blender.app/Contents/MacOS/Blender
$B -b --python tools/art/procedural/build_power.py -- <S>/proc_power.glb
$B -b --python tools/art/procedural/bake_projected.py -- <S>/proc_power.glb front.png side.png <S>/tex_Power.glb 60000
$B -b --python tools/art/glb_to_unity_fbx.py -- --input <S>/tex_Power.glb \
   --out-fbx ../Hashkrieg_Assets/3d/unity_ready/Buildings/Legion/Power/SM_BLDG_Legion_Power.fbx \
   --out-texture ../Hashkrieg_Assets/3d/unity_ready/Buildings/Legion/Power/T_BLDG_Legion_Power_BC.png \
   --fit footprint --size 9.0 --texture-size 2048 --budgets 20000 8000 2000 --keep-slivers
# Basisfarbe auf mittlere Luminanz 0,12 heben (Konzeptbilder sind sehr dunkel), dann:
$B -b --python tools/art/asset_pass.py -- --asset BLDG_Legion_Power --src ../Hashkrieg_Assets/3d/unity_ready --dst Assets/_Project/Art
```

`front.png`/`side.png` sind das auf das Objekt beschnittene Konzeptbild bzw. ein
Ausschnitt seiner Wandfläche. Zielmaße: Fabrik 12 m, alle anderen 9 m
(Footprint). Vorderseite der Bauskripte ist Blender −Y; `bake_projected.py`
dreht sie auf die Tripo-Konvention +X, die `glb_to_unity_fbx.py` erwartet.

Eine KI-Form (Hunyuan3D 2.1) wurde verworfen: deren Community-Lizenz gilt nicht
in der EU.
