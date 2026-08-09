# Sprint 13: Die erste echte Netzpartie — zwei Menschen, zwei Rechner, ein Server

**Version:** 1.2.0 | **Status:** teilweise umgesetzt (13.1 und 13.7 seit `e15f5e6`); 13.2 bis 13.5 offen | **Verantwortungsbereich:** Netzstrang | **Sprint:** 13 | **Vorgänger:** [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) (A1–A7 umgesetzt, A8 Stufen 2–4 offen) | **Parallel zu:** [13.0](13-0_Sprint_Freigabe.md), [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **UX-Gate:** human (13.1 ist Oberfläche) | **Leitsatz:** der Beweis ist eine gespielte Runde, kein grüner Test

## Ziel

Zwei Menschen an zwei Rechnern spielen eine vollständige Partie über den Relay
auf dem VPS. Vorher hat das niemand getan.

Sprint 12 hat den Netzpfad gebaut und headless bis Tick 10.023 bewiesen. Was
fehlte, war nicht Technik, sondern der Weg vom Doppelklick bis zum laufenden
Match. Die Oberfläche dafür steht seit `e15f5e6` (13.1), der Linux-Build
ebenfalls (13.7). Offen bleibt: der Relay läuft auf keinem erreichbaren Host,
und niemand hat die Partie gespielt.

**Wer den Rest erledigen kann:** 13.2 braucht Zugangsdaten zum VPS. 13.3 braucht
einen Menschen an zwei Unity-Fenstern. 13.4 und 13.5 brauchen zwei Menschen an
zwei Rechnern. Keines davon ist ein Agentenauftrag; der Großauftrag vom
2026-08-09 ([AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md)) weist alle vier
ausdrücklich als nicht enthalten aus. Was hier noch fehlt, fehlt an Zugang und
an Menschen, nicht an Code.

## Ausgangslage — geprüft, nicht übernommen

| Punkt | Stand |
|---|---|
| Lockstep, Barrier, Protokoll | steht (A1–A5), 547/547 SimRunner-Tests grün |
| `MatchConfig`/`MatchBootstrap`/`MatchRunner` | verdrahtet (A6), aber nur programmatisch befüllbar |
| Relay-Server, systemd-Unit, `deploy.sh` | vorhanden (A7), nie auf einem Linux-Host gelaufen |
| A8 Stufe 1 (headless Zwei-Klienten-Soak) | nachgewiesen |
| A8 Stufen 2–4 (zwei Fenster, LAN, VPS) | **offen** |
| Verbindungsoberfläche | umgesetzt, `e15f5e6` (13.1) — `MainMenuController.BuildNetworkJoin` |
| Linux-Build | umgesetzt, `e15f5e6` (13.7) — `tools/packaging/build-linux.sh` |

## Pakete

### 13.1 · Der Verbindungsdialog

Das kleinstmögliche Stück Oberfläche, das eine Partie startbar macht:
Serveradresse, Port, Match-Code, Slotwahl (Host/Gast), Verbinden, Abbrechen.
Dazu ein Statusband, das die Zustände des `RelayMatchClient` ehrlich anzeigt —
verbinde, warte auf Gegenspieler, Fingerprint abgelehnt, Peer verloren.

Kein Komfort, keine Persistenz, keine Freundesliste. Das kommt in
[Sprint 14](14_Sprint_Lobby.md).

**Wichtig:** Die Fehlerzustände sind das eigentliche Feature. Ein Spieler, der
nicht versteht, warum nichts passiert, hält das Spiel für kaputt — auch wenn
der Fingerprint genau richtig abgelehnt hat.

### 13.2 · Der Relay läuft auf dem VPS

`deploy.sh bootstrap` auf dem echten Host, systemd-Unit aktiv, nur der
Relay-Port offen, Match-Token verpflichtend. Das Runbook in
[../../tech/RelayServer.md](../../tech/RelayServer.md) wird dabei gegen die
Wirklichkeit gelesen und korrigiert, wo es abweicht.

Zugangsdaten gehören nicht ins Repository und nicht in dieses Dokument.

### 13.3 · A8 Stufe 2 — zwei Fenster auf einem Rechner

Zwei Unity-Instanzen, ein lokaler Relay, eine vollständige Runde bis zu einem
Siegzustand. Erster Punkt, an dem ein Mensch die Netzpartie tatsächlich sieht.

### 13.4 · A8 Stufe 3 — LAN

Zwei Rechner, ein Netz. Fängt die Klasse von Fehlern, die bei Loopback nie
auftritt: MTU, Nagle, echte Latenz, Firewall.

### 13.5 · A8 Stufe 4 — über den VPS

Zwei Rechner, getrennte Netze, Relay im Internet. Das ist das Sprintziel.
Ergebnis kommt in den [GrayboxLog](../GrayboxLog.md): Commit, Dauer, Ticks,
Endzustand beider Seiten, und was sich falsch angefühlt hat.

### 13.6 · ~~Der Baseline-Wächter~~ → verschoben

Liegt jetzt als Paket F4 in
[Sprint 13.0](13-0_Sprint_Freigabe.md). Grund: Der Strang, der den Wächter
auslösen wird, fängt vor diesem Sprint an. Ein Wächter, der später kommt als die
PRs, die er prüfen soll, prüft nichts.

Die Nummer bleibt frei, damit Verweise auf „Paket 13.7" gültig bleiben.

**Folge für diesen Sprint:** Er fasst `.github/workflows/` nicht mehr an und ist
damit auch gegen Sprint 13.0 schreibkonfliktfrei.

### 13.7 · Der Linux-Build

`tools/packaging/` kann heute nur macOS. Damit kann am Netznachweis nur
teilnehmen, wer einen Mac hat — der Einheitenstrang wäre von genau der Runde
ausgeschlossen, deren Verhalten er baut. Ein Zwei-Spieler-Nachweis mit nur einer
Plattform ist zudem ein schwächerer Nachweis.

Nötig ist ein `build-linux.sh` nach dem Muster von `build-mac.sh`, das denselben
`NovaBuildCommit` einbrennt und ihn ohne Unity auslesbar macht (`cat
ProjectNova_Data/NovaBuildCommit.txt`). Das Packaging-README bekommt beide Wege.

Vorbedingung: Das Unity-Linux-Build-Modul muss installiert sein. Fehlt es, ist
das der erste Schritt und wird notiert.

Dieses Paket kommt **vor** 13.4, sonst ist Stufe 3 nur zwischen zwei Macs
belegbar.

**Umgesetzt mit `e15f5e6`:** `BuildScript.BuildLinux64` und
`tools/packaging/build-linux.sh` nach dem Muster von `build-mac.sh`, mit dem
Stempel in `ProjectNova_Data/NovaBuildCommit.txt`.

**Aktenfehler, der dabei offen blieb:** Beide Packaging-Skripte brennen den
`NovaBuildCommit` ein, aber **keine einzige C#-Datei liest ihn** — das Spiel
kennt seinen eigenen Build nicht und kann ihn weder anzeigen noch melden. Der
Stempel ist damit nur von außen lesbar (`defaults read … NovaBuildCommit`, `cat
ProjectNova_Data/NovaBuildCommit.txt`). Der fehlende Leser wird nicht hier
nachgezogen, sondern als Paket 14.0 im Großauftrag vom 2026-08-09
([AUFTRAG_Grossblock.md](AUFTRAG_Grossblock.md)); ohne ihn ist der
Build-Abgleich in Sprint 14 nicht baubar.

## Schreibhoheit

| Pfad | |
|---|---|
| `Scripts/Gameplay/UI/`, `Scripts/Gameplay/Input/` | 13.1 |
| `Scripts/Gameplay/Match/` | 13.1, und die Systemregistrierung auf Zuruf aus 13B (siehe [Parallelbetrieb](13-15_Parallelbetrieb.md), „Neue Systeme") |
| `Scripts/Networking/` | nur falls ein Abnahmefund es erzwingt |
| `tools/Nova.RelayServer/`, `docs/tech/RelayServer.md` | 13.2 |
| `tools/packaging/` | 13.7 |
| `docs/production/GrayboxLog.md` | 13.3–13.5 |

Disjunkt gegen [13B](13B_Sprint_Einheitenverhalten.md) (Simulation und AI) und
gegen [13.0](13-0_Sprint_Freigabe.md) (Governance, `.github/workflows/`,
Wurzeldateien). Alle drei können nebeneinander laufen.

**Keine Datei unter `Scripts/Simulation/` oder `Scripts/AI*`.** Wenn ein
Abnahmefund eine Simulationsänderung verlangt, wird er notiert und an
Sprint 13B übergeben — nicht hier gefixt.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Lobby, Matchvermittlung, Spielerliste | [Sprint 14](14_Sprint_Lobby.md) |
| Reconnect nach Verbindungsabbruch | [Sprint 15](15_Sprint_Netzstabilitaet.md) |
| Fraktionswahl | Waffenidentität liegt bei [13B](13B_Sprint_Einheitenverhalten.md); die Auswahloberfläche kommt mit der Lobby |
| Mehr als zwei Slots | der Relay ist auf zwei ausgelegt, das bleibt so |
| Strang C (Wirtschaftsdruck) | simulationsverändernd, siehe Parallelbetrieb — Sprint 16 |

## Risiken

| Risiko | Umgang |
|---|---|
| Unity-Lizenzhandshake bricht ab (`505` in Sprint 12) | vor 13.3 klären; ohne EditMode-Tests keine belastbare Aussage zur Präsentationsschicht |
| Der VPS-Relay ist erreichbar, aber ohne Token offen | Match-Token ist Pflicht, nicht Option; Firewall-Regel wird dokumentiert |
| NAT/Firewall verhindert Stufe 4 | Stufe 3 zuerst; scheitert Stufe 4 am Netz und nicht am Code, wird das so notiert |
| Ein 13B-Merge mitten in der Abnahme | Merge-Fenster ist während Abnahmeläufen zu |
| **Kein zweiter Mensch verfügbar** | Der eigentliche Engpass dieses Sprints, siehe unten |

### Der zweite Mensch

13.1 bis 13.3 sind allein machbar. **13.4 und 13.5 nicht** — sie verlangen
definitionsgemäß zwei Menschen an zwei Rechnern. Das ist keine Technik-, sondern
eine Verfügbarkeitsfrage, und sie entscheidet über das Sprintziel.

| Kandidat | Was fehlt |
|---|---|
| Der zweite Maintainer | Zeit; Rückkehr in den nächsten Wochen angekündigt |
| Der Einheitenstrang | nichts mehr an Technik — der Linux-Build steht seit `e15f5e6` |

Die Reihenfolge lautete: **13.7 wird früh gebaut, nicht am Ende.** Das ist
eingelöst. Der Build war nicht Beiwerk, sondern die Bedingung dafür, dass Stufe 3
und 4 überhaupt jemanden zum Mitspielen haben; diese Bedingung ist jetzt erfüllt,
der Engpass ist ab hier reine Verfügbarkeit. Steht kein zweiter Mensch bereit,
endet der Sprint sauber
nach 13.3 und die Stufen 3 und 4 werden als offen ausgewiesen, statt sie als
erledigt zu behaupten.

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Zwei Menschen haben an zwei Rechnern über den VPS-Relay eine Partie bis zu
   einem Siegzustand gespielt.
3. Der Ablauf steht im GrayboxLog — mit Commit, Ticks, Endzustand beider Seiten.
4. Ein Dritter kann aus dem Runbook heraus einen Relay aufsetzen, ohne zu fragen.
5. Es gibt einen Linux-Build, und mindestens eine Abnahmestufe wurde
   plattformübergreifend gespielt.

Punkt 2 ist nicht durch Punkt 1 ersetzbar. Genau das ist der Sinn der
verhaltensbezogenen Abnahme.

## Changelog-Notiz

Netzpartie zu zweit über den eigenen Relay spielbar: Verbindungsdialog mit
ehrlichen Fehlerzuständen, Relay-Betrieb auf dem VPS, A8 Stufen 2–4
nachgewiesen und Linux-Build bereitgestellt.

## Versionsrelevanz

`minor` — neue spielbare Fähigkeit, keine Vertragsbrüche.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.2.0 | 2026-08-09 | Stand nach `e15f5e6` nachgezogen: 13.1 (Verbindungsdialog) und 13.7 (Linux-Build) sind umgesetzt, der Sprint steht damit auf „teilweise umgesetzt". Ausgangslage berichtigt (Verbindungsoberfläche nicht mehr „existiert nicht"; Zeile zum Linux-Build ergänzt). Einordnung der Restarbeit ergänzt: 13.2 braucht VPS-Zugangsdaten, 13.3 einen Menschen an zwei Fenstern, 13.4 und 13.5 zwei Menschen an zwei Rechnern — im Großauftrag vom 2026-08-09 ausdrücklich nicht enthalten. Aktenfehler bei 13.7 vermerkt: kein C#-Code liest den `NovaBuildCommit`, der Leser kommt als Paket 14.0 | Producer / Agent (Umsetzung) |
| 1.1.0 | 2026-08-08 | Paket 13.6 (Baseline-Wächter) nach [Sprint 13.0](13-0_Sprint_Freigabe.md) verschoben, weil der auslösende Strang früher anfängt; damit auch `.github/workflows/` aus der Schreibhoheit raus und der Sprint gegen 13.0 konfliktfrei. Paket 13.7 (Linux-Build) ergänzt und als Vorbedingung für Abnahmestufe 3 markiert. Abschnitt „Der zweite Mensch" ergänzt: 13.4 und 13.5 sind verfügbarkeits-, nicht technikbegrenzt | Producer / Agent (Umsetzung) |
| 1.0.0 | 2026-08-08 | Erstfassung | Producer / Agent (Umsetzung) |
