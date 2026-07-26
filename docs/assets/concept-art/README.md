# Hashkrieg — Concept-Art-Entwürfe

**Version:** 0.1.0 | **Status:** Entwurf – Concept-Art-Entwürfe, kein Gate-Nachweis | **Verantwortungsbereich:** Art Direction | **Sprint:** 7

## Zweck

Dieser Ordner enthält den ersten Satz Concept-Art-**Entwürfe zur Formfindung** für
*Hashkrieg*: 34 Bilder, eines je Fraktion (Allianz/Legion) und Rolle, generiert gegen
den [Concept-Art-Style-Guide](../ConceptArtStyleGuide.md). Das sind **keine
Produktionsassets, keine Spiel-Assets und kein Gate-Nachweis** — es existiert
weiterhin kein einziges 3D-Asset im Projekt. Die Bilder dienen der Formfindung und
Prüfung des Style-Guides, nicht dem Import in Unity.

## Abhängigkeiten

- [`../ConceptArtStyleGuide.md`](../ConceptArtStyleGuide.md) — verbindlicher
  Bildstandard, gegen den jedes Bild in diesem Ordner geprüft wird.
- [`../../vision/Lore.md`](../../vision/Lore.md) — Weltentwurf, aus dem die
  Formensprache je Fraktion abgeleitet ist.

## 1. Ordneraufbau

| Pfad | Zweck |
|---|---|
| `full/` | 34 Concept-Art-Entwürfe als PNG, 1024 × 1024 Pixel, volle Auflösung |
| `web/` | dieselben 34 Entwürfe als JPG, verkleinert für schnelle Sichtung |
| `KONTAKTBOGEN.jpg` | ein Übersichtsblatt mit allen 34 Bildern als Kontaktabzug |
| `PROVENANCE.json` | Herkunftsnachweis je Bild: Modell, Endpunkt, Referenzbild, SHA-256, Lizenzlage |
| `prompts.json` | die tatsächlich verwendeten Prompts je Asset, maschinenlesbar |
| `prompts-scrimage.txt` | dieselben Prompts als reiner Text, für schnelles Nachlesen |
| `style/` | zwei Stilplatten (Allianz, Legion) als Referenzbild für die Generierung |
| `tools/` | zwei Python-Skripte zur Reproduktion: `build_styleplate.py`, `generate_v2.py` |

## 2. Namensschema

Jede Bilddatei folgt dem Schema `<fraktion>_<domäne>_<rolle>.png` (bzw. `.jpg` unter
`web/`), zum Beispiel `alliance_building_HQ.png` oder `legion_unit_BattleTank.png`.

- **Fraktion:** `alliance` oder `legion`
- **Domäne:** `building` oder `unit`
- **Rolle:** konkrete Rollenbezeichnung, z. B. `HQ`, `Power`, `Refinery`,
  `BattleTank`, `Harvester`, `Builder`

## 3. Herkunft

Alle 34 Bilder sind KI-generiert mit OpenAI `gpt-image-1` über den Endpunkt
`v1/images/edits`. Als Referenzbild diente je Fraktion eine **Stilplatte** (eine
Materialtafel aus Farbwelt, Oberflächen und Leuchtlinien-Anmutung, ohne erkennbare
Form) — bewusst kein Objektbild, weil ein Objektbild die Silhouetten aller
generierten Assets zuvor unerwünscht vereinheitlicht hatte. Der vollständige
Nachweis je Bild — Modell, Endpunkt, Referenzbild, `sourceFileHash` (SHA-256), Prompt
und Lizenzlage — steht in [`PROVENANCE.json`](PROVENANCE.json).

## 4. Reproduktion

- `tools/build_styleplate.py` baut die beiden Stilplatten unter `style/` aus
  Ausschnitten vorhandener Bilder neu.
- `tools/generate_v2.py <asset-id>` erzeugt ein einzelnes Bild anhand seiner
  `assetId` aus `PROVENANCE.json` neu.

Der API-Schlüssel wird in beiden Skripten über eine curl-Konfigurationsdatei mit
Rechten `600` eingelesen und **nie** als Kommandozeilenargument übergeben, damit er
nicht in der Prozessliste erscheint.

## 5. Bekannte Schwächen

- **Sichtbare Farbdrift** zwischen zwei Erzeugungsläufen: Die zuerst erzeugten
  Legion-Bilder leuchten kräftiger orange als die später nachgezogenen.
- Bei **Legion-Radar** und **Legion-Bauarbeiter** ist noch leichte Räumlichkeit statt
  strenger Frontalität sichtbar — die in Abschnitt 1 des Style-Guides geforderte
  orthografisch anmutende Frontalansicht ist an diesen beiden Bildern nicht
  vollständig erreicht.

## Offene Punkte

- Die Farbdrift zwischen den Legion-Erzeugungsläufen ist noch nicht behoben und
  müsste für eine konsistente Fraktionsfarbwelt nachgezogen werden.
- Die leichte Räumlichkeit bei Legion-Radar und Legion-Bauarbeiter ist noch nicht
  korrigiert.

## Nächste Schritte

1. Farbdrift zwischen den Legion-Bildern angleichen.
2. Legion-Radar und Legion-Bauarbeiter auf strenge Frontalität nachgenerieren.
3. Freigabe des Gesamtsatzes durch die Art Direction, bevor daraus produktionsnahe
   Referenzen abgeleitet werden.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-26 | Erstfassung: 34 Concept-Art-Entwürfe, Ordneraufbau, Namensschema, Herkunft, Reproduktion und bekannte Schwächen dokumentiert | Technical Writer |
