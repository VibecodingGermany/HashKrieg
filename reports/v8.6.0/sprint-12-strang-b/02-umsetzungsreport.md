# Sprint 12 Strang B – Umsetzungsreport

**Stand:** 2026-08-08 | **Entscheidung:** D-090 | **Ergebnis:** technisch
umgesetzt; manuelle 60-Einheiten-Sicht-/Gegenhörabnahme offen

## Ergebnis

Strang B macht den vorhandenen Hitscan-Kampf sichtbar und hörbar, ohne
Simulation, Netzwerkzustand, Replayformat, Fingerprint oder deterministische
Baselines zu verändern. Die Präsentation liest nur Fog-of-War-freigegebene
Snapshots. Unity-Bordmittel liefern gedeckelte Effekte; zwölf Tier-0-Cues
laufen über einen D-039-konformen Audio-Service und einen authorisierten Mixer.

| Paket | Ergebnis |
|---|---|
| B1 | `VisibleCombatFrameDiffer`: Shot/Hit/sicherer Death/UnitReady aus fog-sichtbaren `TryGetUnit`-Snapshots; gleiche Ticks ignoriert, Tick-Rücklauf setzt die Baseline zurück, mehrdeutiges Verschwinden bleibt still. |
| B2 | Mündungsstoß, kopierte Hitscan-Spur ≤ 0,1 s, Trefferstoß; höchstens 64 aktive Effekte und acht Lichter, Überlauf wird verworfen. |
| B3 | Tod hält den exakten View 0,8 s, trennt Picking/Collider und gibt danach die gebundene Poolidentität zurück; Gebäude erhalten Rauch, aber keine persistente Trümmerfläche. |
| B4 | 35 unveränderte Kenney-CC0-OGGs, drei Batch-Sidecars mit Einzelhashes und ein ehrliches Musik-Sidecar mit vier unvollständigen Suno-Datensätzen. |
| B5 | Headless Quellcode-Guard statt des nicht ausführbaren Effekt-Schalter-A/B-Tests; Produktionscode außerhalb `Simulation/**` bleibt frei von `GetUnitRef(` und unerlaubtem `.Random`-Zugriff. |
| Audio | Zwölf `SoundEventSO`, `MIX_Master`, `UnityAudioService`, 30 One-Shot-/24 räumliche Stimmen, 3–4 Instanzen je Schlüssel, atomare Layer, Prioritäts-Stealing, keine Warteschlange und wirksamer SFX-Regler. |

## Verbindliche Grenzen und Planabweichungen

Die vollständige Begründung steht in D-090; das ScopeLedger hält dieselben
Punkte als Planvergleich. Keine dieser Abweichungen wurde still vorgenommen.

1. Dedizierter Differ mit vollständiger `EntityId` statt zweier paralleler
   Slot-Arrays in `UnitViewManager`.
2. Sichere, bewusst unvollständige Todesheuristik statt vollständiger Ableitung
   aus jedem Verschwinden. Eigene mobile Einheiten dürfen direkt gelten;
   Gebäude/fremde Einheiten nur bei Tickdelta 1 und genau einem sichtbaren,
   korrelierten Schuss. Mehrdeutiges bleibt stumm.
3. Unity-Meshpartikel und Laufzeitmaterialien statt einer prozedural erzeugten
   radialen Textur.
4. Kein persistentes Trümmer-/Decal-Artefakt für Gebäude.
5. D-039-Service statt direktem `AudioSource.PlayOneShot` im Effektcontroller.
6. Genau 35 unveränderte, pack-first abgelegte OGGs statt semantisch
   umbenannter/konvertierter WAV-Dateien.
7. `ALR_BaseUnderAttack` bleibt Tier 1; keine Quelle oder auditive Abnahme wurde
   für 12B erfunden.
8. Optionale Flipbook-Stufe 5 ausgelassen.
9. Headless Produktionsquellen-Guard statt A/B-State-Hash-Test mit
   Effektschalter. `RawUnits` ist wegen bestehender Altlasten nicht global Teil
   des Guards; der neue Differ selbst nutzt es nicht.
10. Kein separater Effektschalter. Der deterministische Grenzschutz kommt aus
    dem Quellcode-Guard; SFX verwenden die vorhandene Einstellung.
11. Kamera-Listener bleibt Ist-Stand; Fokuspunkt-Listener ist eine offene
    Gegenhöralternative.
