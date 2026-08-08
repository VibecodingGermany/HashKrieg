# Sprint 13: Die erste echte Netzpartie — zwei Menschen, zwei Rechner, ein Server

**Status:** geplant | **Vorgänger:** [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) (A1–A7 umgesetzt, A8 Stufen 2–4 offen) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** der Beweis ist eine gespielte Runde, kein grüner Test

## Ziel

Zwei Menschen an zwei Rechnern spielen eine vollständige Partie über den Relay
auf dem VPS. Vorher hat das niemand getan.

Sprint 12 hat den Netzpfad gebaut und headless bis Tick 10.023 bewiesen. Was
fehlt, ist nicht Technik, sondern der Weg vom Doppelklick bis zum laufenden
Match: es gibt **keine Oberfläche, in die jemand Serveradresse und Match-Code
eintippen könnte**, und der Relay läuft auf keinem erreichbaren Host.

## Ausgangslage — geprüft, nicht übernommen

| Punkt | Stand |
|---|---|
| Lockstep, Barrier, Protokoll | steht (A1–A5), 547/547 SimRunner-Tests grün |
| `MatchConfig`/`MatchBootstrap`/`MatchRunner` | verdrahtet (A6), aber nur programmatisch befüllbar |
| Relay-Server, systemd-Unit, `deploy.sh` | vorhanden (A7), nie auf einem Linux-Host gelaufen |
| A8 Stufe 1 (headless Zwei-Klienten-Soak) | nachgewiesen |
| A8 Stufen 2–4 (zwei Fenster, LAN, VPS) | **offen** |
| Verbindungsoberfläche | **existiert nicht** |

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

### 13.6 · Der Baseline-Wächter

Ein CI-Job, der fehlschlägt, wenn ein PR Simulationsverhalten **und** eine
Determinismus-Baseline im selben Zug ändert (Regel siehe
[13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md)). Überschreibbar durch ein
Maintainer-Label, damit ein bewusster Baseline-Reset weiterhin möglich ist.

Dieses Paket ist die Voraussetzung dafür, dass Sprint 13B ohne ständiges
Nachfassen laufen kann.

## Schreibhoheit

| Pfad | |
|---|---|
| `Scripts/Gameplay/UI/`, `Scripts/Gameplay/Input/` | 13.1 |
| `Scripts/Gameplay/Match/` | 13.1 |
| `Scripts/Networking/` | nur falls ein Abnahmefund es erzwingt |
| `tools/Nova.RelayServer/`, `docs/tech/RelayServer.md` | 13.2 |
| `.github/workflows/` | 13.6 |
| `docs/production/GrayboxLog.md` | 13.3–13.5 |

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

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Zwei Menschen haben an zwei Rechnern über den VPS-Relay eine Partie bis zu
   einem Siegzustand gespielt.
3. Der Ablauf steht im GrayboxLog — mit Commit, Ticks, Endzustand beider Seiten.
4. Ein Dritter kann aus dem Runbook heraus einen Relay aufsetzen, ohne zu fragen.
5. Der Baseline-Wächter läuft auf jedem PR.

Punkt 2 ist nicht durch Punkt 1 ersetzbar. Genau das ist der Sinn von Tier 1.

## Changelog-Notiz

Netzpartie zu zweit über den eigenen Relay spielbar: Verbindungsdialog mit
ehrlichen Fehlerzuständen, Relay-Betrieb auf dem VPS, A8 Stufen 2–4
nachgewiesen, Baseline-Wächter in CI.

## Versionsrelevanz

`minor` — neue spielbare Fähigkeit, keine Vertragsbrüche.
