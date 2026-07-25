# Asset-Register (Asset Audit)

**Version:** 1.2.1 | **Status:** sprint-freigegeben – MS-1 rebaselined | **Verantwortungsbereich:** Technical Art Director / Lead Environment Artist / Project Owner | **Sprint:** 5

## Zweck

Das **verbindliche Asset-Register** von *Project Nova*: pro benötigter Asset-Kategorie die kanonische Bedarfsmenge (mit führendem GDD-Dokument), eine Kandidatenquelle, Lizenz, Kostenschätzung (0 € Budget per D-054), URP-/Qualitätsbewertung, geschätzter Anpassungsaufwand und die **BUY / MODIFY / BUILD**-Klassifikation nach der Rubrik aus [ProcurementStrategy.md](ProcurementStrategy.md) §3. Dieses Dokument ist das zentrale Sprint-5-Ergebnis (Asset Audit) und die Eingabe für die Produktionsplanung (Sprint 6).

Die Bedarfsmengen folgen der **Single-Source-of-Truth-Regel** (D-047): jede Zahl verweist auf das führende GDD-Dokument, statt sie hier neu festzulegen. Die historische [../../RTS_Asset_Pipeline.md](../../RTS_Asset_Pipeline.md) (APL) ist eine Wunschliste und wird von diesem Register **abgelöst** (Reconciliation §2).

## Abhängigkeiten

- [ProcurementStrategy.md](ProcurementStrategy.md) – Strategie B (D-053), Klassifikations-Rubrik, Bewertungsdimensionen
- [Licenses.md](Licenses.md) – Lizenz-Register (Seats/Attribution/Weitergabe je Quelle)
- [BuildBacklog.md](BuildBacklog.md) – Detail zu allen BUILD/MODIFY-Eigenbauten
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Polycount-/Textur-/LOD-Budgets, Kauf-Prüfung §6
- [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) – Marktquellen und Preisniveaus
- Führende GDD-Quellen: [../gamedesign/Factions.md](../gamedesign/Factions.md), [Buildings.md](../gamedesign/Buildings.md), [Infantry.md](../gamedesign/Infantry.md), [Vehicles.md](../gamedesign/Vehicles.md), [Aircraft.md](../gamedesign/Aircraft.md), [NeutralUnits.md](../gamedesign/NeutralUnits.md), [Biomes.md](../gamedesign/Biomes.md), [Maps.md](../gamedesign/Maps.md), [Resources.md](../gamedesign/Resources.md), [CommanderSystem.md](../gamedesign/CommanderSystem.md)

## MS-1-Override (D-056)

Das Register bewahrt den Vollspielbedarf als Beschaffungsinventar. Für MS-1
werden jedoch ausschließlich die zwei Fraktionen, neun Gebäuderollen, acht
Einheitenrollen und Glutrinne aus
[MVPContentManifest.md](../production/MVPContentManifest.md) produziert.
Evolvierte, Luft, Eliten, Superwaffen, Drohnen, Commander und weitere Biome
sind Post-MVP.

## 1. Fraktionen (Kontext für alle Einheiten-Kategorien)

Drei asymmetrische Fraktionen ([../gamedesign/Factions.md](../gamedesign/Factions.md), D-008): **Allianz** (High-Tech, Azurblau/Stahlgrau), **Legion** (Masse/Industrie, Rostrot/Ocker), **Evolvierte** (biologisch, Violett/Bio-Grün). Als grobe Regel über alle Einheiten-Kategorien:

- **Allianz & Legion** = menschlich-mechanisch → im Synty-Militär-/Sci-Fi-Stil käuflich abgedeckt → überwiegend **BUY/MODIFY**.
- **Evolvierte** = organisch-biologisch → im Store kaum abgedeckt → überwiegend **MODIFY/BUILD** (siehe [BuildBacklog.md](BuildBacklog.md)).

## 2. Reconciliation: kanonische Zahlen vs. RTS_Asset_Pipeline.md (APL)

Die APL nennt an mehreren Stellen überholte Mengen; die GDD-Entscheidungen aus Sprint 2 sind maßgeblich:

