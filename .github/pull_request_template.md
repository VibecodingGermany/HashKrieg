<!-- Danke für deinen Beitrag zu Project Nova! Bitte fülle die Punkte kurz aus. -->

## Was & Warum
<!-- Was ändert dieser PR und warum? 1–3 Sätze. -->

## Betroffene Dokumente / Bereiche
<!-- z. B. docs/tech/Pathfinding.md, docs/gamedesign/Economy.md -->

## Entscheidungen
<!-- Neue/geänderte Entscheidungen als D-IDs, falls zutreffend. Sonst "keine". -->

## Checkliste
- [ ] Änderungen folgen dem [Dokumentationsstandard](../docs/meta/DocumentationStandard.md) (Kopfzeile, Pflichtabschnitte, `Änderungsverlauf`)
- [ ] `Änderungsverlauf` + Version im/in den betroffenen Dokument(en) aktualisiert
- [ ] Bei Struktur-Änderung: [docs/README.md](../docs/README.md)-Index nachgezogen
- [ ] Eintrag unter `[Unreleased]` in [CHANGELOG.md](../CHANGELOG.md) ergänzt
- [ ] Entscheidungen (falls) mit ≥3 Alternativen im [DecisionLog](../docs/production/DecisionLog.md)
- [ ] Conventional-Commit-Titel · CI `docs-check` grün · `quality-gate` nach G0 grün
- [ ] Kein Gate-Status aus Schema 1.2 (dauerhaft integrity-only); aktuell endet jeder Pass-Versuch zusätzlich mit `E_AUTHORIZATION_BOOTSTRAP`; Docs-only-Scope explizit
- [ ] Trust-Bundle-Änderung ohne Gate-Fortschritt; Gate-Evidence erst an einem nachfolgenden sauberen Subject
- [ ] Gate-Autorisierung: D-066-Receipt-Vertrag ist separat implementiert und real belegt (G0-A1 allein autorisiert keinen Pass)
- [ ] Performance-Command und -Messung verwenden dieselbe `environmentId`; Windows-x64-/Mac-M2-Methoden sind getrennt
- [ ] Kriterien an kanonische Check-Artefakte gebunden; Performance-Schwellen je 120-s-Lauf und kombiniert ausgewertet

<!-- Merge nach main erfolgt ausschließlich per PR mit grüner CI und Review. Details: CONTRIBUTING.md / AGENTS.md -->
