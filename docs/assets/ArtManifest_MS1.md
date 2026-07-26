# Art-Manifest MS-1

**Version:** 0.3.0 | **Status:** Entwurf – MS-1 Art-Strang verbindlich, kein Gate-Nachweis | **Verantwortungsbereich:** Technical Art / Producer | **Sprint:** 7

## Zweck

Dieses Dokument spezifiziert das vollständige Art-Manifest für alle 34 MS-1-Assets (2 Fraktionen × 9 Gebäuderollen + 2 Fraktionen × 8 Einheitenrollen) als menschenlesbares Spezifikationsblatt. Es begleitet die maschinenlesbare Fassung [art-manifest-ms1.json](art-manifest-ms1.json) und legt für jedes Asset Namen, Budgetklasse, Tri-/Textur-Budget, Pfadkonvention, Silhouette, Teamfarben-Platzierung, Animationsbedarf und Beschaffungsstrategie fest. Es liegt bewusst in `docs/assets/` und nicht in `quality/content/` – es ist **kein Gate-Artefakt** und begründet keine Meilenstein- oder G4-Abnahme. Verbindliche Rollen-, ID- und Namensquelle ist [`quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json).

## Abhängigkeiten

- [`quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json) – kanonische Rollen, IDs, `displayName`, `tier` (MS-1-Roster: 2 Fraktionen, 9 Gebäuderollen, 8 Einheitenrollen)
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Tri-Budgets pro Asset-Klasse (LOD0–LOD2), Textur-Budgets, Mipmap-/Atlas-Regeln
- [../gamedesign/Buildings.md](../gamedesign/Buildings.md) – Gebäuderollen, Kosten, Energie, TP-Klasse, Footprint-Annahmen (§6)
- [../gamedesign/Vehicles.md](../gamedesign/Vehicles.md) – Fahrzeugrollen, Panzerungsklasse, Werte-Rahmen
- [../gamedesign/Factions.md](../gamedesign/Factions.md) – Silhouetten-Regeln, Teamfarben, Fraktionsidentität Allianz/Legion
- [../vision/Vision.md](../vision/Vision.md) – Lesbarkeit-vor-Realismus-Grundsatz, Fraktions-Codes
- [Licenses.md](Licenses.md), [AssetRegister.md](AssetRegister.md) – Lizenz- und Beschaffungsregister (BUY/MODIFY/BUILD-Klassifikation, Grundlage für `sourceStrategy` unten)
- [art-manifest-ms1.json](art-manifest-ms1.json) – maschinenlesbares Gegenstück dieses Dokuments

## 1. Feldsemantik und Ableitungsregeln

