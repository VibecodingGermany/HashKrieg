# Sprint 15: Netzstabilität — wenn die Leitung wackelt, endet nicht die Partie

**Status:** geplant | **Vorgänger:** [14_Sprint_Lobby.md](14_Sprint_Lobby.md) | **Parallel zu:** [13B](13B_Sprint_Einheitenverhalten.md) | **Regelwerk:** [13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md) | **Leitsatz:** ein Abbruch nach vierzig Minuten ist schlimmer als kein Netzwerkmodus

## Ziel

Nach Sprint 14 ist eine Partie bequem startbar. Sie ist aber immer noch
zerbrechlich: Jeder Verbindungsabriss beendet sie endgültig, ein Desync sagt
nur, *dass* etwas auseinanderlief, und wer im falschen Moment das WLAN
wechselt, hat verloren.

Sprint 15 macht aus einem funktionierenden Netzwerkmodus einen belastbaren.

## Pakete

### 15.1 · Reconnect

Ein Client, der die Verbindung verliert, kann zurückkommen. Der Relay hält den
Slot eine begrenzte Zeit, der Gegner sieht „Verbindung unterbrochen — warte",
und der Rückkehrer holt über die `NOVAREC2`-Checkpoints auf.

Die Bausteine dafür existieren: der Relay schreibt bereits lückenlose
Tickframes mit 50-Tick-Checkpoints und terminalem Footer. Was fehlt, ist der
Weg zurück in eine laufende Session.

**Grenze:** Reconnect gilt für Verbindungsverlust, nicht für einen
abgestürzten Client. Ein neu gestartetes Spiel müsste den kompletten Zustand
nachladen — das ist ein eigenes Paket und nicht dieses.

### 15.2 · Desync-Forensik, die eine Antwort gibt

Heute endet ein Hash-Mismatch die Session und legt einen Spool ab. Was fehlt,
ist die Frage danach: *welches System* lief auseinander. Ziel ist ein
Erstbefund, der ein Subsystem benennt statt eines Ticks.

Das ist die Absicherung für den Parallelbetrieb: Wenn ein 13B-Merge
Determinismus bricht, wollen wir das in Minuten wissen, nicht in einer
Bisect-Sitzung.

### 15.3 · Eingabeverzögerung, die sich anpasst

`inputDelayTicks` steht fest auf 3 im Netzprofil (1–60 erlaubt). Über LAN ist
das zu viel, über eine schlechte Leitung zu wenig. Ziel ist eine Anpassung an
die gemessene Laufzeit — vorsichtig, mit Hysterese, und ohne den Determinismus
anzufassen: die Verzögerung ist Teil des Handshakes, nicht der Simulation.

### 15.4 · Der Rundenabschluss

Eine Partie endet heute technisch korrekt, aber ohne Abschluss. Ergebnisbild,
Dauer, Endzustand, und die Möglichkeit, die Aufzeichnung zu behalten.

### 15.5 · Der Relay überlebt Betrieb

Was ein Server braucht, um länger als eine Sitzung zu laufen: Logrotation,
Aufräumen alter `.novarec`-Dateien, ein Health-Endpunkt, Neustart ohne
Datenverlust. Das Runbook wird entsprechend fortgeschrieben.

## Schreibhoheit

| Pfad | |
|---|---|
| `Scripts/Networking/` | 15.1–15.3 |
| `Scripts/Gameplay/Match/`, `Scripts/Gameplay/UI/` | 15.1, 15.4 |
| `tools/Nova.RelayServer/` | 15.1, 15.5 |
| `docs/tech/RelayServer.md` | 15.5 |

**Keine Datei unter `Scripts/Simulation/` oder `Scripts/AI*`.** 15.2 liest die
Simulation aus, sie verändert sie nicht.

## Bewusst nicht in diesem Sprint

| | Warum |
|---|---|
| Wiedereinstieg nach Client-Absturz | braucht vollständigen Zustandstransfer, eigener Sprint |
| Rollback-Netcode | Lockstep ist die getroffene Entscheidung (D-089); Rollback wäre ein anderes Spiel |
| Mehr als zwei Slots | unverändert |
| Serverseitige Anti-Cheat-Prüfung | der Relay simuliert nicht, das ist Absicht |

## Risiken

| Risiko | Umgang |
|---|---|
| Reconnect öffnet einen Weg, den Zustand zu manipulieren | der Rückkehrer bekommt Checkpoints vom Relay, nicht vom Gegner; Fingerprint wird erneut geprüft |
| Adaptive Verzögerung wird selbst zur Desync-Quelle | Verzögerung ist Handshake-Zustand, nie Simulationseingabe; Änderung nur an Tickgrenzen mit beidseitiger Bestätigung |
| 15.2 findet nichts, weil der Fehler in 13B liegt | genau dafür ist es da — der Befund geht als Fund an 13B zurück |
| Slot-Vorhalten blockiert den Relay | harte Obergrenze, danach wird der Slot freigegeben |

## Fertig wenn

1. `dotnet test tools/Nova.SimRunner.Tests` grün.
2. Eine Partie überlebt einen absichtlich herbeigeführten Verbindungsabriss
   auf einer Seite und läuft bis zum Siegzustand weiter.
3. Ein künstlich erzeugter Desync liefert einen Erstbefund, der ein Subsystem
   benennt.
4. Der Relay läuft über mehrere Partien hinweg ohne Neustart und ohne
   volllaufendes Dateisystem.
5. Notiert im [GrayboxLog](../GrayboxLog.md).

## Changelog-Notiz

Netzstabilität: Reconnect nach Verbindungsverlust, Desync-Erstbefund mit
Subsystem-Zuordnung, adaptive Eingabeverzögerung, Rundenabschluss und
Dauerbetrieb des Relays.

## Versionsrelevanz

`minor`.

## Danach

Sprint 16 nimmt **Strang C aus Sprint 12** auf (Knappheit, Lager, Radar, Low
Power, Bauvoraussetzungs-Kette, Platzierungsregeln). Er ist
simulationsverändernd und läuft deshalb erst, wenn der Netzstrang steht — und
in Abstimmung mit [13B](13B_Sprint_Einheitenverhalten.md), weil dann beide
Stränge dieselbe Simulation bewegen.