12. Bei Multi-Tick-Aufholen dürfen Zwischen-Cues verloren gehen; es gibt kein
    nachträgliches Effekt- oder Audiogewitter.
13. `verifiedBy` bleibt unter der eng begrenzten D-090-Tier-1-Ausnahme leer.
14. Alle vier Suno-Datensätze bleiben `incomplete`: Menütrack ohne damaliges
    lokales MP3 und exakten Loop-Befehl; Ingame 01 zusätzlich ohne belegte
    private Coverwurzel; Ingame 02/03 ohne exakten Konvertierungsbefehl.
15. Mixer-Authoring nutzt reflektierte Unity-6000.5.4f1-Interna und bricht bei
    Signaturdrift hart ab.
16. `MenuMusicPlayer` und `MusicDirector` bleiben eine explizite D-090-
    Übergangsausnahme zu D-039. Zwei Stimmen sind dafür reserviert.
17. Gain, Cooldowns und Prioritäten sind konservative Startwerte bis zur
    gespielten Gegenhörabnahme.

## Dateien

### Laufzeit und Authoring

- `Assets/DefaultVolumeProfile.asset`
- `Assets/_Project/Editor/BootstrapSceneGenerator.cs`
- `Assets/_Project/Editor/Sprint12BAuthoring.cs` plus `.meta`
- `Assets/_Project/Scenes/Bootstrap.unity`
- `Assets/_Project/Scripts/Gameplay/Audio.meta`
- `Assets/_Project/Scripts/Gameplay/Audio/AudioContracts.cs` plus `.meta`
- `Assets/_Project/Scripts/Gameplay/Audio/SoundEventSO.cs` plus `.meta`
- `Assets/_Project/Scripts/Gameplay/Audio/UnityAudioService.cs` plus `.meta`
- `Assets/_Project/Scripts/Gameplay/CombatFeedback.meta`
- `Assets/_Project/Scripts/Gameplay/CombatFeedback/VisibleCombatFrameDiffer.cs` plus `.meta`
- `Assets/_Project/Scripts/Gameplay/CombatFeedback/CombatEffectController.cs` plus `.meta`
- `Assets/_Project/Scripts/Gameplay/Match/UnitViewManager.cs`
- `Assets/_Project/Scripts/Gameplay/Match/PathfindingTestBootstrap.cs`
- `Assets/_Project/Scripts/Presentation/UI/SfxSettingsBridge.cs` plus `.meta`
- `Assets/_Project/Scripts/Presentation/UI/BuildMenuHud.cs`
- `Assets/_Project/Scripts/Presentation/UI/CommandCardHud.cs`
- `Assets/_Project/Scripts/Presentation/UI/MainMenuController.cs`
- `Assets/_Project/Scripts/Presentation/UI/MatchFrameHud.cs`
- `Assets/_Project/Scripts/Presentation/UI/RtsDeviceInput.cs`
- `Assets/_Project/Scripts/Presentation/UI/MenuMusicPlayer.cs`

Die bestehenden Strang-A-Anteile in gemeinsam berührten Presentation-Dateien
wurden erhalten. Insbesondere bleiben Relay-Start-Gating, terminale
Netzmeldungen und gesperrte Relay-Pause unverändert.

### Tests

- `Assets/Tests/EditMode/Gameplay/VisibleCombatFrameDifferTests.cs` plus `.meta`
- `Assets/Tests/EditMode/Gameplay/CombatPresentationBudgetTests.cs` plus `.meta`
- `Assets/Tests/PlayMode/CombatDeathViewHoldTests.cs` plus `.meta`
- `tools/Nova.SimRunner.Tests/PresentationSourceBoundaryTests.cs`

### Audio-Artefakte

- `Assets/_Project/Audio/Mixer.meta`, `Mixer/MIX_Master.mixer` plus `.meta`
- `Assets/_Project/Audio/Events.meta` und zwölf `SND_*.asset` jeweils plus `.meta`
- `Assets/_Project/Audio/Sfx.meta`, der Ordnerbaum
  `Sfx/Kenney/{SciFi,Impact,Interface}` samt Ordner-Metas
- 35 OGGs jeweils plus `.meta`: 11 Sci-Fi-, 11 Impact- und 13 Interface-
  Dateien. Die vollständige Einzelpfad-/Hashliste ist in den drei jeweiligen
  `PROVENANCE.json`-`files[]` enthalten.
