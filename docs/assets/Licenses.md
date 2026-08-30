# Lizenz-Register

**Version:** 1.6.0 | **Status:** sprint-freigegeben (laufend fortzuschreiben) | **Verantwortungsbereich:** Producer / Technical Director / Project Owner | **Sprint:** 12

## Zweck

Zentrales **Lizenz-Register** für alle externen und KI-generierten Asset-Quellen von *Hashkrieg*: Lizenzmodell, Seat-Regeln, Attributionspflichten, Weitergabe-/Repo-Beschränkungen und offene Lizenz-Detailfragen je Quelle (gemäß D-054: 0 € Open-Source & KI-Pipeline). Dieses Dokument ist ein verbindliches Sprint-5-Exit-Kriterium und wird **bei jedem Asset-Import fortgeschrieben**. Es ist die Freigabe-Grundlage dafür, welche Assets im öffentlichen Git-Repo liegen dürfen.

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
| **SIL Open Font License 1.1 (Schriften, z. B. Rajdhani)** | **OFL-1.1** | unbegrenzt | ja | Namensnennung nicht vorgeschrieben; der Lizenztext (`OFL.txt`) muss beiliegen | **Öffentlich im GitHub-Repo erlaubt** und im Spiel einbettbar; die Schriftdateien dürfen **nicht für sich allein verkauft** werden, und ein „Reserved Font Name" darf nicht für abgeleitete Schriften weiterverwendet werden |
| **Hunyuan3D 2.1 – generierte Meshes**[^1] | Tencent Hunyuan 3D 2.1 Community License (lokal/self-hosted, Open Source) | unbegrenzt | ja | nein | **Vollständig öffentlich im GitHub-Repo erlaubt** (nur das erzeugte Mesh, nicht die Modellgewichte) |
| **Hunyuan3D 2.1 – vortrainierte Modellgewichte**[^1] | Tencent Hunyuan 3D 2.1 Community License | – | – | – | **Keine Weitergabe der Modellgewichte selbst** ins Repo (nur lokale/self-hosted Nutzung zur Mesh-Erzeugung) |
| **Hunyuan3D 1.0/2.0 (Vorgängerversionen)** | Tencent Hunyuan Non-Commercial License | – | **nein** | – | Nicht nutzbar für Repo-Assets (kein kommerzieller Freigabepfad) |
| **OpenAI Image API** | kommerziell, Output-Eigentum liegt beim Nutzer | unbegrenzt | ja | nein | Öffentlich im GitHub-Repo erlaubt |
| **Suno (Bezahltarif)**[^3] | kommerzielle Nutzung, Output-Eigentum beim Nutzer – **nach Auskunft des Inhabers zu seinem Tarif, ohne eigene AGB-Prüfung** | unbegrenzt | ja | nein | Öffentlich im GitHub-Repo erlaubt |
| **Sonniss GDC Bundle**[^2] | Royalty-Free Audio | unbegrenzt | ja | nein | **Keine Rohdateien/Bundles im öffentlichen Repository** – nur zur Verwendung *in* Spielbuilds royalty-free lizenziert, nicht zur Weitergabe als Sammlung |
| **Mixamo (Adobe)** | Kostenlos für Games | unbegrenzt | ja | nein | **Rohdaten (FBX/Rigs) nicht als lose Packs verteilen**, im Game-Build unbegrenzt |
| **Sketchfab** | CC0 oder CC-BY (**nur nach Einzelprüfung je Modell**) | unbegrenzt | ja (bei CC0/CC-BY) | CC-BY: **ja (Credits)** | Öffentlich erlaubt **nur nach dokumentierter Einzelprüfung** je Modell; CC-BY mit Attribution in `CREDITS.md` |
| **Meshy (Free-Tier) / Tripo3D (Free-Tier)** | CC BY 4.0 (Free-Tier) bzw. ungeklärt/uneinheitlich | – | ohne belegbares kommerzielles Nutzungsrecht **und** Output-Eigentum | – | **Gesperrt für Repo-Assets.** Nutzung nur für nicht eingecheckte Konzept-/Ideenreferenz; das Ergebnis wandert nicht ins Repository |
| **Neu aufkommende KI-3D-/Asset-Anbieter** | ungeprüft | – | ungeprüft | – | **Default-Deny**, bis eine dokumentierte Einzelprüfung vorliegt |