| Feld | Bedeutung | Ableitungsregel |
|---|---|---|
| `assetId` | eindeutige ID | `<faction>.<domain>.<role>`, z. B. `alliance.building.HQ` |
| `faction` / `domain` / `role` / `displayName` / `tier` | Rollendaten | zeichengenau aus `mvp-v1.json`; Gebäude führen dort kein `tier`-Feld, daher `null` (siehe Offene Punkte) |
| `assetClass` | Budgetklasse | begründet aus [AssetBudget.md](../tech/AssetBudget.md) §1 zugeordnet: `BuildingStandard` (alle neun Gebäuderollen außer Verteidigungsplattform), `DefenseStructure` (Verteidigungsplattform, siehe Begründung unten), `Infantry` (Basis-/Panzerabwehrinfanterie), `VehicleLight` (Builder, Harvester, Scout, Light Tank, Artillery), `VehicleHeavy` (Battle Tank) |
| `triBudget` | LOD0/1/2-Dreieckszahlen | zeichengenau aus AssetBudget.md §1 zur gewählten Klasse übernommen |
| `textureSet` | Auflösung + Maps | Domänen-basiert aus AssetBudget.md §2: Gebäude 2048² Atlas, Einheiten 1024² Atlas, je 3 Maps (BaseColor/Normal/Mask), BC7/BC3 |
| `paths` | Datei-/Ordnerpfade | nach der bindenden Namenskonvention (siehe §2) |
| `footprintCells` | Grid-Footprint | für die in [Buildings.md](../gamedesign/Buildings.md) §6 namentlich genannten Rollen von dort übernommen (`footprintSource: "gamedesign"`); für die dort offen gelassenen Rollen Power, Refinery, Barracks, ResearchLab als art-seitige Arbeitsannahme festgelegt (`footprintSource: "art-assumption"`, siehe §3/§4 und Offene Punkte). Bei Einheiten (`domain: "unit"`) grundsätzlich `null` – Einheiten haben keinen Grid-Footprint |
| `footprintMeters` | Grid-Footprint in Metern | berechnet aus `footprintCells` mit der art-seitigen Arbeitsannahme Grid-Zellgröße = 3,0 m (parallel in `ArtAssetStandard.md` verankert): `x`/`y` in Zellen × 3,0 m. Für Gebäude durchgängig gesetzt; bei Einheiten bleibt `footprintMeters: null` |
| `footprintSource` | Herkunft des Footprint-Werts (nur Gebäude) | `"gamedesign"`, wenn der Wert zeichengenau aus [Buildings.md](../gamedesign/Buildings.md) §6 übernommen wurde; `"art-assumption"`, wenn er hier art-seitig festgelegt wurde, weil Buildings.md ihn offen lässt. Reine Arbeitsannahme, keine Simulationsvorgabe – die Simulation darf den Wert überschreiben; die Modellmaße folgen dann der final gültigen Zellzahl, nicht umgekehrt |
| `silhouetteBrief` | Formensprache + Funktionslesbarkeit | Fraktionssprache aus Factions.md kombiniert mit der Rollenfunktion aus Buildings.md/Vehicles.md |
| `teamColorPlacement` | Teamfarben-Zone + Flächenanteil | Allianz: Panzerkanten/Leuchtelemente, 10–20 %; Legion: großflächige Platten, 40–60 % (Factions.md) |
| `animationNeeds` | benannter (nicht spezifizierter) Animationsbedarf | aus Rollenfunktion abgeleitet, Gebäude i. d. R. statisch mit optionalen Idle-Elementen |
| `sourceStrategy` / `sourceStrategyRationale` | CC0-Base oder AI-Generated | begründet aus [AssetRegister.md](AssetRegister.md) §3.3–§3.5 (Allianz/Legion durchgehend BUY/MODIFY auf CC0-Basis) |
| `verticalSlice` | Vertical-Slice-Flag | `true` nur für `alliance.building.HQ`, `alliance.unit.LightTank`, `legion.building.HQ`, `legion.unit.LightTank` |
| `status` | Produktionsstatus | durchgehend `"specified"` – es existiert noch kein einziges produziertes Asset |

**Zu `DefenseStructure` (Verteidigungsplattform):** [AssetBudget.md](../tech/AssetBudget.md) §1 kennt keine eigene Zeile „DefenseStructure"; die Verteidigungsplattform ist dort funktional am nächsten an der Zeile „Mauer-/Verteidigungsmodul (D-008)" (≤1.500/600/200 Tris), da das Podest als Trägerstruktur für ein austauschbares Waffenmodul dient. Diese Zuordnung ist **diskutabel** und wird unten unter „Offene Punkte" vermerkt.

## 2. Bindende Namens- und Pfadkonvention

- 1 Unity-Unit = 1 Meter.
- Faction-Token in Dateinamen: `Alliance` / `Legion` (PascalCase); die Manifest-ID bleibt `alliance`/`legion`.
- Ordner: `Assets/_Project/Art/Buildings/<Faction>/<Role>/` bzw. `Assets/_Project/Art/Units/<Faction>/<Role>/`.
- Mesh: `SM_BLDG_<Faction>_<Role>.fbx` / `SM_UNIT_<Faction>_<Role>.fbx`, LOD-Meshes darin als `_LOD0`/`_LOD1`/`_LOD2`.
- Texturen: `T_<BLDG|UNIT>_<Faction>_<Role>_BC.png` / `_N.png` / `_MSK.png`.
- Material: `M_<BLDG|UNIT>_<Faction>_<Role>.mat`; Prefab: `PF_<BLDG|UNIT>_<Faction>_<Role>.prefab`.
- Mask-Kanäle: R = Metallic, G = Occlusion, B = TeamMask, A = Smoothness.

Diese Konvention ist maschinenlesbar im `conventions`-Block von [art-manifest-ms1.json](art-manifest-ms1.json) hinterlegt.

