# Demo-Runbook – erste spielbare Runde (Glutrinne-Graybox)

**Version:** 0.3.0 | **Status:** Entwurf – Graybox-Spur, kein Gate-Nachweis | **Verantwortungsbereich:** Producer / Technical Writer | **Sprint:** 7

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
- [DecisionLog.md](DecisionLog.md) – D-077 (Startaufstellung, Harvester-Produzent,
  Raffinerie-Prereq, HQ-Sieg, KI-Slot)
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

## 2. Was die Demo zeigt (Spielstand GB-005, D-077)

- **Karte „Glutrinne" (Blockout):** Wüstengetönte 128×128-Ebene, dunkler
  Kartenrand-Rahmen, Aetherium-Kristallmarker (cyan) auf den beiden Feldern,
  die das kanonische Match registriert (lokal (7,7), gegnerisch (119,119)).
- **Startaufstellung je Slot:** HQ + 1 Builder + 3.000 AE. Slot 0 (Mensch) =
  Allianz, Slot 1 = Legion. Mehr gibt es nicht — der Kernloop wird gespielt,
  nicht geschenkt.
- **Der Kernloop:** Raffinerie bauen (Y) → Harvester produzieren (Q, kommt aus
  der **Raffinerie**, nicht aus dem HQ) → Harvester erntet das Feld und liefert
  ab → Kaserne (Shift+B) → Infanterie (Shift+Q). Die Raffinerie braucht kein
  Kraftwerk mehr; ab Raffinerie + Kaserne (35 > 30 HQ-Power) wird eins fällig (B).
- **Der Computergegner spielt mit:** Die Legion baut ihre Basis spiegelbildlich
  auf, fährt eigene Harvester-Kreise, produziert Infanterie und greift in
  Wellen an. Sie sieht nur, was ihr Team aufgeklärt hat (FoW-legal).
- **Sieg:** Wer das gegnerische **Hauptquartier zerstört**, gewinnt (daneben
  gilt weiterhin: Totalvernichtung, gegenseitige Vernichtung = Unentschieden,
  Zeitlimit 45 Min). Das Ergebnis steht in der Statusleiste oben links.
- **HUD:** Eine einzeilige Statusleiste (Credits, Power, Ergebnis) ist immer
  sichtbar; das volle Diagnose-Panel (Tick, Census, Waffenprofile,
  Befehlslegende) schaltet **F3** zu.
- **Darstellung:** Die 3D-Modelle werden zur Laufzeit auf ihren
  Sim-Footprint normiert — nichts überlappt mehr, und Modelle bleiben ohne
  Logikänderung austauschbar. Form kodiert Rolle, Farbe kodiert Fraktion
  (D-072); Gesundheit verdunkelt den Farbton.
- **Fog of War:** Gerendert wird ausschließlich die committed Teamsicht;
  Gegner ohne Aufklärung haben keinen Proxy.

## 3. Steuerung (Graybox)

| Eingabe | Wirkung |
|---|---|
| LMB Klick / Drag | Auswahl einzeln / Box |
| RMB | Bewegen |
| S | Stopp |
| A | Angriff auf Gegner unter dem Cursor (sonst schlichtes Move — **keine** Zielerfassung bei Ankunft) |
| H | Nächstes freies Aetherium-Feld abernten |
| R | Ladung abliefern |
| P | Pause / Fortsetzen |
| F3 | Diagnose-Panel ein/aus (Statusleiste bleibt immer sichtbar) |
| B / Shift+B | Kraftwerk / Kaserne bauen |
| C / V / T | Lager / Fahrzeugfabrik / Forschungslabor (Forschung schaltet T2 frei) |
| G / F / Y | Radar / Verteidigungsplattform / Raffinerie bauen |
| Q / Shift+Q | Harvester (an der Raffinerie) / Basis-Infanterie (an der Kaserne) produzieren |
| U / N | Builder (am HQ) / Panzerabwehr-Infanterie (T2) produzieren |
| E / Shift+E | Spähfahrzeug / Leichter Panzer (Fahrzeugfabrik nötig) |
| D / Shift+D | Kampfpanzer / Artillerie (T2 nötig) |
| Pfeiltasten / Bildschirmrand | Kamera schwenken |
| Mausrad | Zoom (12–90 m Höhe) |
| Z, X | Kamera rotieren |

Alle Platzierungs-/Produktionsbefehle zeigen ihr Ergebnis (`accepted` /
Ablehnungsgrund) in der Zeile „Last command" des F3-Panels. Das HQ ist
bewusst **nicht** belegt — MS-1 baut es nur zum Matchstart.

## 4. Ablaufvorschlag (ca. 15 Minuten)

