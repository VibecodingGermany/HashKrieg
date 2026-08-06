# Sprint: Baubarkeit, HUD-Lesbarkeit und Kartenbild

**Status:** umgesetzt (2026-08-06) | **Vorgänger:** HUD-Sprint (D-084) und Hauptmenü-Sprint (D-083), beide committet (`706a394`, `6f03280`) | **Nachfolger:** [09_Sprint_Gefecht_und_Rundenrahmen.md](09_Sprint_Gefecht_und_Rundenrahmen.md), unverändert | **Leitsatz:** was die Bauleiste verspricht, muss auch passieren

Dieser Sprint wird **vor** Sprint 09 gezogen. Grund: Sprint 09 baut das Gefecht aus, aber
man kommt heute nicht bis zum Gefecht — die Runde endet bei „Gebäude platziert, nichts
passiert". Ein Blocker vor einem Ausbau.

## Ergebnis (2026-08-06)

- **TEIL 1 umgesetzt:** Auto-Dispatch per Move-Intent beim Platzieren
  (`RtsDeviceInput.DispatchBuilderToConstructionSite`, Builder-Wahl gespiegelt über
  `CommandCardPresenter.TryFindRepairBuilder`, Zielzelle wie die KI); Warnung „Kein
  Builder — Bau pausiert. Builder im HQ bauen." sofort im Befehlsstatus und dauerhaft in
  der Statuszeile über der Bauleiste, solange eine eigene Baustelle ohne Builder ist;
  Baustellen-Card mit Zustandszeile (kein Builder → Builder unterwegs → im Bau, X % —
  fertig in ~Y s, aus `BuildTicks`/`ProgressRaw`/`ProductionSpeedMultiplierQ16`);
  pausierte Baustellen pulsieren amber in der Welt (`ConstructionSiteMarkerView`).
- **TEIL 2 umgesetzt:** Rechtsklick prüft `IsPointerOverHud`; Platzierung nur noch bei
  grünem Geist (`_placementValid`), Fehlklick lässt den Geist scharf; `EstimateHeight`
  rechnet GUILayout-Margins und Panel-Padding mit.
- **TEIL 3 umgesetzt:** `HudLayout`-Zonenmodell (einzige Screen-Lesestelle; Mathematik in
  `HudLayoutMath`, EditMode-getestet), F3-Panel opak + ScrollView + zonenbegrenzt,
  Bauleiste 62 px / zwei Zeilen / harte „…"-Kürzung, Sperrgrund per Hover in der
  Statuszeile.
- **TEIL 4 umgesetzt:** prozedurale Sandtextur (512², fester Seed, 32×32-Kachelung),
  84 Streufelsen ohne Collider (ausgespart um Basen/Felder), schräge warme Sonne +
  weiche Schatten + Distanznebel + Ambient-Gradient im Szenengenerator, Kartenrand als
  Verwitterungs-Schleier statt Balken, Minimap-Unerkundet ~15 % statt Schwarz.
- **Verifikation:** `dotnet test tools/Nova.SimRunner.Tests` **420/420**; SimRunner-Hash
  `0x2FBEC31FBC0BF430` (Standard) und Fingerprint `0xF866FDC042D260E1` / Final
  `0xD8650F4DEDE1494C` (DETERMINISM_10000) **identisch zum Stand vor dem Sprint**;
  `dotnet build` aller betroffenen Projekte 0 Fehler/0 Warnungen. Szenen-Regeneration
  und PlayMode-Durchlauf folgen als eigener Schritt (Szene ist Maschinenausgabe).
- **Entscheidung:** als D-085 im [DecisionLog](../DecisionLog.md) protokolliert.

## 1. Befund aus der Spielsitzung (2026-08-06)

Zwei Screenshots des Inhabers, gegengelesen im Code:

- **Gebäude werden platziert, aber nie gebaut.** Drei Baustellen stehen auf der Karte,
  Geld ist abgebucht (3.000 → 1.550 AE), der Fortschrittsbalken der Command Card steht
  bei wenigen Prozent und bewegt sich nicht.
