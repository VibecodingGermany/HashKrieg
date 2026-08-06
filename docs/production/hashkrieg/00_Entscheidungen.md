# Inhaberentscheidungen zum Hashkrieg-Übergang

**Version:** 0.1.0 | **Status:** Entwurf – Entscheidungsprotokoll, noch ohne D-IDs | **Verantwortungsbereich:** Orchestrator | **Sprint:** 7

## Zweck

Protokoll der Inhaberentscheidungen zur Planungsmappe
[README.md](README.md). Die Entscheidungen werden hier zuerst festgehalten und
**erst am Ende gesammelt** als D-IDs in
[../DecisionLog.md](../DecisionLog.md) übertragen — der DecisionLog ist
Single-Writer, und D-077 aus der parallelen Arbeitsspur ist dort noch nicht
eingetragen.

## Abhängigkeiten

- [README.md](README.md) – die Entscheidungsfragen E-1 bis E-4
- [02_Masterplan.md](02_Masterplan.md) – die Phasen, die daran hängen
- [../DecisionLog.md](../DecisionLog.md) – Zielort der D-IDs

## Stand

| Frage | Thema | Stand |
|---|---|---|
| E-1 | Ablage der Art-Binärdaten | **entschieden** |
| E-2 | Tripo-Sperre / Provenienz der 34 Assets | **entschieden** |
| E-3 | Umfang der Umbenennung | **entschieden** |
| E-4 | Hashkrieg als Fiktion oder als Mechanik | **entschieden** |
| E-5 | Frontalitätsregel für Props | **entschieden** |

---

## E-1 — Ablage der Art-Binärdaten

**Entscheidung (2026-08-06, Inhaber):**
**Externes Zip-Paket in einem geteilten Google-Drive-Ordner.** Kein Git LFS,
keine Binärdaten im Repository.

**Begründung des Inhabers:** Zwei Entwickler, der Ordner ist bereits eingerichtet
und die Ablagestruktur festgelegt. *„Keep it stupid simple."*

**Status:** Bereits umgesetzt. Die Regelung ist in
[../../assets/AssetPackage.md](../../assets/AssetPackage.md) vollständig
beschrieben — Paketname, Größe, Inhalt, SHA-256, Zugangsweg und
Installationsablauf. Die dortige Begründung deckt sich mit der Entscheidung: LFS
kostet auf einem öffentlichen Repository Bandbreitenkontingent pro Clone und
zwingt jedem Mitwirkenden eine `git-lfs`-Installation auf.

### Was daraus folgt

1. **`.gitignore` bleibt unverändert.** Der Ausschluss von `*.fbx`, `*.png`,
   `*.mat`, `*.prefab` samt `.meta` ist gewollt, kein Versehen.

2. **`AssetMappingRegistry.asset` bleibt LEER im Repository.**
   [../../assets/AssetPackage.md](../../assets/AssetPackage.md) §2 legt das
   ausdrücklich fest: Die Registry ist ein Derivat und füllt sich beim Import
   über `ArtAssetAutoSync` selbst.
   → **Die 72 Zeilen Mappings, die derzeit uncommittet im Arbeitsbaum liegen,
   dürfen nicht committet werden.** Sie sind Sitzungsartefakt, nicht Quelltext.
   Fallweise: `Tools/Project Nova/Sync Art Asset Registry` erzeugt sie nach dem
   Paket-Import neu.

3. **Ein Clone ohne Paket bleibt ein spielbares Graybox-Spiel.** Das ist der
   erklärte Entwurfszweck — ohne Prefab fällt die Darstellung sauber auf die
   Primitive zurück, statt unsichtbare Einheiten zu zeigen.

4. **CI kann dauerhaft nicht visuell testen.** Bewusst in Kauf genommen.

### Offene Restarbeit aus dieser Entscheidung

- [../../assets/AssetPackage.md](../../assets/AssetPackage.md) §3 trägt unter
  „Ablage" noch „geteilter Ordner, Zugang auf Anfrage" **ohne Link**, und
  „Nächste Schritte" Punkt 1 („Geteilten Ordner anlegen, Paket hochladen, Link
  in §3 eintragen") ist laut Inhaber bereits erledigt. Beides nachziehen.
- Das Paket wird bewusst nicht öffentlich verlinkt, solange die Lizenzfelder
  ungeklärt sind → hängt an **E-2**.