1. **Start (0:00):** Play → Kamera steht über der eigenen Basis (unten links,
   Allianz): HQ, ein Builder, das cyane Kristallfeld. Statusleiste oben links
   zeigt 3.000 AE.
2. **Raffinerie (0:15):** Mit Y eine Raffinerie in Feld-Nähe setzen (der
   Builder muss in Reichweite stehen, sonst pausiert die Baustelle). Credits:
   3.000 → 2.300 AE.
3. **Wirtschaft (1:30):** Sobald die Raffinerie fertig ist, mit Q zwei
   Harvester bestellen; den Kreislauf am Feld beobachten (ernten → abliefern →
   Credits steigen).
4. **Ausbau (3:00):** Kaserne (Shift+B); danach ist ein Kraftwerk (B) fällig
   (LOW POWER halbiert die Produktionsgeschwindigkeit). Infanterie (Shift+Q)
   zur Verteidigung — **die Legion baut in dieser Zeit ihre eigene Basis auf.**
5. **Gegenwehr (6:00):** Die erste gegnerische Angriffswelle trifft ein.
   Infanterie per Box auswählen, mit A auf einen Angreifer klicken; Schaden
   und Gesundheits-Tint beobachten.
6. **Gegenstoß (9:00):** Eigene Truppe Richtung (120,120) schicken; die
   Legion-Basis erscheint, sobald eigene Einheiten sie aufklären. **Ziel: das
   gegnerische Hauptquartier zerstören** — das beendet das Spiel sofort.
7. **Abschluss:** Ergebnis in der Statusleiste (VICTORY / DEFEAT); per F3 die
   Details (Tick, Census, Sieg-Code) zeigen.

## 5. Bekannte Grenzen (ehrlich, Stand GB-005)

- **Die KI ist bewusst einfach:** feste Build-Order, nur Infanterie-Wellen,
  kein Nachschub-Management jenseits der Grundregeln, kein Reagieren auf den
  Spieler (kein Konter, kein Rückzug). Ihre Peer-Session ist nicht
  snapshot-serialisiert.
- **Keine Zielerfassung:** Einheiten erwidern kein Feuer; die
  Verteidigungsplattform kann nie schießen; A ohne Gegner unter dem Cursor
  ist ein schlichtes Move. (Die KI umgeht das mit expliziten Attack-Orders.)
- `Stop` löscht das Angriffsziel nicht; Angriffe auf eigene Einheiten sind zulässig.
- Nach Siegentscheid tickt der Host weiter; es gibt keinen Ergebnisbildschirm
  (nur die Statusleiste / F3).
- Aetherium-Felder sind endlich, aber statisch (kein Nachwachsen, keine
  Warnung); das Manifest-Layout mit 5 Feldern und 2 Angriffswegen ist G4-Scope
  – der Blockout zeigt bewusst nur die zwei real registrierten Felder.
- Erledigt seit GB-005 (hier nur als Historie): das Vollbild-Debug-Overlay ist
  standardmäßig aus (F3); die 3D-Modelle überlagern sich nicht mehr
  (Laufzeit-Normierung auf den Sim-Footprint); der KI-Slot spielt; der
  Harvester kommt aus der Raffinerie; die Raffinerie braucht kein Kraftwerk
  mehr; HQ-Verlust beendet das Spiel.

## 6. Assets ablegen – so funktioniert die Drop-Zone

**Status:** Die ersten 34 Assets sind produziert, liegen aber **als Paket
ausserhalb des Repositories** — siehe [../assets/AssetPackage.md](../assets/AssetPackage.md).
Ein frischer Clone zeigt deshalb Graybox-Primitive; wer das Paket entpackt,
sieht die Modelle. Beides ist ein gültiger Stand, Mischbetrieb inklusive.

Die Ablage ist vorbereitet: alles, was konventionkonform hineinfällt, wird
automatisch registriert und erscheint im Spiel anstelle des Primitivs.

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
| 0.2.0 | 2026-08-05 | Stand GB-004: volle Tastenbelegung aller 17 Rollen, Pause auf P, Wirtschaftskreislauf nach dem Footprint-Fix als funktional vermerkt, Ablauf auf Fahrzeugfabrik/Forschung ausgeweitet | Technical Writer |
| 0.3.0 | 2026-08-06 | Stand GB-005 (D-077): Start HQ + Builder + 3.000 AE, Kernloop-Ablauf neu (Raffinerie → Harvester → Kaserne), KI-Gegner aktiv, Sieg bei HQ-Zerstörung, Statusleiste + F3-Panel, Skalierungsreparatur vermerkt | Agent |
