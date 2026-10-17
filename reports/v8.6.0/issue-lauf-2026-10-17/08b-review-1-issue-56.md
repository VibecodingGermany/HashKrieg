---
issue: 56
paket: P-56
branch: docs/56-medic-planning
basis: 08b1a5e (gestapelt auf feat/67-defence-offline-low-power)
runde: 1
agent: Reviewer (read-only, Opus 5.5)
datum: 2026-10-17
verdict: APPROVED
---

# Review Runde 1 – Issue #56 (Sanitäter-Planung)

## Ergebnis

**APPROVED.** Kein Befund der Schwere hoch. Sachaussagen zur Reparaturzone
stimmen mit dem Code überein, der Schreibumfang ist eingehalten, der Doku-Check
ist grün. Drei kleinere Unschärfen (unten) können vor dem Commit oder in einer
Folgerunde nachgezogen werden.

## Was geprüft wurde (und was gelaufen ist)

| Prüfung | Ergebnis |
|---|---|
| `git status --short` / `git diff` vollständig gelesen | nur CHANGELOG.md, ArmorSystem.md, Buildings.md, Economy.md, Infantry.md, OpenQuestions.md geändert, Bericht 08 untracked – deckungsgleich mit dem Schreibumfang; HEAD 08b1a5e (#67) unberührt |
| `python3 .github/scripts/check_docs.py` (selbst ausgeführt) | Exit 0, „OK: 206 Markdown-Dateien und 5 Quality-JSONs geprüft“, keine toten Links; nur Hinweise zu fehlenden Kopfzeilen in unberührten Dateien. Builder meldete 205 – Differenz erklärt sich plausibel durch den danach angelegten Bericht 08 |
| Code-Abgleich `ConstructionSystem.cs` (`ProcessPassiveRepairZones`, Z. ~1212–1348) und `SimClock.cs` | `PassiveRepairRateHpPerTick = 1`, `PassiveRepairRadiusCells = 3`, `TicksPerSecond = 10`; kostenlos, nur eigene Einheiten, nur die vom Gebäude produzierbaren Rollen, erste passende Zone gewinnt (keine Stapelung), unter Low Power nur gerade Ticks. Alle Aussagen in ArmorSystem/Economy/Buildings/Infantry dazu korrekt |
| Rechnung „3 %/s liegt unter der Zone“ | 3 % von 90 HP = 2,7 HP/s gegen 10 HP/s (Low Power 5 HP/s) – korrekt |
| Commit `c1a5ebf` | existiert: „feat(construction): passive Reparaturzone an Fahrzeugfabrik und Kaserne (#55) (#140)“ |
| D-027.4 / D-027.6 / D-056 / D-047 im DecisionLog | Text gibt D-027.4 (Legion ohne Infanterie-Heiler) und D-027.6 (Heilschwarm stapelt nicht auf Regeneration) sinngemäß richtig wieder; D-056 Punkt 6 schließt generische Fähigkeiten/Status/Kanäle/Auren aus – korrekt zitiert. Kein Widerspruch |
| Kopfversionen und Verlaufszeilen | alle fünf Doku-Dateien gehoben (Infantry 0.6.0, ArmorSystem 0.6.0, Buildings 0.10.0, Economy 0.8.0, OpenQuestions 1.12.3), je eine Verlaufszeile, Autor „Orchestrator“ |
| CHANGELOG | eine Zeile unter `[Unreleased]` / „Geändert“, „Refs #56“ vorhanden |
| D-ID / künftige D-Nummer | keine neue D-ID, keine konkrete künftige D-Nummer genannt |
| Link `../production/hashkrieg/06_Narrative.md` | Datei existiert |
| `dotnet test` | nicht ausgeführt – reines Doku-Paket, kein Code berührt |

## Befunde

1. **mittel – Infantry.md, Tabelle „MS-1-Stand gegen Vollspielziel“, Zeile
   „Heilung eigener Infanterie“:** nennt „Passive Reparaturzone an Kaserne und
   Fahrzeugfabrik“. Nach Code (E-3-Abbildung über `ProducerRole`) heilt die
   Fahrzeugfabrik keine Infanterie, nur die Kaserne. Vorschlag: „Passive
   Reparaturzone an der Kaserne“ (Fahrzeugfabrik steht korrekt in der
   Fahrzeugzeile). Kein Blocker, aber eine schiefe Aussage gegen den Code.
2. **niedrig – Infantry.md, gleiche Zeile:** „nur zwischen Gefechten“ ist die
   Designabsicht, nicht das Verhalten: die Zone heilt auch unter Beschuss, sie
   verliert nur gegen jede Waffe. Vorschlag: „heilt auch im Gefecht, ist aber
   auf Erholung zwischen Gefechten ausgelegt“.
3. **niedrig – Infantry.md, Abschnitt Abgrenzung:** „für 90-HP-Infanterie“
   nennt einen Wert, den `SimDefinitions` besitzt; das steht leicht quer zur
   Selbstauskunft „keine Zahlen dupliziert“ in CHANGELOG und Bericht. Vorschlag:
   „für MS-1-Infanterie (Max-HP aus `SimDefinitions`)“.

## Nebenbefunde (außerhalb des Diffs)

- Economy.md, Zeile „Reparatur (Fahrzeuge)“ (unverändert) sagt, bis zur
  Umsetzung der Repair-Drohne gelte der 30-%-Startwert. In MS-1 repariert der
  Builder nur Gebäude; Fahrzeuge heilt ausschließlich die kostenlose
  Fabrik-Zone. Die neue Zeile darunter stellt das richtig, die alte bleibt
  missverständlich – Kandidat für eine spätere Bereinigung.

## Ehrlichkeit des Builder-Berichts

Ehrlich. Was als gelaufen gemeldet ist (check_docs.py, git status), ist
nachvollziehbar gelaufen; `dotnet test` ist offen als nicht ausgeführt
benannt. Einzige Abweichung: die Behauptung „keine Zahlen dupliziert“
übersieht die 90 HP (Befund 3) – keine Täuschung, eine Unschärfe.

## Commit-Titel

`docs(gamedesign): plan the medic and fence it off from the repair zone` –
Conventional Commit, englisch, ≤ 72 Zeichen. Passt.