### Auswirkung auf den Masterplan

[02_Masterplan.md](02_Masterplan.md) **0.3 entfällt** — die Ablage ist
entschieden und eingerichtet. Es bleibt die Doku-Nachpflege oben und die
Nicht-Commit-Regel für die Registry in 0.1.

---

## E-2 — Tripo-Sperre und Provenienz

**Entscheidung (2026-08-06, Inhaber):**
Die 34 Tripo-Modelle sind **Platzhalter**, keine Produktionsassets. Sie bleiben
im Einsatz und werden vom Grafiker **gestaffelt ersetzt**.

**Begründung:** Die Entwicklung läuft ununterbrochen weiter und das Spiel sieht
ab sofort nach etwas aus, ohne dass ein Rechtsrisiko entsteht — es wird nichts
ausgeliefert. Die Lizenzfrage wird erst bei einer Veröffentlichung scharf, und
bis dahin sind die kritischen Stücke ersetzt.

### Die Schlupfloch-Frage, ausdrücklich verworfen

[../../assets/Licenses.md](../../assets/Licenses.md) §2 Regel 6 sperrt den
Tripo3D-Free-Tier wörtlich für **eingecheckte** Assets. Nach [E-1](#e-1--ablage-der-art-binärdaten)
liegen die 34 Modelle nicht im Repository, sondern im Zip-Paket — die Sperre
griffe nach dem Wortlaut also nicht.

**Diese Lesart wird bewusst nicht verwendet.** Der in derselben Regel genannte
Grund ist *„ohne belegbares kommerzielles Nutzungsrecht **und**
Output-Eigentum"*. Der gilt unabhängig vom Ablageort: Wer ein Spiel mit diesen
Meshes ausliefert, braucht die Rechte so oder so. Der Ablageort ist ein
Schlupfloch, kein Argument.

### Ersetzungsreihenfolge

Nach Spielbarkeit sortiert, nicht nach Optik. Die ersten vier sind
**Spielbarkeitsdefekte, keine Kosmetik** — der Spieler muss teure T2-Einheiten
von billigen T1-Einheiten unterscheiden können.

| # | Rolle(n) | Warum zuerst |
|---|---|---|
| 1 | Alliance LightTank + BattleTank | im Spiel nicht unterscheidbar; das Concept trennt sie über ein Doppelrohr, das an keinem Modell existiert |
| 2 | Legion LightTank + BattleTank | dito (Concept trennt über Kühlung, am Modell nicht vorhanden) |
| 3 | Legion ResearchLab + Power | Rollenzuordnung nicht sicher belegt |
| 4 | Alliance VehicleFactory | per Ausschluss zugeordnet, Container ohne modelliertes Tor |
| 5 | Rest | nach Bedarf und Kapazität |

### Was daraus folgt

1. **Die 34 Provenienz-Datensätze tragen `placeholder — nicht ausliefern`**
   statt gefüllter Lizenzfelder. Das ist ehrlicher als leere `_TODO`-Felder und
   macht den Zustand für jeden sofort lesbar.
2. **Keine Tripo-AGB-Recherche nötig** — sie wäre nur bei Option „Produktionsassets"
   erforderlich gewesen.
3. **`ArtManifest_MS1.md` §8 bleibt inhaltlich gültig**, muss aber den
   tatsächlichen Zustand abbilden: Die 34 Einträge sind weder `CC0-Base` noch
   produziert, sondern Tripo-Platzhalter. Der heutige Widerspruch (das Manifest
   behauptet, es existiere kein produziertes Asset) löst sich damit auf.
4. **Vor Veröffentlichung ist ein Vollständigkeitscheck Pflicht:** kein Asset mit
   `placeholder`-Provenienz darf in einem ausgelieferten Build stecken.
5. **[03_Bestellliste_Grafik.md](03_Bestellliste_Grafik.md) P1-10** („Mesh-Nacharbeit
   am Tripo-Erstsatz") wird von *Nacharbeit an fremden Meshes* zu
   *Neulieferung gegen die Concept-Blätter* — und rückt für die Positionen 1–4
   oben faktisch auf P0-Rang.

### Auswirkung auf den Masterplan

[02_Masterplan.md](02_Masterplan.md) **0.4 ändert sich**: Statt 34 Lizenzfelder
nachzutragen, werden die Datensätze als Platzhalter markiert und der
Manifest-Widerspruch aufgelöst. Aufwand fällt von M auf S.

---

## E-3 — Umfang der Umbenennung

**Entscheidung (2026-08-06, Inhaber):**
**Nur die Marke.** Alles, was nach außen sichtbar ist, heißt Hashkrieg. Die
Code-Identität und der Repository-Name bleiben unverändert.

| Wird umbenannt | Bleibt |
|---|---|
| `productName`, `companyName` | `namespace Nova.*` (226 Stellen) |
| Fenstertitel | die 17 `Nova.*.asmdef` |
| Build-Ausgabe (`Hashkrieg.app` / `.exe`) | `github.com/VibecodingGermany/Project_Nova` |
| `README.md`, `AGENTS.md`, `CONTRIBUTING.md`, `GOVERNANCE.md`, `docs/**` | `NOVA_FIXED_POINT` (Vertragskonstante) |
| Menüpfade `Tools/Hashkrieg/…` samt der fünf Doku-Stellen, die sie zitieren | `tools/Nova.SimRunner/` |
| | `INovaLogger` und Verwandte (Typnamen) |

**Begründung:** Interne Codenamen sind bei Spielen normal — nach außen ist alles
Hashkrieg. Der atomare 800-Zeilen-Commit über 17 Assemblies entfällt damit
vollständig, ebenso das Risiko, dass ein einzelner geänderter asmdef-Name die
Unity-Kompilation lahmlegt.

### Was daraus folgt

1. **[05_Umbenennung.md](05_Umbenennung.md) Stufe 1 und 2 werden ausgeführt.**
   Stufe 3 (GitHub-Repo), Stufe 4 (Code-Identität) und Stufe 5 (Prüfverträge
   nachziehen) **entfallen** — Letztere nur, weil die Assembly-Namen
   unverändert bleiben.
2. **Die Doppelpflege des Build-Ausgabepfads bleibt die einzige echte Falle.**
   `ProjectNova.exe` / `.app` steht in `Assets/_Project/Editor/BuildScript.cs`
   **und** als Erwartungswert in `quality/scripts/run_gate_check.py`. Wer nur
   eine Seite ändert, bekommt einen Gate-Fehlschlag, der wie ein Build-Fehler
   aussieht.
3. **`CHANGELOG.md` wird nicht umgeschrieben** — Historie bleibt Historie, es
   kommt nur ein neuer `[Unreleased]`-Eintrag dazu.
4. **Menüpfade und Runbooks zusammen ändern.** Sechs `[MenuItem]` und sieben
   `[CreateAssetMenu]` sind wörtlich in `README.md`,
   [../DemoRunbook.md](../DemoRunbook.md), [../GrayboxLog.md](../GrayboxLog.md)
   und [../../assets/AssetPackage.md](../../assets/AssetPackage.md) zitiert.
5. **Kein Beschluss-Vorbehalt mehr:** Stufe 0 aus
   [05_Umbenennung.md](05_Umbenennung.md) ist mit dieser Entscheidung erledigt.
   Es braucht kein Zielschema für Namespaces, weil keine umbenannt werden.
6. **Der Widerspruch im README verschwindet.** Dort steht heute noch, der
   Umbenennungsbeschluss sei „noch nicht vollzogen".

### Auswirkung auf den Masterplan

[02_Masterplan.md](02_Masterplan.md) **Phase 6 schrumpft von zweistufig (S + L)
auf eine einzige S-Aufgabe** und kann jederzeit in eine Lücke gelegt werden —
sie hängt an keiner anderen Phase und braucht keinen sauberen Arbeitsbaum.

**Später jederzeit nachholbar:** Repo-Rename und Code-Identität sind durch diese
Entscheidung nicht verbaut, nur verschoben. Die Analyse in
[05_Umbenennung.md](05_Umbenennung.md) bleibt dafür gültig.

---

## E-4 — Hashkrieg als Fiktion oder als Mechanik

**Entscheidung (2026-08-06, Inhaber):**
**Hashkrieg ist Name, Welt und Fraktionsidentität — nicht Mechanik.** Aetherium
bleibt die Ressource, der Kernloop bleibt klassisch. Die Mechanik-Inversion
bleibt Post-MVP-Reserve.

Das entspricht **Option B** aus
[../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) §8 — der
Empfehlung des Konzeptpapiers selbst. Auch **Option C** (Cherry-Picks:
öffentlicher Wirtschafts-Ticker, Anteils-Einkommen) wird nicht gezogen.

### Was gilt

| Verbindlich | Reserve für später |
|---|---|
| Titel, Lore, Fraktionsbegründung, Namensdoktrin | öffentlicher Hashrate-Ticker |
| Aetherium → Raffinerie → Credits | Anteils-Einkommen als Anti-Stall-Formel |
| Endliche Felder als Match-Taktgeber | Halving-Ereignis |
| Strom als zweite Größe (Low-Power-Regel) | 51-%-Attacke als Wirtschafts-Superwaffe |
| | Hot Wallet / Cold Storage |
| | Akku-Konvois statt Harvester |

### Begründung und Bestätigung durch die laufende Arbeit

Die parallel laufende Umstellung **D-077** bewegt den Eröffnungsloop
ausdrücklich **in Richtung klassisches C&C** (Raffinerie zuerst bauen, Harvester
kommt aus der Raffinerie). Ein Pivot auf die Mechanik-Inversion würde diese
Arbeit teilweise entwerten. Zusätzlich sind DecisionLog, Content-Manifest und
Asset-Manifeste durchgehend auf Aetherium kalibriert.

### Was daraus folgt

1. **Der Masterplan bleibt unverändert.** Er ging von dieser Lesart bereits aus.
2. **Der Anti-Stall-Druck kommt aus endlichen Feldern**, nicht aus einer
   Anteilsformel → [02_Masterplan.md](02_Masterplan.md) 1.3 bleibt wie
   beschrieben, insbesondere die Absenkung der Feldreserve von 2.000.000 AE.
3. **[../../vision/Lore.md](../../vision/Lore.md) §7 muss ersetzt werden.** Alle
   fünf Zeilen der Tabelle „Wie das Spiel selbst erzählt" setzen
   Hashkrieg-Mechaniken voraus, die nun ausdrücklich nicht gebaut werden. Ersatz
   liegt in [06_Narrative.md](06_Narrative.md) §2: sieben Zeilen, die
   ausschließlich auf vorhandener Mechanik sitzen. Die alte Tabelle bleibt als
   Reserve stehen und wird als solche markiert.
4. **[../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md)
   behält Status „nicht verbindlich"** und wird um einen Hinweis ergänzt, dass
   E-4 Option B gewählt hat.
5. **Der Name „Hashkrieg" bleibt trotzdem inhaltlich gedeckt.** Die Lore trägt
   ihn ohne die Mechanik: Die Kette *Aetherium → Strom → Rechenleistung → Anteil*
   ist Weltbegründung, und §8 macht den Titel selbst zur Parteinahme („Die
   Legion nennt es Hashkrieg, die Allianz die Konsolidierung").

---

## E-5 — Frontalitätsregel gilt nur noch zweckgebunden

**Entscheidung (2026-08-06, Inhaber):**
Die strikte Frontalelevation gilt **nur noch für Assets, die im Spiel als Rolle
gelesen werden müssen.** Reine Kulissen-Props dürfen in Dreiviertelansicht
gezeichnet werden.

**Anlass:** Ein erster Generierungslauf über 16 Prop-Bilder fiel mit 14 von 16
durch. Alle drei Prüf-Linsen nannten unabhängig dieselbe Hauptursache.

### Die Ursache, weil sie sich wiederholen würde

Die 34 bestehenden Blätter zeigen **Fahrzeuge und Gebäude — Objekte mit einer
natürlichen Vorderseite.** Ein Felsen, ein Baugerüst, ein Trümmerhaufen hat
keine. `gpt-image-1` fällt bei solchen Motiven auf den
Dreiviertel-Produktshot zurück, unabhängig davon, wie deutlich die Regel im
Prompt steht. Das ist keine Prompt-Schwäche, sondern eine Eigenschaft der
Motivklasse — schärfere Formulierung hilft nicht.

Hinzu kam: Eine streng frontal gezeichnete Gerüstkiste **ist** ein flaches
Gitter. Die Regel und das Motiv widersprechen sich.

| Gruppe | Regel | Begründung |
|---|---|---|
| **A** — Aetherium (voll/erschöpft), Baustellen 2×2/3×3/4×4, Trümmer 2×2/3×3/4×4, Rally-Flagge | strikte Frontalelevation wie die 34 | stehen ständig im Spielbild, müssen als Rolle lesbar sein |
| **B** — Felsen, Kliffs, toter Strauch, Wrack, Aetherium-Splitter | Dreiviertelansicht erlaubt | reine Kulisse, dient nur als Image-to-3D-Eingabe, trägt keine Rollenlesbarkeit |

**Für beide Gruppen unverändert verbindlich:** freischwebend ohne Boden, Sockel,
Geröll oder Kontaktschatten · flacher Hintergrund `#0B1017` · malerisch statt
fotorealistisch · Emissiv-Anteil 5–12 % (erschöpftes Aetherium: null).

### Technische Folge

**Die Referenzplatte entfällt für neutrale Props.** Bei den 34 Fraktions-Assets
trägt sie die Palette; neutrale Props haben keine Palette zu übernehmen, es
bleibt nur die Verunreinigung — im ersten Lauf schimmerte die Plattenstruktur
im Hintergrund durch und zog den Stil ins Fotorealistische. Statt
`POST /v1/images/edits` mit Referenzbild wird `POST /v1/images/generations` mit
reinem Text verwendet.

### Was daraus folgt

1. **[03_Bestellliste_Grafik.md](03_Bestellliste_Grafik.md) braucht diese
   Unterscheidung**, damit der Grafiker Kulissen-Props nicht unnötig in
   Frontalelevation liefert.
2. **Die Abnahmeprüfung wird gruppenabhängig.** Ein Gruppe-B-Asset wegen
   Dreiviertelansicht abzulehnen ist ein Bewertungsfehler.
3. **Der Style-Guide-Abnahmepunkt „füllt rund 78 % der Bildhöhe" ist für die
   v2-Pipeline überholt.** Der Generator sendet nominal 72 %, und das Modell
   überzieht systematisch — gemessen wurden im Pilotstapel 78 bis 97 %. Gegen
   78 % zu prüfen ließe jedes korrekt erzeugte Bild durchfallen.

---

## Zusammenfassung: alle vier Entscheidungen

| Frage | Entscheidung | Wirkung auf den Plan |
|---|---|---|
| E-1 | Externes Zip-Paket (Google Drive), kein LFS | 0.3 entfällt; Registry bleibt leer im Repo |
| E-2 | Die 34 Tripo-Modelle sind Platzhalter, gestaffelt ersetzen | 0.4 wird kleiner (M→S); Bestellliste P1-10 wird Neulieferung |
| E-3 | Nur die Marke wird umbenannt | Phase 6 schrumpft von S+L auf ein einziges S |
| E-4 | Hashkrieg = Name und Welt, Mechanik bleibt Reserve | Masterplan unverändert; Lore §7 wird ersetzt |
| E-5 | Frontalitätsregel nur für rollenlesbare Assets | Bestellliste und Abnahme werden gruppenabhängig |

**Gemeinsame Linie:** Alle Entscheidungen wählen die einfachere Variante und
verschieben Aufwand, ohne ihn zu verbauen. Der Masterplan wird dadurch an drei
Stellen kürzer und an keiner länger.

## Offene Punkte

- Übertragung nach [../DecisionLog.md](../DecisionLog.md) als D-078 bis D-081,
  sobald D-077 dort eingetragen ist (Single-Writer-Regel).
- Aus E-1: Drive-Link in
  [../../assets/AssetPackage.md](../../assets/AssetPackage.md) §3 nachtragen,
  „Nächste Schritte" Punkt 1 streichen.
- Aus E-2: 34 Provenienz-Datensätze als `placeholder` markieren,
  Manifest-Widerspruch in
  [../../assets/ArtManifest_MS1.md](../../assets/ArtManifest_MS1.md) §8 auflösen.
- Aus E-3: Doppelpflege des Build-Ausgabepfads beachten
  (`BuildScript.cs` **und** `quality/scripts/run_gate_check.py`).
- Aus E-4: [../../vision/Lore.md](../../vision/Lore.md) §7 ersetzen,
  Konzeptpapier um den Hinweis auf Option B ergänzen.

## Nächste Schritte

1. Die fünf Doku-Nachzieharbeiten oben ausführen (alle klein, alle unabhängig).
2. Phase 6 (Marken-Rename) ausführen — hängt an nichts.
3. Phase 0 abschließen, dann Phase 1 als ersten Sprint anlegen.
