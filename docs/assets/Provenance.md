# Provenienznachweis für Art-Assets

**Version:** 0.1.0 | **Status:** Entwurf – MS-1 Art-Strang, kein Gate-Nachweis | **Verantwortungsbereich:** Producer / Project Owner | **Sprint:** 7

## Zweck

Definiert das verbindliche Verfahren und Datenschema, mit dem die Herkunft und Lizenzlage **jedes einzelnen Art-Assets** (3D-Mesh, Textur, Audio, Font) von *Project Nova* nachweisbar dokumentiert wird, bevor es ins Repository aufgenommen wird. Während [Licenses.md](Licenses.md) den Lizenzrahmen je **Quelle** festhält, beschreibt dieses Dokument den Nachweis je **einzelnem Asset**: welche Pflichtfelder ein Provenienz-Datensatz enthält, wo er abgelegt wird und welchen Freigabe-Workflow er durchläuft. Es operationalisiert D-054 (0-€-Strategie, CC0-Basis plus KI-Generierung, siehe [ProcurementStrategy.md](ProcurementStrategy.md)) auf Ebene des einzelnen Imports.

## Abhängigkeiten

- [Licenses.md](Licenses.md) – Lizenzrahmen je Quelle (§1), verbindliche Lizenz-Regeln (§2), Erwerbs-Ledger (§3)
- [ProcurementStrategy.md](ProcurementStrategy.md) – D-054, 0-€-Strategie, Repo-Hygiene (§4)
- [AssetRegister.md](AssetRegister.md) – welches Asset aus welcher Kategorie/Quelle stammt
- [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md) – Doku-Pflichtaufbau, Grundprinzipien
- `CREDITS.md` (Repo-Root) – öffentliche Attributionsliste; wird gemäß [Licenses.md](Licenses.md) §4 **erst mit dem ersten attributionspflichtigen Import** angelegt. Vorlage siehe §6.

## 1. Provenienz-Datensatz je Asset

Für **jedes Art-Asset** wird vor Aufnahme ins Repository ein vollständiger Provenienz-Datensatz erstellt. Er dient als lückenlose, prüfbare Kette vom Ursprung des Materials bis zum abgelegten Datei-Artefakt. Die folgenden Felder sind für alle `originType`-Werte verbindlich:

| Feld | Beschreibung |
|---|---|
| `assetId` | Eindeutiger Bezeichner, Format `<faction>.<domain>.<role>`, z. B. `alliance.building.HQ` |
| `originType` | `cc0-import` \| `ai-generated` \| `original-work` \| `derived` |
| `sourceName` | Name der Quelle bzw. des Urhebers |
| `sourceUrl` | Direkte URL zur Ursprungsressource |
| `licenseId` | SPDX-Kennung wo verfügbar, z. B. `CC0-1.0`, `CC-BY-4.0` |
| `licenseUrl` | URL zum verbindlichen Lizenztext |
| `retrievedAt` | ISO-Datum des Abrufs (`YYYY-MM-DD`) |
| `sourceFileHash` | SHA-256 der Originaldatei zum Abrufzeitpunkt |
| `attributionRequired` | Bool: Attributionspflicht laut Lizenz |
| `attributionText` | Vorgeschriebener oder gewählter Attributionstext, falls zutreffend |
| `redistributionAllowed` | Bool: Weitergabe im öffentlichen Repo laut Lizenz erlaubt |
| `modifications` | Freitext: welche Änderungen am Ausgangsmaterial vorgenommen wurden |
| `derivedFrom` | `assetId` oder Quelldatei-Referenz bei abgeleiteten Assets (nur bei `derived`) |
| `verifiedBy` | Person, die die Vier-Augen-Verifikation durchgeführt hat |
| `verifiedAt` | ISO-Datum der Verifikation |

**Zusätzlich verbindlich bei `originType: ai-generated`:**

| Feld | Beschreibung |
|---|---|
| `aiProvider` | Anbieter des KI-Dienstes, z. B. „Tencent Hunyuan3D" |
| `aiModel` | Modellname |
| `aiModelVersion` | Modell-/API-Version |
| `promptText` | Vollständiger, verwendeter Prompt |
| `inputImages` | Pfade/Hashes eingesetzter Referenzbilder (bei Image-to-3D) |
| `generatedAt` | ISO-Datum der Generierung |
| `providerTermsUrl` | URL der zum Generierungszeitpunkt gültigen Nutzungsbedingungen |
| `providerTermsRetrievedAt` | ISO-Datum, an dem die Nutzungsbedingungen abgerufen/archiviert wurden |
| `commercialUseGranted` | Bool: kommerzielle Nutzung laut Bedingungen zum Generierungszeitpunkt gestattet |
| `outputOwnership` | Wörtliches Zitat der Klausel zum Eigentum an generierten Outputs |