[^1]: Präzisierung 2026-07-25 (vormals in v1.1.0 pauschal als „Open Source / Public Domain" mit uneingeschränkter Repo-Freigabe geführt): Kommerzielle Nutzung ist nur unter der Lizenz der **Version 2.1** gegeben; die vortrainierten Modellgewichte selbst dürfen nicht weiterverteilt werden, die damit erzeugten Meshes hingegen schon. Beleg: [SourceCatalog_MS1.md](SourceCatalog_MS1.md) §2 „Hunyuan3D (Tencent)". Kein Rechtsrat – bei Zweifel im Einzelfall menschliche Entscheidung einholen.

[^2]: Korrektur 2026-07-25 (restriktive Lesart, Project-Owner-Entscheidung): Diese Zeile führte das Sonniss-GDC-Audio-Bundle bis einschließlich v1.2.0 widersprüchlich als „öffentliches Repo erlaubt (lizenzfrei)", während [AssetRegister.md](AssetRegister.md) §3.11 im selben Zeitraum bereits das Gegenteil dokumentierte. Aufgelöst zugunsten der restriktiven Lesart: Die Sonniss-Bundles sind royalty-free zur Verwendung *in* Spielen lizenziert, nicht zur Weiterverbreitung als Rohdatei-Sammlung – Sonniss-Rohdateien dürfen daher nicht ins öffentliche Repository. Übereinstimmende Quelle: [AssetRegister.md](AssetRegister.md) §3.11. Kein Rechtsrat – bei Zweifel im Einzelfall menschliche Entscheidung einholen.

[^3]: Aufnahme 2026-08-06 (Project-Owner-Entscheidung, D-083): Suno ist der **erste bezahlte Anbieter-Tier** im Projekt und steht damit im Wortlaut gegen §2 Regel 3 (keine Kaufkosten) und Regel 5 (kein bezahlter Anbieter-Tier für MS-1); Regel 6 führte ihn bis dahin nicht in der Whitelist. Der Inhaber hat den Tarif am 2026-08-06 zweckgebunden für die Menümusik freigegeben und die Ausnahme in Regel 5 benannt. **Grundlage der Lizenzangabe ist die Auskunft des Inhabers zu seinem Tarif, nicht eine eigene Prüfung der Suno-AGB** – anders als bei den übrigen Zeilen liegt hier kein Recherchebeleg in [SourceCatalog_MS1.md](SourceCatalog_MS1.md) vor. Kein Rechtsrat – bei Zweifel im Einzelfall menschliche Entscheidung einholen.

## 2. Verbindliche Lizenz-Regeln (D-054)

1. **Öffentliche Repository-Tauglichkeit.** Alle CC0- und KI-generierten Assets dürfen direkt im öffentlichen GitHub-Repository (`VibecodingGermany/HashKrieg`) geteilt werden.
2. **CC-BY = Attribution-Pflicht.** Jedes CC-BY-Modell (v. a. Sketchfab) wird beim Erwerb/Import in `CREDITS.md` (ab erstem CC-BY-Import) mit Autor, Titel, Quelle und Lizenz-URL erfasst.
3. **Keine Per-Seat-Kaufkosten (0 € Budget).** Es werden keine kostenpflichtigen Per-Seat-Store-Packs erworben.
4. **Mixamo-Nutzung.** Mixamo-Clips dürfen im Unity-Projekt eingebunden und gerendert werden; eine Weitergabe loser Raw-Clips an Dritte außerhalb des Projekts ist zu vermeiden.
5. **0 € ist hart für MS-1 (Project-Owner-Entscheidung, 2026-07-25).** Für MS-1 wird kein bezahlter Anbieter-Tier eingesetzt (kein Budget, MVP-Priorität). Alle Repo-Assets stammen aus den in §1 gelisteten kostenlosen/CC0-Quellen oder aus lokal/self-hosted betriebener KI-3D-Generierung (Hunyuan3D 2.1). **Ausnahme (Project-Owner-Entscheidung, 2026-08-06, D-083):** Der **Suno-Bezahltarif** ist für die Menümusik zugelassen, weil er kommerzielle Nutzung und Output-Eigentum gewährt und das Abo ohnehin beim Inhaber besteht. **Erweiterung (Project-Owner-Entscheidung, 2026-08-07, D-086):** dieselbe Ausnahme gilt für die drei Ingame-Musikthemen (`MUS_Ingame_Hashkrieg_01..03.ogg`) — dieselbe Quelle, derselbe Tarif, gleiche Rechtslage. Die Ausnahme bleibt auf diese Quelle und diese Zwecke begrenzt und erzeugt kein Präzedenzrecht; für alle übrigen Kategorien bleibt das 0-€-Prinzip in Kraft. Jede weitere bezahlte Quelle braucht eine eigene, hier benannte Ausnahme.
6. **Anbieter-Whitelist/-Blacklist für Repo-Assets (Project-Owner-Entscheidung, 2026-07-25).** Erlaubt für das öffentliche Repo: CC0-Quellen (Quaternius, Kenney, Poly Haven, ambientCG), Hunyuan3D 2.1 (lokal/self-hosted, nur die erzeugten Meshes, nicht die Modellgewichte), OpenAI Image API (Output-Eigentum beim Nutzer), Sketchfab nur nach dokumentierter Einzelprüfung je Modell (CC0 oder CC-BY mit Attribution), **Suno im Bezahltarif (Output-Eigentum beim Nutzer, zweckgebundene Ausnahme nach Regel 5)** sowie **Schriften unter OFL-1.1, solange der Lizenztext mitgeliefert wird** (beide ergänzt 2026-08-06, D-083). Gesperrt für Repo-Assets: Meshy Free-Tier, Tripo3D Free-Tier sowie jeder Anbieter, dessen Free-Tier keine belegbare kommerzielle Nutzung **und** kein Output-Eigentum gewährt – diese Dienste sind ausschließlich für nicht eingecheckte Konzept-/Ideenreferenz zulässig. Neu aufkommende Anbieter gelten bis zur dokumentierten Einzelprüfung als gesperrt (Default-Deny). Kein Rechtsrat – Einzelfallzweifel gehen an eine menschliche Entscheidung.

## 3. Erworbene Lizenzen / Asset-Imports (Ledger)

_Import-Protokoll – laufend zu befüllen._ Jede freigegebene CC0-/KI-Quelle erhält hier eine Zeile:

| Datum | Paket/Quelle | Lizenztyp | Seats | Kosten | Attribution nötig? | Repo-Freigabe |
|---|---|---|---|---|---|---|
| 2026-07-24 | Quaternius Sci-Fi & Kenney Kits | CC0 | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |
| 2026-08-07 | Kenney – Sci-Fi Sounds 1.0 (`Assets/_Project/Audio/Sfx/Kenney/SciFi`) | CC0 1.0 | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |
| 2026-08-07 | Kenney – Impact Sounds 1.0 (`Assets/_Project/Audio/Sfx/Kenney/Impact`) | CC0 1.0 | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |
| 2026-08-07 | Kenney – Interface Sounds 1.0 (`Assets/_Project/Audio/Sfx/Kenney/Interface`) | CC0 1.0 | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |
| 2026-08-06 | Suno (Bezahltarif) – Menümusik `Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg` | kommerziell, Output-Eigentum beim Nutzer[^3] | unbegrenzt | Abotarif des Inhabers (benannte Ausnahme zu Regel 5) | nein | Ja (öffentliches Repo) |
| 2026-08-07 | Suno (Bezahltarif) – Ingame-Musik `Assets/_Project/Audio/Music/MUS_Ingame_Hashkrieg_01..03.ogg` (Themen 1_orc / 2 (2) / 3 (1), nach OGG-Vorbis konvertiert) | kommerziell, Output-Eigentum beim Nutzer[^3] | unbegrenzt | Abotarif des Inhabers (Ausnahme-Erweiterung Regel 5, D-086) | nein | Ja (öffentliches Repo) |
| 2026-08-06 | OpenAI Image API (gpt-image-1) – Key Art `Assets/_Project/UI/UI_KeyArt_MainMenu.jpg` | kommerziell, Output-Eigentum beim Nutzer | unbegrenzt | 0 € | nein | Ja (öffentliches Repo) |
| 2026-08-06 | Rajdhani (Indian Type Foundry) – `Assets/_Project/UI/Fonts/Rajdhani-Regular.ttf`, `Rajdhani-Bold.ttf` | OFL-1.1 | unbegrenzt | 0 € | nein, aber `Assets/_Project/UI/Fonts/OFL.txt` muss mitgeliefert werden | Ja (öffentliches Repo) |

## Offene Punkte

- **`CREDITS.md`** wird mit dem ersten CC-BY-Import angelegt (keine Platzhalter-Dokumente, [../meta/DocumentationStandard.md](../meta/DocumentationStandard.md)).
- **Der Import vom 2026-08-06 löst `CREDITS.md` nicht aus — geprüft, entschieden, nicht offen gelassen.** Regel 2 bindet die Attributionspflicht ausdrücklich an CC-BY. Keine der drei neuen Quellen ist CC-BY: Suno und OpenAI Image API verlangen keine Namensnennung, und OFL-1.1 verlangt sie ebenfalls nicht — es verlangt die Mitlieferung des Lizenztextes, was etwas anderes ist. Der Rajdhani-Copyright-Header nennt zudem **keinen** „Reserved Font Name", die Umbenennungsklausel greift also nicht. `CREDITS.md` bleibt damit unangelegt, bis wirklich ein CC-BY-Asset importiert wird. Wer diese Prüfung später wiederholt: der Auslöser ist die Lizenz, nicht die Zahl der Quellen.
- **OFL-1.1-Beilagepflicht.** `Assets/_Project/UI/Fonts/OFL.txt` muss bei jeder Weitergabe der Schriftdateien mitgehen — Repo, Build und Paket gleichermaßen. Wer die Fonts verschiebt oder umbenennt, verschiebt die Lizenzdatei mit; ohne sie ist die Weitergabe nicht gedeckt.
- **Audio-Provenienz D-090:** Die drei Kenney-Pack-Sidecars und ihre
  `files[]`-Hashes liegen vor. Das Musik-Sidecar erfasst Menü- und drei
  Ingame-Tracks, bleibt aber bei allen vier Datensätzen `incomplete`, weil
  echte Ursprungs- oder Konvertierungsbelege fehlen. Diese Lücken werden nicht
  durch Vermutungen geschlossen. Für Key Art und die beiden TTFs fehlen die
  in [Provenance.md](Provenance.md) verlangten Asset-Datensätze weiterhin; die
  KI-Pflichtfelder kann nur der Inhaber mit Originalbelegen liefern.

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
| 1.4.0 | 2026-08-06 | Menü-Assetimport freigegeben (D-083): §1 um Suno (Bezahltarif, mit Fußnote [^3] zur fehlenden eigenen AGB-Prüfung) und SIL Open Font License 1.1 erweitert; §2 Regel 5 um die zweckgebundene Suno-Ausnahme und Regel 6 um beide Quellen ergänzt; §3 Ledger um Menümusik, Key Art und Rajdhani mit konkreten Dateipfaden befüllt; unter „Offene Punkte" entschieden, dass dieser Import **keine** `CREDITS.md` auslöst (keine CC-BY-Quelle), dazu OFL-Beilagepflicht und fehlende `PROVENANCE.json`-Datensätze vermerkt | Project Owner / Producer |
| 1.5.0 | 2026-08-07 | Ingame-Musik freigegeben (D-086): Regel-5-Ausnahme um die drei Ingame-Themen erweitert (dieselbe Quelle/Tarif/Rechtslage); §3-Ledger um `MUS_Ingame_Hashkrieg_01..03.ogg` ergänzt (OGG-Vorbis-Konvertierung aus den Suno-MP3s; Prompt-/Provenienzfelder stehen wie bei der Menümusik beim Inhaber aus) | Project Owner / Agent (Protokoll) |
| 1.6.0 | 2026-08-08 | Drei Kenney-Audiopacks als konkrete CC0-Importe mit 0 €, ohne Attribution und mit öffentlicher Repo-Freigabe erfasst; Musik-Sidecar als vorhanden, aber ehrlich unvollständig präzisiert | Producer / Agent (Umsetzung) |
