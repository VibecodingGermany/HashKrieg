# BUILD/MODIFY-Backlog (Eigenbau-Assets)

**Version:** 1.1.0 | **Status:** sprint-freigegeben – MS-1 rebaselined | **Verantwortungsbereich:** Art Director / Technical Artist | **Sprint:** 5

## Zweck

Bündelt alle im [AssetRegister.md](AssetRegister.md) als **BUILD** oder aufwändig **MODIFY** klassifizierten Assets zu einem priorisierten Eigenbau-Backlog mit Begründung und grober Aufwandsschätzung (Person-Tage). Dies ist die **Kern-Eingabe für die Produktionsplanung (Sprint 6)** und für die Aufwandsschätzung des Zeit-/Kapazitätsrisikos ([../production/RiskAnalysis.md](../production/RiskAnalysis.md), R-16). Es macht sichtbar, dass der reale Produktionsaufwand nicht im Kaufpreis, sondern im Eigenbau der Signature- und Evolvierten-Assets liegt.

## Abhängigkeiten

- [AssetRegister.md](AssetRegister.md) – Klassifikation je Kategorie
- [ProcurementStrategy.md](ProcurementStrategy.md) – BUILD-Definition (§3), Repo-Hygiene
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Polycount-/Textur-/LOD-/VFX-Budgets als Bauvorgabe
- Führende GDD-Quellen: [../gamedesign/Resources.md](../gamedesign/Resources.md) (Aetherium), [../gamedesign/Factions.md](../gamedesign/Factions.md) (Evolvierte), [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md)

## 1. Warum diese Assets nicht käuflich sind

| Grund | Betroffen | Beleg |
|---|---|---|
| **Signature/USP** – funktional einzigartig (Nachwachsen, Ausbreitung, Verseuchung) | Aetherium-Feld + Shader + Wachstums-VFX | D-010, [../gamedesign/Resources.md](../gamedesign/Resources.md) |
| **Biologisch-organisch** – im Store (Synty-Militär) nicht abgedeckt | gesamte Evolvierten-Fraktion | [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) §2/§4 |
| **Gameplay-verzahnt** – kein generisches Kauf-Asset passt | RTS-UI-Layout (Command-Card/Minimap) | [AssetRegister.md](AssetRegister.md) §3.12 |
| **Identität** – muss unverwechselbar sein | Commander-Art/-VO, Logos/Banner/Ladebildschirme | D-009, TPD §7.2 |

## 2. Backlog (priorisiert)

Priorität: **P0** = für MS-1 beziehungsweise den G0–G5-Pfad zwingend;
**P1/P2** = Post-MVP. Aufwand grob und **vor G4-Kalibrierung** (siehe Offene
Punkte).

