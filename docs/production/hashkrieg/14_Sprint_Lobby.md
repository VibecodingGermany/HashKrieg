# Sprint 14: Die Lobby — ein Match finden, ohne sich vorher anzurufen

**Status:** geplant | **Vorgänger:** [13_Sprint_Netzpartie.md](13_Sprint_Netzpartie.md) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** der Match-Code ist ein Werkzeug, kein Ritual

## Ziel

Nach Sprint 13 ist eine Netzpartie möglich, aber sie beginnt mit einem Anruf:
Serveradresse durchgeben, Match-Code ausdenken, gleichzeitig auf Verbinden
drücken. Sprint 14 ersetzt das durch eine Lobby.

Ein Spieler legt ein Match an und bekommt einen Code. Der zweite gibt den Code
ein und landet im selben Match — inklusive Fraktionswahl und Bereitschaft.

## Architektur: Vermittlung getrennt vom Transport

Die Lobby läuft über das bestehende **Supabase-Projekt** ausserhalb des
Repositories. Der Relay bleibt, was er ist: ein dummer, zustandsarmer
Lockstep-Verteiler für genau zwei Peers.

Warum getrennt: Der Relay ist die Komponente, an der Determinismus und
Latenz hängen. Jede Zeile Match-Verwaltung darin ist eine Zeile, die beim
Debuggen eines Desyncs im Weg steht. Vermittlung ist ausserdem ein anderes
Problem — sie braucht Persistenz, Accounts und Ausfallsicherheit, also genau
das, was der Relay bewusst nicht hat.

**Zugangsdaten gehören nicht ins Repository.** Der Client bekommt sie über
Konfiguration, nicht über eingecheckte Werte. Öffentlicher Schlüssel und
Projekt-URL sind kein Geheimnis, Service-Rollen-Schlüssel schon.

## Pakete

### 14.1 · Match anlegen und beitreten

Ein Spieler legt ein Match an: Supabase vergibt Code, Relay-Adresse, Seed und
Slotzuordnung. Der zweite tritt über den Code bei. Beide sehen, wer da ist.

Der Code ist kurz, vorlesbar und mehrdeutigkeitsfrei — keine Verwechslung
zwischen `0`/`O` und `1`/`I`/`l`.

### 14.2 · Fraktionswahl

Die Auswahloberfläche für Allianz und Legion. Die Waffenidentität dahinter
kommt aus [Sprint 13B](13B_Sprint_Einheitenverhalten.md) — ohne sie ist die
Wahl kosmetisch, deshalb hängt die Abnahme dieses Pakets an B1.

Bis B1 gemergt ist, ist die Wahl trotzdem baubar: Sie schreibt in das bereits
vorhandene `factionPerSlot` aus `MatchConfig`.

### 14.3 · Bereitschaft und Start

Beide Seiten bestätigen. Erst dann verbindet sich der Client zum Relay. Das
räumt die häufigste Fehlerursache aus Sprint 13 ab — dass einer wartet,
während der andere noch im Menü steht.

### 14.4 · Fingerprint-Abgleich vor dem Verbinden

Der Relay lehnt ungleiche Builds ab (A4) — richtig, aber spät. Die Lobby
vergleicht den Build-Commit schon beim Beitreten und sagt es im Klartext:
„Ihr habt unterschiedliche Versionen, hol dir Build `<commit>`."

Das ist die Stelle, an der die Rebuild-Kadenz aus dem
[Parallelbetrieb](13-15_Parallelbetrieb.md) für Spieler sichtbar wird —
und der Grund, warum sie nicht wehtut.

### 14.5 · Match-Token statt geteiltem Geheimnis

Der Relay verlangt seit A6 ein Match-Token. Bisher ist das ein von Hand
abgestimmter Wert. Die Lobby vergibt es pro Match und übergibt es beiden
Clients — der Token wird damit kurzlebig statt dauerhaft.

## Schreibhoheit

| Pfad | |
|---|---|
| `Scripts/Gameplay/UI/`, `Scripts/Gameplay/Input/` | 14.1–14.4 |
| `Scripts/Gameplay/Match/` | `MatchConfig`-Befüllung aus der Lobby |
| neuer Lobby-Client (Ort in 14.1 zu entscheiden) | |
| `tools/Nova.RelayServer/` | nur 14.5, Token-Vergabe |
| Supabase-Projekt (ausserhalb des Repos) | Schema, Policies |

**Keine Datei unter `Scripts/Simulation/` oder `Scripts/AI*`.**

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Accounts, Login, Profile | ein Code reicht; Accounts bringen Nutzerdaten und damit Tier 3 |
| Matchmaking, Ranglisten, Chat | kein Publikum, das sie füllt |
| Reconnect | [Sprint 15](15_Sprint_Netzstabilitaet.md) |
| Mehr als zwei Spieler | der Relay kann zwei |
| Zuschauer | Replays lösen das besser |

## Risiken

| Risiko | Umgang |
|---|---|
| Nutzerdaten in Supabase lösen Tier 3 aus | keine personenbezogenen Daten; Match-Code und Build-Commit sind keine Nutzerdaten. Vor dem ersten Feld, das es wäre, D-ID |
| Service-Rollen-Schlüssel landet im Client | nur der öffentliche Schlüssel geht in den Build; Row-Level-Security ist Pflicht, nicht Kür |
| Lobby fällt aus, Spiel wird unspielbar | Direktverbindung aus Sprint 13 bleibt als Weg erhalten und wird nicht entfernt |
| 14.2 hängt an B1 | Oberfläche baubar ohne B1, Abnahme erst mit B1 |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Zwei Menschen, die sich nicht absprechen, finden über einen Code zueinander
   und spielen eine Partie — ohne dass jemand eine IP-Adresse eintippt.
3. Ungleiche Builds werden in der Lobby erkannt und im Klartext erklärt.
4. Der direkte Weg aus Sprint 13 funktioniert weiterhin.
5. Notiert im [GrayboxLog](../GrayboxLog.md).

## Changelog-Notiz

Lobby über Supabase: Match per Code anlegen und beitreten, Fraktionswahl,
Bereitschaft, Build-Abgleich vor dem Verbinden, kurzlebige Match-Token.

## Versionsrelevanz

`minor`.