| Kategorie | APL (historisch) | **Kanonisch (GDD)** | Entscheidung | Führendes Dokument |
|---|---|---|---|---|
| Gebäude | 18/Fraktion ≈ 54 | **12/Fraktion = 36** | D-008 | [Buildings.md](../gamedesign/Buildings.md) |
| Infanterie | 24 | **24** (8/Fraktion) | – | [Infantry.md](../gamedesign/Infantry.md) |
| Fahrzeuge | 36 | **36** (12/Fraktion) | – | [Vehicles.md](../gamedesign/Vehicles.md) |
| Luftfahrzeuge | 21 | **21** (7/Fraktion) | – | [Aircraft.md](../gamedesign/Aircraft.md) |
| Spezial-/Elite | 15 | **0 (MS-1) → 9 (Vollspiel)** + 3 Superwaffen | D-056 / D-015 / D-023 | [Vehicles.md](../gamedesign/Vehicles.md) / [Buildings.md](../gamedesign/Buildings.md) |
| Drohnen | 6 (Typen) | **3/Fraktion = 9** | D-014 | [Vehicles.md](../gamedesign/Vehicles.md) |
| Neutrale | Tiere/Banditen/Mutanten/**Händler**/Türme | Critters 10 + Lager-Sätze + 3 Türme, **kein Handel** | D-016 | [NeutralUnits.md](../gamedesign/NeutralUnits.md) |
| Biome | 10 | **10** | D-017 | [Biomes.md](../gamedesign/Biomes.md) |
| Karten | 10 | **12** (1/4/8/12-Roadmap) | D-017 | [Maps.md](../gamedesign/Maps.md) |
| Marine (Paket 07) | 6 „optional" | **0 – gestrichen** | D-013 | [../production/DecisionLog.md](../production/DecisionLog.md) |
| Commander | – | **3** (1/Fraktion) | D-009 | [CommanderSystem.md](../gamedesign/CommanderSystem.md) |

Ein nicht-destruktiver **Korrekturhinweis** an der Spitze der APL verweist auf dieses Register (siehe [../../RTS_Asset_Pipeline.md](../../RTS_Asset_Pipeline.md)).

## 3. Register nach Kategorie

Spalten: **Bedarf** (kanonische Menge), **Kandidatenquelle**, **Lizenz** (Detail in [Licenses.md](Licenses.md)), **Kosten** (Einschätzung, Listen-/Sale-Spanne), **URP/Qualität**, **Aufwand** (Person-Tage, grob), **Klasse**.

### 3.1 Environment / Biome / Terrain

| Posten | Bedarf | Kandidatenquelle | Lizenz | Kosten | URP/Qualität | Aufwand | Klasse |
|---|---|---|---|---|---|---|---|
| Biome-Basis-Kits | 10 Biome ([Biomes.md](../gamedesign/Biomes.md)) | Synty Nature-Serie (Swamp, Snow, Meadow), Apocalypse Wasteland; CC0: Kenney/Quaternius | Synty-EULA (5 Seats) / CC0 | 150–450 USD (Bundles) | Synty URP ok; CC0 = FBX ohne Material → URP-Setup | 1–2 PT/Biom Material-Pass | **MODIFY** |
| Terrain-Tooling | 1 Toolchain | Gaia Pro VS **oder** MapMagic 2 (Core kostenlos) | per Seat | 0–200 USD | URP-kompatibel; Vegetation-Shader kritisch | 2–3 PT Einrichtung | **BUY** |
| Vegetation-Shader | – | The Vegetation Engine (o. ä.) | per Seat | 30–80 USD | Ersatz-Shader nötig, da Fremd-Bäume Built-in | 1–2 PT | **MODIFY** |
| Straßen/Brücken/Deko | Kartenbedarf | Synty-Packs (in Biome-Kits enthalten) | Synty-EULA | in Kits | ok | Kit-Bashing | **BUY** |
| Aetherium-Verseuchungs-Layer | pro Biom | — nicht käuflich — | eigen | – | Signature-Overlay | siehe [BuildBacklog.md](BuildBacklog.md) | **BUILD** |

