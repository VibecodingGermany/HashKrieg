# Übergabe: MS-1 Art-Strang (Vorbereitung)

Erstellt: 2026-07-25 · Orchestrator (Claude) · Branch zum Zeitpunkt der Erstellung: `feat/g1-kernel-integration`

Dieser Report begleitet einen **parallel vorbereiteten Art-/Asset-Strang**.

> **Stand 2026-07-25, nachgetragen:** Die Abschnitte 2 bis 4 waren ursprünglich als
> Einfügeblöcke gedacht, weil die „heißen" Dateien mit Einzelschreiber-Regel
> (`CHANGELOG.md`, `docs/README.md`, `docs/production/DecisionLog.md`) zunächst nicht
> angefasst wurden. Auf Anweisung des Projektinhabers wurden sie anschließend durch
> **einen einzelnen, serialisiert laufenden Schreiber** eingetragen. Die Abschnitte 2
> bis 4 sind damit **erledigt** und dienen nur noch der Nachvollziehbarkeit. Die
> tatsächlich vergebenen Entscheidungs-IDs sind **D-066 bis D-070** — nicht die in
> Abschnitt 4 vorgeschlagene D-065, die zwischenzeitlich von der parallelen Session
> belegt wurde. Abschnitt 5 ist ebenfalls abgearbeitet: alle dort gelisteten Punkte
> wurden entschieden und umgesetzt.

---

## 1. Entstandene Dateien

| Datei | Inhalt |
|---|---|
| `docs/assets/ArtAssetStandard.md` | Ordnerbaum, Datei-/LOD-Namensschema, Import-Settings, Material-Standard, Team-Mask-Kanalbelegung, Texel-Density-Vorschlag |
| `docs/assets/ArtManifest_MS1.md` | Spezifikationsblätter für alle 34 MS-1-Art-Assets |
| `docs/assets/art-manifest-ms1.json` | Maschinenlesbares Gegenstück, abgeleitet aus `quality/content/mvp-v1.json` |
| `docs/assets/SourceCatalog_MS1.md` | Recherchierter CC0-/KI-Beschaffungskatalog, Lizenzbefunde, Masken-Workflows, Rechtsrisiken |
| `docs/assets/Provenance.md` | Provenienz-Schema je Asset, Ablageformat, Freigabe-Workflow, Ausschlusskriterien, `CREDITS.md`-Vorlage |
| `docs/assets/VerticalSlice_MS1.md` | Tiefe Spezifikation der vier Slice-Assets inkl. Bild-Briefs und Abnahmekriterien |
| `docs/assets/reference/` | Orthographische Referenzblätter (KI-generiert) samt `PROVENANCE.json` |

Alle Dokumente tragen Version `0.1.0`, Status `Entwurf – MS-1 Art-Strang, kein Gate-Nachweis`.
`python3 .github/scripts/check_docs.py` läuft mit Exit-Code 0 durch.

**Nicht angelegt wurde `CREDITS.md`.** Ein zunächst erzeugter leerer Entwurf wurde wieder
entfernt: `docs/assets/Licenses.md` §4 und die Regel „keine Platzhalter-Dokumente"
(Goldene Regel 7) verlangen die Anlage erst beim ersten attributionspflichtigen Import.
Die verbindliche Struktur liegt stattdessen als Vorlage in `docs/assets/Provenance.md` §6.

**Nicht angelegt wurden Ordner unter `Assets/`.** Der Art-Baum ist spezifiziert, aber
nicht erzeugt — das Unity-Projekt gehört der parallelen Implementierungs-Session.

---

## 2. Block für `CHANGELOG.md` unter `## [Unreleased]`

