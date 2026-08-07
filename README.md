# Project Nova

**Dokumentversion:** 0.17.0 | **Status:** unveröffentlichter Entwicklungsstand, spielbar | **Verantwortungsbereich:** Executive Producer / Technical Writer | **Stand:** 2026-08-07

> Ein Echtzeitstrategiespiel in der Tradition von **Command &amp; Conquer** — Basisbau,
> Ernte, Armee, Karte kontrollieren. Gebaut mit Unity und C#, offen entwickelt.
> **Arbeitstitel in Umstellung: _Hashkrieg_.**
>
> **Es ist spielbar.** Eine vollständige Runde gegen die KI läuft seit dem
> 7. August 2026 — [ausprobieren](#4-das-spiel-ausprobieren).

| Allianz | | Legion | |
|---|---|---|---|
| ![Allianz Kommandozentrale](docs/assets/concept-art/web/alliance_building_HQ.jpg) | ![Allianz Lynx](docs/assets/concept-art/web/alliance_unit_LightTank.jpg) | ![Legion Gefechtsstand](docs/assets/concept-art/web/legion_building_HQ.jpg) | ![Legion Räuber](docs/assets/concept-art/web/legion_unit_LightTank.jpg) |

<sub>Concept-Art, keine Bildschirmfotos aus dem Spiel. Der komplette Satz: [Kontaktbogen](docs/assets/concept-art/KONTAKTBOGEN.jpg)</sub>

## Zweck

Diese Seite ist der Einstieg in Repository, Projektstatus und Dokumentation.
Sie unterscheidet bewusst zwischen vorhandenem Prototypcode und dem, was
tatsächlich läuft — und sie sagt offen, welche Entscheidung als Nächstes ansteht.

## Abhängigkeiten

- [GOVERNANCE.md](GOVERNANCE.md) – Tier-Modell, aktives Governance-Tier
- [AGENTS.md](AGENTS.md) – verbindliche Arbeitsregeln
- [CONTRIBUTING.md](CONTRIBUTING.md) – Branch-, PR- und Review-Ablauf
- [docs/README.md](docs/README.md) – vollständiger Wiki-Index
- [docs/vision/Lore.md](docs/vision/Lore.md) – Weltentwurf *Hashkrieg*
- [docs/vision/Konzept_Hashkrieg.md](docs/vision/Konzept_Hashkrieg.md) – die
  Mechanik-Variante, über die entschieden werden muss
- [docs/production/MVPRecoveryPlan.md](docs/production/MVPRecoveryPlan.md) –
  Inhalt der Stufen G0 bis G5 (Evidenzvertrag ruht)
- [docs/production/MVPContentManifest.md](docs/production/MVPContentManifest.md) –
  exakter MS-1-Inhalt
- [docs/production/DecisionLog.md](docs/production/DecisionLog.md) – alle
  Entscheidungen mit Alternativen
- [docs/production/ScopeLedger.md](docs/production/ScopeLedger.md) – Register
  aller Verschiebungen gegenüber dem MS-1-Inhalt

## 1. Die Richtung: Hashkrieg

Die Welt ist nicht an einem Krieg zugrunde gegangen, sondern an einer
**Abrechnung**. Als Rechenleistung die einzige Größe wurde, die noch Wert
bemaß, verfiel am Tag des Großen Abschlusses jeder Anspruch, der nicht durch
nachgewiesene Rechenleistung gedeckt war. Renten, Anleihen, Grundbücher. Nicht
gestohlen — nur nicht abgerechnet. Es war eine korrekt ausgeführte Regel, und
genau daran zerbricht die Welt bis heute: **Es gibt niemanden, den man dafür
hängen könnte.**

Beide Fraktionen haben denselben Tag erlebt und entgegengesetzte Lehren gezogen:

| | **Allianz** | **Legion** |
|---|---|---|
| Lehre | *Es war ein Buchhaltungsfehler* | *Genau so fängt es wieder an* |
| Ziel | eine verlässliche Abrechnungsinstanz | die Kette dauerhaft umkämpft halten |
| Baut | für die Ewigkeit: versiegelt, teuer, symmetrisch | für morgen früh: offen, billig, ersetzbar |
| Widerspruch | braucht dafür ein Monopol | braucht dafür den ewigen Krieg |

**Frieden ist nicht Unwille, sondern Arithmetik.** Einkommen ist Anteil, nicht
Ertrag: Wer nicht wächst, schrumpft. Und die 51-Prozent-Mehrheit ist für die
Allianz der Sieg und für die Legion das Weltende — beide haben recht.

Der vollständige Weltentwurf inklusive Kampagnen-Anker steht in
[docs/vision/Lore.md](docs/vision/Lore.md).

## 2. Die offene Entscheidung — hier lohnt sich Mitreden

Das ist gerade die **wichtigste ungeklärte Frage** des Projekts, und sie ist
bewusst offen: Ändern wir die ökonomische Grundschleife, oder nicht?

**Heute (und in jedem RTS seit 1995):** Ressourcen fließen **nach innen**.
Harvester fahren raus, sammeln Aetherium, bringen es heim. Die Basis verbraucht.

**Die Variante „Hashkrieg":** Energie fließt **nach außen**. Du erzeugst Strom
im Zentrum deiner Basis und lieferst ihn per Konvoi an Rechenfarmen an der
Peripherie, die Ertrag erwirtschaften. Jedes Watt ist eine Entscheidung —
**rechnet es, oder schießt es?**

Was daran hängt:

| | bleibt gleich | ändert sich |
|---|---|---|
| Sammler-Loop | Ladung, Ladezeit, Docks, verwundbare Konvois | die **Richtung** kehrt sich um |
| Biome | Karten und Gelände | bekommen eine **wirtschaftliche** Identität (Kühlung) |
| Wirtschaftsknick | Taktgeber ins Endgame | wird zum angekündigten **Halving** |
| Superwaffe | teuer, sichtbar, sabotierbar | wird zur **51-Prozent-Attacke** |
| Fog of War | gilt für die Karte | gilt **nicht** für die Konten: jeder sieht die Einnahmen aller |

Die vollständige Analyse mit Mechanik-Mapping, Match-Bogen und drei bewerteten
Optionen steht in
[docs/vision/Konzept_Hashkrieg.md](docs/vision/Konzept_Hashkrieg.md).
Kurzfassung der dortigen Empfehlung: **MS-1 wie geplant fertigstellen** — die
Mechanik ist mechanisch fast identisch und validiert sich dabei selbst —, den
Hashkrieg-Umbau **danach** als Prototyp erproben.

Gegenmeinungen sind ausdrücklich erwünscht. Wenn du dazu etwas zu sagen hast:
[ein Issue aufmachen](https://github.com/VibecodingGermany/Project_Nova/issues/new)
und in zwei Sätzen begründen, welche Option du für richtig hältst.

## 3. Projektstatus

**Phase:** Implementierungs-Recovery · **Governance:** Tier 1

> ### 🎮 Der Kernloop ist geschlossen
>
> **Am 7. August 2026 wurde die erste vollständige Runde gespielt** — vom
> Hauptmenü über Basisbau, Ernte und Truppenproduktion bis zum Gefecht und
> zum Ergebnisbildschirm. Ernten, bauen, kämpfen, gewinnen, neu anfangen:
> das Spiel trägt sich zum ersten Mal selbst.

| Ergebnisstufe | Status |
|---|---|
| Spielbar | **ja** — lokales 1v1 gegen eine KI, eine Runde von Anfang bis Ende |
| Kernloop | **geschlossen** (2026-08-07) |
| MS-0 | offen — Kern läuft, Cross-Plattform- und Perf-Nachweise stehen aus |
| MS-1 / MVP | nicht erreicht — Lücken im [ScopeLedger](docs/production/ScopeLedger.md) |
| Alpha | nicht begonnen |

### Was seit Juli entstanden ist

Fünf aufeinander aufbauende Sprints haben aus einer Simulation ohne Zugang ein
Spiel gemacht, das man starten, bedienen und gewinnen kann:

| | Was daraus wurde |
|---|---|
| **Spielbare Kernschleife** (D-077) | Klassischer C&amp;C-Start: HQ, ein Builder, 3.000 AE. **Slot 1 wird von einer KI gespielt** — sie baut, erntet, produziert Truppen und greift in Wellen an, und sie tut das über denselben versiegelten Befehlspfad wie ein menschlicher Gegner im Netzwerk. Eine Runde endet mit der Zerstörung des feindlichen Hauptquartiers. |
| **Hauptmenü und Einstellungen** (D-083) | Menü mit Key Art, Titel und Musik statt Direktstart ins Match. Musik- und SFX-Regler, Renderdetail, vSync, Auflösung, Vollbild — als lesbares JSON gespeichert. Erstes UI-Toolkit-UI im Projekt. |
| **Bedienbares HUD** (D-084) | Bauleiste mit allen neun Gebäuden samt Sperrgrund, Kommandokarte, Minimap mit Kamerafenster, Platzierungsvorschau, Auswahl- und Sammelpunktmarker. Alles Sichtbare ist auch anklickbar. |
| **Bauen und Kartenbild** (D-085) | Baustellen werden fertig: Der Builder fährt selbst zur Baustelle, die Karte sagt, was sie tut („kein Builder", „im Bau, 43 %", „fertig in ~12 s"). Dazu ein Zonenmodell, das überlappende HUD-Panels konstruktionsbedingt ausschließt, und eine Wüste aus prozeduraler Textur, Streufelsen und warmem Licht — ohne ein einziges neues Asset. |
| **Gefecht und Rundenrahmen** (D-086, D-087) | **Der Schritt, der den Loop schließt.** Einheiten und Türme erfassen Ziele selbst und erwidern Feuer — vorher brauchte jeder einzelne Schuss einen Klick. Der Harvester fährt seinen Kreislauf allein: hin, ernten, abliefern, wieder von vorn. Dazu Lebensbalken, Ergebnisbildschirm mit *Neue Runde*, sichtbare Pause, Kontrollgruppen 1–9 und Ingame-Musik. |

Dazu durchgehend: **Fraktionsidentität** (Allianz und Legion unterscheiden sich in
Schadensmatrix, Waffenwerten, Kosten und Harvester-Kapazität), ein Simulationskern
mit rund 900 automatisierten Tests, und **34 3D-Assets**, die über eine
Drop-in-Pipeline einfahren (§5).

### Was noch fehlt

Ehrlich und ohne Beschönigung. Der Loop läuft — aber ein Spiel ist mehr als ein
funktionierender Loop:

- **Einheiten stehen übereinander.** Eine Move-Order schickt alle markierten
  Einheiten auf *dieselbe* Zelle, und die Abstandsrechnung greift nur, solange
  sie in Bewegung sind — wer ankommt, wird zum unbeweglichen Teil des Stapels.
  Ohne Formation gibt es keine Frontlinie und kein Flankieren.
- **Einheiten laufen durch Gebäude.** Gebäude-Grundflächen landen nie im
  Kostenfeld der Wegfindung, also kennt die Simulation keinen belegten Raum.
- **Kein Attack-Move.** Truppen feuern zwar von selbst, halten unterwegs aber
  nicht an, um zu kämpfen.
- **Keine Soundeffekte.** Musik ja, Gefechtsgeräusche nein.
- **Kein Speichern.** Die Simulation kann ihren Zustand vollständig
  serialisieren und hash-identisch fortsetzen — es fehlt nur das Schreiben auf
  die Platte.

Die ersten beiden Punkte sind analysiert und Inhalt des laufenden Sprints
[Truppenführung](docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md).

Das Repository enthält einen unvollständig integrierten Prototyp. Dateien,
Typen und isolierte Tests sind kein Fertignachweis — führend bleibt der
[Implementierungs-Audit](docs/production/ImplementationAudit_2026-07-24.md).
Was stattdessen als Nachweis zählt, definiert [GOVERNANCE.md](GOVERNANCE.md):
**grüne CI plus eine gespielte und protokollierte Runde.**

Seit D-076 gilt **Governance-Tier 1** (zwei Entwickler, kein Publikum). Das
Gate-Regime G0–G5 mit Evidence- und Receipt-Verträgen blockiert nichts mehr; es
ist vollständig erhalten und ruht bis Tier 3 — siehe
[quality/README.md](quality/README.md).

## 4. Das Spiel ausprobieren

**Eine Runde dauert etwa 15 bis 30 Minuten** und läuft so: Menü → Neues Spiel →
Raffinerie bauen → Harvester produzieren → Kraftwerk und Kaserne → Armee bauen →
zur Gegnerbasis → feindliches Hauptquartier zerstören → Ergebnisbildschirm.

Lokales 1v1 gegen eine KI, mit Menü, Musik, Bauleiste, Minimap, Fog of War,
Schadensmatrix und Siegauswertung. Was läuft und was nicht, steht weiter unten —
bitte vor dem ersten Start lesen.

### Variante 1: im Editor (empfohlen)

1. Projekt in **Unity `6000.5.4f1`** öffnen (exakter Pin, kein Auto-Upgrade).
2. `Assets/_Project/Scenes/Bootstrap.unity` öffnen und **Play** drücken.
3. Das **Hauptmenü** erscheint. „Neues Spiel" startet das Match: 128×128-Karte,
   du bist Slot 0 (Allianz), Slot 1 spielt die KI (Legion).

Ohne das Art-Paket rendert das Spiel Graybox-Primitive statt 3D-Modelle — es ist
in beiden Fällen vollständig spielbar (§5).

Die Szene ist **Maschinenausgabe**. Wenn sie beschädigt oder veraltet ist, wird
sie über das Menü `Tools/Project Nova/Create Bootstrap Scene` neu erzeugt — die
`.unity`-Datei wird nie von Hand bearbeitet.

### Steuerung

**Man braucht diese Tabelle nicht mehr.** Seit dem HUD-Sprint ist alles Nötige
anklickbar: Bauleiste unten, Kommandokarte rechts, Minimap links. Die Tasten sind
Abkürzungen für Geübte. Verbindlich ist der Code (`RtsDeviceInput`).

| Eingabe | Wirkung |
|---|---|
| Linke Maustaste, Klick | Eigene Einheit oder eigenes Gebäude auswählen, sonst Auswahl leeren |
| Linke Maustaste, Ziehen | Box-Auswahl · mit `Shift` zur bestehenden Auswahl hinzufügen |
| `Strg`+`1`…`9` · `1`…`9` | Kontrollgruppe setzen · Kontrollgruppe abrufen |
| Rechte Maustaste | Bewegen; mit einem eigenen Produktionsgebäude in der Auswahl: Sammelpunkt setzen |
| Mittlere Maustaste, Ziehen | Kamera drehen · `Leertaste` setzt sie zurück |
| Pfeiltasten, Bildschirmrand | Kamera schwenken · Mausrad Zoom · `Z` / `X` drehen |
| `S` · `A` · `H` · `R` | Stop · Angriff · Ernten · Ladung abliefern |
| `Y` `B` `C` `Shift`+`B` `V` `T` `G` `F` | Gebäude platzieren: Raffinerie, Kraftwerk, Lager, Kaserne, Fahrzeugfabrik, Forschungslabor, Radar, Verteidigungsplattform |
| `U` `Q` `N` `E` `Shift`+`E` `D` `Shift`+`D` | Einheit in Auftrag geben: Builder, Harvester, Panzerabwehr, Späher, Leichter Panzer, Kampfpanzer, Artillerie |
| `P` · `F3` | Simulation pausieren · Debug-Panel ein- und ausblenden |

Beim Platzieren folgt ein Baugeist dem Cursor — grün heißt gültig, rot heißt
nicht. Linksklick setzt, Rechtsklick oder `Escape` bricht ab.

Es gibt **kein Speichern und kein Laden** — der Menüeintrag „Laden" ist sichtbar,
aber ausgegraut. Die Simulation kann ihren Zustand vollständig serialisieren und
hash-identisch fortsetzen; es fehlt nur das Schreiben auf die Platte.

### Variante 2: fertige Player

Beide Player entstehen im Verzeichnis `Builds/`, das **gitignoriert** ist. Sie
liegen also nur auf der Maschine, die sie gebaut hat. Neu bauen im Batchmode:

```bash
/Applications/Unity/Hub/Editor/6000.5.4f1/Unity.app/Contents/MacOS/Unity \
  -quit -batchmode -nographics -projectPath "$PWD" \
  -executeMethod Nova.Editor.BuildScript.BuildMacOSArm64   # oder BuildWindows64
```

**macOS** — `Builds/MacOSArm64/ProjectNova.app` ist ein **unsigniertes, lokales
Artefakt** ohne Notarisierung; Gatekeeper blockiert den ersten Start:

```bash
xattr -dr com.apple.quarantine "Builds/MacOSArm64/ProjectNova.app"
open "Builds/MacOSArm64/ProjectNova.app"
```

**Windows** — `Builds/Windows64/ProjectNova.exe` ist ein unsignierter
Mono-Player, **von macOS aus gebaut**. SmartScreen warnt beim ersten Start.
Ehrliche Einschränkung: Der Build ist abgeschlossen, wurde aber **nie
ausgeführt** — der erste Windows-Start ist gleichzeitig der erste echte Test.

### Was läuft — und was nicht

**Läuft — eine vollständige Runde, Ende zu Ende:** Lockstep-Kern mit 10 Hz,
Befehle ausschließlich durch den versiegelten Command-Pfad; Menü, Einstellungen
und Musik; Auswahl, Kontrollgruppen, Bewegung, Flow-Field-Pathfinding; Basisbau
von der Bauleiste bis zum fertigen Gebäude; der Harvester-Kreislauf ohne
Mikromanagement; Produktionswarteschlangen mit Sammelpunkt; Fog of War;
Schadens- und Panzerungsmatrix; Einheiten und Türme, die selbst Ziele erfassen
und Feuer erwidern; eine KI, die baut, erntet, Truppen produziert und in Wellen
angreift; Lebensbalken, sichtbare Pause und ein Ergebnisbildschirm mit
*Neue Runde*.

**Läuft noch nicht:**

- **Einheiten stapeln sich.** Eine Move-Order schickt alle markierten Einheiten
  auf dieselbe Zelle, und angekommene Einheiten fallen aus der Abstandsrechnung
  heraus — der Stapel bleibt. Ohne Formation gibt es keine Frontlinie.
- **Einheiten laufen durch Gebäude.** Grundflächen landen nie im Kostenfeld der
  Wegfindung.
- **Kein Attack-Move.** Truppen feuern von selbst, halten unterwegs aber nicht
  zum Kämpfen an.
- **Keine Soundeffekte.** Musik ja, Gefechtsgeräusche nein.
- **Lager und Radar kosten Geld und tun nichts.** Zwei von neun Gebäuden warten
  noch auf ihre Wirkung.

Die ersten beiden Punkte sind analysiert und Inhalt des laufenden Sprints:
[11_Sprint_Truppenfuehrung.md](docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md).
Die vollständige Liste der Verschiebungen steht im
[ScopeLedger](docs/production/ScopeLedger.md), das Sitzungsprotokoll im
[GrayboxLog](docs/production/GrayboxLog.md).

## 5. Concept-Art

[![Kontaktbogen aller 34 Concept-Art-Entwürfe](docs/assets/concept-art/KONTAKTBOGEN.jpg)](docs/assets/concept-art/KONTAKTBOGEN.jpg)

34 Entwürfe, 17 Rollen je Fraktion, alle im selben Format und derselben
Lichtsetzung. Der Leuchtakzent trägt die Fraktionsidentität — Cyan gegen
Orange — und entspricht der Teamfarben-Maske im späteren Spielasset.

- [Bildstandard](docs/assets/ConceptArtStyleGuide.md) – Rahmung, Licht, Palette,
  Formensprache, Maßstabsanker, Abnahmekriterien
- [Ordner und Herkunftsnachweis](docs/assets/concept-art/README.md) – Provenienz
  je Bild inklusive Modell, Prompt und SHA-256

**Status der Concept-Art:** Entwürfe zur Formfindung, keine Produktionsassets.

### 3D-Assets

**34 Modelle sind produziert und im Spiel** — je Fraktion die neun Gebäude- und
acht Einheitenrollen. Sie liegen bewusst **nicht im Repository**: rund 105 MB
hätten es mehr als verdoppelt und wären später nur per History-Rewrite wieder
herauszubekommen. Stattdessen werden sie als Paket verteilt
([AssetPackage.md](docs/assets/AssetPackage.md)) und fahren per Drop-in ein — ein
konventionskonformes Prefab wird beim Import automatisch registriert.

Ein frischer Clone ist deshalb **immer spielbar**: fehlt das Paket, rendert das
Spiel Graybox-Primitive, bei denen die Form die Rolle und die Farbe den Spieler
kodiert. Mit Paket stehen dieselben Einheiten als Modelle da. Die Simulation
merkt davon nichts.

## 6. Mitmachen

Das Projekt ist offen und wird gerade von sehr wenigen Leuten getragen.
Mithilfe ist willkommen — besonders in diesen Bereichen:

| Du kannst… | Dann schau hier |
|---|---|
| **mitentscheiden**, ob die Wirtschaft umgedreht wird | §2 und [Konzept_Hashkrieg.md](docs/vision/Konzept_Hashkrieg.md) |
| **eine Runde spielen** und sagen, wo es sich falsch anfühlt | §4 — genau so sind die letzten drei Blocker gefunden worden |
| **3D-Assets bauen** aus den Concept-Art-Vorlagen | [Bildstandard](docs/assets/ConceptArtStyleGuide.md) und [AssetBudget](docs/tech/AssetBudget.md) |
| **an der Simulation arbeiten** (C#, deterministisch, Unity-frei) | [SimulationCore](docs/tech/SimulationCore.md) und [CodingGuidelines](docs/tech/CodingGuidelines.md) |
| **die KI stärker machen** — sie spielt, aber schlicht | [SkirmishAi_Spec](docs/tech/modules/SkirmishAi_Spec.md) |
| **Sound beisteuern** — im Gefecht ist es bisher still | [Audioplan](docs/production/hashkrieg/04_Audioplan.md) |
| **Doku verbessern** | [DocumentationStandard](docs/meta/DocumentationStandard.md) |

Zwei Dinge, die den Einstieg leichter machen: Der Simulationskern ist
**Unity-frei** und läuft headless über `tools/Nova.SimRunner` — man braucht
Unity nur für die Darstellung. Und jede Entscheidung im Projekt steht mit
mindestens drei geprüften Alternativen im
[DecisionLog](docs/production/DecisionLog.md); nichts wird still geändert.

Ablauf steht in [CONTRIBUTING.md](CONTRIBUTING.md). Fragen gern als
[Issue](https://github.com/VibecodingGermany/Project_Nova/issues).

## 7. Closed-Core MS-1

D-056 begrenzt MS-1 auf:

- Allianz gegen Legion, Mensch gegen KI;
- Glutrinne, Wüste, S, 128×128, klares Wetter;
- je neun Gebäude- und acht Einheitenrollen;
- vollständiges Aetherium einschließlich endlicher Reserve, Nachwachsen,
  Ausbreitung, permanenter Überernte und KI-Feldmanagement;
- Pause, Save/Load/Recovery und das definierte Accessibility-Minimum.

Evolvierte, Luft, T3, Zusatzkarten, Multiplayer, Kampagne, Telemetrie,
Steam/Cloud und finale Art/Audio sind Post-MVP.

## 8. Tech-Stack

- **Engine:** Unity `6000.5.4f1`, Revision `d550df8bd089`
- **Rendering:** URP
- **Sprache:** C#
- **Simulation:** Unity-freier, autoritativer `Nova.Simulation`-Kern,
  Q16.16-Fixed-Point ab G1
- **Host:** Unity und `Nova.SimRunner` verwenden dieselben Core-/Sim-Quellen

Automatische Editor-Upgrades sind verboten. Eine Re-Evaluierung benötigt nach
G5 oder bei einem belegten Engine-Blocker eine neue D-ID.

## 9. Repository-Struktur

```text
Project Nova/
├── Assets/                Unity-Projekt und Prototypcode
├── docs/                  Living-Documents-Wiki
│   ├── vision/            Weltentwurf, Kernspielgefühl, Zielgruppe
│   ├── gamedesign/        Vollspiel-GDD mit MS-1-Overrides
│   ├── tech/              technische Verträge
│   ├── assets/            Art-Standard und Concept-Art
│   └── production/        Entscheidungen, Gates, Risiken, Planung
├── quality/
│   ├── content/           maschinenlesbares MVP-Manifest
│   ├── scenarios/         kanonische Abnahmeszenarien
│   ├── schemas/           Evidence-Schema; keine Platzhalter-Evidence
│   └── scripts/           Schema-, Semantik- und Integritätsprüfung
├── tools/                 unter anderem Nova.SimRunner
├── AGENTS.md
├── CONTRIBUTING.md
└── CHANGELOG.md
```

## 10. Arbeitsweise

`main` ist PR-only. Arbeit erfolgt auf kurzen
`feat/`, `fix/`, `docs/`, `chore/`, `refactor/` oder `codex/`-Branches,
gefolgt von Squash-Merge und linearer Historie. Es gibt keinen dauerhaften
Integrationsbranch.

Pflichtchecks sind `docs-check` und für Quality-Verträge `integrity`. Dieser
Teil des `quality-gate` prüft nur Verträge und Negative Controls. Ein
Authorize-Job existiert bis G0-A2 bewusst nicht. Eine Änderung am Trust-Bundle
wird ohne Gate-Fortschritt gemergt und kann sich nicht selbst autorisieren.

## 11. Lizenz

© 2026 VibecodingGermany / Dennis Westermann. **Alle Rechte vorbehalten.**

Es liegt derzeit **keine Open-Source-Lizenz** vor. Ansehen, Ausprobieren und
Mitwirken per Pull Request sind ausdrücklich erwünscht; eine Weiterverbreitung
als eigenes Werk ist nicht freigegeben. Wer beitragen möchte, kann das tun —
die Lizenzfrage wird vor einer Veröffentlichung geklärt und ist als offener
Punkt geführt.

## Offene Punkte

- **Die Wirtschaftsfrage aus §2** ist die wichtigste offene Entscheidung.
- Eine formale Lizenz ist noch festzulegen. Sie entscheidet, unter welchen
  Bedingungen Beiträge Dritter angenommen werden können.
- Der Umbenennungsbeschluss auf *Hashkrieg* ist im Bestand dieses Repositories
  noch nicht vollzogen — Repo, Code und Wiki laufen weiter unter *Project Nova*.
- Q-018 (Preis) und Q-019 (Telemetrie) bleiben offen und blockieren MS-1 nicht.

## Nächste Schritte

Der laufende Sprint bringt den Truppen bei, sich den Platz zu teilen
([11_Sprint_Truppenfuehrung.md](docs/production/hashkrieg/11_Sprint_Truppenfuehrung.md)):
Formationsverteilung statt einer gemeinsamen Zielzelle, Abstand halten auch im
Stand, und Gebäude, um die herum gelaufen wird statt hindurch.

Danach zur Bewertung, in dieser Reihenfolge:

1. **Soundeffekte** — zwölf Geräusche trennen „klingt kaputt" von „klingt wie ein
   Spiel". Das Gefecht ist bisher stumm.
2. **Wirtschaftsdruck** — endliche Aetheriumfelder geben der Runde einen Bogen und
   einen Grund, um Gebiet zu kämpfen.
3. **Gebäude mit Wirkung** — Lager und Radar kosten Geld und tun nichts.
4. **KI-Ausbau** — der Gegner spielt, aber schlicht.

Die Gate-Kette G0–G5 ruht unter Tier 1 und wird erst wieder aufgenommen, wenn das
Projekt ein Publikum hat.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.7.1 | 2026-07-24 | Recovery-Baseline nach Implementierungs-Audit | Executive Producer / Lead Technical Director |
| 0.8.0 | 2026-07-24 | Closed-Core MS-1, exakten Engine-Pin, G0-offenen Status und Quality-Verträge D-056–D-061 aufgenommen | Executive Producer / Technical Writer |
| 0.8.1 | 2026-07-24 | Evidence-Semantikvalidator ergänzt und Dokumentstruktur korrigiert | Technical Writer / Lead QA Engineer |
| 0.8.2 | 2026-07-24 | Sprint-6-Endstatus und auf G0 begrenzten Start von Sprint 7 eindeutig formuliert | Executive Producer / Technical Writer |
| 0.9.0 | 2026-07-24 | D-062-Evidence-Kette sowie Victory-, MatchConfig- und Commander-MS-1-Overrides ergänzt | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.10.0 | 2026-07-24 | D-063-Schema 1.2, kanonische Check-Artefakte, Drei-Lauf-Messung und Protected-CI-Trustpfad aufgenommen | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.11.0 | 2026-07-24 | D-064: Schema 1.2 auf Integrität begrenzt, G0-A vor G0-B gestellt und subject-unabhängigen Schema-1.3-Bootstrap verankert | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.12.0 | 2026-07-25 | D-066: G0-A1-Integritätsgrundlage vom zweiphasigen G0-A2-Receipt-Authorizer getrennt und zirkulären Pass-Pfad entfernt | Executive Producer / Technical Writer / Lead QA Engineer |
| 0.13.0 | 2026-07-26 | Abschnitt „Das Spiel ausprobieren" mit Editor-Start, Steuerungslegende, Player-Anleitung und ehrlicher Abgrenzung des Graybox-Stands ergänzt | Technical Writer |
| 0.14.0 | 2026-07-26 | Abschnitt zum neuen Arbeitstitel *Hashkrieg* ergänzt: Weltentwurf, Concept-Art-Satz und Style-Guide verlinkt | Technical Writer |
| 0.15.0 | 2026-07-26 | Neu gegliedert und bebildert: Hashkrieg-Richtung nach vorn gezogen, die offene Wirtschaftsentscheidung als eigener Abschnitt sichtbar gemacht, Mitmach-Abschnitt mit Einstiegspunkten ergänzt, Projektstatus um Graybox und Fraktionsidentität aktualisiert, Lizenzlage präzisiert | Technical Writer |
| 0.17.0 | 2026-08-07 | **Kernloop geschlossen:** erste vollständige Runde gespielt. Sprint „Gefecht und Rundenrahmen" (D-086, D-087) als fünfte Zeile in den Werdegang aufgenommen; Projektstatus von „fast geschlossen" auf „geschlossen" gezogen und der Kopf sagt jetzt in der ersten Zeile, dass das Spiel spielbar ist; „Was noch fehlt" komplett ersetzt (die vier alten Punkte sind erledigt) durch Einheiten-Stapelung, Wegfindung durch Gebäude, fehlendes Attack-Move und stumme Gefechte; Steuerung um Kontrollgruppen und additive Auswahl ergänzt; Nächste Schritte auf Sprint 11 umgestellt; Sprint-Feld im Kopf entfernt, weil die Sprint-7-Zählung seit der Hashkrieg-Reihe nicht mehr trägt | Technical Writer |
| 0.16.0 | 2026-08-06 | Auf den Stand nach vier Sprints gezogen (D-077, D-083, D-084, D-085): Projektstatus nennt jetzt die spielende KI, Menü, bedienbares HUD und funktionierendes Bauen; §4 korrigiert den Start (Menü statt Direktstart), führt die vollständige Steuerung und ersetzt die überholte „was die Graybox nicht kann"-Liste durch den echten offenen Rest; §5 dokumentiert die 34 3D-Assets und die Drop-in-Pipeline statt „es existiert kein 3D-Asset"; Mitmach-Tabelle und Nächste Schritte auf den laufenden Sprintplan umgestellt | Technical Writer |