Footprint-Meter-Werte in den beiden folgenden Tabellen sind berechnet mit der art-seitigen Arbeitsannahme Grid-Zellgröße = 3,0 m (Zellen × 3,0 m). Für HQ, Storage, VehicleFactory, Radar und DefensePlatform stammt der Zellwert aus [Buildings.md](../gamedesign/Buildings.md) §6 (`footprintSource: "gamedesign"`). Für Power, Refinery, Barracks und ResearchLab – dort ausdrücklich als offen markiert – legt dieses Dokument die Werte art-seitig fest (`footprintSource: "art-assumption"`, Begründung unter „Offene Punkte"). Beide Kategorien sind Arbeitsannahmen der Art-Abteilung, keine Simulationsvorgaben: Die Simulation darf sie überschreiben, und die Modellmaße folgen dann der final gültigen Zellzahl, nicht umgekehrt.

## 3. Gebäude – Allianz (9)

| Rolle | Name | assetClass | Tri-Budget (LOD0/1/2) | Textur | Footprint (Zellen) | Footprint (m) | footprintSource | Vertical Slice |
|---|---|---|---|---|---|---|---|---|
| HQ | Kommandozentrale | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | gamedesign | Ja |
| Power | Fusionsreaktor | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| Refinery | Aetherium-Aufbereiter | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | art-assumption | Nein |
| Storage | Depot | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |
| Barracks | Ausbildungszentrum | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| VehicleFactory | Fahrzeugwerk | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | gamedesign | Nein |
| ResearchLab | Forschungslabor | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| Radar | Radarstation | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |
| DefensePlatform | Aegis-Plattform | DefenseStructure | 1.500 / 600 / 200 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |

## 4. Gebäude – Legion (9)

| Rolle | Name | assetClass | Tri-Budget (LOD0/1/2) | Textur | Footprint (Zellen) | Footprint (m) | footprintSource | Vertical Slice |
|---|---|---|---|---|---|---|---|---|
| HQ | Gefechtsstand | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | gamedesign | Ja |
| Power | Schwerer Generator | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| Refinery | Schmelzofen | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | art-assumption | Nein |
| Storage | Bunkerdepot | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |
| Barracks | Rekrutenlager | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| VehicleFactory | Montagehalle | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 4×4 | 12,0 × 12,0 m | gamedesign | Nein |
| ResearchLab | Kriegslabor | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 3×3 | 9,0 × 9,0 m | art-assumption | Nein |
| Radar | Funkposten | BuildingStandard | 20.000 / 8.000 / 2.000 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |
| DefensePlatform | Geschützstellung | DefenseStructure | 1.500 / 600 / 200 | 2048² BC/N/MSK | 2×2 | 6,0 × 6,0 m | gamedesign | Nein |

## 5. Einheiten – Allianz (8)

| Rolle | Name | Tier | assetClass | Tri-Budget (LOD0/1/2) | Textur | Vertical Slice |
|---|---|---|---|---|---|---|
| Builder | Pionier „Atlas" | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| Harvester | Sammler „Demeter" | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| BasicInfantry | Rifleman | 1 | Infantry | 4.000 / 1.500 / 400 | 1024² BC/N/MSK | Nein |
| AntiArmorInfantry | Rocket Soldier | 2 | Infantry | 4.000 / 1.500 / 400 | 1024² BC/N/MSK | Nein |
| ScoutVehicle | Jackal-Aufklärer | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| LightTank | Lynx | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Ja |
| BattleTank | Aegis | 2 | VehicleHeavy | 15.000 / 6.000 / 1.500 | 1024² BC/N/MSK | Nein |
| Artillery | Longbow | 2 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |

## 6. Einheiten – Legion (8)

| Rolle | Name | Tier | assetClass | Tri-Budget (LOD0/1/2) | Textur | Vertical Slice |
|---|---|---|---|---|---|---|
| Builder | Vorarbeiter | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| Harvester | Schürfer | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| BasicInfantry | Rekrut | 1 | Infantry | 4.000 / 1.500 / 400 | 1024² BC/N/MSK | Nein |
| AntiArmorInfantry | Raketenschütze | 2 | Infantry | 4.000 / 1.500 / 400 | 1024² BC/N/MSK | Nein |
| ScoutVehicle | Hyäne (Buggy) | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |
| LightTank | Räuber | 1 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Ja |
| BattleTank | Koloss | 2 | VehicleHeavy | 15.000 / 6.000 / 1.500 | 1024² BC/N/MSK | Nein |
| Artillery | Donnerkanone | 2 | VehicleLight | 8.000 / 3.000 / 800 | 1024² BC/N/MSK | Nein |

## 7. Silhouette, Teamfarben und Animationsbedarf

**Fraktionssprache (Factions.md, verbindlich für alle Assets):**

- **Allianz:** eckig-präzise Formensprache, klare Kanten, vertikale Akzente (Antennen, Geschütztürme), glatte Panzerflächen. Teamfarbe (Azurblau) an Panzerkanten und Leuchtelementen, Zielkorridor 10–20 % der sichtbaren Fläche.
- **Legion:** wuchtige, horizontal gestreckte Formensprache, Rohre/Raketenständer/Schornsteine, unregelmäßige genietete Panzerplatten. Teamfarbe (Rostrot/Ocker) großflächig auf Panzerplatten, Zielkorridor 40–60 % der sichtbaren Fläche.

Die konkrete Funktionslesbarkeit jeder Rolle (z. B. „Radarschale als dominantes Erkennungsmerkmal" bei Radar, „Raketenwerfer auf der Schulter" bei AntiArmorInfantry) ist pro Asset im `silhouetteBrief`-Feld der JSON-Datei hinterlegt und kombiniert die Fraktionssprache mit der Rollenfunktion aus Buildings.md/Vehicles.md.

**Animationsbedarf (nur benannt, nicht spezifiziert):**

- Gebäude: grundsätzlich statisch; optionale Idle-Elemente (Antennen-/Radardrehung, Leuchtpulsieren, Rauch-/Abluft-VFX), kein Rig.
- Builder: Bauarm-/Werkzeuganimation während der Errichtung.
- Harvester: Ladearm-/Schaufelanimation beim Abbau und bei der Entladung.
- BasicInfantry / AntiArmorInfantry: Humanoid-Rig nötig (Idle/Lauf/Schuss[/Nachladen]/Tod, Mecanim).
- ScoutVehicle / LightTank / BattleTank / Artillery: rig-los (Code-Animation); BattleTank zusätzlich Turmrotation und Rohrrückstoß, Artillery zusätzlich Rohrrückstoß beim Schuss und Aufstellungsanimation.

## 8. Beschaffungsstrategie (`sourceStrategy`)

Alle 34 Assets sind als `CC0-Base` klassifiziert. Begründung: [AssetRegister.md](AssetRegister.md) §3.3–§3.5 stuft Allianz- und Legion-Gebäude, -Fahrzeuge und -Infanterie durchgehend als **BUY** bzw. **MODIFY** auf CC0-Basis (Synty/Kenney/Quaternius-Kitbash) ein; **BUILD** ist dort ausschließlich für Evolvierte-Assets, Aetherium-Geometrie/-Shader und Fraktions-Signaturen vorgesehen – keines dieser Fälle liegt im MS-1-Scope (nur Allianz/Legion). Kein Asset in diesem Manifest ist daher `AI-Generated` im Sinne eines vollständigen Eigenbaus.

**0-€-Beschaffungsstrategie (Projektinhaber-Vorgabe):** Bezahlte Anbieter-Tiers sind für eingecheckte Assets kategorisch ausgeschlossen; erlaubt sind ausschließlich CC0-Quellen, lokal/self-hosted betriebenes Hunyuan3D 2.1 und die OpenAI Image API. Meshy Free und Tripo3D Free sind für eingecheckte Assets gesperrt. Geprüft: Kein Asset in diesem Manifest trägt `sourceStrategy: AI-Generated` oder ist einem der gesperrten Anbieter zugeordnet; alle 34 Einträge sind `CC0-Base` gemäß obiger Begründung. Sollte ein künftiger Wechsel auf `AI-Generated` erfolgen, ist im `sourceStrategyRationale`-Feld zusätzlich zu vermerken, dass die Generierung über Hunyuan3D 2.1 lokal erfolgt.

## Offene Punkte

- **Footprints der Rollen Power, Refinery, Barracks und ResearchLab (art-seitig festgelegt, Project-Owner-Entscheidung 2026-07-25):** [Buildings.md](../gamedesign/Buildings.md) §6 nennt Footprint-Werte namentlich nur für HQ/Fahrzeugfabrik/Superwaffe (4×4) sowie Lager/Radar/Plattform (2×2) und markiert diese selbst als Annahme („Footprints (Status: Abgleich mit Maps.md läuft)"); für Power, Refinery, Barracks und ResearchLab bleibt der Wert dort offen. Dieses Dokument legt sie art-seitig fest, um die Assets spezifizierbar zu machen: Refinery 4×4 (Harvester-Andockung erfordert die größere Fläche), Power/Barracks/ResearchLab je 3×3 (mittelgroße Produktions-/Versorgungsbauten zwischen den bereits dokumentierten 2×2-Kleinbauten und dem 4×4-HQ). Kennzeichnung im `footprintSource`-Feld als `"art-assumption"` (gegenüber `"gamedesign"` für die bereits in Buildings.md benannten Rollen). **Diese Werte sind reine Art-Arbeitsannahmen, keine Simulationsvorgaben** – die Simulation darf sie überschreiben, sobald das Baugrid final ist; die Modellmaße folgen dann der final gültigen Zellzahl, nicht umgekehrt.
- **`tier` bei Gebäuden:** `quality/content/mvp-v1.json` führt für Gebäude kein `tier`-Feld (nur für Einheiten). Alle Gebäude-Einträge haben daher `tier: null`.
- **assetClass „DefenseStructure":** Kein eigener Budget-Eintrag in [AssetBudget.md](../tech/AssetBudget.md) §1; die Zuordnung zur Zeile „Mauer-/Verteidigungsmodul (D-008)" (1.500/600/200 Tris) ist eine begründete, aber diskutable Annahme (siehe §1 oben).
- **Builder/Harvester als `VehicleLight`:** Beide tragen laut [Vehicles.md](../gamedesign/Vehicles.md) die Panzerungsklasse „Schwer", wurden hier aber als Nicht-Kampf-Nutzfahrzeuge der Klasse Fahrzeug leicht/mittel zugeordnet (kein eigener Nutzfahrzeug-Budgeteintrag vorhanden) – diskutabel.

## Nächste Schritte

1. Fachliche Prüfung der `DefenseStructure`- und `VehicleLight`-Klassenzuordnung (Offene Punkte) durch Lead Performance Engineer / Technical Art Director.
2. Finale Abstimmung der art-seitig festgelegten Footprints (Power, Refinery, Barracks, ResearchLab) mit Maps.md/Simulation nachholen, sobald das Baugrid final ist; bei Abweichung überschreibt die Simulation die hier gesetzten Annahmen.
3. Nach CC0-/KI-Quellenauswahl je Asset (ProcurementStrategy.md-Workflow): konkrete Kandidatenquelle und Lizenz in [AssetRegister.md](AssetRegister.md) bzw. [Licenses.md](Licenses.md) nachtragen – nicht in diesem Manifest.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.3.0 | 2026-07-25 | Footprints für Power, Refinery, Barracks, ResearchLab art-seitig festgelegt (Project-Owner-Entscheidung): Refinery 4×4, Power/Barracks/ResearchLab je 3×3, jeweils identisch für beide Fraktionen; neues Feld `footprintSource` (`gamedesign`/`art-assumption`) in Abschnitt 1, 3, 4 eingeführt und für alle 18 Gebäude nachgetragen; Abschnitt 2, Offene Punkte und Nächste Schritte entsprechend aktualisiert – Werte bleiben art-seitige Arbeitsannahmen, keine Simulationsvorgaben | Technical Art |
| 0.2.0 | 2026-07-25 | Neues Feld `footprintMeters` (Abschnitt 1, 3, 4) ergänzt, berechnet aus `footprintCells` mit Grid-Zellgröße 3,0 m; Rollen ohne Footprint (Power, Refinery, Barracks, ResearchLab) bleiben `NICHT DEFINIERT`; 0-€-Beschaffungsstrategie in Abschnitt 8 geprüft und dokumentiert (kein Asset `AI-Generated` oder gesperrtem Anbieter zugeordnet) | Technical Art |
| 0.1.0 | 2026-07-25 | Erstfassung: vollständiges MS-1-Art-Manifest für 34 Assets (Spezifikation, kein Gate-Nachweis) | Technical Art |
