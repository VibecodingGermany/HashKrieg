# Übergang Project Nova → Hashkrieg — Planungsmappe

**Version:** 0.7.2 | **Status:** fortgeschriebene Planungsmappe – Sprint 13 zur Hälfte gemergt (13.1, 13.7 seit `e15f5e6`), die offenen Pakete 13.2–13.5 warten auf Zugangsdaten und zwei Menschen an zwei Rechnern; **Sprint 16 ist nach dem ersten Betatest vorgezogen** und läuft seit D-095 parallel zum Einheitenstrang; Sprint 18 neu angelegt | **Verantwortungsbereich:** Project Owner / Orchestrator / Producer | **Sprint:** 16

## Zweck

Diese Mappe ist die vollständige Bestandsaufnahme und der ausführbare Plan für
den Übergang von *Project Nova* zu *Hashkrieg*. Sie entstand aus einem
nur-lesenden Audit über beide Arbeitsstände (dieses Repository und das externe
Paket `Hashkrieg_Assets`) und ändert selbst **keine Zeile Code**.

Sie richtet sich an zwei Leser:

- den **Inhaber**, der aus §"Die vier Entscheidungen" heraus entscheidet, was
  gebaut wird und was nicht;
- den **ausführenden Agenten** (Kimi), der die Arbeitspakete aus
  [02_Masterplan.md](02_Masterplan.md) abarbeitet.

Sie ist **kein Gate-Nachweis**. Der Gate- und Meilensteinstatus steht
ausschließlich in [../MVPRecoveryPlan.md](../MVPRecoveryPlan.md) und
[../../../GOVERNANCE.md](../../../GOVERNANCE.md).

## Abhängigkeiten

- [../../../GOVERNANCE.md](../../../GOVERNANCE.md) – Governance-Tier 2 (D-091/D-105): Meilenstein-Nachweis ist grüne CI plus gespielte Runde; einzelne PRs dürfen sichtbar zurückgestellt integriert werden
- [../MVPContentManifest.md](../MVPContentManifest.md) – autorisierter MS-1-Sollinhalt
- [../ScopeLedger.md](../ScopeLedger.md) – registrierte Zurückstellungen
- [../DemoRunbook.md](../DemoRunbook.md) – heutiger spielbarer Umfang
- [../../vision/Lore.md](../../vision/Lore.md) – verbindlicher Weltentwurf
- [../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) – Konzeptvariante, ausdrücklich **nicht** verbindlich

## Die Mappe