```markdown
### Hinzugefügt

- Art-Asset-Standard (`docs/assets/ArtAssetStandard.md`): Ordnerbaum unter
  `Assets/_Project/Art/`, Datei- und LOD-Namensschema, Import-Settings für Modelle und
  Texturen, Material-Standard und Team-Farben-Masken-Kanalbelegung für MS-1.
- MS-1-Art-Manifest (`docs/assets/ArtManifest_MS1.md`, `docs/assets/art-manifest-ms1.json`):
  Spezifikationsblätter für alle 34 Art-Assets (9 Gebäude- und 8 Einheitenrollen je
  Fraktion), abgeleitet aus `quality/content/mvp-v1.json` und `docs/tech/AssetBudget.md`.
- Beschaffungskatalog (`docs/assets/SourceCatalog_MS1.md`): recherchierte CC0- und
  KI-Quellen je Rolle, Lizenzbefunde der KI-Anbieter, Team-Masken-Workflows und
  Rechtsrisiken der 0-€-Strategie nach D-054.
- Provenienz-Verfahren (`docs/assets/Provenance.md`): Pflichtfeld-Schema je Asset,
  Sidecar- und Ledger-Format, Freigabe-Workflow, Ausschlusskriterien, `CREDITS.md`-Vorlage.
- Vertical-Slice-Spezifikation (`docs/assets/VerticalSlice_MS1.md`) für
  Allianz-Kommandozentrale, Lynx, Legion-Gefechtsstand und Räuber, inklusive
  orthographischer Referenzblätter unter `docs/assets/reference/`.
```

Kategorie `Entschieden` erst ergänzen, wenn der DecisionLog-Eintrag aus §4 tatsächlich
geschrieben wurde.

---

## 3. Zeilen für den Index in `docs/README.md`

Im Abschnitt `assets/` einzufügen. Die Zeilen sind hier bewusst **ohne Markdown-Link-Syntax**
notiert, weil `check_docs.py` Links zeilenweise und ohne Rücksicht auf Code-Fences prüft und
Beispiel-Links in diesem Report sonst als tote Links gewertet würden. Beim Einfügen jede Zeile
im Stil der bestehenden Index-Einträge als Link formatieren, Linkziel jeweils
`assets/<Dateiname>`, Version `0.1.0`, Status `Entwurf`:

| Linkziel | Beschreibung für den Index |
|---|---|
| `assets/ArtAssetStandard.md` | Ordner-, Namens-, Import- und Material-Standard für Art-Assets |
| `assets/ArtManifest_MS1.md` | Spezifikationsblätter der 34 MS-1-Art-Assets |
| `assets/SourceCatalog_MS1.md` | CC0-/KI-Beschaffungskatalog und Lizenzbefunde |
| `assets/Provenance.md` | Provenienz- und Lizenznachweis-Verfahren je Asset |
| `assets/VerticalSlice_MS1.md` | Vertical-Slice-Spezifikation der vier Erst-Assets |

---

## 4. Entwurf für `docs/production/DecisionLog.md`

Zuletzt vergebene ID war **D-064** — vor dem Eintrag gegen den aktuellen Stand prüfen,
da parallel gearbeitet wird.

