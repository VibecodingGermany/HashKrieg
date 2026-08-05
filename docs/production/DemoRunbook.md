# Demo-Runbook – erste spielbare Runde (Glutrinne-Graybox)

**Version:** 0.1.0 | **Status:** Entwurf – Graybox-Spur, kein Gate-Nachweis | **Verantwortungsbereich:** Producer / Technical Writer | **Sprint:** 7

## Zweck

Dieses Runbook führt durch die **erste Demo-Runde** von *Project Nova* auf dem
Graybox-Stand: Projekt öffnen, Match starten, zeigen, was funktioniert, und
ehrlich benennen, was (noch) nicht funktioniert. Es richtet sich an den
Inhaber und an jeden, der die Demo vorführt oder danach Assets ablegt.

Es ist **kein Gate-Nachweis** (D-067 K1): Nichts hier belegt G0–G5. Der
Gate-Status steht ausschließlich in [MVPRecoveryPlan.md](MVPRecoveryPlan.md).

## Abhängigkeiten

- [GrayboxLog.md](GrayboxLog.md) – Sitzungsprotokolle GB-001 bis GB-003
- [ScopeLedger.md](ScopeLedger.md) – registrierte Zurückstellungen hinter dem Manifest
- [MVPContentManifest.md](MVPContentManifest.md) – MS-1-Sollinhalt (Glutrinne, Rollen, Start)
- [../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md) – Ordner-/Namenskonvention der Art-Ablage
- [../assets/Provenance.md](../assets/Provenance.md) – Provenienzpflicht vor Repo-Aufnahme eines Assets
- [../assets/VerticalSlice_MS1.md](../assets/VerticalSlice_MS1.md) – Priorisierung der vier Erst-Assets

## 1. Voraussetzungen

- Unity `6000.5.4f1` (D-060-Pin), URP-Projekt wie im Repository.
- Szene: `Assets/_Project/Scenes/Bootstrap.unity` – **Maschinenausgabe**, bei
  Änderungsbedarf nie handeditieren, sondern
  `Tools/Project Nova/Create Bootstrap Scene` ausführen.
- Nach dem Öffnen des Projekts einmal Play drücken: Das Match startet von
  selbst (`MatchBootstrap.AutoStart`).

## 2. Was die Demo zeigt (Spielstand GB-003)

- **Karte „Glutrinne" (Blockout):** Wüstengetönte 128×128-Ebene, dunkler
  Kartenrand-Rahmen, Aetherium-Kristallmarker (cyan) auf den beiden Feldern,
  die das kanonische Match registriert (lokal (7,7), gegnerisch (119,119)).
- **Startaufstellung je Slot:** HQ + Raffinerie (fertig), 2 Harvester,
  1 Builder, 4 Basis-Infanterie. Slot 0 (Mensch) = Allianz, Slot 1 = Legion.
- **Wirtschaft:** Die beiden eigenen Harvester ernten ab Tick 1 automatisch;
  Aetherium-Stand und Fraktionen sind im Debug-HUD sichtbar.
- **Bau und Produktion:** Kraftwerk (B), Kaserne (Shift+B), Harvester (Q),
  Basis-Infanterie (Shift+Q) – alles über den kanonischen Command-Pfad.
- **Fog of War:** Gerendert wird ausschließlich die committed Teamsicht;
  Gegner ohne Aufklärung haben keinen Proxy.
- **Kampf und Sieg:** Schadensmatrix (D-074) mit Panzerungsklassen,
  Waffenprofil der Auswahl im HUD; Siegauswertung Elimination /
  gegenseitige Vernichtung / Zeitlimit (Tick 27.000) mit HUD-Anzeige.
- **Fraktionsfarben:** Form kodiert Rolle, Farbe kodiert Fraktion (D-072);
  Gesundheit verdunkelt den Farbton.

## 3. Steuerung (Graybox)

| Eingabe | Wirkung |
|---|---|
| LMB Klick / Drag | Auswahl einzeln / Box |
| RMB | Bewegen |
| S | Stopp |
| A | Angriff (Gegner unter dem Cursor), sonst Move als A-Move-Annäherung |
| H | Nächstes freies Aetherium-Feld abernten |
| R | Ladung abliefern |
| B / Shift+B | Kraftwerk / Kaserne platzieren (3×3-Footprint, Ursprung = linke untere Zelle) |
| Q / Shift+Q | Harvester / Basis-Infanterie in Auftrag geben |
| Pfeiltasten / Bildschirmrand | Kamera schwenken |
| Mausrad | Zoom (12–90 m Höhe) |
| Z, X | Kamera rotieren |

## 4. Ablaufvorschlag (ca. 15 Minuten)

1. **Start (0:00):** Play → Kamera steht über der eigenen Basis (unten links,
   Allianz). Sandfläche, Kristallfeld, HQ-/Raffinerie-Proxies zeigen.
2. **Wirtschaft (0:30):** Harvester-Kreis am cyanen Feld beobachten;
   Aetherium-Stand im HUD steigt.
3. **Ausbau (2:00):** Kraftwerk (B) und Kaserne (Shift+B) setzen, Harvester
   nachbestellen (Q), Infanterie (Shift+Q) produzieren.