| Dokument | Inhalt |
|---|---|
| [00_Entscheidungen.md](00_Entscheidungen.md) | **Die vier Inhaberentscheidungen — alle getroffen (2026-08-06), mit ihrer Wirkung auf den Plan** |
| [01_Bestandsaufnahme.md](01_Bestandsaufnahme.md) | Was existiert wirklich — Simulation, Content, Art, Präsentation, Ökonomie, geprüft gegen den Code |
| [02_Masterplan.md](02_Masterplan.md) | Der priorisierte Plan in sieben Phasen, top-down: erst Spielmechanik, dann Bedienbarkeit, dann Politur |
| [03_Bestellliste_Grafik.md](03_Bestellliste_Grafik.md) | Was der Grafiker liefern soll, in vier Prioritätsstufen, mit Dateinamen, Specs und Zielordner |
| [04_Audioplan.md](04_Audioplan.md) | Sound-Katalog, lizenzsaubere Open-Source-Quellen, Ordner- und Namenskonvention |
| [05_Umbenennung.md](05_Umbenennung.md) | Der Rename Nova → Hashkrieg in sechs Stufen, nach Risiko sortiert |
| [06_Narrative.md](06_Narrative.md) | Fraktionsnamen, Mechanik-als-Erzählung, realistisch baubare Minimal-Kampagne |
| [07_CC0_Quellen.md](07_CC0_Quellen.md) | Welche Kulissen-Assets aus freien CC0-Paketen kommen statt gebaut zu werden — mit Prüfvermerk |
| [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) | Sprint 12; Strang A A1–A7 umgesetzt und A8 Stufe 1 über 10.023 TCP-Ticks nachgewiesen, manuelle Loopback-/LAN-/VPS-Stufen offen (D-089) |
| [12B_Sprint_Sichtbares_Gefecht.md](12B_Sprint_Sichtbares_Gefecht.md) | Sprint 12 Strang B; fog-sicheres VFX- und Tier-0-Audio technisch umgesetzt, manuelle 60-Einheiten-Sicht-/Gegenhörabnahme offen (D-090) |
| [13-0_Sprint_Freigabe.md](13-0_Sprint_Freigabe.md) | Historischer Sprint-13.0-Rollout; Lizenz, Tier-2-Wechsel, damaliges Zwei-Maintainer-/Fork-Modell und CI-Wächter vor dem ersten externen PR; Reviewmodell durch D-105 ersetzt |
| [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Parallelbetrieb 13–18 — Regelwerk für die parallele Arbeit** — Schreibhoheit, Determinismus-Baselines, Merge-Fenster, Zugangsmodell für externe Beitragende; seit **D-095** trennt die **Dateihoheit** die Stränge, nicht mehr der Verhaltensraum |
| [13_Sprint_Netzpartie.md](13_Sprint_Netzpartie.md) | Sprint 13 (Maintainer); Verbindungsdialog, Relay auf dem VPS, A8 Stufen 2–4 als gespielte Abnahme |
| [13B_Sprint_Einheitenverhalten.md](13B_Sprint_Einheitenverhalten.md) | Sprint 13B (externer Beitragender, PR-only); Einheitenverhalten, KI und Legion-Waffenidentität — fortlaufend parallel zu 13–15 |
| [14_Sprint_Lobby.md](14_Sprint_Lobby.md) | Sprint 14 (Maintainer); Match per Code über Supabase, Fraktionswahl, Build-Abgleich vor dem Verbinden |
| [15_Sprint_Netzstabilitaet.md](15_Sprint_Netzstabilitaet.md) | Sprint 15 (Maintainer); Reconnect, Desync-Erstbefund, adaptive Eingabeverzögerung, Dauerbetrieb des Relays |
| [16-19_Betatest_Einordnung.md](16-19_Betatest_Einordnung.md) | **Einordnung des ersten Betatest-Berichts** in die Sprintfolge — Issues #43–#58, nach Schreibhoheit geschnitten, mit den offenen Inhaberentscheidungen |
| [16_Sprint_Wirtschaft.md](16_Sprint_Wirtschaft.md) | Sprint 16 (Netzstrang, **vorgezogen**); Strang C aus Sprint 12 und die **acht** Betatest-Befunde (#43, #44, #45, #46, #47, #48, #53, #54) im selben Schreibbereich — Knappheit, Lager, Radar, Low Power, Bauvoraussetzungen, Platzierung, Reparaturkosten |
| [17_Sprint_Zugangsprotokoll.md](17_Sprint_Zugangsprotokoll.md) | Sprint 17 (Maintainer); Zugriffsprotokoll, Sperrliste und Erstmeldung — Paket A läuft im Großauftrag als **Block 4 hinter der Lobby**, weil es die Lobby-Functions aus Sprint 14 voraussetzt; es liegt bis auf den `.partial`-Fix in `RelayServerCore.cs` ausserhalb des Repos |
| [18_Sprint_Befehl_und_Auswahl.md](18_Sprint_Befehl_und_Auswahl.md) | Sprint 18 (Netzstrang); Auswahl nach Rolle, sichtbares Angriffsziel mit Nachsetzen über zwei Intents, Formationsausrichtung — Eingabe und Darstellung, kein neuer `CommandKind` |
| [AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md) | **Der gebündelte Arbeitsauftrag an den ausführenden Agenten, Blöcke 0 bis 4** — Auswahlrahmen, Sprint 16, Sprint 18, Sprint 14, Sprint 17 Paket A. Er ist die verbindliche Reihenfolge; wo er von einer Sprintdatei abweicht, gilt er |
| [Testberichte/](Testberichte/) | **Anonymisierte** Fassungen der eingegangenen Betatest-Berichte, je Build und Kennung — Ablauf: [../Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) |

## Das Wichtigste in fünf Sätzen

1. **Der Content ist vollständig.** 34 von 34 MS-1-Rollen existieren dreifach —
   als Design, als Code-Definition und als 3D-Asset. Es fehlt **kein Gebäude**,
   das Kraftwerk am allerwenigsten.
2. **Der Gegner spielt mit — seit D-077.** `SkirmishAiSystem` ist in
   `MatchRunner` zwischen Kampf und Sieg registriert und spielt Slot 1. Der
   ursprüngliche Satz an dieser Stelle — die fehlende Registrierung sei die
   einzige echte Blockade zwischen „Sandkasten" und „Spiel" — galt bis zum
   2026-08-06 und ist seitdem überholt. Was heute im Weg steht, steht in den
   Betatest-Befunden, nicht in der Systemregistrierung.
3. **Zwei Gebäude sind Attrappen.** Lager und Radar kosten Geld und Strom und
   tun nichts. Das ist die zutreffende Fassung der Vermutung „pro Fraktion
   fehlen zwei Gebäude".
4. **Die halbe fertige Arbeit ist unerreichbar.** Alle 17 Legion-Definitionen
   und -Assets sind fertig, aber es gibt keine Fraktionswahl — der Mensch kann
   nur Allianz spielen.
5. **Die 34 Art-Assets liegen nicht im Repository.** `.gitignore` schließt
   FBX, PNG, MAT und PREFAB aus; ein frischer Clone zeigt Graubox-Würfel. Das
   ist der teuerste ungelöste Punkt, weil das Registry-Asset bereits auf
   Dateien verweist, die dort nie ankommen.

> **Stand der Grundlinie:** Diese Mappe beschreibt `main` @ `15dfe73` plus den
> Arbeitsbaum vom 2026-08-06 vormittags. Zeitgleich lief eine
> Wirtschafts-Umstellung (**D-077**, klassischer C&C-Eröffnungsloop)
> uncommittet im selben Arbeitsbaum. Sie erledigt einen Teil von Masterplan 1.3
> vorweg — Einzelheiten im Kasten „Die Grundlinie bewegt sich" in
> [01_Bestandsaufnahme.md](01_Bestandsaufnahme.md).

## Die vier Entscheidungen, die nur der Inhaber treffen kann

> **Alle vier sind am 2026-08-06 getroffen worden.** Wortlaut, Begründung und
> Folgen stehen in [00_Entscheidungen.md](00_Entscheidungen.md). Der Abschnitt
> hier beschreibt weiterhin die Fragestellung, damit nachvollziehbar bleibt,
> worüber entschieden wurde.
>
> | Frage | Entscheidung |
> |---|---|
> | E-1 | Externes Zip-Paket in Google Drive, kein Git LFS |
> | E-2 | Die 34 Tripo-Modelle sind Platzhalter und werden gestaffelt ersetzt |
> | E-3 | Nur die Marke wird umbenannt, Code-Identität bleibt `Nova.*` |
> | E-4 | Hashkrieg ist Name und Welt; die Mechanik-Inversion bleibt Reserve |

Diese vier Entscheidungen sind getroffen; die folgenden Unterabschnitte
bleiben als historische Entscheidungsgrundlage erhalten. Alles Weitere ist
Handwerk oder in den jeweiligen Sprint-Ergebnisblöcken als Restprüfung
ausgewiesen.

### E-1 — Wie kommen die Binärdaten in die Welt?

Heute: `.gitignore` Zeile 90–96 schließt alle Art-Binärdaten aus, das
Registry-Asset verweist trotzdem auf sie. Drei Wege:

| Weg | Vorteil | Preis |
|---|---|---|
| **Git LFS** (Empfehlung) | Ein `git clone` liefert ein spielbares Projekt; CI kann später Screenshots bauen | Muss **vor** dem ersten Art-Commit eingeführt werden, sonst History-Rewrite |
| Externes Paket-Zip | Ändert nichts am Repo | Zweiter Entwickler braucht einen zweiten Kanal; das Zip existiert heute nirgends auffindbar |
| Binärdaten roh in Git | Sofort | 107 MB wachsend, dauerhaft im Verlauf |

**Konsequenz bei Nichtentscheidung:** Das Registry-Asset darf nicht committet
werden, sonst zeigen 34 GUID-Referenzen in jedem frischen Clone ins Leere.

### E-2 — Gilt die Tripo-Sperre?

[../../assets/ArtManifest_MS1.md](../../assets/ArtManifest_MS1.md) §8 sperrt
Tripo3D Free ausdrücklich für eingecheckte Assets. Genau von diesem Anbieter
stammen alle 34 Modelle. Entweder wird die Sperre aufgehoben (mit begründeter
Prüfung der Anbieter-AGB) oder die 34 Modelle sind Wegwerf-Platzhalter. Solange
das offen ist, kann die Provenienzpflicht nicht erfüllt werden — und ohne sie
darf nichts ins öffentliche Repository.

### E-3 — Wie weit geht die Umbenennung?

Nur die Marke (Titel, Fenster, README, Repository) oder auch die Code-Identität
(17 Assemblies, 226 Namespaces, 560 using-Zeilen)? Beides ist vertretbar, aber
die Antwort bestimmt, ob es ein Nachmittag oder ein isolierter Sprint wird.
Details und Reihenfolge: [05_Umbenennung.md](05_Umbenennung.md).

### E-4 — Bleibt Hashkrieg eine Fiktion, oder wird es eine Mechanik?

[../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) trägt
zwei Ebenen: einen **Namens- und Weltentwurf** (Aetherium, Allianz, Legion —
der ist längst verbindlich und in [../../vision/Lore.md](../../vision/Lore.md)
ausgearbeitet) und eine **Mechanik-Inversion** (öffentlicher Hashrate-Ticker,
Anteils-Einkommen, Halving, 51-Prozent-Attacke — die ist ausdrücklich nicht
beschlossen und existiert nirgends im Code).

Dieser Plan geht durchgehend von der ersten Ebene aus: *Hashkrieg ist der Name
und die Welt, Aetherium bleibt die Ressource.* Die Mechanik-Inversion bleibt
Post-MVP-Reserve. Wenn das anders gewollt ist, ändert sich Phase 3 des
Masterplans grundlegend — sonst nichts.

## Wie dieser Plan gelesen werden will

- **Top-down nach Spielgefühl, nicht nach Aufwand.** Phase 1 macht aus dem
  Sandkasten ein Spiel. Phase 2 macht es bedienbar. Erst danach kommt, was gut
  aussieht.
- **Jedes Arbeitspaket nennt seinen Beleg.** Wo eine Aussage auf einer
  Codestelle beruht, steht der Symbolname dabei — bewusst nicht die
  Zeilennummer, die veraltet beim ersten Edit.
- **Skips sind erlaubt, stille Skips nicht.** Wer ein Paket überspringt, trägt
  den Grund in [../ScopeLedger.md](../ScopeLedger.md) nach.

## Offene Punkte

- E-1 bis E-4 sind entschieden; offen sind nur ihre jeweils dokumentierten
  Umsetzungsfolgen.
- Die Aufwandsklassen (S/M/L/XL) im Masterplan sind Schätzungen aus der
  Codelage, nicht gemessene Werte. Sie sortieren, sie planen nicht.
- Fünf Steuerdokumente stehen noch auf dem mit D-076 abgeschafften
  Gate-Regime ([../SprintPlanning.md](../SprintPlanning.md),
  [../RiskAnalysis.md](../RiskAnalysis.md), [../Roadmap.md](../Roadmap.md),
  [../OpenQuestions.md](../OpenQuestions.md),
  [../StatusSnapshot_2026-08-05.md](../StatusSnapshot_2026-08-05.md)). Wer sie
  als Ist-Stand liest, plant gegen ein Regime, das nicht mehr gilt.

## Nächste Schritte

1. **Sprint 13 ist zur Hälfte gemergt.** 13.1 (Verbindungsdialog) und 13.7
   (Linux-Build) liegen mit Commit `e15f5e6` auf `main`. Offen sind **13.2 bis
   13.5** aus [13_Sprint_Netzpartie.md](13_Sprint_Netzpartie.md): das
   Relay-Deployment auf dem VPS und die Abnahmestufen 2 bis 4.
2. **Diese vier Pakete kann kein Agent erledigen.** 13.2 braucht Zugangsdaten
   zum Server. 13.4 und 13.5 brauchen zwei Menschen an zwei Rechnern. Sie warten
   auf den Inhaber, nicht auf Arbeitskraft — und sie halten deshalb nichts auf,
   was daneben laufen kann.
3. **Als nächstes läuft der Großauftrag**
   ([AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md)), fünf Blöcke am Stück:
   - **Block 0 — Auswahlrahmen** (#49): eine Konstante in
     `GroundMarkerVisuals.cs`, Wirkung sofort sichtbar.
   - **Block 1 — [Sprint 16](16_Sprint_Wirtschaft.md):** die Wirtschaft trägt
     sich selbst.
   - **Block 2 — [Sprint 18](18_Sprint_Befehl_und_Auswahl.md):** Befehl und
     Auswahl werden lesbar.
   - **Block 3 — [Sprint 14](14_Sprint_Lobby.md):** Match per Code über
     Supabase, Fraktionswahl, Build-Abgleich.
   - **Block 4 — [Sprint 17](17_Sprint_Zugangsprotokoll.md) Paket A:** liegt bis
     Es läuft als **Block 3 des Großauftrags**, weil es die Lobby-Functions
     aus Sprint 14 voraussetzt — die seit `b4e75e5` auf `main` liegen.
     `.partial`-Leck in `Assets/_Project/Scripts/Networking/RelayServerCore.cs`.
     `ResetMatch` verwirft den Aufzeichnungsstrom und merkt sich den Pfad,
     löscht die Datei aber nie. Paket B wartet weiterhin auf Sprint 15.
4. **Alles, was `SimDefinitions` anfasst, liegt vor dem VPS-Rollout.** Das sind
   in Sprint 16 die Pakete **16.7** (Feldwerte und Feldanzahl) und **16.8**
   (Bauvoraussetzungs-Bitmaske). Der Relay vergleicht den Definitions-Hash
   **serverseitig**; nach dem Rollout kostet dieselbe Zahlenänderung einen
   Serverzugang und einen Redeploy
   ([13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md), §Definitions-Hash).
5. **Sprint 19 bleibt danach und ist kein Codeauftrag.** Die beiden Art-Befunde
   #57 (Gebäude durchsichtig und hohl) und #58 (Maßstab des Radarturms) sind
   Arbeit am Asset; #58 hängt zusätzlich an der offenen Frage #19. Sprint 16
   Paket 16.2 macht #57 sichtbarer, behebt es aber ausdrücklich nicht.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.4.0 | 2026-08-08 | Sprint 13.0 und D-091 als Voraussetzung für externe Beiträge unter Tier 2 ergänzt | Producer / Agent (Umsetzung) |
| 0.5.0 | 2026-08-09 | Sprint 17 (Zugangsprotokoll, Sperrliste, Erstmeldung) aufgenommen; Paket A als vorziehbar vermerkt | Producer / Agent (Umsetzung) |
| 0.6.0 | 2026-08-09 | Ersten Betatest-Bericht **anonymisiert** aufgenommen und in die Sprintfolge eingeordnet (Issues #43–#58); Testberichte-Ordner angelegt, Ablauf in [../Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) festgelegt | Orchestrator |
| 0.7.0 | 2026-08-09 | Nach der Inhaberentscheidung zum ersten Betatest: **Sprint 16 vorgezogen**, [16_Sprint_Wirtschaft.md](16_Sprint_Wirtschaft.md) und [18_Sprint_Befehl_und_Auswahl.md](18_Sprint_Befehl_und_Auswahl.md) in die Mappe aufgenommen, Regelwerk als Parallelbetrieb 13–18 mit Trennung über Dateihoheit (**D-095**) fortgeschrieben, Großauftrag mit den Blöcken 0 bis 4 eingehängt. „Nächste Schritte" neu geschrieben: Sprint 13 halb gemergt (`e15f5e6`), 13.2–13.5 nicht durch einen Agenten erledigbar, `SimDefinitions`-Pakete vor dem VPS-Rollout. Punkt 2 der Fünf-Sätze-Zusammenfassung nach **D-077** berichtigt — die Skirmish-KI ist registriert und spielt | Orchestrator |
| 0.7.1 | 2026-08-09 | Index gegen den Großauftrag und die Sprintdateien nachgezogen: Sprint 17 Paket A ist nicht „sofort vorziehbar", sondern läuft als **Block 3 hinter der Lobby**, die seit `b4e75e5` auf `main` gebaut ist, und es berührt mit dem `.partial`-Fix in `RelayServerCore.cs` genau **eine** Datei unter `Assets/`. Sprint 16 trägt **acht** Betatest-Befunde (#43, #44, #45, #46, #47, #48, #53, #54), nicht sechs. Alle Markdown-Links dieser Datei gegen das Dateisystem geprüft — kein toter Link | Orchestrator |
| 0.7.2 | 2026-08-10 | D-105 nachgezogen: alleinige Projektleitung, sichtbare Spielabnahme-Zurückstellung und den historischen Charakter des D-091-Zwei-Maintainer-Rollouts im Index kenntlich gemacht | Project Owner / Orchestrator |
