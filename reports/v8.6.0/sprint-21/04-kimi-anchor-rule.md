---
Bericht: 04-kimi-anchor-rule (Sprint 21)
Worker: Kimi K3 (cc-Backend, --mode rw), 2026-08-18
Auftraggeber: Orchestrator
---

> Hinweis: Der Anfang der Worker-Ausgabe wurde beim Einsammeln abgeschnitten
> (`| tail -30` beim Dispatch, Fehler des Orchestrators). Erhalten sind die
> Entscheidungen, das Testergebnis und das Verdikt.

2. **`Assets/Tests/EditMode/Simulation/ConstructionSystemTests.cs`** — neuer Test `ValidatePlacement_EveryCompletedBuildingExtendsTheZone_ItsSiteDoesNot`.
3. **`tools/Nova.SimRunner.Tests/ConstructionSystemTests.cs`** — derselbe neue Test (gespiegelte .NET-Lane).

**Neue Regel in einem Satz:** Jedes eigene, lebende, fertiggestellte Gebäude ist ein Bauanker und schiebt die Bauzone um seinen eigenen Radius (Footprint-Distanz ≤ 8) weiter — Baustellen niemals.

## Testergebnisse

- **Vor der Änderung (Baseline, via `git stash` hergestellt):** 725/725 grün.
- **Nach der Änderung:** **726/726 grün** — keine roten Tests, die Liste ist leer. Es gab also nichts einzuordnen (keine Baseline-Bewegung, kein echter Fehler).
- **Diskriminierungsnachweis:** Der neue Test wurde einmal gegen die *alte* Produktionsregel laufen gelassen (Produktionsdatei allein gestasht, Tests drin gelassen) und schlägt dort exakt fehl: `Expected: Applied / But was: RejectedInvalidTarget` — er pinnt also genau die Regeländerung, nichts Beliebiges.

## Einordnung einzelner erwarteter, aber ausgebliebener Rottöne

- **`CanonicalAiOutcomeTests`** (pinnt Tick 2726 / `0x10B83E94F86F2E55`): **blieb grün.** Die kanonische KI-Partie versucht nie eine Platzierung, deren Validierungsausgang unter der neuen Regel kippt — der gepinnte Ausgang bewegt sich nicht. Die im Auftrag erwartete Bewegung trat nicht ein; ein Baseline-Neusetzungs-PR ist nach dieser Suite nicht nötig.
- **`RulesHash64`: hat sich NICHT bewegt.** Der Hash wird in `MatchFingerprint.ComputeRulesHash64` ausschließlich aus numerischen Regelkonstanten gebaut (u. a. `BuildInfluenceRadiusCells`); die Anker-Rollenliste war nie ein gehashtes Feld. Da `Simulation/Replays/` für mich eingefroren ist, habe ich keine Revision angefasst. Falls die Regelidentität die Ankerregel künftig abbilden soll, braucht es eine `RulesRevisionV4` mit neuem Hash-Feld — das ist eine Entscheidung außerhalb meines Schreibbereichs und ich melde sie hiermit zurück.
- **`BuildZoneCapacityTests`** (pinnt 15 Kraftwerke): grün — der Scan ist auf die HQ-Startzone begrenzt und Power war bereits Ankerrolle; identische Platzierungsmenge.
- **Bestehende Tests angepasst:** keiner. Kein bestehender Test schrieb die alte Dreierliste fest (der Einfluss-Test `..._AtDistanceEight` pinnt nur own/living/completed/Distanz-8 sowie Feind/Tot/Baustelle — alles weiterhin wahr). Die Dreierliste stand nur in den Docstrings. Neu hinzugekommen ist ein Verhaltenstest (Kategorie „echte Verhaltensannahme", kein Hash-Pin).

## Was ich nicht prüfen konnte

- Die Unity-EditMode-Lane läuft in dieser Umgebung nicht (kein Unity); die gespiegelte Datei wurde textidentisch synchronisiert — Unity-CI muss sie bestätigen.
- Der `IsActiveSite`-Guard in `IsInsideBuildInfluence` ist strukturell defensiv: `_buildings` enthält im Normalfluss nur fertige Placements (`CompleteSite` deaktiviert die Baustelle vor dem Anlegen des Placements), die Prüfung kann derzeit nicht feuern — sie dokumentiert die Regel explizit, wie beauftragt.
- Ob der Auftraggeber `RulesHash64` trotz unveränderter Konstanten bewegt haben will (s. o.) — das erfordert einen Eingriff in eingefrorenen Bereich.

STATUS: DONE
- Regel umgesetzt: jedes eigene, lebende, fertiggestellte Gebäude ist Bauanker; Baustellen explizit ausgeschlossen (`IsActiveSite`); Docstrings auf den geltenden Stand.
- 726/726 Tests grün (Baseline vorher 725/725); null rote Tests — die erwartete Baseline-/Pin-Bewegung trat nicht ein (KI-Ausgang und RulesHash64 unverändert, da konstantenbasiert gehasht).
- Neuer Test `ValidatePlacement_EveryCompletedBuildingExtendsTheZone_ItsSiteDoesNot` in beiden Lanes; gegen die alte Regel nachweislich rot (Applied vs. RejectedInvalidTarget).
- Keine Baseline-, Pin-, CHANGELOG-, docs- oder Frozen-Area-Datei angefasst; Rückfrage an den Orchestrator: RulesHash64-Revision (V4) für die Ankerregel gewünscht?
/Users/denniswestermann/.claude/skills/kimi/scripts/kimi-agent.sh: line 270: hen: command not found

[exited with code 0]