Karten (12, [Maps.md](../gamedesign/Maps.md)) sind **Level-Design aus Biom-Bausteinen**, kein Kauf-Asset; sie entstehen aus den Biome-Kits + Aetherium-Layer.

### 3.2 Aetherium / Ressourcen (Signature-USP)

| Posten | Bedarf | Kandidatenquelle | Lizenz | Kosten | URP/Qualität | Aufwand | Klasse |
|---|---|---|---|---|---|---|---|
| Kristall-Basisgeometrie | Mutterkristall + Ausläufer ([Resources.md](../gamedesign/Resources.md)) | Stylized Crystals Megapack (Piloto) als Rohform | Asset-Store-EULA | 0–25 USD | **URP-Badge pro Pack einzeln prüfen** (PBR-Pack = nicht URP!) | Basis übernehmen | **MODIFY** |
| Aetherium-Shader (Glow, Wachstumsstufen, Verseuchung) | 1 Shader-System | — nicht käuflich — | eigen | – | Kernfeature, umweltverändernd | siehe [BuildBacklog.md](BuildBacklog.md) | **BUILD** |
| Partikel/VFX Kristallwachstum | – | — eigen (Teil VFX §3.10) | eigen | – | – | – | **BUILD** |

Aetherium ist das **Signature-Element** (D-010) und funktional einzigartig (Nachwachsen, Ausbreitung, Überernte) – kein Store-Asset bildet das ab.

### 3.3 Gebäude (36 = 12/Fraktion)

Führend: [Buildings.md](../gamedesign/Buildings.md) (D-008). Modulare Sci-Fi-Baukästen + fraktionsspezifisches Kit-Bashing sind der Kohärenz-Weg (größter Einzelposten der Pipeline).

| Fraktion | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Allianz | 12 | Synty Sci-Fi City/Outpost (modular) | in Packs | 0,5–1 PT/Gebäude Dach-/Detail-Kitbash | **MODIFY** |
| Legion | 12 | Synty Sci-Fi + Apocalypse (industriell/roh) | in Packs | 0,5–1 PT/Gebäude | **MODIFY** |
| Evolvierte | 12 | — kaum Store-Deckung (organisch) — | – | 1,5–3 PT/Gebäude Eigenbau | **BUILD** |

Bau-/Trümmer-Zustände (D-012) liegen im selben Polycount-Budget ([AssetBudget.md](../tech/AssetBudget.md) §1), sind aber je Gebäude als Material-/Mesh-Swap zu erstellen (im Aufwand enthalten).

### 3.4 Infanterie (24 = 8/Fraktion)

Führend: [Infantry.md](../gamedesign/Infantry.md). Humanoid-Rig ⇒ Animationen zwischen Packs retargetbar (senkt Mischrisiko).

| Fraktion | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Allianz | 8 | Synty Military/Sci-Fi (fully rigged), Sidekick-Varianten | 35–150 USD/Pack | Teamfarben-/Material-Pass 0,25 PT/Typ | **BUY** |
| Legion | 8 | Synty Military (rau/Masse) | in Packs | Material-Pass | **BUY** |
| Evolvierte | 8 | — organisch, nicht abgedeckt — | – | 1–2 PT/Typ Eigenbau + Rig | **BUILD** |

Capture-Einheiten (Engineer/Saboteur/Tunnelgräber, D-022) sind Teil der 8/Fraktion, kein Zusatz.

### 3.5 Fahrzeuge (36 = 12/Fraktion)

Führend: [Vehicles.md](../gamedesign/Vehicles.md). Rig-los (Code-Animation) ⇒ kein Rigging-Aufwand.

| Fraktion | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Allianz | 12 | Synty Sci-Fi/Military/Mech Packs | 50–150 USD/Pack | Material-/LOD-Pass 0,25–0,5 PT/Typ | **BUY** |
| Legion | 12 | Synty Military | in Packs | Material-/LOD-Pass | **BUY/MODIFY** |
| Evolvierte | 12 | biologische Entsprechungen — nicht abgedeckt | – | 1,5–2,5 PT/Typ Eigenbau | **BUILD** |

