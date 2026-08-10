# Demo-Runbook – erste spielbare Runde (Glutrinne-Graybox)

**Version:** 0.6.0 | **Status:** Entwurf – Graybox-Spur, kein Gate-Nachweis | **Verantwortungsbereich:** Producer / Technical Writer | **Sprint:** 16

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
  Raffinerie-Prereq, HQ-Sieg, KI-Slot), D-083 (Hauptmenü als Overlay, UI Toolkit,
  `AutoStart = false`, Einstellungen als JSON)
- [hashkrieg/08_Sprint_Hauptmenue.md](hashkrieg/08_Sprint_Hauptmenue.md) – Umfang
  und bewusste Auslassungen des Menü-Sprints
- [../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md) – Ordner-/Namenskonvention der Art-Ablage
- [../assets/Provenance.md](../assets/Provenance.md) – Provenienzpflicht vor Repo-Aufnahme eines Assets
- [../assets/VerticalSlice_MS1.md](../assets/VerticalSlice_MS1.md) – Priorisierung der vier Erst-Assets

## 1. Voraussetzungen

- Unity `6000.5.4f1` (D-060-Pin), URP-Projekt wie im Repository.
- Szene: `Assets/_Project/Scenes/Bootstrap.unity` – **Maschinenausgabe**, bei
  Änderungsbedarf nie handeditieren, sondern
  `Tools/Project Nova/Create Bootstrap Scene` ausführen.
- Nach dem Öffnen des Projekts einmal Play drücken: Es erscheint das
  **Hauptmenü** über dem Key Art, die Menümusik läuft. Das Match startet
  **nicht** mehr von selbst — `MatchBootstrap.AutoStart` steht seit D-083 im
  Szenengenerator auf `false`; „Neues Spiel" ruft `StartGrayboxMatch()` (idempotent)
  und blendet das Menü aus. „Beenden" verlässt das Spiel; im Editor beendet es
  den Play-Modus.
- Ton kommt aus einem `AudioListener` an der Kamera. Ist es still, ist die
  Musiklautstärke in den Einstellungen auf 0 oder Musik ist ausgeschaltet —
  siehe §7.

## 2. Was die Demo zeigt (Spielstand Sprint 16.7, D-077/D-102 + Hauptmenü, D-083)

