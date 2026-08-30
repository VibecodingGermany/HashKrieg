# Beschaffungsstrategie & Audit-Methodik

**Version:** 1.1.0 | **Status:** sprint-freigegeben | **Verantwortungsbereich:** Producer / Lead Environment Artist / Project Owner | **Sprint:** 5

## Zweck

Legt die verbindliche Beschaffungsstrategie für alle Assets von *Hashkrieg* fest und definiert die **Audit-Methodik**, nach der jedes benötigte Asset im [AssetRegister.md](AssetRegister.md) klassifiziert wird (**BUY / MODIFY / BUILD**). Dieses Dokument ratifiziert die in Sprint 1 vorbereitete Entscheidungsvorlage aus [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) als **D-053** und die verbindliche Inhaberentscheidung **D-054 (0 € Open-Source & KI-Asset-Pipeline)**. Es operationalisiert die Bewertungsdimensionen zu einer Prüf-Checkliste. Verbindlich für: Producer (Budget/Beschaffung), Lead Environment Artist, Technical Art Director, alle Mitwirkenden mit Asset-Erstellungsberechtigung.

## Abhängigkeiten

- [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) – Marktrecherche und Strategie-Vorlage (A/B/C), Kategorie-Analyse
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – technische Kauf-Prüfung (§6), Polycount-/Textur-/LOD-Budgets, Gesamtdeckel 1,8 GB
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-006 (URP), D-053 (Beschaffung B), D-054 (0 € Open-Source & KI-Pipeline), D-047 (ein Wert, eine Quelle)
- [../production/RiskAnalysis.md](../production/RiskAnalysis.md) – R-04 (visuelle Inkohärenz gekaufter Assets)
- [AssetRegister.md](AssetRegister.md), [Licenses.md](Licenses.md), [BuildBacklog.md](BuildBacklog.md) – nachgelagerte Sprint-5-Ergebnisdokumente

## 1. Ratifizierte Strategie (D-053 & D-054)

**Strategie B-Zero – 0 € Open-Source & KI-Asset-Pipeline (D-054).** Als rein organisches Open-Source-Projekt verfügt Hashkrieg über **0 € Budget** (Beschluss des Project Owners). Statt kommerzieller Asset-Store-Bundles stützt sich die Beschaffung auf:

