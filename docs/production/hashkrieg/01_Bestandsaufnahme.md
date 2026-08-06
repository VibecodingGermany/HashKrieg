# Bestandsaufnahme — was am 2026-08-06 wirklich existiert

**Version:** 0.1.0 | **Status:** Entwurf – datierter Ist-Stand, kein Gate-Nachweis | **Verantwortungsbereich:** Orchestrator | **Sprint:** 7

## Zweck

Die geprüfte Inventur beider Arbeitsstände: dieses Repository (`main` @ `15dfe73`
plus drei uncommittete Dateien) und das externe Paket
`Hashkrieg_Assets`. Jede Aussage hier beruht auf einer gelesenen Datei, nicht
auf einem Statusfeld in einem anderen Dokument.

Der Unterschied zu [../StatusSnapshot_2026-08-05.md](../StatusSnapshot_2026-08-05.md)
ist wesentlich: der Snapshot beschreibt den Stand **vor** dem Art-Import. Seither
sind 34 Assets eingetroffen und aufbereitet worden.

> ### ⚠ Die Grundlinie bewegt sich
>
> Während dieser Inventur (2026-08-06, 08:47–09:11) lief **parallel eine
> Wirtschafts-Umstellung unter der Kennung D-077** im selben Arbeitsbaum. Sie
> ist zum Zeitpunkt dieses Dokuments **uncommittet und mitten in der Arbeit** —
> der Code zitiert D-077 bereits, im [../DecisionLog.md](../DecisionLog.md)
> existiert die Nummer noch nicht.
>
> Was D-077 ändert (aus dem Arbeitsbaum gelesen, nicht aus einem Beschluss):
> Startguthaben 1.000 → 3.000 AE · die Raffinerie ist **nicht mehr
> vorplatziert** und braucht **kein Kraftwerk** mehr als Voraussetzung · der
> **Harvester wird von der Raffinerie produziert**, nicht mehr vom HQ · die zwei
> Start-Harvester entfallen. Der klassische C&C-Eröffnungsloop also.
>
> **Betroffene Abschnitte dieser Bestandsaufnahme:** §3.4 (die Harvester-Abkürzung
> wird gerade entfernt) und Teile von §1 (zwei Definitionszeilen und zwei
> Producer-Zuweisungen ändern sich).
>
> **Zwei Folgen, die nicht übersehen werden dürfen:**
> 1. D-077 ändert Werte in `SimDefinitions` und damit `DefinitionsHash64` —
>    das ist **replay-brechend** (§7 Punkt 1). Aufgezeichnete Replays vor D-077
>    sind entwertet.
> 2. `quality/content/mvp-v1.json` ist der einzige versionspflichtige Vertrag
>    des Repositories und wurde auf `schemaVersion 1.1.0` gezogen. Wer diesen
>    Plan gegen die alte Fassung liest, plant gegen einen überholten Sollstand.

## Abhängigkeiten

- [../MVPContentManifest.md](../MVPContentManifest.md) – Soll-Inhalt MS-1
- [../../assets/AssetImport_Tripo_2026-08-06.md](../../assets/AssetImport_Tripo_2026-08-06.md) – Importprotokoll des Erstsatzes
- [../DemoRunbook.md](../DemoRunbook.md) – heutiger spielbarer Umfang
- [../GrayboxLog.md](../GrayboxLog.md) – Sitzungsprotokolle GB-001 bis GB-004

## 1. Die Deckungsrechnung: 34 / 34 / 34

Das zentrale Ergebnis der Inventur. Für alle 17 MS-1-Rollen (9 Gebäude +
8 Einheiten) mal 2 Fraktionen existiert:

| Ebene | Stand | Beleg |
|---|---|---|
| Design-Dokument | 34/34 | [../../gamedesign/Buildings.md](../../gamedesign/Buildings.md), [../../gamedesign/Vehicles.md](../../gamedesign/Vehicles.md), [../../gamedesign/Infantry.md](../../gamedesign/Infantry.md) |
| Code-Definition mit Werten | 34/34 | `SimDefinitions.TotalDefinitionCount = 34`, DefinitionIds 1–34 lückenlos |
| Concept-Art (1024², styleguide-konform) | 34/34 | `Hashkrieg_Assets/img/` |
| 3D-Rohmodell (Tripo GLB) | 34/34 | `Hashkrieg_Assets/3d/unsorted/`, Zuordnung in `3d/unity_ready/convert_report.json` |
| Aufbereitetes FBX mit LOD0/1/2 | 34/34 | `Assets/_Project/Art/**/SM_*.fbx` |
| BaseColor-Textur | 34/34 | Einheiten 1024², Gebäude 2048² |
| Material + Prefab mit LODGroup | 34/34 | `M_*.mat`, `PF_*.prefab` |
| Eintrag in `AssetMappingRegistry` | 34/34 | **uncommittet im Arbeitsbaum** |

