# Lizenz-Register

**Version:** 1.3.0 | **Status:** sprint-freigegeben (laufend fortzuschreiben) | **Verantwortungsbereich:** Producer / Technical Director / Project Owner | **Sprint:** 5

## Zweck

Zentrales **Lizenz-Register** für alle externen und KI-generierten Asset-Quellen von *Project Nova*: Lizenzmodell, Seat-Regeln, Attributionspflichten, Weitergabe-/Repo-Beschränkungen und offene Lizenz-Detailfragen je Quelle (gemäß D-054: 0 € Open-Source & KI-Pipeline). Dieses Dokument ist ein verbindliches Sprint-5-Exit-Kriterium und wird **bei jedem Asset-Import fortgeschrieben**. Es ist die Freigabe-Grundlage dafür, welche Assets im öffentlichen Git-Repo liegen dürfen.

## Abhängigkeiten

- [ProcurementStrategy.md](ProcurementStrategy.md) – Strategie B-Zero (D-054), Repo-Hygiene §5
- [AssetRegister.md](AssetRegister.md) – welche Kategorie aus welcher Quelle bezogen wird
- [../research/AssetStore_Landschaft.md](../research/AssetStore_Landschaft.md) – Lizenzfallen-Analyse (§Querschnitt Punkt 2)
- [../tech/AssetBudget.md](../tech/AssetBudget.md) §6 – Lizenz-Kriterium der Prüfung
- [SourceCatalog_MS1.md](SourceCatalog_MS1.md) – Recherchebeleg zu Anbieter-Lizenzlagen (u. a. Hunyuan3D-Versionsdifferenzierung §2, Quellenmatrix §6), Grundlage der Korrekturen in diesem Dokument

## 1. Lizenz-Rahmen je Quelle

| Quelle | Lizenzmodell | Seats | Kommerziell | Attribution | Weitergabe / Öffentliches Repo |
|---|---|---|---|---|---|
| **Quaternius / Kenney / Poly Haven / ambientCG** | **CC0 (Public Domain)** | unbegrenzt | ja | nein | **Vollständig öffentlich im GitHub-Repo erlaubt** |
| **Hunyuan3D 2.1 – generierte Meshes**[^1] | Tencent Hunyuan 3D 2.1 Community License (lokal/self-hosted, Open Source) | unbegrenzt | ja | nein | **Vollständig öffentlich im GitHub-Repo erlaubt** (nur das erzeugte Mesh, nicht die Modellgewichte) |
| **Hunyuan3D 2.1 – vortrainierte Modellgewichte**[^1] | Tencent Hunyuan 3D 2.1 Community License | – | – | – | **Keine Weitergabe der Modellgewichte selbst** ins Repo (nur lokale/self-hosted Nutzung zur Mesh-Erzeugung) |
| **Hunyuan3D 1.0/2.0 (Vorgängerversionen)** | Tencent Hunyuan Non-Commercial License | – | **nein** | – | Nicht nutzbar für Repo-Assets (kein kommerzieller Freigabepfad) |
| **OpenAI Image API** | kommerziell, Output-Eigentum liegt beim Nutzer | unbegrenzt | ja | nein | Öffentlich im GitHub-Repo erlaubt |
| **Sonniss GDC Bundle**[^2] | Royalty-Free Audio | unbegrenzt | ja | nein | **Keine Rohdateien/Bundles im öffentlichen Repository** – nur zur Verwendung *in* Spielbuilds royalty-free lizenziert, nicht zur Weitergabe als Sammlung |
| **Mixamo (Adobe)** | Kostenlos für Games | unbegrenzt | ja | nein | **Rohdaten (FBX/Rigs) nicht als lose Packs verteilen**, im Game-Build unbegrenzt |
| **Sketchfab** | CC0 oder CC-BY (**nur nach Einzelprüfung je Modell**) | unbegrenzt | ja (bei CC0/CC-BY) | CC-BY: **ja (Credits)** | Öffentlich erlaubt **nur nach dokumentierter Einzelprüfung** je Modell; CC-BY mit Attribution in `CREDITS.md` |
| **Meshy (Free-Tier) / Tripo3D (Free-Tier)** | CC BY 4.0 (Free-Tier) bzw. ungeklärt/uneinheitlich | – | ohne belegbares kommerzielles Nutzungsrecht **und** Output-Eigentum | – | **Gesperrt für Repo-Assets.** Nutzung nur für nicht eingecheckte Konzept-/Ideenreferenz; das Ergebnis wandert nicht ins Repository |
| **Neu aufkommende KI-3D-/Asset-Anbieter** | ungeprüft | – | ungeprüft | – | **Default-Deny**, bis eine dokumentierte Einzelprüfung vorliegt |