```markdown
### D-065 – Kanalbelegung der Art-Mask-Textur

**Datum:** 2026-07-25 · **Status:** vorgeschlagen · **Bereich:** Technical Art / Rendering

**Kontext:** `docs/tech/Rendering.md` fordert eine dedizierte Team-Farben-Maske im
Textur-Set, legt aber nicht fest, welcher Kanal sie trägt. `docs/tech/AssetBudget.md`
begrenzt auf ein Textur-Set (Albedo/Normal/Mask) pro Asset, wodurch vier Kanäle für
Metallic, Occlusion, Smoothness und Team-Maske zur Verfügung stehen.

**Entscheidung:** R = Metallic · G = Occlusion · B = TeamMask · A = Smoothness.

**Begründung:** Metallic in R und Smoothness in A entsprechen der URP-Lit-Konvention.
Dadurch rendert jedes Asset auch ohne den projekteigenen `NovaUnit`-Shader auf reinem
URP Lit korrekt — lediglich ohne Teamfarbe. Das hält den Art-Strang unabhängig vom
Shader-Strang. Die Team-Maske liegt in B, weil sie großflächig und weich ist und die
BC7-Kompression im geteilten RGB-Block dort am wenigsten sichtbare Artefakte erzeugt.

**Alternativen:**
1. R = Metallic, G = Smoothness, B = Occlusion, A = TeamMask — verlustärmste Kodierung
   der Maske im separaten Alpha-Block, bricht aber die URP-Lit-Kompatibilität; Assets
   sähen ohne Custom-Shader falsch aus.
2. Separate einkanalige Team-Maskentextur — maximale Qualität und Flexibilität,
   verletzt aber die Ein-Textur-Set-Regel aus `docs/tech/AssetBudget.md` und erhöht
   Speicher- und Sampler-Last je Asset.
3. Team-Maske über Vertex Colors statt Textur — spart eine Texturebene, ist aber an die
   Mesh-Auflösung gebunden und in den LOD-Stufen nicht stabil reproduzierbar.

**Konsequenz:** Verbindlich für alle Art-Assets ab MS-1; dokumentiert in
`docs/assets/ArtAssetStandard.md`. Der `NovaUnit`-Shader muss die Maske aus dem B-Kanal
lesen — die Shader-Implementierung selbst ist nicht Teil dieser Entscheidung.
```

---

## 5. Offene Entscheidungen für den Projektinhaber

| # | Sachverhalt | Warum eskaliert |
|---|---|---|
| 1 | `docs/assets/Licenses.md` §1 führt Hunyuan3D pauschal als kommerziell nutzbar mit voller Repo-Freigabe. Die Recherche in `docs/assets/SourceCatalog_MS1.md` belegt, dass dies nur für Version 2.1 gilt und Pretrained-Modelle nicht weiterverteilt werden dürfen. | Sachliche Korrektur an einem bereits sprint-freigegebenen Dokument (v1.1.0), das einem anderen Verantwortungsbereich gehört. |
| 2 | Für rechtlich eindeutige Eigentumsrechte am KI-Output ist praktisch ein bezahlter Anbieter-Tier nötig (Größenordnung 20 $/Monat). Das steht im Zielkonflikt mit dem strikten 0-€-Anspruch aus D-054. | Budget- und Strategieentscheidung, kein technisches Detail. |
| 3 | `docs/assets/AssetRegister.md` ordnet MS-1-Assets weiterhin Synty-Kits mit Status BUY/MODIFY zu, obwohl D-054 die 0-€-Pipeline festlegt. | Widerspruch zwischen zwei sprint-freigegebenen Dokumenten; die Zeilen sollten als historisch markiert oder rebaselined werden. |
| 4 | Die Fraktions-Hex-Paletten in `docs/assets/VerticalSlice_MS1.md` sind ein Vorschlag. Die Gamedesign-Doku nennt nur Farbnamen. | Art-Direction-Freigabe. |
| 5 | Gebäude-Footprints in Metern und die Grid-Zellgröße sind nirgends definiert; `docs/gamedesign/Buildings.md` markiert die Footprints selbst als Annahme. | Berührt Simulation/Grid und war deshalb bewusst außerhalb dieses Strangs. |
| 6 | `docs/tech/NamingConvention.md` (v0.4.0) kennt nur ScriptableObject-Namen und sollte einen Verweis auf die Art-Ebene erhalten. `docs/tech/FolderStructure.md` (v1.2.0) enthält keinen Art-Zweig, ist aber als G0-A/G0-B-Nachweisziel markiert. | Änderung an Gate-Zieldokumenten wurde bewusst unterlassen. |

---

## 6. Ausdrücklich nicht Gegenstand dieses Strangs

Keine GameDatabase- oder ScriptableObject-Anbindung, keine Simulationslogik, keine
Shader-Implementierung, keine Prefab- oder Szenenintegration, kein Ordner unter
`Assets/`, keine Gate- oder Meilenstein-Behauptung. Alle Dokumente formulieren
Anforderungen, keine Erfüllungen. Es existiert weiterhin kein einziges produktives
Art-Asset im Repository.