**Es fehlt keine Rolle.** Die Vermutung „pro Fraktion fehlen mindestens zwei
Gebäude, unter anderem das Kraftwerk" trifft auf das Roster nicht zu: Das
Kraftwerk ist Allianz `defId 5` (450 AE, +100 Strom) und Legion `defId 22`
(350 AE, +80 Strom), mit FBX, Prefab, Registry-Eintrag und Hotkey `B`.

Woher der Eindruck kommt, ist trotzdem nachvollziehbar — und in zwei Punkten
sogar berechtigt, siehe §3.

## 2. Was mechanisch funktioniert

Die Simulation ist der reifste Teil des Projekts. Acht Systeme laufen in
festgelegter Tick-Reihenfolge in `MatchRunner.InitializeMatch`:

| System | Stand | Anmerkung |
|---|---|---|
| `SimulationKernel` | fertig | Lockstep 10 Hz, State-Hash, zweiphasiges Snapshot-Restore |
| `EconomySystem` | fertig | Auto-Zyklus ernten → abliefern → ernten, Strombilanz |
| `ConstructionSystem` | fertig | Platzierung, Baustellen, Reparatur, Verkauf, T2-Freischaltung |
| `ProductionSystem` | fertig | Warteschlangen je Gebäude, Rally Points, deterministisches Spawnen |
| `PathfindingSystem` | fertig | Flow-Field mit Multi-Destination-Cache |
| `MovementSystem` | fertig | Fixpunkt-Steering, kein `float` im hash-relevanten Pfad |
| `FogOfWarSystem` | fertig | drei Zustände je Team, 5-Hz-Recompute, Radar-Pings |
| `CombatSystem` | fertig | Hitscan, 6×6-Schadensmatrix, sichtgesteuerte Zielerlaubnis |
| `VictorySystem` | teilweise | drei Ergebniscodes, Zeitlimit Tick 27.000 |

Dazu vollständig und stark getestet, aber im Spiel ungenutzt: das
Snapshot-Format, das Replay-Format und der Command-Stream (12 von 13
Befehlsarten sind real ausführbar).

Testlage: 822 Testmethoden über zwei Spuren (Unity EditMode und das Headless-Tool
`tools/Nova.SimRunner.Tests`). Die beiden Spuren spiegeln sich zu großen Teilen —
sieben Testklassen haben identische Methodenzahlen. Das ist als
Determinismus-Doppelspur beabsichtigt, die *effektive* Abdeckung ist dadurch
aber kleiner, als die Rohzahl suggeriert.

## 3. Was nicht funktioniert — nach Wirkung sortiert

### 3.1 Der Gegner ist untätig (der eine echte Blocker)

`SkirmishAiSystem` implementiert `ISimSystem` korrekt, wird aber im gesamten
Produktivcode **nie instanziiert oder registriert**. Die einzige Fundstelle
außerhalb der eigenen Datei ist `Assets/Tests/EditMode/AI/SkirmishAiTests.cs`.

Und selbst registriert würde es nicht kämpfen: `ExecuteTick` hat zwei Zweige —
ein Kraftwerk an der hartkodierten Konstante `(40,40)` bauen (für beide Slots
identisch, also mitten auf der Karte statt in der eigenen Basis) und einen
Builder einreihen, wenn die Warteschlange leer ist. Keine Kampfeinheiten, kein
Angriff, keine Expansion, keine Aufklärung.

Zusätzlich verletzt es das Command-Gesetz: es ruft `TryPlaceBuilding` und
`TryQueueUnit` direkt auf, statt `CommandIntent`s durch die Ingress zu schicken.
`MatchRunner` legt ausdrücklich das Gegenteil fest. Folge: KI-Aktionen landen
nie im Record-Stream und wären bei Replay oder Netzwerk sofort desynchron.

