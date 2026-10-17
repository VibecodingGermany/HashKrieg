---
issue: 56
paket: P-56
branch: docs/56-medic-planning
agent: Builder (Sonnet 5.5)
datum: 2026-10-17
status: DONE (nicht committet, siehe Auftrag)
---

# Issue #56 – Sanitäter in die Planung aufnehmen (nur Doku)

## Aufgabe
Sanitäter als Planung festhalten und gegen die passive Reparaturzone (#55, c1a5ebf) abgrenzen; veraltete Aussagen berichtigen. Code geprüft: `ConstructionSystem.cs` (`PassiveRepairRateHpPerTick`, `PassiveRepairRadiusCells`, `ProcessPassiveRepairZones`: kostenlos, nur eigene Einheiten der produzierbaren Rollen, erste passende Zone gewinnt, bei Low Power nur gerade Ticks) und `SimClock.TicksPerSecond`. Im Text stehen nur Konstantennamen (D-047).

## Files Changed
- docs/gamedesign/Infantry.md – 0.5.0 → 0.6.0; neuer Abschnitt „Sanitäter“ vor den Offenen Punkten; Verlaufszeile
- docs/gamedesign/ArmorSystem.md – 0.5.0 → 0.6.0; Reparaturabschnitt (Einleitung, Repair-Fahrzeug-Zeile, Infanterie-Zeile) berichtigt; Verlaufszeile
- docs/gamedesign/Economy.md – 0.7.0 → 0.8.0; Zeile „Passive Produktionszone“ (0 AE) in der Reparaturtabelle; Verlaufszeile
- docs/gamedesign/Buildings.md – 0.9.0 → 0.10.0; §7 zwei Verweiszeilen; Verlaufszeile
- docs/production/OpenQuestions.md – 1.12.2 → 1.12.3; Q-048 auf #56 gekürzt, #55 als erledigt (c1a5ebf); Verlaufszeile
- CHANGELOG.md – eine Zeile unter [Unreleased] / Geändert, „Refs #56“

## Prüfungen
- `python3 .github/scripts/check_docs.py` – OK: 205 Markdown-Dateien, 5 Quality-JSONs geprüft, keine toten Links (nur Hinweise zu fehlenden Kopfzeilen in unberührten Dateien).
- `git status` – nur die sechs genannten Dateien plus dieser Bericht geändert.
- `dotnet test` nicht ausgeführt: reines Doku-Paket, kein Code berührt.

## Nebenbefunde
- Der Code-Docstring nennt die Zone „E-3“; dieses Label wurde bewusst nicht übernommen (Verweis stattdessen auf #55/c1a5ebf).
- ArmorSystem.md nennt den Medic-Richtwert 3 %/s; nach Code heilt die Zone 90-HP-Infanterie schneller, der Richtwert ist also überholt (jetzt so vermerkt).
- Die Quellen-Zeilennummern der Spezifikation (a6858d1) wurden nachgeprüft; die Stellen stimmten inhaltlich.
- Die Zone gilt auch für die Legion; das berührt D-027.4 nicht formal (keine Einheit), ist aber im Text als Beobachtungsfrage vermerkt.

## Offene Fragen an den Inhaber
1. Soll der Stapelregel-Vorschlag („je Ziel nur die stärkste Quelle“) als Richtung gelten, oder bleibt er offen bis zur gespielten Runde?
2. Soll die Legion trotz Zone ohne Feldheilung bleiben (D-027.4 unverändert), oder wird das nach der Runde neu bewertet?
3. #56 bleibt per „Refs #56“ offen (wie vom Inhaber gewünscht).

## Vorschlag Commit-Titel
`docs(gamedesign): plan the medic and fence it off from the repair zone`

## Vorschlag PR-Beschreibung
Was: Abschnitt „Sanitäter“ in Infantry.md (MS-1-Stand vs. Vollspielziel, Abgrenzung zur passiven Reparaturzone aus #55, Stapelregel, Umsetzungsform, D-027.4, Namensfrage ohne Vorgriff auf #15); Berichtigungen in ArmorSystem.md, Economy.md, Buildings.md §7; Q-048 gekürzt.
Warum: Die Zone (c1a5ebf) widersprach den Designdokumenten; der Betatest-Wunsch war nur Planung.
Bereich: reine Doku, kein Code, keine neue Einheit, keine Zahlen dupliziert (D-047).
Changelog: [Unreleased] / Geändert. Tests: check_docs.py grün; dotnet test nicht nötig.
Refs #56