**Begründung der Feldwahl (Kurzfassung):**

- `sourceFileHash` ist unverzichtbar, weil er die Originaldatei zum Abrufzeitpunkt fixiert. Ohne Hash lässt sich später nicht beweisen, dass die im Repo liegende Datei tatsächlich mit der zum Zeitpunkt der Freigabe geprüften Quelle identisch ist — weder gegenüber Dritten noch bei internem Audit.
- `providerTermsRetrievedAt` ist unverzichtbar, weil KI-Anbieter ihre AGB/Nutzungsbedingungen ändern können, ohne dass sich das rückwirkend an einer laufenden URL nachweisen lässt. Nur ein dokumentierter Abrufzeitpunkt (idealerweise mit archiviertem Volltext) belegt, welche Bedingungen zum Zeitpunkt der Generierung tatsächlich galten.
- `promptText` und `inputImages` sind nötig, um im Streitfall nachvollziehen zu können, was tatsächlich erzeugt wurde und ob das Ergebnis als eigenständige Schöpfung oder als Bearbeitung eines Inputs zu werten ist.
- `outputOwnership` ist nötig, weil „kommerzielle Nutzung erlaubt" allein nicht klärt, wem die Rechte am Output zustehen; das wörtliche Zitat vermeidet spätere Fehlinterpretation einer paraphrasierten Zusammenfassung.
- `derivedFrom` ist nötig, um Bearbeitungsketten (CC0-Basis + KI-Retopo + Community-Kitbashing) lückenlos bis zur Ursprungsquelle zurückverfolgen zu können.
- `verifiedBy`/`verifiedAt` sind nötig, weil die Freigabe laut §3 eine Vier-Augen-Prüfung voraussetzt; ohne dieses Feld wäre nicht nachvollziehbar, ob diese Prüfung stattgefunden hat.

In Zweifelsfällen zur Lizenzauslegung selbst (z. B. Reichweite einer Klausel) trifft dieses Dokument keine Rechtsaussage; solche Fälle werden an eine menschliche Entscheidung eskaliert (siehe §3, Ausschlusskriterien).

## 2. Ablageformat und Ort

Der Provenienz-Datensatz wird in zwei Formaten geführt:

1. **Sidecar-Datei** `<AssetOrdner>/PROVENANCE.json` — liegt direkt neben dem Mesh/der Textur/der Audiodatei, für die sie gilt. Enthält genau einen Datensatz mit den in §1 definierten Feldern.
2. **Aggregierter Ledger** `docs/assets/provenance-ledger.json` — ein Sammelindex, der alle Sidecar-Datensätze referenziert bzw. dupliziert, um eine repo-weite Übersicht ohne Verzeichnis-Traversierung zu ermöglichen.

Beide Formate werden in diesem Abschnitt beispielhaft dargestellt. **Die folgenden JSON-Blöcke sind Beispiele, kein reales Asset** — sie verwenden bewusst fiktive Bezeichner (`example.building.Sample`) und dürfen nicht als tatsächlicher Repo-Inhalt missverstanden werden. Die Dateien selbst werden durch dieses Dokument nicht angelegt.

### Beispiel, kein reales Asset — CC0-Fall (`PROVENANCE.json`)

```json
{
  "assetId": "example.building.Sample",
  "originType": "cc0-import",
  "sourceName": "Quaternius Modular Sci-Fi Megakit",
  "sourceUrl": "https://quaternius.com/packs/example-placeholder",
  "licenseId": "CC0-1.0",
  "licenseUrl": "https://creativecommons.org/publicdomain/zero/1.0/",
  "retrievedAt": "2026-07-20",
  "sourceFileHash": "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b85",
  "attributionRequired": false,
  "attributionText": "",
  "redistributionAllowed": true,
  "modifications": "Re-Topologie und UV-Unwrap für URP-Material-Standard, keine geometrische Neuerstellung",
  "derivedFrom": "",
  "verifiedBy": "Beispielname Verifizierer",
  "verifiedAt": "2026-07-21"
}
```

### Beispiel, kein reales Asset — KI-Fall (`PROVENANCE.json`)

```json
{
  "assetId": "example.prop.Sample",
  "originType": "ai-generated",
  "sourceName": "Tencent Hunyuan3D",
  "sourceUrl": "https://hunyuan3d.example-placeholder.invalid/generation/000000",
  "licenseId": "",
  "licenseUrl": "",
  "retrievedAt": "2026-07-22",
  "sourceFileHash": "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08",
  "attributionRequired": false,
  "attributionText": "",
  "redistributionAllowed": true,
  "modifications": "Retopo, LOD-Erstellung, Material-Angleichung an URP-Standard",
  "derivedFrom": "",
  "verifiedBy": "Beispielname Verifizierer",
  "verifiedAt": "2026-07-23",
  "aiProvider": "Tencent",
  "aiModel": "Hunyuan3D",
  "aiModelVersion": "2.0-placeholder",
  "promptText": "sci-fi cargo crate, low-poly, modular, example placeholder prompt",
  "inputImages": [],
  "generatedAt": "2026-07-22",
  "providerTermsUrl": "https://hunyuan3d.example-placeholder.invalid/terms",
  "providerTermsRetrievedAt": "2026-07-22",
  "commercialUseGranted": true,
  "outputOwnership": "Platzhalter-Zitat: „Nutzer erhält alle Rechte am generierten Output.\" (Beispieltext, nicht real geprüft)"
}
```