4. **Aufklärung (5:00):** Infanterie per Box auswählen und Richtung Kartenmitte
   schicken; Nebel lichtet sich nur entlang der Sicht.
5. **Gefecht (8:00):** Mit A auf eine gegnerische Einheit klicken; Schaden,
   Konterwerte und Gesundheits-Tint im HUD zeigen.
6. **Gegnerbasis (12:00):** Bis (120,120) vordringen; die Legion-Basis ist
   spiegelverkehrt aufgebaut. **Ehrlich sagen: der Gegner bleibt untätig** –
   der KI-Slot erhält noch keine Befehle (G3).
7. **Abschluss:** Siegcodes im HUD erklären (Elimination / Zeitlimit 45 Min).

## 5. Bekannte Grenzen (ehrlich, Stand GB-003)

- **Der KI-Slot ist untätig** – keine Gegenwehr, keine gegnerische Expansion.
- **Keine Zielerfassung:** Einheiten erwidern kein Feuer; die
  Verteidigungsplattform kann nie schießen; A-Move ist eine Move-Annäherung.
- **Nur 4 von 17 Rollen belegt** (B, Shift+B, Q, Shift+Q): 33 der 36
  Matrixzellen sind ohne weitere Tastenbelegung unerreichbar.
- `Stop` löscht das Angriffsziel nicht; Angriffe auf eigene Einheiten sind zulässig.
- Pause existiert (`MatchRunner.PauseMatch()`), ist aber an keine Taste gebunden;
  kein Save/Load in der Bedienschicht.
- Nach Siegentscheid tickt der Host weiter; es gibt keinen Ergebnisbildschirm
  (nur HUD-Codes).
- Aetherium-Felder sind endlich, aber statisch (kein Nachwachsen, keine
  Warnung); das Manifest-Layout mit 5 Feldern und 2 Angriffswegen ist G4-Scope
  – der Blockout zeigt bewusst nur die zwei real registrierten Felder.
- Look & Feel ist bis zum ersten menschlichen Durchlauf unverifiziert
  (alle bisherigen Läufe headless).

## 6. Assets ablegen – so funktioniert die Drop-Zone

**Status:** Es liegt noch **kein einziges 3D-Asset** im Projekt. Die Ablage
ist vorbereitet; alles, was konventionkonform hineinfällt, wird automatisch
registriert und erscheint im Spiel anstelle des Primitivs.

1. **Zielordner** nach [../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md) §1:
   `Assets/_Project/Art/Units/<Faction>/<Role>/` bzw.
   `Assets/_Project/Art/Buildings/<Faction>/<Role>/`
   (`<Faction>` = `Alliance`/`Legion`; `<Role>` = Manifest-Rolle, z. B.
   `LightTank`, `HQ`). Fraktionsübergreifendes nach `Art/Shared/`,
   `.blend`-Quellen nach `Art/Source/`.
2. **Namen** nach §2: `SM_...fbx` (Mesh, LODs als `_LOD0/1/2`-Objekte in
   derselben FBX), `T_..._BC/_N/_MSK.png`, `M_....mat`,
   **`PF_....prefab` – nur das Prefab koppelt ans Spiel.**
3. **Beim Import passiert automatisch:** Import-Settings nach §4 (Scale 1.0,
   keine FBX-Materialien, BC7, Masken linear) und Registrierung des Prefabs in
   `Assets/_Project/Data/Registries/AssetMappingRegistry.asset` unter seiner
   Definitions-Id. Manuell nachholbar: `Tools/Project Nova/Sync Art Asset Registry`.
4. **Im Spiel:** Der `UnitViewManager` rendert die registrierte Definitions-Id
   als Prefab (Legion-Einheit → Legion-Prefab); alles ohne Prefab bleibt
   Graybox-Primitiv. Ein Mischbetrieb ist also ab dem ersten Asset möglich.
5. **Vor der Repo-Aufnahme:** Provenienzdatensatz nach
   [../assets/Provenance.md](../assets/Provenance.md) (SHA-256, Lizenz, bei KI
   Prompt/Provider) – ohne Nachweis kommt nichts ins Repository.
6. **Priorität:** die vier Vertical-Slice-Assets (Allianz-/Legion-HQ,
   Allianz-/Legion-LightTank), deren orthografische Referenzen bereits unter
   `docs/assets/reference/` liegen.

## Offene Punkte

- Erster menschlicher Play-Durchlauf steht noch aus; Rückmeldungen kommen als
  GB-Eintrag ins [GrayboxLog.md](GrayboxLog.md).
- Tastenbelegung der übrigen Rollen, Pause-Bindung und Feuererwidern sind
  bekannte Lücken (siehe §5) und gehören nicht in diese Spur, ohne die
  Schreibumfangs-Regeln zu berühren.

## Nächste Schritte

1. Demo-Runde nach §4 durchlaufen und Rückmeldung protokollieren.
2. Erste PF_*-Assets gemäß §6 ablegen (Reihenfolge: Vertical-Slice-Priorität).
3. Nachgelagert bleiben G0-A2/G0-B/G1 (Gate-Pfad, unberührt von dieser Spur).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-05 | Erstfassung: Demo-Ablauf, Steuerung, bekannte Grenzen, Asset-Ablage-Anleitung (Stand GB-003) | Technical Writer |