- **Hauptmenü mit Musik (D-083):** Key Art im Vollbild, Titel „HASHKRIEG",
  vier Einträge — **Neues Spiel / Laden / Einstellungen / Beenden**. „Laden" ist
  sichtbar, aber **ausgegraut** („kommt später"): die Snapshot-Schicht kann den
  vollständigen Matchzustand serialisieren, aber nichts schreibt je auf Platte.
  Die Einstellungen überleben den Neustart (§7). Es ist das erste UI-Toolkit-UI
  des Projekts; das übrige HUD bleibt bis auf Weiteres OnGUI-Wegwerfcode.
- **Karte „Glutrinne" (Blockout):** Wüstengetönte 128×128-Ebene, dunkler
  Kartenrand-Rahmen und Aetherium-Kristallmarker (cyan) auf allen fünf
  kanonischen Feldern: Start `(7,7)` / `(117,117)` und Expansion
  `(24,40)` / `(100,84)` mit je 9.000 AE sowie Zentrum `(62,62)` mit
  15.000 AE (D-102).
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
- **Bedienbares HUD (D-084):** Selektierte Einheiten und Gebäude tragen einen
  grünen Bodenmarker. Am unteren Rand steht die **Bauleiste** (alle Gebäude
  mit Kosten/Bauzeit, ausgegraut mit Grund), ein Klick öffnet die
  **Ghost-Platzierung** (grün/rot). Rechts daneben die **Command Card** des
  selektierten Objekts (Einheiten-Aktionen, Gebäude-Queue mit Fortschritt,
  Verkaufen/Reparieren). Unten links die **Minimap** mit Nebelstand,
  Fraktionspunkten und Kamera-Rahmen — Klick springt dorthin. Der
  **Nebel des Krieges** liegt als Abdunklung auf der Karte (unerforscht
  schwarz, erforscht dunkel, sichtbar klar).
- **HUD (Debug):** Eine einzeilige Statusleiste (Credits, Power, Ergebnis) ist
  immer sichtbar; das volle Diagnose-Panel (Tick, Census, Waffenprofile,
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
| Z, X **oder mittlere Maustaste + Drag** | Kamera rotieren |
| Space | Kamera-Rotation zurücksetzen |

Alle Bau- und Produktionsaktionen sind zusätzlich **ohne Tastatur** erreichbar:
Bauleiste unten (Klick öffnet die Ghost-Platzierung: LMB setzt, RMB/ESC bricht
ab), Command Card rechts unten für das selektierte Objekt (Einheiten-Aktionen,
Queue mit Fortschrittsbalken, Verkaufen/Reparieren). Rechtsklick auf den Boden
mit selektiertem Produktionsgebäude setzt dessen Sammelpunkt (Flagge + Linie).

Alle Platzierungs-/Produktionsbefehle zeigen ihr Ergebnis (`accepted` /
Ablehnungsgrund) in der Zeile „Last command" des F3-Panels. Das HQ ist
bewusst **nicht** belegt — MS-1 baut es nur zum Matchstart.

## 4. Ablaufvorschlag (ca. 15 Minuten)

Die Zeitmarken zählen **ab Matchstart**, also ab „Neues Spiel" — Schritt 1
steht davor und dauert so lange, wie man ihn zeigen will.

1. **Menü (vor 0:00):** Play → Key Art, Titel, Musik, vier Einträge. Kurz
   zeigen, dass „Laden" bewusst ausgegraut ist, und in den Einstellungen die
   Musik leiser drehen (wirkt sofort). Dann „Neues Spiel".
2. **Start (0:00):** Kamera steht über der eigenen Basis (unten links,
   Allianz): HQ, ein Builder, das cyane Kristallfeld. Statusleiste oben links
   zeigt 3.000 AE. Im Match selbst ist es still — es gibt keine Geräusche,
   weil es keine SFX gibt (§5).
3. **Raffinerie (0:15):** Mit Y eine Raffinerie in Feld-Nähe setzen (der
   Builder muss in Reichweite stehen, sonst pausiert die Baustelle). Credits:
   3.000 → 2.300 AE.
4. **Wirtschaft (1:30):** Sobald die Raffinerie fertig ist, mit Q zwei
   Harvester bestellen; den Kreislauf am Feld beobachten (ernten → abliefern →
   Credits steigen).
5. **Ausbau (3:00):** Kaserne (Shift+B); danach ist ein Kraftwerk (B) fällig
   (LOW POWER halbiert die Produktionsgeschwindigkeit). Infanterie (Shift+Q)
   zur Verteidigung — **die Legion baut in dieser Zeit ihre eigene Basis auf.**
6. **Gegenwehr (6:00):** Die erste gegnerische Angriffswelle trifft ein.
   Infanterie per Box auswählen, mit A auf einen Angreifer klicken; Schaden
   und Gesundheits-Tint beobachten.
7. **Gegenstoß (9:00):** Eigene Truppe Richtung (120,120) schicken; die
   Legion-Basis erscheint, sobald eigene Einheiten sie aufklären. **Ziel: das
   gegnerische Hauptquartier zerstören** — das beendet das Spiel sofort.
8. **Abschluss:** Ergebnis in der Statusleiste (VICTORY / DEFEAT); per F3 die
   Details (Tick, Census, Sieg-Code) zeigen. Zurück ins Menü führt kein Weg —
   die Runde endet mit dem Beenden des Spiels (§5).

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
  (nur die Statusleiste / F3) **und keinen Rückweg ins Hauptmenü**.
- **„Laden" im Menü ist ohne Funktion** und deshalb ausgegraut. Die
  Snapshot-Schicht serialisiert den vollständigen Matchzustand und setzt ihn
  hash-identisch fort — aber nichts schreibt je auf Platte: kein
  Runtime-Datei-I/O, kein Save-Format, keine Slots.
- **Der SFX-Regler wirkt auf nichts.** Er wird gespeichert und angewandt,
  sobald es SFX gibt; heute gibt es keine. Im Menü ist er als solcher
  gekennzeichnet — bitte in der Vorführung nicht als Feature zeigen.
- **Render-Detail ändert weniger, als es verspricht.** Die sechs Quality-Level
  unterscheiden sich real in 19 Feldern (u. a. `lodBias`, Anisotropie,
  Partikelbudget), teilen sich aber **ein** URP-Asset — `renderScale`, Schatten
  und MSAA bleiben über alle Stufen gleich. Wer den Unterschied sehen will,
  braucht zusätzliche `NovaUrp`-Kopien; das ist bewusst nicht gebaut.
- **Kein Pause-Menü, kein Restart, keine Fraktions- oder Kartenwahl, keine
  Tastenbelegung.** Das Menü ist der Einstieg, nicht die Spielverwaltung; die
  Fraktionswahl hängt an `InitialStateHash` und wäre eine Determinismus-,
  keine Menü-Änderung (D-083).
- Aetherium-Felder sind endlich, aber statisch (kein Nachwachsen, keine
  Warnung). Das Fünf-Feld-Layout ist seit D-102 registriert und sichtbar;
  die zwei ausgearbeiteten Angriffswege bleiben G4-Scope.
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

## 7. Einstellungen – was gespeichert wird und wie man es zurücksetzt

Alle Menü-Einstellungen liegen in **einer** Datei:

```
<Application.persistentDataPath>/settings.json
```

`Application.persistentDataPath` ist plattformabhängig — unter macOS
`~/Library/Application Support/<Company>/<Product>`, unter Windows
`%USERPROFILE%\AppData\LocalLow\<Company>\<Product>`. Den exakten Pfad meldet
der Editor in der Konsole, wenn das Schreiben fehlschlägt.

- **Inhalt:** Musik an/aus + Lautstärke, SFX an/aus + Lautstärke,
  Render-Detail (Quality-Level), vSync, Auflösung, Vollbild. Klartext-JSON,
  lesbar und von Hand editierbar.
- **Wann angewandt:** beim Start über `[RuntimeInitializeOnLoadMethod(BeforeSceneLoad)]`,
  also szenenunabhängig und ohne Boot-Objekt; danach nach jeder Änderung im
  Menü.
- **Zurücksetzen vor einer Vorführung:** Datei löschen. Beim nächsten Start
  gelten die Vorgabewerte (Musik an, Lautstärke 0,4 — die Spur ist mit
  −11,8 LUFS laut gemastert, deshalb nicht 1,0).
- **Kaputte Datei blockiert nichts:** unlesbares oder fehlerhaftes JSON wird
  verworfen, das Spiel startet mit den Vorgabewerten und schreibt eine Warnung
  in die Konsole. Auch ein fehlgeschlagener Schreibvorgang bricht nichts ab —
  die Einstellungen gelten dann nur für diese Sitzung.
- **Kein `PlayerPrefs`, kein `AudioMixer`** (D-083): die Musiklautstärke geht
  direkt auf `AudioSource.volume`.

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
| 0.4.0 | 2026-08-06 | Hauptmenü (D-083): §1 korrigiert – Play zeigt das Menü, das Match startet über „Neues Spiel" (`AutoStart = false`), nicht mehr von selbst; §2 um Menü, Key Art und Menümusik ergänzt; §4 um einen Menüschritt vorangestellt und durchnummeriert (Zeitmarken zählen ab Matchstart); §5 um „Laden" ausgegraut, wirkungslosen SFX-Regler, gemeinsames URP-Asset über alle sechs Render-Detail-Stufen und fehlenden Rückweg ins Menü erweitert; neues §7 zu `settings.json` in `Application.persistentDataPath` (Inhalt, Zurücksetzen, Verhalten bei kaputter Datei) | Agent |
| 0.5.0 | 2026-08-06 | Bedienbares HUD (D-084): §2 um Bauleiste/Ghost-Platzierung, Command Card, Minimap, sichtbaren Nebel und Selektionsmarker ergänzt; §3 um MMB-Drag-Rotation, Space-Reset und die tastaturfreien Wege über Bauleiste/Command Card erweitert | Agent |
| 0.6.0 | 2026-08-10 | D-102/Sprint 16.7 nachgezogen: fünf endliche Aetheriumfelder samt Markerpositionen und Reserven ersetzen die alte Zwei-Feld-Beschreibung; Angriffswege bleiben G4-Scope | Codex / Dennis Westermann |