### 3.6 Luftfahrzeuge (21 = 7/Fraktion)

Führend: [Aircraft.md](../gamedesign/Aircraft.md). Eigene Steering-Schicht (D-034), rig-los.

| Fraktion | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Allianz | 7 | Synty Sci-Fi Space/City (Flugobjekte) | in Packs | Material-/LOD-Pass | **BUY/MODIFY** |
| Legion | 7 | Synty | in Packs | Material-/LOD-Pass | **MODIFY** |
| Evolvierte | 7 | organisch (Flugbrut) — nicht abgedeckt | – | 1,5–2,5 PT/Typ Eigenbau | **BUILD** |

### 3.7 Elite-Einheiten & Superwaffen

Führend Elite-Werte: [Vehicles.md](../gamedesign/Vehicles.md) (D-015); Superwaffen: [Buildings.md](../gamedesign/Buildings.md) (D-023).

| Posten | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Elite (Titan-Mech, Mobile Festung) | 2 von 3 menschlich (Post-MVP; Vollspiel 3/Fraktion) | Synty Mech Pack als Basis | ~50 USD | 2–3 PT Hero-Detail/LOD (2× 2048² Ausnahmebudget) | **MODIFY** |
| Elite Evolvierte (Alpha-Mutant) | 1 | — organisch — | – | 3 PT Eigenbau | **BUILD** |
| Superwaffen-Gebäude | 3 (1/Fraktion) | Kitbash aus Gebäude-Kits | in Packs | 2–3 PT/Stk Signature-Aufbau | **MODIFY/BUILD** |

Elite sind Hero-Assets (max. 1–3 gleichzeitig, D-015) → höheres Textur-/Poly-Ausnahmebudget zulässig.

### 3.8 Drohnen (9 = 3/Fraktion)

Führend: [Vehicles.md](../gamedesign/Vehicles.md)/[Aircraft.md](../gamedesign/Aircraft.md) (D-014, Produktion in bestehenden Fabriken). Kleine, oft instanzierte Meshes → strenges Budget.

| Fraktion | Bedarf | Kandidatenquelle | Aufwand | Klasse |
|---|---|---|---|---|
| Allianz/Legion | 3 + 3 | Synty-Kleinobjekte / CC0 | Material-Pass | **BUY/MODIFY** |
| Evolvierte | 3 | organische Sporen/Keime — eigen | 0,5–1 PT/Typ | **BUILD** |

### 3.9 Neutrale Einheiten

Führend: [NeutralUnits.md](../gamedesign/NeutralUnits.md) (D-016). **Kein Handelssystem/Händler** (gestrichen).

| Posten | Bedarf | Kandidatenquelle | Kosten | Aufwand | Klasse |
|---|---|---|---|---|---|
| Critters (Ambient-Fauna) | 10 Typen, biomgebunden | CC0 (Quaternius Animals), Synty | 0–40 USD | Material-Pass | **BUY** |
| Feindliche Lager (Banditen/Mutanten) | Einheiten-Reskins bestehender Fraktions-Meshes | intern (Reuse) | – | 0,5 PT/Variante Reskin | **MODIFY** |
| Capturebare Geschütztürme | 3 Modulvarianten (MG/Flak/Rakete) | Teil Gebäude-Verteidigungsmodul | in Gebäude-Kits | Reuse | **MODIFY** |

### 3.10 VFX

Führend Budgets: [AssetBudget.md](../tech/AssetBudget.md) §4. **Größtes URP-Filterproblem** – nur Packs mit explizitem URP-Support.

| Posten | Kandidatenquelle | Lizenz | Kosten | URP | Klasse |
|---|---|---|---|---|---|
| Allgemeine Kampf-VFX (Mündung, Treffer, Explosion) | Hovl Studio (stylized), Vefects URP-Linien; War FX (frei) | Asset-Store-EULA | 0–40 USD/Pack | **explizit URP-Packs** wählen | **BUY/MODIFY** |
| Schilde/Laser/EMP | stylized VFX-Packs | EULA | 10–40 USD | URP prüfen | **MODIFY** |
| Aetherium-Wachstum/Verseuchung | — eigen (Signature) | eigen | – | – | **BUILD** |
| Wetter/Hazard (D-028) | Basis-Packs + eigen | gemischt | – | screen-space-nah | **MODIFY** |