| # | Asset-Paket | Umfang | Prio | Aufwand (grob) | Bauvorgabe |
|---|---|---|---|---|---|
| B-01 | **Aetherium-Feld (Signature-Asset)** | Mutterkristall + Ausläufer-Stufen + verseuchtes Terrain-Overlay | **P0** | 4–6 PT | Kristall ≤1.000 Tris LOD0 ([AssetBudget.md](../tech/AssetBudget.md) §1) |
| B-02 | **Aetherium-Shader** | Glow, Wachstumsstufen, Überernte-/Verseuchungs-Zustände | **P0** | 3–5 PT | URP-Shader-Graph, eigener Material-Standard |
| B-03 | **Aetherium-VFX** | Wachstum, Ernte, Verseuchungs-Ausbreitung | P1 | 2–3 PT | [AssetBudget.md](../tech/AssetBudget.md) §4-Budgets |
| B-04 | **Evolvierte Infanterie** | 8 Typen (Mutant … Alpha-Mutant) inkl. Rig + Retarget | **P1** | 10–16 PT | ≤4.000 Tris LOD0, Humanoid-Rig wo möglich |
| B-05 | **Evolvierte Fahrzeuge** | 12 biologische Entsprechungen | P1 | 18–30 PT | rig-los, ≤8–15k Tris je Klasse |
| B-06 | **Evolvierte Luftbrut** | 7 Typen | P2 | 11–18 PT | ≤10.000 Tris LOD0 |
| B-07 | **Evolvierte Gebäude** | 12 organische Strukturen inkl. Bau-/Trümmerstufen | P1 | 18–36 PT | ≤20.000 Tris LOD0, Wachstums-Bauzustand |
| B-08 | **Evolvierte Drohnen/Keime** | 3 Typen | P2 | 1,5–3 PT | strenges Instanz-Budget |
| B-09 | **Alpha-Mutant (Elite)** | 1 Hero-Einheit | P1 | 3 PT | Ausnahmebudget (2× 2048²) |
| B-10 | **RTS-UI-Layout** | Command-Card, Ressourcenleiste, Minimap, Gruppenporträts | **P0** | 4–6 PT | uGUI/UI Toolkit; Icons zugekauft |
| B-11 | **Fraktions-Signaturen** | 3× Logo/Banner/Farbpalette/Ladebildschirm | P1 | 3–5 PT | Teamfarben-Masken-Standard |
| B-12 | **Commander-Art + VO** | 3 Portraits/Key Art + Voice-Sets (~42–55 Lines Post-MVP) | P1 | Art 3–5 PT + VO-Auftrag | [CommanderSystem.md](../gamedesign/CommanderSystem.md) |
| B-13 | **Superwaffen-Signatur-Aufbau** | 3 Gebäude-Kitbash (Ionenstrahl/Thermobar/Kristallsturm) | P2 | 6–9 PT | ≤35.000 Tris ([AssetBudget.md](../tech/AssetBudget.md) §1) |
| B-14 | **MODIFY-Sammelposten** | Teamfarben-/LOD-/Material-Pässe für alle gekauften Allianz/Legion-Einheiten (~50 Typen) | P0–P1 | 0,25–1 PT je Typ | Material-Standard, LOD-Ketten |

**Grobsumme Eigenbau (P0–P2, ohne VO-Auftrag):** Größenordnung **~110–180 Person-Tage** – dominiert von der kompletten Evolvierten-Fraktion (B-04–B-08 + B-09) und den Aetherium-Signatur-Assets (B-01–B-03). Diese Zahl ist die zentrale Realität für die Kapazitäts-/Zeitplanung (R-16) und **muss** in Sprint 6 in die Roadmap einfließen.

## 3. Kritischer Pfad für MS-1

Der MS-1-Pfad braucht zuerst **B-01/B-02** (Aetherium als
Signature-Referenz-Frame), **B-10** (bedienbares RTS-UI) und die für die
Manifesttypen nötigen Teile von **B-14**. D-056 hat den Scope verbindlich auf
Allianz und Legion begrenzt; B-04–B-09 sind Post-MVP und kein G0–G5-Kriterium.
Der exakte Umfang steht in
[MVPContentManifest.md](../production/MVPContentManifest.md).

## Offene Punkte

- **Alle Person-Tage sind grobe Vorab-Schätzungen** ohne kalibrierten Referenz-Frame. Nach dem Phase-0-Bau von B-01 (Aetherium-Signature) sind sie in v1.1.0 nachzujustieren.
- **Post-MVP-Priorisierung:** Die Reihenfolge von B-04–B-09 wird erst nach G5
  mit einer neuen Scope-Entscheidung festgelegt.
- **VO-Aufwand** hängt am Composer-/Sprecher-Vertrag ([Licenses.md](Licenses.md), Offene Punkte).

## Nächste Schritte

1. Ab G4 nur die P0-Anteile gegen das MS-1-Manifest integrieren.
2. B-01/B-02 zuerst bauen und als Signature-Referenz vermessen.
3. Post-MVP-Backlog und Aufwand erst nach G5 neu entscheiden.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-22 | Erstfassung Sprint 5: 14 Eigenbau-Pakete priorisiert, Grobsumme ~110–180 PT, kritischer Phase-0-Pfad markiert | Art Director / Technical Artist |
| 1.1.0 | 2026-07-24 | Fraktions-Scope gemäß D-056 geschlossen und P0-Pfad auf das MS-1-Manifest begrenzt | Art Director / Technical Artist |
