# Übergang Project Nova → Hashkrieg — Planungsmappe

**Version:** 0.6.0 | **Status:** fortgeschriebene Planungsmappe – Sprint 12 Stränge A/B technisch umgesetzt, gespielte Abnahmen offen; Sprint 13.0 bereitet externe Beiträge unter Tier 2 vor; erster Betatest-Bericht eingeordnet | **Verantwortungsbereich:** Orchestrator / Producer | **Sprint:** 13

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

- [../../../GOVERNANCE.md](../../../GOVERNANCE.md) – Governance-Tier 2 (D-091): Nachweis ist grüne CI plus gespielte Runde
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
| [13-0_Sprint_Freigabe.md](13-0_Sprint_Freigabe.md) | Sprint 13.0; Lizenz, Tier-2-Wechsel, Maintainer-/Fork-Modell und CI-Wächter vor dem ersten externen PR |
| [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Regelwerk für die parallele Arbeit ab Sprint 13** — Schreibhoheit, Determinismus-Baselines, Merge-Fenster, Zugangsmodell für externe Beitragende |
| [13_Sprint_Netzpartie.md](13_Sprint_Netzpartie.md) | Sprint 13 (Maintainer); Verbindungsdialog, Relay auf dem VPS, A8 Stufen 2–4 als gespielte Abnahme |
| [13B_Sprint_Einheitenverhalten.md](13B_Sprint_Einheitenverhalten.md) | Sprint 13B (externer Beitragender, PR-only); Einheitenverhalten, KI und Legion-Waffenidentität — fortlaufend parallel zu 13–15 |
| [14_Sprint_Lobby.md](14_Sprint_Lobby.md) | Sprint 14 (Maintainer); Match per Code über Supabase, Fraktionswahl, Build-Abgleich vor dem Verbinden |
| [15_Sprint_Netzstabilitaet.md](15_Sprint_Netzstabilitaet.md) | Sprint 15 (Maintainer); Reconnect, Desync-Erstbefund, adaptive Eingabeverzögerung, Dauerbetrieb des Relays |
| [16-19_Betatest_Einordnung.md](16-19_Betatest_Einordnung.md) | **Einordnung des ersten Betatest-Berichts** in die Sprintfolge — Issues #43–#58, nach Schreibhoheit geschnitten, mit den offenen Inhaberentscheidungen |
| [17_Sprint_Zugangsprotokoll.md](17_Sprint_Zugangsprotokoll.md) | Sprint 17 (Maintainer); Zugriffsprotokoll, Sperrliste und Erstmeldung — Paket A liegt ausserhalb des Repos und ist sofort vorziehbar |
| [Testberichte/](Testberichte/) | **Anonymisierte** Fassungen der eingegangenen Betatest-Berichte, je Build und Kennung — Ablauf: [../Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) |

## Das Wichtigste in fünf Sätzen

1. **Der Content ist vollständig.** 34 von 34 MS-1-Rollen existieren dreifach —
   als Design, als Code-Definition und als 3D-Asset. Es fehlt **kein Gebäude**,
   das Kraftwerk am allerwenigsten.
2. **Der Gegner spielt nicht mit.** `SkirmishAiSystem` wird von
   `MatchRunner` nie registriert. Das ist die einzige echte Blockade zwischen
   „Sandkasten" und „Spiel".
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

1. **Sprint 13.0 mergen und den Remote-Rollout abschließen.** D-091 entscheidet
   den Tier-2-Wechsel; die neuen Required Checks, eine Maintainer-Peer-Review
   und native Code-Owner-Prüfung müssen nach ihrem ersten erfolgreichen
   Folge-PR-Lauf in der Branch Protection aktiviert und mit einem bewusst falschen PR negativ
   getestet werden.
2. Sprint-12-Strang B im aktuellen macOS-Build in einem dichten Gefecht sehen
   und gegenhören; den Befund im Umsetzungsreport ergänzen.
3. Die offenen A8-Netzwerkstufen aus [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md)
   mit zwei Fenstern, im LAN und auf dem VPS spielen — das ist
   [Sprint 13](13_Sprint_Netzpartie.md).
4. Strang C ist geplant als **Sprint 16**, nach dem Netzstrang: er verändert
   Simulationszustand und Baselines und hebt damit die Trennung auf, die den
   Parallelbetrieb erst möglich macht (siehe
   [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md)).
5. **Sprint 17 Paket A** ([17_Sprint_Zugangsprotokoll.md](17_Sprint_Zugangsprotokoll.md))
   liegt vollständig im Supabase-Projekt und berührt keine Datei unter
   `Assets/`. Es ist deshalb an der Sprintreihenfolge vorbei vorziehbar,
   sobald das Projekt steht — Paket B wartet auf Sprint 15.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.4.0 | 2026-08-08 | Sprint 13.0 und D-091 als Voraussetzung für externe Beiträge unter Tier 2 ergänzt | Producer / Agent (Umsetzung) |
| 0.5.0 | 2026-08-09 | Sprint 17 (Zugangsprotokoll, Sperrliste, Erstmeldung) aufgenommen; Paket A als vorziehbar vermerkt | Producer / Agent (Umsetzung) |
| 0.6.0 | 2026-08-09 | Ersten Betatest-Bericht **anonymisiert** aufgenommen und in die Sprintfolge eingeordnet (Issues #43–#58); Testberichte-Ordner angelegt, Ablauf in [../Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) festgelegt | Orchestrator |
