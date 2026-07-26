---
role: builder
sprint: v8.6.0/sprint-00
task: Referenzblatt-Generierung Vertical-Slice-Assets (MS-1)
date: 2026-07-25
---

# Imagegen-Report — Orthographische Referenzblätter Vertical Slice MS-1

## Auftrag

Vier orthographische Drei-Ansichten-Referenzblätter für die MS-1-Vertical-Slice-Assets
(`alliance.building.HQ`, `alliance.unit.LightTank`, `legion.building.HQ`,
`legion.unit.LightTank`) über die OpenAI Images API generieren, im Repo ablegen und
gemäß `docs/assets/Provenance.md` §1–§2 nachweisen. Prompts wurden wörtlich aus
[docs/assets/VerticalSlice_MS1.md](../../../docs/assets/VerticalSlice_MS1.md) Abschnitt 4
übernommen, keine eigenen Prompts erfunden.

## Vorgehen

Der Skill `imagegen` stand in dieser Ausführungsumgebung (Builder-Subagent ohne
`Skill`-Tool) nicht zur Verfügung; es wurde direkt gegen
`POST https://api.openai.com/v1/images/generations` (Modell `gpt-image-1`) generiert,
wie im Auftrag als Fallback vorgesehen. Der API-Key wurde ausschließlich zur Laufzeit
aus `/Volumes/2TB_CodingProjekte/Coding_Projekte/B-RollMaster6000Puls/.env` in eine
Shell-Variable geladen (`OPENAI_API_KEY`), nie ausgegeben, nie in eine Datei
geschrieben. Anfrage-Bodies wurden mit `jq -n --arg` gebaut (kein Shell-Interpolieren
des Keys), Antworten per `curl -o <datei>` direkt auf Platte geschrieben (nie in
Konsolenausgabe), Bilddaten per Python aus `b64_json` dekodiert.

## Erzeugte Bilder

Alle vier Prompts wurden **unverändert wörtlich** aus `VerticalSlice_MS1.md` §4.1–4.4
verwendet — **keine Prompt-Anpassungen nötig**, kein Asset wurde übersprungen. Format:
`1536x1024` (nächstliegende von der API unterstützte Auflösung zum geforderten
16:9-Querformat ≥2048×1152; `gpt-image-1` unterstützt aktuell `1024x1024`,
`1536x1024`, `1024x1536` als feste Größen — `1536x1024` wurde gemäß Auftrag "Format
1536x1024 (Querformat)" gewählt), Qualität `high` (höchste verfügbare Stufe).

| Datei | assetId | Auflösung | SHA-256 |
|---|---|---|---|
| `docs/assets/reference/REF_BLDG_Alliance_HQ_ortho.png` | `alliance.building.HQ` | 1536×1024 | `607b8f12673334d67d055edd4ee833027582e8317348f3fa4877c1754198087f` |
| `docs/assets/reference/REF_UNIT_Alliance_LightTank_ortho.png` | `alliance.unit.LightTank` | 1536×1024 | `949d668671854699caf24a216e2d9bacfcf32845d64d482a80079addd2238659` |
| `docs/assets/reference/REF_BLDG_Legion_HQ_ortho.png` | `legion.building.HQ` | 1536×1024 | `f2adf3cfaeafe138073db56c8bd410fe5eaedfd3fe26a423da2366328303f0c0` |
| `docs/assets/reference/REF_UNIT_Legion_LightTank_ortho.png` | `legion.unit.LightTank` | 1536×1024 | `bc21b541a24229118d344e36ac4a85e0c7b0237cfac5af9da148f01f87d2b4d3` |

Verwendetes Modell: `gpt-image-1` (OpenAI Images API, Endpoint
`/v1/images/generations`). Die API-Response enthält keine gesonderte
Modell-Versionskennung über `gpt-image-1` hinaus (kein Snapshot-Tag im Payload).

## Geschätzte API-Kosten

Aus den `usage`-Feldern der vier API-Responses (Text-Input-Tokens je Prompt ~243–263,
Output-Image-Tokens je Bild konstant 6.208 bei `size=1536x1024`, `quality=high`):

- Summe Input-Text-Tokens: 1.019 → bei $5 / 1M Input-Tokens ≈ **$0,005**
- Summe Output-Image-Tokens: 4 × 6.208 = 24.832 → bei $40 / 1M Output-Image-Tokens ≈ **$0,993**
- **Geschätzte Gesamtkosten: ≈ $1,00** für alle vier Bilder

(Schätzung auf Basis der zum Zeitpunkt der Generierung öffentlich bekannten
`gpt-image-1`-Preisstruktur für Text-Input- und Bild-Output-Tokens; keine
Rechnungsstellung eingesehen, reine Token-Hochrechnung.)

## Provenienznachweis

`docs/assets/reference/PROVENANCE.json` wurde neu angelegt, ein Eintrag pro Bild nach
dem Schema aus `docs/assets/Provenance.md` §1 (Pflichtfelder je `originType:
"ai-generated"`). Alle vier Prompt-Texte sind vollständig und wörtlich enthalten;
`sourceFileHash` je Datei per `shasum -a 256` ermittelt (siehe Tabelle oben, identisch
zu den Werten in `PROVENANCE.json`); `outputOwnership` ist ein wörtliches Zitat der
OpenAI-Klausel zur Rechteübertragung am Output aus den OpenAI Terms of Use
(`https://openai.com/policies/terms-of-use`, Abschnitt „Content Ownership").

**Hinweis:** Das Feld `verifiedBy` ist in jedem Eintrag als „builder (automatisierte
Generierung; Vier-Augen-Prüfung nach Provenance.md §3 Schritt 7 steht noch aus)"
dokumentiert — die in `Provenance.md` §3 Schritt 7 geforderte Vier-Augen-Prüfung durch
eine zweite Person hat im Rahmen dieses Auftrags nicht stattgefunden und wird hiermit
explizit als offen markiert, statt fälschlich als erledigt auszuweisen.

`docs/assets/provenance-ledger.json` (der aggregierte Sammelindex) wurde **nicht**
angelegt/ergänzt, da dies außerhalb des zugewiesenen Schreib-Scopes dieses Auftrags
liegt (nur `docs/assets/reference/**` und dieser Report).

## Abschlussprüfungen

- `python3 .github/scripts/check_docs.py` → `OK: 126 Markdown-Dateien, 5
  Quality-JSONs und Evidence-Negativkontrollen geprüft.` (Exit 0)
- Key-Leak-Prüfung (Suche nach dem OpenAI-Key-Präfix im gesamten Repo,
  `.git`/`Library` ausgeschlossen) → keine Treffer (leere Ausgabe, Grep-Exit 1 =
  nicht gefunden)

## Nicht Teil dieses Auftrags

Keine Gate- oder Meilenstein-Behauptung. Dieser Report dokumentiert ausschließlich die
Erzeugung von vier Referenzbildern und deren Provenienznachweis, keine Aussage über
Fertigstellung, Freigabe oder Erreichen eines Produktions-Gates.

## Dateien

- `docs/assets/reference/REF_BLDG_Alliance_HQ_ortho.png` (neu)
- `docs/assets/reference/REF_UNIT_Alliance_LightTank_ortho.png` (neu)
- `docs/assets/reference/REF_BLDG_Legion_HQ_ortho.png` (neu)
- `docs/assets/reference/REF_UNIT_Legion_LightTank_ortho.png` (neu)
- `docs/assets/reference/PROVENANCE.json` (neu)
- `reports/v8.6.0/sprint-00/06-imagegen-report.md` (dieser Report, neu)