**Wirkung:** Der Spieler läuft zwölf Minuten zur Gegnerbasis und zerstört neun
wehrlose Objekte. Ohne Antagonist gibt es keinen Grund für Wirtschaft, Techkette
oder Armee.

### 3.2 Zwei Gebäude je Fraktion sind Attrappen

Das ist die zutreffende Fassung der Owner-Vermutung — mit anderen Namen als
vermutet.

| Rolle | Definition | Art | Wirkung |
|---|---|---|---|
| **Lager** (`defId 6` / `23`) | ja | ja | **keine.** Eine repo-weite Suche nach `UnitRole.Storage` liefert außerhalb von Definitionstabelle, Tests und Würfelgröße null Treffer. Die D-024-Lagerkapazität (+2.000 AE) existiert nicht; `EconomySystem` kennt gar keine Obergrenze für Credits. |
| **Radar** (`defId 10` / `27`) | ja | ja | **keine.** Radar-Pings entstehen in `FogOfWarSystem` bei *jeder* eigenen Einheit, nicht am Gebäude. |

Beide kosten AE, beide ziehen Strom, beide bringen nichts. Ein Spieler, der sie
baut, wird bestraft.

Teilweise wirksam ist die **Verteidigungsplattform**: sie schießt, aber ab Werk
fest bewaffnet und für beide Fraktionen identisch. Das im Design zentrale
Modulsystem (MG / Rakete, mit eigenen Kosten und Voraussetzungen) fehlt
vollständig — damit fehlt der einzige Verteidigungs-Entscheidungsraum des MVP.

### 3.3 Die halbe fertige Arbeit ist unerreichbar

Alle 17 Legion-Definitionen sind vollständig — Werte, FBX, Prefabs,
Registry-Einträge. Aber `RtsDeviceInput` verdrahtet die Hotkeys fest auf die
Allianz-Ids 1–17, und `MatchBootstrap` setzt Slot 0 hart auf Alliance. Es gibt
keine Fraktionswahl. **17 von 34 fertigen Definitionen sind für den Menschen
nicht spielbar.**

`SimDefinitions.ToDefinitionId(faction, role)` existiert bereits und wird von
`MatchBootstrap` genutzt — die Ableitung ist also da, sie wird an der
Eingabeschicht nur nicht verwendet.

### 3.4 Die Ökonomie hat keinen Druck

Vier Befunde, die zusammen den zentralen wirtschaftlichen USP aushebeln:

- **Feldreserve ist 2.000.000 AE** statt der 9.000/15.000 AE aus dem Manifest.
  Bei zwei Harvestern dauert die Erschöpfung rund vierzehn Stunden. Es gibt
  keinen Expansionsdruck und keinen Endspiel-Knick.
- **Beide Harvester stehen gleichzeitig in Reichweite von Feld und Raffinerie** —
  bewusst so gebaut, damit der Zyklus „ohne Laufen" läuft. Damit fehlt die
  komplette Logistik: Anfahrt, Konvoi-Verwundbarkeit, Standortentscheidung.
- **Nur 2 Felder statt 5.** Ohne die zwei Expansions- und das umkämpfte
  Zentralfeld hat Kartenkontrolle keinen Ort.
- **Keine Feldanatomie.** Kein Mutterkristall, keine Ausläufer, kein
  Nachwachsen, keine Überernte-Stufen, keine Beschießbarkeit. Aus dem
  14-Felder-Datenmodell in [../../gamedesign/Resources.md](../../gamedesign/Resources.md)
  existieren drei.

Die Ressource heißt im Code durchgehend und konsistent **Aetherium** (Einheit
`AE`). Es gibt keine zweite Ressource, keinen Compute, kein Halving — korrekt,
denn diese Mechaniken sind nicht beschlossen.

### 3.5 Bedienbarkeit: das Spiel ist ohne Anleitung nicht bedienbar

- **Kein produktives UI-System.** Kein Canvas, kein UI Toolkit, kein
  TextMeshPro, keine Font-Datei. Die gesamte Anzeige läuft über zwei
  IMGUI-Dateien. `DebugHud` erklärt sich selbst für gate-untauglich.
- **Keine Bauleiste.** Gebaut wird über dreizehn auswendig zu lernende
  Einzeltasten. Kosten, Voraussetzungen und Verfügbarkeit sind nirgends
  sichtbar.