- **Das F3-Panel liegt über allem.** Es überdeckt die Minimap vollständig und läuft mit
  halbtransparentem Hintergrund quer durch die Bauleiste; Welt-Geometrie scheint durch
  den Text.
- **Die Bauleisten-Beschriftung wird abgeschnitten.** Die dritte Textzeile
  („benötigt Raffinerie", „nicht genug Aetherium") passt nicht in die Buttonhöhe und
  wird mitten im Wort geschnitten.
- Aus dem F3-Panel selbst, tick 2177: `Forces: slot 0 0u/4b | slot 1 15u/3b` — der
  Spieler hat **null Einheiten**, die KI fünfzehn.

## 2. Ursache — drei Schichten, eine Wurzel

**Wurzel:** Eine Baustelle macht ausschließlich dann Fortschritt, wenn ein eigener
Builder in Chebyshev-Reichweite 1 des 3×3-Footprints steht:

```
ConstructionSystem.cs:607 — if (!IsInReachOfFootprint(in builder, site.OriginX, site.OriginY)) continue;
ConstructionSystem.cs:603 — if (site.AssignedBuilderRaw == 0) continue;   // kein Builder: pausiert
```

Die Zuweisung sucht den eigenen Builder mit dem kleinsten Index — **aber niemand schickt
ihn hin.** Wer ein Gebäude irgendwo auf die Karte setzt, bekommt eine Baustelle, die
exakt null Fortschritt macht, bis er den Builder von Hand danebenstellt. Nichts im HUD
sagt das.

**Die KI hat genau diese Verdrahtung, der Spieler nicht.** `SkirmishAiSystem.cs:299-329`
trägt den Kommentar „the assigned Builder must stand in Chebyshev reach <= 1 of the site
footprint or the site pauses — walk it there" und schickt ihren Builder per `MovePayload`
zur Baustelle. Der menschliche Platzierungspfad (`RtsDeviceInput` →
`RtsIntentDispatcher.PlaceBuilding`) sendet nur den Bau-Befehl, keinen Move.