- **Freie CC0 Public Domain Bibliotheken** ([Quaternius](https://quaternius.com/) mit Modular Sci-Fi Megakit & Riggings, [Kenney.nl](https://kenney.nl/), Poly Pizza, OpenGameArt) als Basis für Gebäude, Fahrzeuge und Terrain-Props.
- **KI-gestützte 3D-Mesh-Generierung (Open Source / Free Tools)** wie **Hunyuan3D** (Tencent / Open Source), **Meshy** und **Tripo3D** für schnelles Prototyping, Blockouts und organische Strukturen.
- **KI-Textur- & Icon-Generierung** (Stable Diffusion, Texture Lab, UI-Icon-Kits) für nahtlose Sci-Fi-Materialien, Aetherium-Karten und RTS-Command-Icons.
- **Blender-KI-Addons & MCP Server Workflows:** Einbindung von KI-Prompts direkt in den Blender-Workflow unserer Community-Artists und optionale Anbindung via MCP-Server für automatisierte Asset-Entwürfe.
- **Sonniss GDC Game Audio Bundles** (kostenlose, royalty-freie SFX) und CC0-Audio für Sound und Musik.
- **Mixamo (Adobe)** für kostenlose Humanoid-Animationen und Auto-Rigging.

**Aetherium-Assets (Shader/Wachstum/Verseuchung), die Evolvierten-Fraktion (biologisch) und alle Fraktions-Signaturen** werden als **MODIFY/BUILD** klassifiziert – sie werden per KI-Drafting und Community-Kitbashing in Blender auf CC0-Basis erstellt (siehe [BuildBacklog.md](BuildBacklog.md)).

### Geprüfte Alternativen (D-053 / D-054)

| Strategie | Kosten Kernumfang | Stil-Kohärenz | Abdeckung 3 Fraktionen | GitHub Repo-Tauglichkeit | Verworfen/Status |
|---|---|---|---|---|---|
| **A: Asset Store only (Synty)** | ~300–800 USD | hoch | Lücke bei Evolvierten + Aetherium | Nein (Rohdaten-Verbot im öffentlichen Repo) | Verworfen (Budget & Repo-Hürde) |
| **B: Commercial Multi-Store-Mix (D-053)** | ~200–600 USD | mittel (Material-Pass nötig) | am besten, aber heterogen | Nein (Rohdaten-Verbot für Kauflizenzen) | Ersetzt durch D-054 (0 € Budget) |
| **B-Zero: 0 € Open-Source & KI (D-054)** | **0 €** | hoch (einheitlicher URP-Material-Standard) | sehr gut (KI & Kitbashing) | **Ja (Vollständig öffentlich teilbar)** | **– Gewählt (D-054) –** |

**Begründung D-054:** Garantiert 0 € Lizenzkosten, ermöglicht die vollständige Mitführung aller 3D- und Audio-Assets im **öffentlichen GitHub-Repository** (ohne Seat- oder Weitergabe-Beschränkungen) und nutzt moderne KI-Tools zur Beschleunigung des Community-Eigenbaus.

## 2. Verbindliche Leitplanken

1. **URP-Kompatibilität ist K.O.-Kriterium.** Importierte CC0-FBX-Dateien und KI-Meshes werden direkt in unseren **einheitlichen URP-Material-Standard mit Teamfarben-Masken** eingebunden.
2. **Keine RTS-Komplett-Frameworks.** Gekaufte Sim-/Determinismus-Kits kollidieren mit D-033/D-035/D-043.
3. **Stil-Anker durch URP-Material-Standard.** Da CC0- und KI-Assets aus unterschiedlichen Quellen stammen, wird visuelle Inkohärenz (R-04) durch einheitliche PBR/Stylized-Shader, Palette-Lock und Post-Processing abgefangen.
4. **Vollständige Transparenz im öffentlichen Repo.** Sämtliche CC0- und KI-Assets dürfen direkt im öffentlichen Git-Repo mitgeführt werden. Das Lizenz-Register ([Licenses.md](Licenses.md)) führt alle Quellen (z. B. CC-BY Attributions) lückenlos auf.
5. **Technische Prüfung bindend.** Jedes Mesh durchläuft die Polycount- und LOD-Prüfung gemäß [../tech/AssetBudget.md](../tech/AssetBudget.md).

## 3. Klassifikations-Rubrik (BUY / MODIFY / BUILD)

| Klasse | Definition | Entscheidungsregel |
|---|---|---|
| **BUY / CC0** | Kostenlose CC0-Basis direkt nutzbar nach Import + Material-Angleichung. | CC0-Lizenz **und** URP-tauglich **und** stilistisch anpassbar. |
| **MODIFY** | CC0- oder KI-Mesh-Basis + Community-Kitbashing in Blender (Retopo, Rigging, LODs). | Basis aus KI oder Quaternius vorhanden; Feinbearbeitung im Team (~0,5–3 PT). |
| **BUILD** | Vollständiger Eigenbau / KI-Prompt-Pipeline (Signature-Assets, Aetherium-Shader). | USP-Element, biologische Evolvierte, RTS-UI-Layout. |

## 4. Repo-Hygiene & Open-Source-Verteilung

- **Öffentliches Repo (`VibecodingGermany/HashKrieg`):** Enthält alle spielbaren CC0-Assets, KI-generierten Meshes/Texturen und Open-Source-Audio-Dateien.
- **CC-BY-Quellen:** Werden in `CREDITS.md` sauber atribuiert (gemäß [Licenses.md](Licenses.md)).

## Offene Punkte

- **Asset-Budget-Obergrenze (Q-035):** Geschlossen durch D-054 auf **0 €**.
- **Seat-Planung (Q-036):** Entfällt, da keine kommerziellen Per-Seat-Store-Lizenzen benötigt werden.
- **Bundle-Kauffenster (Q-037):** Entfällt zugunsten der freien 0 € CC0/KI-Pipeline.

## Nächste Schritte

1. Sprint 6 (Produktionsplanung): 0 € Open-Source & KI-Pipeline in Roadmap und Aufwandsschätzung (R-16) übernehmen.
2. Phase 0: Aetherium-Signature-Asset per KI/Blender-Pipeline prototypen und Performance vermessen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-22 | Erstfassung Sprint 5: Strategie B ratifiziert (D-053), Klassifikations-Rubrik und 4 Bewertungsdimensionen operationalisiert, Repo-Hygiene festgelegt | Producer / Lead Environment Artist |
| 1.1.0 | 2026-07-24 | Update auf D-054 (0 € Open-Source & KI-Asset-Pipeline), Q-035 geschlossen, öffentliche Repo-Freigabe verankert | Project Owner / Producer |