- **Kein Auswahlmarker.** Nach dem Loslassen der Maustaste ist unsichtbar, was
  ausgewählt ist. Die einzige Rückmeldung ist eine Textzeile im F3-Panel — das
  in der uncommitteten Fassung per Default **aus** ist.
- **Keine Platzierungsvorschau.** Ein Bauauftrag wird sofort an die Mausposition
  gesetzt; fünf Ablehnungsgründe sind für den Spieler ununterscheidbar von
  „kaputt".
- **Keine Minimap.** `MinimapRenderer` ist eine 24-zeilige Koordinatenformel,
  deren einziger Aufrufer der Unit-Test ist.
- **Kein Hauptmenü, kein Ergebnisbildschirm, kein Neustart.** Die
  Build-Szenenliste enthält genau eine Szene. Nach Siegentscheid tickt der Host
  weiter; es gibt kein `Application.Quit` und keinen `SceneManager` im gesamten
  Projekt.
- **Kein sichtbarer Nebel.** Der Fog of War filtert, welche Einheiten einen
  Proxy bekommen — das Terrain wird nirgends verdunkelt. Erkundetes und
  unerkundetes Gelände sehen identisch aus.

### 3.6 Kampf ist unlesbar

- **Keine Zielerfassung, kein Attack-Move, keine Verfolgung.** Einheiten
  erwidern kein Feuer. Eine Einheit muss bereits in Waffenreichweite stehen,
  sonst hält sie ihr Ziel ewig und schießt nie. Die Verteidigungsplattform kann
  strukturell nie feuern.
- `Stop` löscht das Angriffsziel nicht; Angriffe auf **eigene** Einheiten sind
  zulässig und werden von der Siegauswertung als gültige Elimination gewertet.
- **Null VFX.** Kein Partikelsystem, kein Mündungsfeuer, keine Explosion, keine
  Trümmer. Eine Einheit stirbt, indem ihr GameObject verschwindet.
- **Keine Lebensbalken.** Trefferzustand wird als Helligkeit des Fraktionstints
  in 16 Stufen kodiert — bei zwanzig Einheiten nicht ablesbar.

### 3.7 Null Audio

Zwei unabhängige Suchen bestätigen es: keine einzige Audiodatei im gesamten
Repository, keine einzige Code-Referenz auf `AudioSource`, `AudioClip`,
`AudioMixer` oder `PlayOneShot`, kein `IAudioService`. In `Bootstrap.unity`
fehlt sogar der `AudioListener` — selbst ein eingebauter Clip wäre stumm.

[../../tech/AudioArchitecture.md](../../tech/AudioArchitecture.md) ist
vollständig ausspezifiziert und D-039 hat das Backend längst entschieden. Es
fehlt ausschließlich die Ausführung.

### 3.8 Kein Terrain

`CostField` wird mit `Array.Fill(_costs, OpenCost)` initialisiert; eine Suche
nach `Terrain`, `Blocked` oder `Impassable` im Setup liefert null Treffer. Die
Karte ist eine vollständig offene Ebene mit vier eingefärbten Randbalken. Es
gibt keine Höhen, keine Hindernisse, keine Wege, keine Deckung.

## 4. Der Art-Stand im Detail

### 4.1 Was gut ist

Der Aufbereitungslauf war sorgfältig: alle 34 Modelle stehen auf `Y = 0`, sind
in X/Z zentriert, zeigen nach `+Z`, halten ihr Tri-Budget und haben eine
LODGroup mit den Standardschwellen. Die Drop-in-Pipeline funktioniert — ein
konventionskonform benanntes `PF_*`-Prefab registriert sich automatisch.

Die 34 Concept-Art-Blätter sind die verbindliche Vorlage für jede Nachbestellung
und decken sich mit dem Style-Guide.

### 4.2 Was fehlt oder schiefliegt

