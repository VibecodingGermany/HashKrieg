# Vorschlag zur Sprintbildung: die Folgen der AE-Verknappung

**Status:** Vorschlag zur Sprintbildung — **kein Sprint, keine Nummer, keine Zusage** | **Quelle:** [Testbericht T-01 vom 10.08.2026](Testberichte/2026-08-10_4053c15_T-01.md), Build `4053c15` | **Issues:** #85–#94 | **Ablauf:** [Nutzerfeedback_Ablauf.md](../Nutzerfeedback_Ablauf.md) | **Leitsatz:** endliche Felder sind kein Wert, sondern ein Systemwechsel

## Worum es geht

Mit [#80](https://github.com/VibecodingGermany/HashKrieg/pull/80) bekamen die
Aetherium-Felder eine endliche Reserve. Der Testbericht vom 10.08.2026 zeigt,
dass diese eine Änderung acht weitere Systeme betrifft, die noch auf der alten
Annahme stehen. Der Tester formuliert es selbst so:

> „Die Einführung endlicher Ressourcen ist wesentlich größer als nur: ‚AE-Spawns
> haben jetzt einen Maximalwert.'"

Zehn Befunde, alle neu, keiner ein Duplikat. Drei liegen nahe an bestehenden
Issues (#50, #52, #12); der Unterschied ist jeweils im Issue benannt.

## Die Befunde nach Schreibhoheit

Der Schnitt läuft über die Schreibhoheit, nicht über das Thema — sonst hebt ein
thematisch schlüssiges Paket die Trennung auf, die den Parallelbetrieb trägt
([13-15_Parallelbetrieb.md](13-15_Parallelbetrieb.md)).

### A · Einheitenstrang (Sprint 13B, externer Beitragende)

| Issue | Befund | Priorität |
|---|---|---|
| #85 | KI erntet nach Erschöpfung endlos auf dem leeren Feld weiter | **kritisch** |

`Assets/_Project/Scripts/AI/` — exklusive Schreibhoheit des Einheitenstrangs.
Deshalb als Issue gemeldet und **nicht** von Maintainer-Seite gefixt.

Der Befund ist kein Strategiemangel, sondern ein Livelock aus einer fehlenden
`IsExhausted`-Prüfung in `TryGetOwnFieldCell`. Das Minimum, das ihn beendet, ist
klein und in einem PR machbar. Die strategische Anforderung aus Abschnitt 8 des
Berichts (Erschöpfung prognostizieren, Felder sichern und bestreiten, Eskorten)
ist davon getrennt zu schneiden — wie, entscheidet der Stranginhaber im Rahmen
von Paket B4.

### B · Maintainer-Strang, Wirtschaft und Oberfläche

| Issue | Befund | Priorität |
|---|---|---|
| #86 | AE-Vorkommen zeigen ihren Restbestand nicht | **kritisch** |
| #87 | Startvorkommen: 9.000 AE gemessen, 10.000 gewünscht | hoch |
| #88 | Mehrfachauswahl: Befehle vom Anführer statt von der Schnittmenge | hoch |
| #91 | Baubereich ist unsichtbar | hoch |
| #93 | Kartendichte: fünf Felder auf 128×128 | hoch |

Alles in `Simulation/Economy/`, `Gameplay/Match/` und `Presentation/` — keine
Berührung mit dem Einheitenstrang, parallel zu A lauffähig.

### C · Vertragsflächen — bewusst keinem Strang zugeschlagen

| Issue | Befund | Naht |
|---|---|---|
| #89 | Patrouille-Befehl | Register/Payload ↔ Bewegungsverhalten |
| #90 | „Bewachen"-Befehl für Eskorten | Register/Payload ↔ Begleit-/Reaktionsverhalten |
| #94 | Zentrale AE-Zone mit Chokepoints | Kartenlage ↔ `CostField`/`IsWalkable` |

Beide Befehle brauchen einen Eintrag im **eingefrorenen** `CommandKind`-Register
(Schema v1, heute 1–17), eine neue Payload und Zustand pro Einheit im Snapshot.
Das ist ein API-/Schema-Vorgang: @api-guardian ist Pflicht, und die
Versionsrelevanz ist eher `major` als `minor`. Wer sie einem Strang still
zuschlägt, bricht entweder die Hoheit oder das Schema.

### D · Inhaberentscheidungen

| Issue | Frage |
|---|---|
| #92 | Wie wächst Territorium — zweites HQ, jedes Gebäude, etwas anderes? |
| #94 | Wird die Kartenmitte ein Gebiet mit Chokepoints? |

#92 ist die dringendere der beiden. Der Code beantwortet die Frage heute schon,
nur hat es niemand ausgesprochen: `BuildInfluenceRadiusCells` misst ab **jedem
eigenen Bauanker**, nicht ab dem HQ — die Bauzone kriecht also mit jedem Gebäude
mit. Ob das gewollt ist, ist eine Festlegung, keine Implementierung.

## Reihenfolge — nur wo eine Abhängigkeit besteht

```
#92 (Territorium entscheiden)
  └─> #91 (Baubereichs-Overlay)      ein Overlay auf eine Regel, die sich
  └─> #93 (Kartendichte)             danach ändert, ist doppelte Arbeit
         └─> #94 (zentrale Zone)     erst die Gesamtzahl, dann die Verteilung

#86 (Restbestand sichtbar)
  └─> #87 (Startmenge)               ohne Anzeige wird geschätzt statt gerechnet;
                                     der Tester lag um 4.000 AE daneben

#85 (KI-Livelock)
  └─> #93 (Kartendichte)             mehr Felder machen den Stillstand nur
                                     auffälliger, solange die KI eines nimmt
```

Ohne Abhängigkeit und jederzeit einzeln machbar: **#88**.

Zwei Beobachtungen zur Reihenfolge, die nicht im Diagramm stehen:

- **#85 und #86 sind beide als kritisch gemeldet, aber ungleich dringend.** #85
  macht den Skirmish nach einigen Minuten sinnlos — das ist der Grund, warum
  dieser Bericht überhaupt sofort bearbeitet wurde. #86 macht eine vorhandene
  Mechanik unlesbar. Beide zuerst, in dieser Reihenfolge.
- **#87 ist eine Zeile und trotzdem nicht trivial.** Die Frage dahinter — wie
  lange soll ein Startfeld tragen? — hängt an Ernterate, Harvester-Zahl und
  Baukosten und sollte einmal gerechnet werden.

## Was hier ausdrücklich nicht passiert

Kein Sprint wird festgeplant, keine Sprintdatei angelegt, keine Nummer vergeben,
keine bestehende Sprintdatei erweitert. Ob aus diesem Vorschlag ein Sprint wird,
ob mehrere zusammengelegt werden oder ob etwas entfällt, entscheidet der Inhaber.

## Nebenbefund für die Umsetzung

Die Feldlage steht **doppelt** im Repo: `Gameplay/Match/MatchBootstrap.cs:164`
und `tools/Nova.SimRunner/Determinism10000Scenario.cs:659`. Wer #87 oder #93
umsetzt und nur eine Stelle ändert, lässt das Determinismus-Szenario driften.
Die Verdopplung selbst wäre einen eigenen Aufräum-Issue wert.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-10 | Erstfassung aus Testbericht T-01, Build `4053c15` | Orchestrator |