[^1]: Präzisierung 2026-07-25 (vormals in v1.1.0 pauschal als „Open Source / Public Domain" mit uneingeschränkter Repo-Freigabe geführt): Kommerzielle Nutzung ist nur unter der Lizenz der **Version 2.1** gegeben; die vortrainierten Modellgewichte selbst dürfen nicht weiterverteilt werden, die damit erzeugten Meshes hingegen schon. Beleg: [SourceCatalog_MS1.md](SourceCatalog_MS1.md) §2 „Hunyuan3D (Tencent)". Kein Rechtsrat – bei Zweifel im Einzelfall menschliche Entscheidung einholen.

[^2]: Korrektur 2026-07-25 (restriktive Lesart, Project-Owner-Entscheidung): Diese Zeile führte das Sonniss-GDC-Audio-Bundle bis einschließlich v1.2.0 widersprüchlich als „öffentliches Repo erlaubt (lizenzfrei)", während [AssetRegister.md](AssetRegister.md) §3.11 im selben Zeitraum bereits das Gegenteil dokumentierte. Aufgelöst zugunsten der restriktiven Lesart: Die Sonniss-Bundles sind royalty-free zur Verwendung *in* Spielen lizenziert, nicht zur Weiterverbreitung als Rohdatei-Sammlung – Sonniss-Rohdateien dürfen daher nicht ins öffentliche Repository. Übereinstimmende Quelle: [AssetRegister.md](AssetRegister.md) §3.11. Kein Rechtsrat – bei Zweifel im Einzelfall menschliche Entscheidung einholen.

## 2. Verbindliche Lizenz-Regeln (D-054)

1. **Öffentliche Repository-Tauglichkeit.** Alle CC0- und KI-generierten Assets dürfen direkt im öffentlichen GitHub-Repository (`VibecodingGermany/Project_Nova`) geteilt werden.
2. **CC-BY = Attribution-Pflicht.** Jedes CC-BY-Modell (v. a. Sketchfab) wird beim Erwerb/Import in `CREDITS.md` (ab erstem CC-BY-Import) mit Autor, Titel, Quelle und Lizenz-URL erfasst.
3. **Keine Per-Seat-Kaufkosten (0 € Budget).** Es werden keine kostenpflichtigen Per-Seat-Store-Packs erworben.
4. **Mixamo-Nutzung.** Mixamo-Clips dürfen im Unity-Projekt eingebunden und gerendert werden; eine Weitergabe loser Raw-Clips an Dritte außerhalb des Projekts ist zu vermeiden.
5. **0 € ist hart für MS-1 (Project-Owner-Entscheidung, 2026-07-25).** Für MS-1 wird kein bezahlter Anbieter-Tier eingesetzt (kein Budget, MVP-Priorität). Alle Repo-Assets stammen aus den in §1 gelisteten kostenlosen/CC0-Quellen oder aus lokal/self-hosted betriebener KI-3D-Generierung (Hunyuan3D 2.1).
6. **Anbieter-Whitelist/-Blacklist für Repo-Assets (Project-Owner-Entscheidung, 2026-07-25).** Erlaubt für das öffentliche Repo: CC0-Quellen (Quaternius, Kenney, Poly Haven, ambientCG), Hunyuan3D 2.1 (lokal/self-hosted, nur die erzeugten Meshes, nicht die Modellgewichte), OpenAI Image API (Output-Eigentum beim Nutzer), Sketchfab nur nach dokumentierter Einzelprüfung je Modell (CC0 oder CC-BY mit Attribution). Gesperrt für Repo-Assets: Meshy Free-Tier, Tripo3D Free-Tier sowie jeder Anbieter, dessen Free-Tier keine belegbare kommerzielle Nutzung **und** kein Output-Eigentum gewährt – diese Dienste sind ausschließlich für nicht eingecheckte Konzept-/Ideenreferenz zulässig. Neu aufkommende Anbieter gelten bis zur dokumentierten Einzelprüfung als gesperrt (Default-Deny). Kein Rechtsrat – Einzelfallzweifel gehen an eine menschliche Entscheidung.

## 3. Erworbene Lizenzen / Asset-Imports (Ledger)

_Import-Protokoll – laufend zu befüllen._ Jede freigegebene CC0-/KI-Quelle erhält hier eine Zeile:

| Datum | Paket/Quelle | Lizenztyp | Seats | Kosten | Attribution nötig? | Repo-Freigabe |
|---|---|---|---|---|---|---|
| 2026-07-24 | Quaternius Sci-Fi & Kenney Kits | CC0 | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |

## Offene Punkte

- **`CREDITS.md`** wird mit dem ersten CC-BY-Import angelegt (keine Platzhalter-Dokumente, [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md)).

## Nächste Schritte

1. Sprint 6: CC0/KI-Pipeline-Vorgaben in Produktionsplanung übernehmen; öffentliche Repo-Struktur finalisieren.
2. Phase 0/Sprint 7: Bei erstem CC-BY-Asset `CREDITS.md` anlegen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.3.0 | 2026-07-25 | Sonniss-GDC-Audio-Bundle-Zeile in §1 korrigiert (restriktive Lesart: Rohdateien nicht öffentlich repo-fähig, nur Verwendung in Spielbuilds royalty-free) und mit Fußnote [^2] versehen, die den bis v1.2.0 bestehenden Widerspruch zu AssetRegister.md §3.11 offenlegt und auflöst | Producer |
| 1.0.0 | 2026-07-22 | Erstfassung Sprint 5: Lizenz-Rahmen je Quelle, verbindliche Lizenz-Regeln, leeres Erwerbs-Ledger angelegt | Producer / Technical Director |
| 1.1.0 | 2026-07-24 | Update auf D-054 (0 € Open-Source & KI-Pipeline), CC0- & KI-Lizenz-Regeln für öffentliches Repo ergänzt | Project Owner / Producer |
| 1.2.0 | 2026-07-25 | Hunyuan3D-Zeile in §1 korrigiert und in Meshes/Modellgewichte/Vorgängerversionen aufgeteilt (Beleg: SourceCatalog_MS1.md); Anbieter-Whitelist/-Blacklist für Repo-Assets und harte 0-€-Regel für MS-1 als Regeln 5–6 in §2 ergänzt; KI-Tools- und Sketchfab-Zeilen in §1 präzisiert (Meshy/Tripo3D Free-Tier gesperrt für Repo, Sketchfab nur nach Einzelprüfung) | Producer |