| Befund | Wirkung |
|---|---|
| **Keine `_MSK`-Teammasken (0 von 34)** | Mangels Maske legt die Präsentation die Fraktionsfarbe per `MaterialPropertyBlock` über *jeden* Renderer — die gelieferte Bemalung ist im Spiel faktisch unsichtbar. Der Weg verstößt zusätzlich gegen den Art-Standard, der `MaterialPropertyBlock` untersagt. |
| **Keine Normal Maps (0 von 34)** | Nieten, Fugen und Panzerkanten — genau das, was den Fraktionsunterschied trägt — lesen sich unter der Spielkamera als glatte Flächen. |
| **Kein Emissive an den 17 Legion-Modellen** | Der orange Leuchtakzent ist laut Style-Guide der Identitätsträger der Legion. Die Allianz trägt ihr Teal wenigstens in der BaseColor. Die Fraktionslesbarkeit auf Distanz ist asymmetrisch. |
| **Alliance LightTank und BattleTank sind nicht unterscheidbar** | Das Concept trennt sie über ein Doppelrohr, das an keinem der beiden Modelle existiert. Der Spieler kann teure T2- nicht von billigen T1-Einheiten unterscheiden — das ist Spielbarkeit, nicht Kosmetik. |
| **Vier Rollenzuordnungen unsicher** | Legion ResearchLab/Power, Legion LightTank/BattleTank, Alliance LightTank/BattleTank, Alliance VehicleFactory (per Ausschluss zugeordnet). |
| **Zwei DefensePlatform-Modelle haben Restsplitter** | Ein abgelöstes Bruchstück schwebt neben dem Sockel — im Spiel sichtbar. |
| **DefensePlatform gegen das falsche Tri-Budget konvertiert** | LOD0 rund 4.500 statt der spezifizierten 1.500 Tris. Der Prüflauf meldet trotzdem grün, weil er die Gebäudeklasse angelegt hat. |
| **34 leere Provenienz-Datensätze** | `licenseId`, `commercialUseGranted`, `attributionRequired`, `verifiedBy` — alle leer, sechs `_TODO` pro Datei. Die Provenienzpflicht ist unerfüllt. |
| **Widerspruch zum Manifest** | `ArtManifest_MS1.md` sagt „es existiert noch kein einziges produziertes Asset" und sperrt in §8 Tripo3D Free ausdrücklich für eingecheckte Assets. Genau von dort stammen alle 34. |

### 4.3 Und der teuerste Punkt: die Assets sind nicht im Repository

`.gitignore` schließt `*.fbx`, `*.png`, `*.mat`, `*.prefab` samt `.meta` aus.
Getrackt sind unter `Art/` ausschließlich 38 `.gitkeep`, 34 `PROVENANCE.json`
und 79 `.meta` — **null Binärdateien**.

Gleichzeitig liegt `AssetMappingRegistry.asset` im Repository und trägt in der
uncommitteten Arbeitskopie 34 GUID-Referenzen auf genau diese ignorierten
Prefabs. Ein beiläufiges `git commit -a` checkt 34 Verweise ein, die in jedem
frischen Clone ins Leere zeigen.

Zusätzlich: `Hashkrieg_Assets` ist **kein Git-Repository** und liegt auf einem
externen Volume. Die 34 Concept-Blätter, die 34 GLB und — entscheidend —
`convert_report.json` mit der einzigen Zuordnung GLB → Spielrolle existieren
genau einmal. Die GLB-Dateinamen sind reine Generator-Prompts
(`steampunk+machine+3d+model (1).glb`); geht das Volume verloren, ist die
Zuordnung nicht rekonstruierbar.

## 5. Das Umbenennungs-Ausmaß in Zahlen

1.590 getrackte Zeilen enthalten „Nova", verteilt auf 97 Dateipfade. 38 Zeilen
nutzen bereits „Hashkrieg" — ausschließlich in der Dokumentation.

| Kategorie | Umfang | Risiko |
|---|---|---|
| Markdown-Prosa | 487 Zeilen | trivial (aber: `CHANGELOG.md` ist Historie und darf nicht umgeschrieben werden) |
| Marke (`productName`, `BuildScript`) | wenige Zeilen | gering — aber der Build-Ausgabepfad steht doppelt, auch im Gate-Prüfskript |
| 17 Assembly-Definitionen | 78 Zeilen | **hoch** — Referenzen sind Klartext-Namen, kein GUID. Nur atomar in einem Commit sicher |
| 226 Namespaces + 560 using-Zeilen | 786 Zeilen | mittel, mechanisch — aber `INovaLogger`, `NullNovaLogger`, `UnityNovaLogger` sind Typnamen und keine Marke |
| GitHub-Repository-Name | 3 Schema-Konstanten | **hoch** — `"const": "VibecodingGermany/Project_Nova"` ist hart in zwei JSON-Schemata und einem Prüfskript validiert |
| `NOVA_FIXED_POINT` | 6 Stellen | **nicht umbenennen** — Build-Flag mit Vertragscharakter, wird ins Determinismus-Artefakt geschrieben und dort per Test hart zugesichert |