Stylized (nicht realistisch), da auf Top-Down-Distanz Silhouette/Timing zählen und realistische Effekte den Synty-Look brechen (R-04).

### 3.11 Audio (SFX / Musik / Sprache)

Führend Budgets: [AssetBudget.md](../tech/AssetBudget.md) §5.

| Posten | Kandidatenquelle | Lizenz | Kosten | Klasse |
|---|---|---|---|---|
| SFX (Waffen, Motoren, UI, Ambience) | **Sonniss GDC Bundles** (>200 GB, royalty-free) | royalty-free, **nicht weitervertreibbar** | 0 USD | **BUY** (kuratieren) |
| Musik | Store-Tracks **oder** Composer-Auftrag | „für Interactive?" prüfen | 20–100 USD/Track o. Auftrag | **BUY/BUILD** |
| Sprache/Commander-VO | Auftrag (Signature, [CommanderSystem.md](../gamedesign/CommanderSystem.md)) | Auftrag/eigen | Auftrag | **BUILD** |

Sonniss-Dateien dürfen **nicht** in das öffentliche Repo (Repo-Hygiene, [ProcurementStrategy.md](ProcurementStrategy.md) §5). Audio-Kohärenz entsteht über die Mixing-/Mastering-Kette (Voice-Management für 100+ Einheiten, [AssetBudget.md](../tech/AssetBudget.md) §5).

### 3.12 UI

Führend Budgets: [AssetBudget.md](../tech/AssetBudget.md) §2 (UI). Weitgehend pipeline-unabhängig (uGUI/UI Toolkit).

| Posten | Kandidatenquelle | Kosten | Klasse |
|---|---|---|---|
| Icon-Bibliothek (Einheiten/Gebäude/Fähigkeiten) | Sci-Fi-Icon-Packs (Store) | 10–40 USD | **BUY** |
| HUD-/Sci-Fi-Rahmen-Elemente | Synty INTERFACE (in Bundles) | in Bundles | **MODIFY** |
| RTS-Layout (Command-Card, Ressourcenleiste, Minimap, Gruppenporträts) | — gameplay-verzahnt, eigen — | – | **BUILD** |

RTS-UI ist stark gameplay-verzahnt; gekaufte HUDs sind generisch → Layout eigen, Icons kaufen (Icons sind der teure Teil).

### 3.13 Animationen

Führend Budgets: [AssetBudget.md](../tech/AssetBudget.md) §3 (LOD/Culling); pipeline-unabhängig (Daten).

| Posten | Kandidatenquelle | Lizenz | Kosten | Klasse |
|---|---|---|---|---|
| Humanoid-Basis (Idle/Lauf/Schuss/Tod/Reparieren/Bauen/Sammeln) | Quaternius Universal Animation Library (CC0, retarget-fähig), Kevin Iglesias, Mixamo | CC0 / EULA / **Mixamo-Rohdaten-Einschränkung** | 0–30 USD/Pack | **BUY** |
| Signature (Hero/Commander/Evolvierte-Organik) | eigen | eigen | – | **BUILD** |

Humanoid-Retargeting gleicht Rig-Unterschiede aus; RTS-Distanz verzeiht generische Animationen. Mixamo-Rohdaten nicht in geteilte Repos (Lizenzdetail → [Licenses.md](Licenses.md)).

### 3.14 Fraktionsidentität & Commander

Führend für das Post-MVP-Zielbild:
[CommanderSystem.md](../gamedesign/CommanderSystem.md) (D-009). D-056
verschiebt Commander-Portraits, Key Art und Voice vollständig hinter MS-1.