### Beispiel, kein reales Asset — `provenance-ledger.json` (Ausschnitt)

```json
{
  "entries": [
    {
      "assetId": "example.building.Sample",
      "originType": "cc0-import",
      "licenseId": "CC0-1.0",
      "sidecarPath": "Assets/_ExamplePlaceholder/Buildings/Sample/PROVENANCE.json",
      "verifiedBy": "Beispielname Verifizierer",
      "verifiedAt": "2026-07-21"
    },
    {
      "assetId": "example.prop.Sample",
      "originType": "ai-generated",
      "licenseId": "",
      "sidecarPath": "Assets/_ExamplePlaceholder/Props/Sample/PROVENANCE.json",
      "verifiedBy": "Beispielname Verifizierer",
      "verifiedAt": "2026-07-23"
    }
  ]
}
```

## 3. Freigabe-Workflow

Vor Aufnahme eines Art-Assets ins Repository durchläuft es folgende Schritte:

1. **Quelle prüfen** — Herkunft, Urheber und Lizenzangabe an der Quelle identifizieren und dokumentieren.
2. **Lizenz gegen `Licenses.md` §1 abgleichen** — sicherstellen, dass die Quelle im bestehenden Lizenzrahmen geführt wird oder dass eine neue Zeile in `Licenses.md` §1 durch den zuständigen Verantwortungsbereich ergänzt wird, bevor das Asset weiterverarbeitet wird.
3. **Hash bilden** — `sourceFileHash` (SHA-256) der Originaldatei zum Abrufzeitpunkt erzeugen.
4. **`PROVENANCE.json` schreiben** — vollständigen Datensatz gemäß §1 als Sidecar-Datei neben dem Asset ablegen.
5. **Ledger-Eintrag** — Referenz-Eintrag in `docs/assets/provenance-ledger.json` ergänzen.
6. **Attribution nach `CREDITS.md`** — falls `attributionRequired: true`, Eintrag in der passenden Tabelle von `CREDITS.md` im Repo-Root ergänzen. Existiert die Datei noch nicht, wird sie in diesem Schritt aus der Vorlage in §6 erzeugt — vorher nicht.
7. **Vier-Augen-Verifikation** — eine zweite Person prüft Datensatz, Hash und Lizenzeinstufung gegenzeichnet über `verifiedBy`/`verifiedAt`.

### Ausschlusskriterien

Ein Asset darf **unter keinen Umständen** ins Repository aufgenommen werden, wenn mindestens eines der folgenden Kriterien zutrifft:

- Die Lizenz ist unklar, nicht auffindbar oder nicht eindeutig einer SPDX-Kennung bzw. einem dokumentierten Lizenztext zuordenbar.
- Es handelt sich um eine NC-Lizenz (Non-Commercial) oder eine Lizenz, die kommerzielle Nutzung ausschließt oder einschränkt.
- Die Lizenz gewährt kein Weitergabe-/Redistributionsrecht für ein öffentliches Repository (`redistributionAllowed: false`).
- Bei KI-generiertem Material ist die kommerzielle Nutzung zum Generierungszeitpunkt nicht durch dokumentierte Anbieterbedingungen belegt (`commercialUseGranted` nicht nachweisbar).
- Es existiert keine verifizierbare Quell-URL bzw. kein archivierbarer Beleg für Herkunft und Lizenztext.

Liegt einer dieser Fälle vor oder besteht begründeter Zweifel an der Auslegung einer Lizenzklausel, wird die Entscheidung nicht getroffen, sondern an eine menschliche Entscheidung (Producer / Project Owner) eskaliert. Dieses Dokument trifft keine Rechtsberatung und keine verbindliche juristische Bewertung einzelner Klauseln.

## 4. Prüfbarkeit

Dieser Abschnitt beschreibt Anforderungen an ein **künftiges** automatisiertes Prüfskript — ein solches Skript existiert derzeit nicht und wird durch dieses Dokument nicht implementiert. Ein künftiger Prüfschritt sollte mindestens folgende Punkte automatisiert abdecken können:

- Jedes im Repository geführte Mesh-/Textur-/Audio-Asset besitzt eine zugehörige `PROVENANCE.json`-Sidecar-Datei.
- Jede referenzierte `licenseId` ist gegen eine gepflegte Whitelist zulässiger Lizenzen (abgeleitet aus `Licenses.md` §1) geprüft.
- Der in `sourceFileHash` hinterlegte Hash stimmt mit dem tatsächlichen Hash der abgelegten Datei überein.
- Alle für den jeweiligen `originType` verbindlichen Pflichtfelder aus §1 sind vollständig befüllt.
- Jeder Ledger-Eintrag in `provenance-ledger.json` verweist auf eine existierende Sidecar-Datei und umgekehrt.

## 5. Abgrenzung zu `Licenses.md`

| Frage | Beantwortet durch |
|---|---|
| Welches Lizenzmodell gilt allgemein für eine Quelle (z. B. Quaternius, Sketchfab)? | `Licenses.md` §1 |
| Welche verbindlichen Lizenz-Regeln gelten projektweit (z. B. Repo-Tauglichkeit, Attributionspflicht)? | `Licenses.md` §2 |
| Wann wurde welches Quellen-Paket importiert und mit welchen Rahmenbedingungen? | `Licenses.md` §3 (Ledger) |
| Woher stammt genau dieses eine Asset, mit welchem Hash, Prompt und Verifikationsstatus? | `Provenance.md` (dieses Dokument) §1–2 |
| Wie läuft die Freigabe eines einzelnen Assets ab, und was schließt sie aus? | `Provenance.md` §3 |
| Was könnte ein künftiges Skript automatisiert prüfen? | `Provenance.md` §4 |

## 6. Vorlage für `CREDITS.md`

`CREDITS.md` wird bewusst **noch nicht angelegt**. [Licenses.md](Licenses.md) §4 und die Regel „keine Platzhalter-Dokumente" aus [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md) verlangen, dass die Datei erst mit dem ersten attributionspflichtigen Import entsteht. Damit dieser Schritt dann ohne Formatdiskussion ausführbar ist, hält dieser Abschnitt die verbindliche Struktur vor.

Beim ersten Eintrag wird `CREDITS.md` im Repo-Root nach folgendem Muster erzeugt — die Abschnitte, für die es noch keinen Eintrag gibt, entfallen ersatzlos (keine leeren Tabellen):

````markdown
# Credits und Attributionen

Diese Datei listet alle Assets von *Project Nova*, deren Lizenz eine Namensnennung
vorschreibt. Ein Asset wird eingetragen, sobald sein Provenienznachweis
`attributionRequired: true` ausweist. Assets ohne Attributionspflicht (CC0-Importe,
KI-Material ohne Namensnennungspflicht) stehen hier nicht, sind aber im
Provenienz-Ledger nachgewiesen.

## 3D-Assets

| Asset / Datei | Quelle | Autor | Lizenz | Änderungen |
|---|---|---|---|---|
| <Pfad des Assets im Repo> | <Quell-URL> | <Urheber> | <SPDX-Kennung> | <was geändert wurde> |

## Texturen und Materialien
## Audio
## Schriften
## Werkzeuge und KI-Dienste

(gleiche Tabellenstruktur, nur bei tatsächlichen Einträgen)

## Verfahren

Verbindliches Verfahren: docs/assets/Provenance.md. Lizenzrahmen je Quelle:
docs/assets/Licenses.md. Ein Eintrag entsteht erst nach durchlaufenem
Freigabe-Workflow (§3) und bestätigter Vier-Augen-Verifikation.
````

`CREDITS.md` liegt im Repo-Root und benötigt daher weder die Wiki-Kopfzeile noch die Pflichtabschnitte des Dokumentationsstandards — die CI prüft beides nur für Dateien unterhalb von `docs/`. Interne relative Links müssen dennoch auf existierende Dateien zeigen.

## Offene Punkte

- `docs/assets/provenance-ledger.json` und die ersten `PROVENANCE.json`-Sidecar-Dateien existieren noch nicht und werden erst beim ersten tatsächlichen Asset-Import gemäß §3 angelegt.
- Die Whitelist zulässiger `licenseId`-Werte für ein künftiges Prüfskript (§4) ist noch nicht als eigenständiges maschinenlesbares Artefakt formalisiert.

## Nächste Schritte

1. Beim ersten realen Asset-Import: `PROVENANCE.json` und `provenance-ledger.json` gemäß §2–3 tatsächlich anlegen.
2. Bei Bedarf: künftiges Prüfskript gemäß §4 spezifizieren und umsetzen, sobald eine ausreichende Anzahl realer Provenienz-Datensätze vorliegt.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-25 | Erstfassung: Provenienz-Datensatzschema, Ablageformat (Sidecar + Ledger) mit Beispielen, Freigabe-Workflow mit Ausschlusskriterien, künftige Prüfbarkeitsanforderungen und Abgrenzung zu Licenses.md | Producer |