Ein Beschluss zur Umbenennung existiert nicht: `Konzept_Hashkrieg.md` trägt
Status „Brainstorm – NICHT verbindlich" und erzeugt keine DecisionLog-Einträge;
eine Suche nach „Hashkrieg", „Umbenenn" oder „Rename" im
[../DecisionLog.md](../DecisionLog.md) liefert null Treffer.

## 6. Governance-Lage

Seit D-076 gilt **Governance-Tier 1**: Nachweis ist grüne CI **plus eine
gespielte und protokollierte Runde**. Hälfte eins läuft. Hälfte zwei fehlt
vollständig — **kein Mensch hat das Spiel je gespielt.**

Das ist der billigste offene Punkt im gesamten Projekt und blockiert jeden
Meilenstein darüber.

Weiter offen: D-067 und D-068 sind Entwürfe ohne Ratifizierung, obwohl der
zugehörige Code längst in `main` läuft. D-074 und D-075 wurden unter Delegation
entschieden und warten auf Inhaber-Bestätigung. Q-040 führt elf
Numerik-Provisorien ohne D-ID.

Drei Dateien liegen uncommittet im Arbeitsbaum auf `main`:
`AssetMappingRegistry.asset`, `UnitViewManager.cs`, `DebugHud.cs`. Der
GrayboxLog nennt uncommittete Sitzungsarbeit selbst als wiederkehrenden Befund.

## 7. Fallen für den ausführenden Agenten

Sechs Dinge, die nirgends sonst zusammenstehen und die man nur einmal falsch
macht:

1. **`DefinitionsHash64` ist replay-brechend.** Jede Wertänderung in
   `SimDefinitions` entwertet aufgezeichnete Replays. Ein Anzeigenamen-Feld darf
   deshalb **nicht** in den Hash einfließen.
2. **Die No-Float-Regel wird per Test erzwungen.** `CommanderSystem` und
   `EvolvedFactionSystem` stehen namentlich auf der Ausnahmeliste — wer sie
   registriert, bricht den Test.
3. **Snapshot-Block-Ids sind vergeben** (100–107, 103 reserviert). Jedes neue
   zustandsbehaftete System braucht eine neue Id und eine Versionsentscheidung.
4. **Die Registrierungsreihenfolge *ist* die Tick-Reihenfolge** und bestimmt den
   State-Hash. Umsortieren ist eine Verhaltensänderung, kein Refactoring.
5. **UI und KI dürfen nur `CommandIntent`s durch die Ingress schicken.**
   Direktaufrufe in Domänensysteme sind der bereits vorhandene Fehler der KI und
   dürfen nicht kopiert werden — auch nicht von Kampagnen-Skripten.
6. **Die drei Headless-Spuren registrieren unterschiedliche Systemmengen**
   (7 / 6 / 8). Wer ein System hinzufügt, muss entscheiden, in welchen Spuren es
   mitläuft — sonst misst die CI etwas anderes als das Spiel.

Dazu zwei tote Datenpfade, die zur Fehldiagnose „Content fehlt" verleiten:

- `Assets/_Project/Scripts/Data/` enthält eine parallele Definitionswelt
  (`UnitDefinitionSO`, `BuildingRegistrySO`, `WeaponRegistrySO`), die mit
  `SimDefinitions` **nicht** verbunden ist. Auf Platte existiert kein einziges
  zugehöriges ScriptableObject.
- `MapDefinitionSO` / `MAP_Glutrinne.asset` wird von keinem Simulationscode
  gelesen; die Kartengröße steht als serialisiertes Feld auf `MatchBootstrap`.

## Offene Punkte

- Die Aufwandsklassen im [Masterplan](02_Masterplan.md) sind aus der Codelage
  geschätzt, nicht gemessen.
- Ob `D-056` (MS-1-Override) nach D-076 unverändert gilt, ist nicht
  ausdrücklich bestätigt.
- [../Milestones.md](../Milestones.md) nennt MS-1 an einer Stelle als „G2 + G4 +
  G5" und lässt G3 aus, führt G3 an anderer Stelle aber doch — genau die
  ausgelassene Stufe ist die größte offene Lücke des Projekts.

## Nächste Schritte

Siehe [02_Masterplan.md](02_Masterplan.md), Phase 0.