**Zweite Schicht — der Builder ist ein Single Point of Failure.** `slot 0 0u` heißt: der
einzige Builder ist tot (die KI greift ab `AttackSquadThreshold` an). Ab da sind *alle*
Baustellen dauerhaft eingefroren. Die KI kennt auch dafür eine Regel
(`SkirmishAiSystem.cs:331` — „Replacement Builder at the HQ when none is alive"); der
Spieler bekommt keinen Hinweis, dass er sich im HQ einen neuen Builder bauen muss.

**Dritte Schicht — keinerlei Rückmeldung.** Die Card zeigt einen Balken
(`CommandCardHud.cs:313`) und sonst nichts. „Baustelle pausiert, weil kein Builder da
ist", „Builder ist unterwegs" und „Simulation läuft nicht" sehen für den Spieler
identisch aus: kaputt. Sprint 09 §7 hat dieses Muster bereits benannt („eine
stillstehende Baustelle sieht aus wie kaputt") — hier ist der konkrete Fall dazu.

## 3. Entscheidung des Inhabers (2026-08-06) — als D-085 zu protokollieren

Von drei möglichen Baumodellen ist gewählt:

> **Das Builder-Modell bleibt. Der Builder wird beim Platzieren automatisch zur
> Baustelle geschickt — genau das, was die KI für sich schon tut.**

Bewusst **nicht** gewählt: das C&C-Modell (Baustelle wächst, solange ein HQ lebt) und das
Hybridmodell (HQ baut langsam, Builder beschleunigt). Beide hätten die
Reichweiten-Regel in `ConstructionSystem` verändert und damit die kanonischen Hashes,
Replay- und Fingerprint-Baselines gebrochen. Die gewählte Variante ist eine reine
Eingabe-Automatik: **ein zusätzlicher Move-Befehl über den ganz normalen Command-Pfad**,
also dieselbe Klasse von Ereignis wie ein Mausklick des Spielers. Keine Regeländerung,
kein Snapshot-Bump, keine neuen Baselines.

Der Preis dieser Entscheidung, offen benannt: Wenn der Builder auf dem Weg zur Baustelle
stirbt, pausiert der Bau — dann muss der Spieler einen neuen Builder bauen und ihn
hinschicken. Deshalb ist die Zustandsanzeige in §4.1 nicht optional, sondern Teil
derselben Entscheidung.

## 4. Sprintinhalt

### 4.1 Bauen funktioniert und sagt, was es tut (Kern)

1. **Auto-Dispatch beim Platzieren.** Wenn die Platzierung abgesetzt wird, zusätzlich
   einen Move-Befehl für den zugewiesenen Builder auf eine Nachbarzelle des Footprints
   absetzen. Die Builder-Wahl muss die der Simulation spiegeln — eigener Builder mit dem
   kleinsten Entity-Index; `CommandCardPresenter.TryFindRepairBuilder` macht das für die
   Reparatur bereits und ist der Präzedenzfall. Die Zielzelle nach demselben
   deterministischen Muster wie die KI: `originX - 1`, Ostseite als Kartenrand-Fallback.
2. **Kein Builder → sichtbare Warnung statt stiller Sackgasse.** Platzieren bleibt
   erlaubt (das Geld liegt dann in einer Baustelle, die per Abbruch 75 % zurückgibt),
   aber die Statuszeile sagt es sofort: „Kein Builder — Bau pausiert. Builder im HQ
   bauen."
3. **Zustand auf der Baustellen-Card.** Der Balken bekommt eine Zeile, die den echten
   Zustand nennt, in der Reihenfolge, in der die Simulation ihn auswertet:
   *kein Builder* → *Builder unterwegs* → *im Bau, 43 %* → *fertig in ~12 s*. Der
   Restwert kommt aus `BuildTicks`, `ProgressRaw` und dem
   `ProductionSpeedMultiplierQ16` des Besitzers, nicht aus einer eigenen Schätzung.
4. **Baustellen sind auch ohne Klick erkennbar.** Eine pausierte Baustelle muss sich in
   der Welt von einer wachsenden unterscheiden (z. B. pulsierender Rahmen oder
   abweichende Einfärbung der Bodenmarkierung, `GroundMarkerVisuals`).

### 4.2 Die drei Eingabedefekte aus Sprint 09 §3 (hierher gezogen)

Sie hängen unmittelbar am Bauen und sind billig:

1. **Rechtsklick kennt die HUD-Sperre nicht** (`RtsDeviceInput.cs:562` prüft
   `IsPointerOverHud` nicht, anders als `:493`, `:525`, `:739`) — ein Rechtsklick auf
   Bauleiste, Minimap oder Card schickt die Armee an den Punkt dahinter.
2. **Roter Baugeist platziert trotzdem** (`RtsDeviceInput.cs:493-499` prüft
   `_placementHasCell` statt `_placementValid`). Zusätzlich klemmt `ToGridCoordinate`
   negative Footprint-Ursprünge auf 0 — am linken/unteren Kartenrand entsteht das
   Gebäude woanders, als der Geist gezeigt hat.
3. **Command Card wird unten abgeschnitten** (`EstimateHeight` rechnet
   GUILayout-Margins und Panel-Padding nicht mit, ~40 px) — die untersten Knöpfe liegen
   außerhalb der `BeginArea` und sind nicht klickbar.

### 4.3 HUD-Überlagerung strukturell abstellen

Heute rechnet jedes Panel seine Bildschirmposition selbst aus
(`DebugHud.cs:127-128`, `MinimapHud.cs:110-115`, `CommandCardHud.cs:480-487`,
`BuildMenuHud.cs:116-123`). Command Card und Minimap fragen immerhin die Bauleiste nach
`OccupiedHeight` — das F3-Panel fragt niemanden und nimmt sich `Screen.height - 16`.
Deshalb überlagert es die Minimap.

**Lösung: ein Zonenmodell.** Eine `HudLayout`-Klasse in `Presentation/UI` besitzt die
Rechtecke in skaliertem GUI-Raum und ist die einzige Stelle, die `Screen.width/height`
liest: Statusstreifen oben, Minimap unten links, Bauleiste unten mittig, Command Card
rechts über der Leiste, Debug-Panel im verbleibenden freien Feld. Jedes Panel fragt seine
Zone ab, statt zu rechnen. Danach ist Überlappung kein Bugfix mehr, sondern
konstruktionsbedingt ausgeschlossen.

Für das F3-Panel zusätzlich: **opaker Hintergrund** (`HudChrome.PanelStyle` statt
`GUI.skin.box` — durchscheinende Weltgeometrie unter Debug-Text ist der Grund, warum der
zweite Screenshot unlesbar ist), Höhe auf die Zone begrenzt und Inhalt in eine
`GUI.ScrollView`, damit zu viel Text scrollt statt auszulaufen.

Für die Bauleiste: Buttonhöhe auf ~62 px, **maximal zwei Zeilen** (Name / Kosten · Zeit),
`wordWrap` aus und harte Kürzung mit `…`, damit kein Label je aus dem Button laufen kann.
Der Sperrgrund („benötigt Raffinerie", „nicht genug Aetherium") wandert aus dem Button in
die Statuszeile über der Leiste und erscheint beim Überfahren.

### 4.4 Kartenbild — Quick Wins, null Assets

Der Boden ist heute eine einfarbige Fläche (`GlutrinneBlockoutView.TintGround`, eine
Unity-`Plane` mit `_sandColor`). **Wichtig für die Lösungswahl:**
`Assets/_Project/Art/**/*.png` ist gitignored — eine heruntergeladene Textur wäre in
jedem frischen Clone weg. Der Standardweg muss deshalb prozedural sein, eine CC0-Textur
kann später als optionales Drop-in über das Art-Paket dazukommen.

1. **Prozedurale Bodentextur zur Laufzeit.** 512×512, deterministisches Wert-Rauschen mit
   festem Seed (kein `UnityEngine.Random`), drei bis vier Sandtöne, überlagert von einem
   sehr niederfrequenten zweiten Rauschen für großflächige Flecken, damit die Kachelung
   nicht sichtbar wird. `wrapMode = Repeat`, `mainTextureScale ≈ (32, 32)` → vier Zellen
   pro Kachel. Erzeugt wie die vorhandenen Materialien mit `HideAndDontSave`, kein Asset
   auf der Platte, keine Lizenzfrage.
2. **Streugeometrie.** 60–100 Felsen/Kiesel aus Primitiven, deterministisch platziert
   (fester Seed, festes Verfahren wie `ClusterOffsets`), ohne Collider, ausgespart um
   beide Startbasen und beide Aetheriumfelder. Bricht die leere Fläche auf, kostet nichts.
3. **Licht und Atmosphäre.** Sonnenstand schräg, warme Farbtemperatur (die passende
   Projekteinstellung ist bereits aktiv), weiche Schatten, leichter sandfarbener
   Distanznebel, Ambient über einen Gradienten mit sandfarbenem Horizont. Der
   sichtbarste Einzeleffekt im ganzen Punkt 4.4 und reine Szenen-Konfiguration.
4. **Kartenrand einbetten.** Statt des flachen dunklen Balkens eine zwei bis drei Zellen
   breite Verwitterungszone als Farbverlauf in der Bodentextur — die Karte wirkt dann
   eingebettet statt abgeschnitten.
5. **Minimap.** Unerkundetes Gebiet ist heute reines Schwarz und liest sich als „kaputt".
   Stattdessen die Geländesilhouette stark abgedunkelt zeichnen (unerkundet ~15 %
   Helligkeit, erkundet-aber-nicht-sichtbar gedimmt, sichtbar voll) — klassisches
   RTS-Verhalten und eine reine Änderung in `MinimapHud.BuildBackground`.
6. **Optional, später:** CC0-Wüstentextur von ambientCG oder Poly Haven (beide stehen in
   der Whitelist von `docs/assets/Licenses.md` §2 Regel 6), 1K Albedo + Normal +
   Roughness, mit `PROVENANCE.json`. Muss **optional** bleiben: der prozedurale Boden ist
   und bleibt der Fallback für jeden Clone ohne Art-Paket.

## 5. Bewusst nicht in diesem Sprint

| Punkt | Warum |
|---|---|
| Auto-Zielerfassung, Feuererwiderung | Sprint 09 §4. Erste echte Simulationsänderung der Reihe, eigene Baselines. |
| Ergebnisbildschirm, Neustart, Pause | Sprint 09 §6. |
| Lebensbalken, Kontrollgruppen | Sprint 09 §5/§7. |
| Wegfall der Builder-Reichweite (C&C-Modell) | In §3 gegen das Auto-Dispatch entschieden. |
| Attack-Move | Neuer `CommandKind` gegen eingefrorenes v1-Register. Eigener Sprint. |
| Terrain-Höhen, echte Geländeverformung | Kartenarbeit, kein Quick Win. |

## 6. Fertig wenn

Ich setze über die Bauleiste eine Raffinerie irgendwohin auf die Karte. Mein Builder
läuft von selbst los, die Card sagt mir „Builder unterwegs", dann „im Bau, 40 %", dann
steht das Gebäude. Stirbt mein Builder, sagt das Spiel mir das, statt still stehen zu
bleiben. Das F3-Panel verdeckt die Minimap nicht mehr, kein Text läuft aus einem Button.
Und die Karte sieht aus wie eine Wüste und nicht wie eine beige Fläche.

---

## 7. Prompt für Kimi

```text
AUFGABE: Baubarkeit, HUD-Lesbarkeit und Kartenbild (Hashkrieg, Branch feat/playable-core-loop)

VORAUSSETZUNG
Arbeitsbaum ist bis auf Assets/_Project/Data/Registries/AssetMappingRegistry.asset sauber.
Diese eine Datei NICHT committen: sie enthaelt GUID-Verweise auf gitignorierte Prefabs,
die in jedem frischen Clone ins Leere zeigen. Der Inhaber hat entschieden, dass sie leer
im Repo bleibt.

BEFUND (Spielsitzung 2026-08-06)
Gebaeude lassen sich platzieren, Geld wird abgebucht, aber nichts wird gebaut. Der
Fortschrittsbalken der Baustelle steht. Zusaetzlich: das F3-Panel liegt halbtransparent
ueber Minimap und Bauleiste, und die Beschriftung der Bauleisten-Buttons wird
mitten im Wort abgeschnitten.

URSACHE (verifiziert im Code, nicht raten, nicht neu diagnostizieren)
1. ConstructionSystem.ProgressSites (Zeile 603 und 607): eine Baustelle macht NUR
   Fortschritt, solange der zugewiesene Builder in Chebyshev-Reichweite <= 1 des
   3x3-Footprints steht. Ohne lebenden eigenen Builder: pausiert.
2. Die Zuweisung sucht den eigenen Builder mit dem kleinsten Entity-Index, aber NIEMAND
   schickt ihn hin. Die KI hat genau diese Verdrahtung fuer sich
   (SkirmishAiSystem.cs:299-329, MovePayload auf eine Nachbarzelle des Footprints) —
   der menschliche Pfad (RtsDeviceInput -> RtsIntentDispatcher.PlaceBuilding) sendet nur
   den Bau-Befehl.
3. Stirbt der einzige Builder, frieren ALLE Baustellen dauerhaft ein, ohne jede Meldung.
   Die KI baut sich in dem Fall Ersatz (SkirmishAiSystem.cs:331); der Spieler erfaehrt
   nichts davon.

ENTSCHEIDUNG DES INHABERS (2026-08-06, als D-085 in docs/production/DecisionLog.md
protokollieren)
Das Builder-Modell BLEIBT. Der Builder wird beim Platzieren automatisch zur Baustelle
geschickt — dasselbe, was die KI fuer sich tut. Die Reichweiten-Regel in
ConstructionSystem wird NICHT angefasst: das waere eine Simulationsaenderung und wuerde
Hash-, Replay- und Fingerprint-Baselines brechen. Der Auto-Dispatch ist ein zusaetzlicher
Move-Befehl ueber den normalen Command-Pfad, also dieselbe Klasse von Ereignis wie ein
Mausklick. Kein Snapshot-Bump, keine neuen Baselines, Command-Register v1 bleibt
eingefroren.

TEIL 1 — BAUEN FUNKTIONIERT UND SAGT, WAS ES TUT (der Kern)
1. Auto-Dispatch: Beim Absetzen einer Platzierung zusaetzlich einen Move-Befehl fuer den
   zugewiesenen Builder auf eine Nachbarzelle des Footprints absetzen. Die Builder-Wahl
   MUSS die der Simulation spiegeln (eigener Builder mit kleinstem Entity-Index).
   Praezedenzfall fuer genau diese Spiegelung im UI:
   CommandCardPresenter.TryFindRepairBuilder. Zielzelle deterministisch wie die KI:
   originX - 1, Ostseite (originX + BuildingFootprintCells) als Kartenrand-Fallback.
2. Kein Builder am Leben: Platzieren bleibt erlaubt (Abbruch gibt 75 % zurueck), aber die
   Statuszeile sagt es sofort: "Kein Builder — Bau pausiert. Builder im HQ bauen."
3. Baustellen-Card: der Balken bekommt eine Zustandszeile in der Auswertungsreihenfolge
   der Simulation — kein Builder / Builder unterwegs / im Bau, 43 % / fertig in ~12 s.
   Restzeit aus BuildTicks, ProgressRaw und ProductionSpeedMultiplierQ16 des Besitzers
   rechnen, nicht schaetzen.
4. Eine pausierte Baustelle muss sich in der Welt sichtbar von einer wachsenden
   unterscheiden (Bodenmarkierung in GroundMarkerVisuals).

TEIL 2 — DREI EINGABEDEFEKTE (aus Sprint 09 §3 hierher gezogen, weil sie am Bauen haengen)
1. RtsDeviceInput.cs:562 — der Rechtsklick-Zweig prueft IsPointerOverHud NICHT, anders
   als :493, :525, :739. Rechtsklick auf Bauleiste/Minimap/Card schickt die Armee an den
   Punkt dahinter.
2. RtsDeviceInput.cs:493-499 — der Platzierungsklick prueft _placementHasCell statt
   _placementValid, der rote Geist platziert also trotzdem. Zusaetzlich klemmt
   ToGridCoordinate negative Footprint-Urspruenge auf 0: am linken/unteren Kartenrand
   entsteht das Gebaeude woanders, als der Geist zeigte.
3. CommandCardHud.EstimateHeight rechnet GUILayout-Margins und Panel-Padding nicht mit
   (~40 px). Die untersten Knoepfe liegen ausserhalb der BeginArea und sind nicht
   klickbar.

TEIL 3 — HUD-UEBERLAGERUNG STRUKTURELL ABSTELLEN
Heute rechnet jedes Panel seine Position selbst (DebugHud.cs:127-128,
MinimapHud.cs:110-115, CommandCardHud.cs:480-487, BuildMenuHud.cs:116-123). Card und
Minimap fragen wenigstens BuildMenuHud.OccupiedHeight ab; das F3-Panel fragt niemanden
und nimmt sich Screen.height - 16 — daher die Ueberlagerung.
- Eine HudLayout-Klasse in Presentation/UI besitzt die Zonen in skaliertem GUI-Raum und
  ist die EINZIGE Stelle, die Screen.width/height liest: Statusstreifen oben, Minimap
  unten links, Bauleiste unten mittig, Command Card rechts ueber der Leiste, Debug-Panel
  im verbleibenden freien Feld. Jedes Panel fragt seine Zone ab, statt zu rechnen.
- F3-Panel: opaker Hintergrund (HudChrome.PanelStyle statt GUI.skin.box —
  durchscheinende Weltgeometrie unter Debug-Text ist der Grund fuer die Unlesbarkeit),
  Hoehe auf die Zone begrenzt, Inhalt in eine GUI.ScrollView.
- Bauleiste: Buttonhoehe ~62 px, MAXIMAL zwei Zeilen (Name / Kosten · Zeit), wordWrap
  aus, harte Kuerzung mit "…". Der Sperrgrund ("benoetigt Raffinerie", "nicht genug
  Aetherium") wandert aus dem Button in die Statuszeile ueber der Leiste und erscheint
  beim Ueberfahren.

TEIL 4 — KARTENBILD, QUICK WINS OHNE EIN EINZIGES ASSET
WICHTIG: Assets/_Project/Art/**/*.png ist gitignored. Eine heruntergeladene Textur waere
in jedem frischen Clone weg. Der Standardweg ist deshalb prozedural; eine CC0-Textur kann
spaeter als optionales Drop-in ueber das Art-Paket dazukommen, nie als Voraussetzung.
1. Prozedurale Bodentextur zur Laufzeit in GlutrinneBlockoutView: 512x512,
   deterministisches Wert-Rauschen mit FESTEM Seed (kein UnityEngine.Random), drei bis
   vier Sandtoene, ueberlagert von einem sehr niederfrequenten zweiten Rauschen fuer
   grossflaechige Flecken gegen sichtbare Kachelung. wrapMode = Repeat,
   mainTextureScale ~ (32, 32). Erzeugung wie die vorhandenen Runtime-Materialien mit
   HideAndDontSave, kein Asset auf der Platte. Hinweis: URP/Lit nimmt material.mainTexture
   ueber das [MainTexture]-Attribut auf _BaseMap an — derselbe Weg, auf dem material.color
   heute schon funktioniert.
2. Streugeometrie: 60-100 Felsen/Kiesel aus Primitiven, deterministisch platziert (fester
   Seed, festes Verfahren wie ClusterOffsets), OHNE Collider, ausgespart um beide
   Startbasen und beide Aetheriumfelder.
3. Licht und Atmosphaere: Sonnenstand schraeg, warme Farbtemperatur, weiche Schatten,
   leichter sandfarbener Distanznebel, Ambient als Gradient mit sandfarbenem Horizont.
   Groesster sichtbarer Effekt fuer den geringsten Aufwand, reine Szenen-Konfiguration.
4. Kartenrand: statt des flachen dunklen Balkens eine zwei bis drei Zellen breite
   Verwitterungszone als Farbverlauf in der Bodentextur.
5. Minimap: unerkundetes Gebiet ist heute reines Schwarz und liest sich als kaputt.
   Stattdessen Gelaendesilhouette stark abgedunkelt (unerkundet ~15 % Helligkeit,
   erkundet-nicht-sichtbar gedimmt, sichtbar voll) in MinimapHud.BuildBackground.

NICHT IN DIESEM SPRINT
Auto-Zielerfassung und Feuererwiderung, Lebensbalken, Ergebnisbildschirm, Neustart,
Pause, Kontrollgruppen — das ist alles Sprint 09
(docs/production/hashkrieg/09_Sprint_Gefecht_und_Rundenrahmen.md) und bleibt dort.
Ebenso: Attack-Move, Terrain-Hoehen, Wegfall der Builder-Reichweite.

DETERMINISMUS-DISZIPLIN
- ConstructionSystem, CombatSystem, EconomySystem und jede andere Simulationsregel
  bleiben unangetastet. Wenn eine Aenderung eine Hash-, Replay- oder Fingerprint-Baseline
  rot macht, ist das in diesem Sprint ein Fehler und kein neu zu setzender Sollwert —
  dann ist versehentlich Simulationsverhalten geaendert worden.
- Der Auto-Dispatch laeuft ueber den bestehenden Command-Pfad. Kein neuer CommandKind,
  kein StateVersion-Bump, keine Aenderung am Wire-Format.
- Alles unter Punkt 4 ist reine Praesentation und darf die Simulation nicht beruehren.

FERTIG WENN
Ich setze eine Raffinerie irgendwohin auf die Karte, mein Builder laeuft von selbst los,
die Card sagt "Builder unterwegs", dann "im Bau, 40 %", dann steht das Gebaeude. Stirbt
mein Builder, sagt das Spiel mir das. Das F3-Panel verdeckt die Minimap nicht mehr, kein
Text laeuft aus einem Button. Und die Karte sieht aus wie eine Wueste.

ABSCHLUSS
- CHANGELOG.md: Eintrag unter [Unreleased]
- docs/production/DecisionLog.md: D-085 (Baumodell, siehe oben)
- docs/production/hashkrieg/10_Sprint_Baubarkeit_und_Kartenbild.md: Status auf
  "umgesetzt" plus kurzes Ergebnis
- NICHT pushen ohne ausdrueckliche Freigabe des Inhabers
```