- drei Kenney-`PROVENANCE.json` jeweils plus `.meta`
- `Assets/_Project/Audio/Music/PROVENANCE.json` plus `.meta`

### Dokumentation

- `CHANGELOG.md`
- `docs/README.md`
- `docs/assets/AssetRegister.md`
- `docs/assets/Licenses.md`
- `docs/assets/Provenance.md`
- `docs/assets/provenance-ledger.json`
- `docs/production/DecisionLog.md`
- `docs/production/ScopeLedger.md`
- `docs/production/hashkrieg/04_Audioplan.md`
- `docs/production/hashkrieg/12_Sprint_Zu_Zweit.md`
- `docs/production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md`
- `docs/production/hashkrieg/README.md`
- `docs/tech/AudioArchitecture.md`
- dieser Report

## Nachweise

| Prüfung | Ergebnis |
|---|---|
| `.dotnet/dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release --no-restore --nologo` | **549/549 bestanden**, 0 übersprungen, 11 s im finalen Wiederholungslauf |
| Unity-Authoring `Nova.Editor.Sprint12BAuthoring.GenerateAll` | **bestanden**; 35 Importer, Mixer, zwölf Events und Bootstrap-Verdrahtung hart validiert |
| Unity EditMode | **521/521 bestanden**, 0 fehlgeschlagen, 6,30 s |
| Unity PlayMode | **8/9 bestanden**. `CombatDeathViewHoldTests.RecycledSlotCannotReuseTheHeldCorpseView` ist grün. Allein der bestehende headless `BarracksSpawnDiagnosisTests` scheitert an `RenderTexture.Create failed`; keine 12B-Compile-/Logikregression. |
| Kenney-Provenienz | **35/35 Zielhashes entsprechen den Sidecars**; `ffprobe -v error` dekodiert 35/35 |
| JSON | fünf Audio-/Ledger-Dateien parsebar; aggregierter Ledger enthält **41** Datensätze |
| macOS-Build | **erfolgreich**, 282.011.596 Byte Buildreport, 269 MB App, Mach-O universal `x86_64` + `arm64`, abschließend gültig ad hoc signiert |
| `git diff --check` | **sauber** |

Der unabhängige Abschlussaudit meldete keine P0/P1-Inkonsistenz und zwei
P2-Testlücken. Daraufhin wurden vier ausführende Audio-Vertragstests
(Cooldown, Schlüsselkonkurrenz, 30/24-Stimmenbudgets, streng niedrigeres
Prioritäts-Stealing und atomare Layer) sowie die vollständige
Death-Hold-Freigabe nach 0,8 s mit Material-/Collider-Restoration und exakter
Pool-Wiederverwendung ergänzt. `Nova.Gameplay.Tests` und
`Nova.PlayMode.Tests` kompilieren mit den von Unity erzeugten Roslyn-RSPs ohne
Diagnostik. Der angeforderte erneute Unity-Lauf wurde jedoch vom
Desktop-Freigabelimit abgewiesen, bevor Unity startete. Diese neuen Assertions
sind daher **nicht ausgeführt** und nicht in die oben stehenden 521/521 bzw.
8/9 eingerechnet; der Produktionscode und die Test-App wurden nach dem zuvor
ausgeführten Lauf nicht mehr verändert.

Während der Abschlussprüfung berührte ein zu breit angesetzter
Leerzeichen-Formatter vorübergehend auch binäre OGGs. FSB- und SHA-Prüfungen
fingen dies vor der Abgabe ab. Alle 35 Dateien wurden aus den bereits geprüften
Kenney-Quellpaketen bytegenau wiederhergestellt; danach liefen Hashprüfung,
`ffprobe`, Authoring, Tests und Build erneut. Die dokumentierten Originalhashes
blieben unverändert.

## Offene manuelle Abnahme

Nicht als bestanden behauptet sind:

- Sichtbarkeit und Klangbalance mit ungefähr sechzig feuernden Einheiten;
- Verständlichkeit von Schuss, Impact und Tod im dichten Mix;
- SFX-Regler im tatsächlichen Optionsfluss;
- Kamera- gegen Fokuspunkt-Listener;
- subjektive Feinabstimmung der konservativen Gain-/Cooldown-/Prioritätswerte.

Test-App:
`Builds/MacOSArm64/ProjectNova.app` (lokaler Dirty-Build
`eef73ae-dirty`, nicht reproduzierbarer Release und nicht notarisiert).