| Posten | Bedarf | Quelle | Klasse |
|---|---|---|---|
| Commander-Portraits + Key Art | 3 (1/Fraktion) | eigen (Signature, TPD §7.2) | **BUILD** |
| Logos/Banner/Farbpaletten | 3 Sätze | eigen | **BUILD** |
| Ladebildschirme | pro Fraktion/Kampagne | eigen | **BUILD** |

## 4. Auswertung (Rollup)

**Klassifikations-Verteilung** (grob, nach Kategorie-Schwerpunkt):

| Klasse | Schwerpunkt | Beispiele |
|---|---|---|
| **BUY** | menschliche Einheiten (Basis), Critters, Icons, SFX, Basis-Animationen, Terrain-Tool | Allianz/Legion-Infanterie, Sonniss-SFX, CC0-Animationen |
| **MODIFY** | Gebäude Allianz/Legion, Fahrzeuge/Luft, Biome, Kristall-Basis, VFX, HUD-Rahmen | Synty-Kitbash + Teamfarben-/LOD-Pass |
| **BUILD** | **alle Evolvierten**, Aetherium (Geometrie+Shader+VFX), Fraktions-Signaturen, RTS-UI-Layout, Commander-VO/Art | siehe [BuildBacklog.md](BuildBacklog.md) |

**Kostenschätzung Kauf-Anteil (D-054):** **0 €** (Open-Source & KI-Pipeline). Alle Basis-Assets werden über freie CC0-Quellen (Quaternius, Kenney, Sonniss Audio) und KI-Generatoren (Hunyuan3D, Meshy, SD) bezogen. Q-035 ist geschlossen (0 €). Der dominierende Realaufwand liegt im **BUILD/MODIFY-Personalaufwand** der Community (v. a. komplette Evolvierten-Fraktion + Aetherium) – quantifiziert in [BuildBacklog.md](BuildBacklog.md), Eingabe für die Aufwandsschätzung R-16 in Sprint 6.

## Offene Punkte

- **Person-Tage-Schätzungen sind grob** und ohne kalibrierten Referenz-Frame (Phase-0-Signature-Asset). Nach dem Phase-0-Bau sind sie in v1.1.0 nachzujustieren (koppelt an R-16).
- **Terrain-Verfahren** (Unity Terrain vs. Custom Mesh) ist noch keinem TDD zugeordnet ([AssetBudget.md](../tech/AssetBudget.md), Offene Punkte) – beeinflusst Environment-Aufwand.
- **Q-035** (Budget-Obergrenze) ist durch D-054 geschlossen (0 € Budget); Q-036 und Q-037 entfallen durch CC0/KI-Strategie.

## Nächste Schritte

1. Sprint 6: BUILD/MODIFY-Aufwand aus [BuildBacklog.md](BuildBacklog.md) in die Roadmap/Meilensteine und die Aufwandsschätzung (R-16) übernehmen (0 € Asset-Budget).
2. Phase 0: Signature-Asset (Aetherium-Feld) bauen und vermessen → Person-Tage- und KI-Pipeline-Schätzungen als v1.1.0 kalibrieren.
3. Laufend: Jeder CC0-/KI-Asset-Import → [Licenses.md](Licenses.md)-Eintrag; Register-Zeile auf „integriert" fortschreiben.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-22 | Erstfassung Sprint 5: vollständiges Asset-Register über 14 Kategorien, kanonische GDD-Zahlen (APL-Reconciliation), BUY/MODIFY/BUILD-Klassifikation, Kosten-/Aufwandsschätzungen | Technical Art Director / Lead Environment Artist |
| 1.1.0 | 2026-07-24 | Update auf D-054 (0 € Open-Source & KI-Pipeline), Q-035 geschlossen (0 € Budget), Kostenschätzung angepasst | Project Owner / Producer |
| 1.2.0 | 2026-07-24 | Vollspielinventar vom verbindlichen Allianz-/Legion-MS-1-Produktionsumfang gemäß D-056 getrennt | Technical Art Director / Lead Environment Artist |
| 1.2.1 | 2026-07-24 | Commander-Art und -Voice im Detailregister eindeutig als Post-MVP-Bedarf markiert | Technical Art Director / Lead Environment Artist |
