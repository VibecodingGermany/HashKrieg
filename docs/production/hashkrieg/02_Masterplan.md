# Masterplan Hashkrieg — sieben Phasen, top-down nach Spielgefühl

**Version:** 0.1.0 | **Status:** Entwurf – Planungsgrundlage, kein Gate-Nachweis | **Verantwortungsbereich:** Orchestrator / Producer | **Sprint:** 7

## Zweck

Der ausführbare Plan. Jede Phase ist ein abgeschlossener Sprint mit eigenem
Schreibbereich; jedes Arbeitspaket nennt Ziel, Ort, Abnahme und Aufwandsklasse.

**Für den ausführenden Agenten:** Lies zuerst
[01_Bestandsaufnahme.md](01_Bestandsaufnahme.md) §7 „Fallen". Sechs davon sind
Fehler, die man nur einmal macht.

## Abhängigkeiten

- [README.md](README.md) – die vier Inhaberentscheidungen E-1 bis E-4
- [01_Bestandsaufnahme.md](01_Bestandsaufnahme.md) – der geprüfte Ist-Stand
- [../MVPContentManifest.md](../MVPContentManifest.md) – Soll-Inhalt MS-1
- [../../../GOVERNANCE.md](../../../GOVERNANCE.md) – Tier 1: grüne CI plus gespielte Runde

## Die Sortierlogik

Nicht nach Aufwand und nicht nach Reifegrad, sondern nach **Rendite fürs
Spielgefühl**:

```
Phase 0  Sicherung          — verhindert Arbeitsverlust. Vor allem anderen.
Phase 1  Es wird ein Spiel  — Gegner, Kampf, Wirtschaftsdruck, Rundenabschluss.
Phase 2  Es wird bedienbar  — Bauleiste, Fraktionswahl, Marker, Menü.
Phase 3  Gebäude wirken     — Lager, Radar, Low-Power, Module, Bauketten.
Phase 4  Es klingt          — Audio von null auf funktional.
Phase 5  Es sieht aus       — Art-Nachbestellung (läuft extern parallel).
Phase 6  Es heißt Hashkrieg — Umbenennung, zweistufig.
Phase 7  Es erzählt         — Namen, Barks, Minimal-Kampagne.
```

**Phase 5 läuft parallel ab Tag eins**, weil der Grafiker extern arbeitet und
niemanden blockiert. **Phase 6 Stufe A kann jederzeit sofort passieren** — das
ist der „Umzug", der sich unmittelbar anfühlt und nichts riskiert.

Aufwandsklassen: **S** ≤ 1 Tag · **M** 1–3 Tage · **L** 3–10 Tage · **XL** > 10 Tage.
Sie sind aus der Codelage geschätzt, nicht gemessen — sie sortieren, sie planen nicht.

---

## Phase 0 — Sicherung

**Ziel:** Nichts von dem, was schon existiert, geht verloren oder wird beim
nächsten Commit kaputtgemacht.
**Muss vor jeder anderen Phase abgeschlossen sein.**

### 0.1 · S · Uncommittete Arbeit sichern

Der Arbeitsbaum auf `main` trägt uncommittete Arbeit aus **zwei verschiedenen
Strängen**, die auseinandergehalten werden müssen:

| Strang | Dateien | Was |
|---|---|---|
| Art-Integration (GB-004) | `AssetMappingRegistry.asset`, `UnitViewManager.cs`, `DebugHud.cs` | die 34 Asset-Mappings und ihre Darstellung |
| **D-077, laufend** | `MatchBootstrap.cs`, `MatchRunner.cs`, `SimDefinitions.cs`, `EconomySystem.cs`, `PlayerEconomyState.cs`, `ConstructionSystem.cs`, `Determinism10000Scenario.cs`, `quality/content/mvp-v1.json` | klassischer C&C-Eröffnungsloop |

Der GrayboxLog nennt uncommittete Sitzungsarbeit selbst als wiederkehrenden
Befund („ein `git checkout` von der Vernichtung entfernt"). Zehn geänderte
Dateien aus zwei Strängen auf `main` sind genau die Lage, in der ein
unbedachtes `git checkout` oder `git stash` Arbeit vernichtet.

**Achtung:** Die Registry darf erst committet werden, wenn E-1 entschieden ist —
sie verweist auf 34 Prefabs, die `.gitignore` ausschließt.

**Abnahme:** Beide Stränge liegen getrennt auf je einem Branch oder Commit,
Arbeitsbaum sauber.

### 0.2 · S · `Hashkrieg_Assets` versionieren

Kein `.git`, externes Volume, existiert genau einmal. Kritisch ist nicht das
Bildmaterial, sondern `3d/unity_ready/convert_report.json` — die **einzige**
Zuordnung GLB → Spielrolle. Die GLB-Dateinamen sind reine Generator-Prompts.

**Abnahme:** Ein zweiter Ort trägt denselben Stand, mit Verifikation über die
SHA-256-Werte aus `img/PROVENANCE.json`.

### 0.3 · M · E-1 umsetzen: Binärdaten-Ablage entscheiden und einrichten

Siehe [README.md](README.md) E-1. Bei der Empfehlung Git LFS: **vor** dem ersten
Art-Commit, sonst braucht es einen History-Rewrite — und den verbietet die
Git-Sicherheitsregel des Projekts.

**Abnahme:** Ein frischer Clone plus dokumentierter Zusatzschritt führt zu einem
Unity-Projekt, das Modelle zeigt statt Würfel.

### 0.4 · M · Provenienz nachtragen, Manifest-Widerspruch auflösen

34 `PROVENANCE.json` mit je sechs `_TODO`. Dazu E-2: `ArtManifest_MS1.md` §8
sperrt Tripo3D Free für eingecheckte Assets — genau der Anbieter der 34 Modelle.
Und `ArtManifest_MS1.md` behauptet weiterhin, es existiere kein produziertes
Asset.

**Abnahme:** Jeder der 34 Datensätze trägt `licenseId`, `licenseUrl`,
`providerTermsUrl`, `commercialUseGranted`, `attributionRequired`,
`redistributionAllowed`, `verifiedBy`, `verifiedAt`. Manifest-Status auf
`produced` gezogen. Falls die Sperre bestehen bleibt: die 34 sind als
Wegwerf-Platzhalter markiert und Phase 5 P0 wird zur Neubestellung.

### 0.5 · M · Ein Abnahmekriterium, das den untätigen Gegner sehen würde

Der heutige End-zu-Ende-Beweis (`GrayboxDemoProofTests`) prüft, dass drei
Screenshots existieren und größer als 10 KiB sind. **Genau deshalb blieb der
untätige Gegner unentdeckt:** der Beweis prüft, dass gerendert wird, nicht dass
gespielt wird.

Gebraucht wird ein Headless-Szenario, das zwei KI-Slots gegeneinander laufen
lässt und verlangt, dass innerhalb von N Ticks ein `MatchOutcome != Undecided`
eintritt **und** beide Slots mehr als M Gebäude gebaut haben.

Ohne dieses Kriterium ist „die KI ist ausgebaut" nicht prüfbar und Phase 1 hat
keinen Endpunkt.

**Abnahme:** Das Szenario läuft in der CI und ist rot, solange 1.1 nicht
umgesetzt ist.

---

## Phase 1 — Es wird ein Spiel

**Ziel:** Eine Runde hat einen Gegner, ein Gefecht, wirtschaftlichen Druck und
ein Ende.

### 1.1 · L · Die Gegner-KI registrieren und ausbauen ← **der eine echte Blocker**

> ### ✔ Wird gerade gelöst — Stand 2026-08-06 nachmittags
>
> Im Arbeitsbaum (uncommittet, parallele Spur) liegt bereits:
> `SkirmishAiSystem` ist in `MatchRunner` **registriert** — zwischen Combat und
> Victory, hinter einem Schalter `enableSkirmishAi`, der auf `true` steht. Das
> System ist von 119 auf 718 Zeilen gewachsen, dazu kamen `AiFactionProfile`
> und Tests in beiden Spuren.
>
> **Teil (b) ist ebenfalls gelöst, und zwar richtig:** Das neue
> `AiPeerCommandTransport` bindet die KI an eine eigene `CommandIngress` und
> leitet deren Records in die Host-Ingress weiter — byte-gleich zum
> Netzwerkpfad. Die KI ruft die Domänensysteme also nicht mehr direkt auf.
>
> **Korrektur zu meiner früheren Empfehlung:** Ich hatte geschrieben, die KI
> gehöre in der Registrierung *vor* Construction und Production. Das galt für
> den Direktaufruf-Entwurf. Über die Ingress werden die Befehle ohnehin zu
> Beginn des Folge-Ticks ausgeführt — die Position im Systemzyklus bestimmt nur
> noch, welchen Zustand die KI *beobachtet*. Spät zu stehen ist damit die
> bessere Wahl, weil sie den fertig aufgelösten Tick sieht.
>
> Offen bleibt der Funktionsumfang unter (c) — dagegen prüfen.

Drei Teile:

**(a) Registrieren.** `SkirmishAiSystem` in `MatchRunner.InitializeMatch`
aufnehmen. Die Registrierungsreihenfolge *ist* die Tick-Reihenfolge und bestimmt
den State-Hash — die Entscheidung ist eine Verhaltensänderung, kein Refactoring,
und muss in allen drei Headless-Spuren nachgezogen werden.

**(b) Umbauen auf den Command-Pfad.** Die KI darf `TryPlaceBuilding` und
`TryQueueUnit` nicht direkt aufrufen. `MatchRunner` legt ausdrücklich fest: *UI
und KI erzeugen nur `CommandIntent`-Werte und reichen sie an die Ingress.* Ohne
diesen Umbau landen KI-Aktionen nie im Record-Stream — bei Replay oder Netzwerk
sofort desynchron. **Der Umbau muss vor dem Funktionsausbau passieren**, sonst
wird er später eine Umbauaktion über gewachsenen Code.

**(c) Funktionsausbau.** Ersetzt die zwei Entscheidungszweige (hartkodiertes
Kraftwerk an `(40,40)`, ein Builder). Minimum für „spielt mit":

- Bauliste je Fraktion, an der eigenen Basis statt an einer globalen Konstante
- Harvester nachbestellen und auf Felder verteilen (die KI kennt heute
  `harvest` und `field` gar nicht — der Gegnerslot erntet nie)
- Armee produzieren nach einfachem Verhältnis, T2 über das Forschungslabor
- Angriffswellen ab Schwellenwert, Rückzug bei Verlust
- Aufklärung mit Spähfahrzeugen

Nicht nötig für MS-1: Mikromanagement, Konterwahl, Basisplanung.

**Beleg:** `Assets/_Project/Scripts/AI/SkirmishAiSystem.cs`,
`Assets/_Project/Scripts/Gameplay/Match/MatchRunner.cs`.
**Abnahme:** Das Szenario aus 0.5 wird grün.

### 1.2 · M · Zielerfassung, Feuererwiderung, Attack-Move

Heute feuert `CombatSystem` nur auf ein per Hand gesetztes `AttackTarget`, und
`MovementSystem` bewegt Einheiten nie auf ihr Ziel zu. Folge: Kampf ist
Einzelklick-Buchhaltung, eigene Einheiten sterben wortlos, und die
Verteidigungsplattform — das einzige bewaffnete Gebäude, 400 AE — kann
strukturell nie feuern.

Umfang:

- Automatische Zielsuche im Waffenradius für bewaffnete Einheiten und Gebäude
- Feuererwiderung bei Beschuss
- Attack-Move (`A` mit Ziel: hinbewegen und unterwegs feuern)
- Verfolgung innerhalb eines Radius, danach zurück zur Ausgangsposition
- `Stop` löscht das Angriffsziel
- Angriffe auf **eigene** Einheiten filtern (heute zulässig und von der
  Siegauswertung als gültige Elimination gewertet)

**Achtung Determinismus:** Zielsuche muss eine feste, index-basierte
Auswahlregel haben — die erste gefundene Einheit ist keine.

### 1.3 · M · Der Wirtschaft Druck geben

> **Achtung, überlappt mit laufender Arbeit.** Die Umstellung **D-077**
> (klassischer C&C-Eröffnungsloop) lag beim Schreiben dieses Plans uncommittet
> im Arbeitsbaum und fasst genau diese Dateien an. Sie erledigt Zeile 2 der
> Tabelle bereits. **Vor Beginn dieses Pakets den Stand von D-077 prüfen** —
> siehe [01_Bestandsaufnahme.md](01_Bestandsaufnahme.md), Kasten „Die Grundlinie
> bewegt sich".

Vier zusammenhängende Eingriffe, alle in `MatchBootstrap` und `EconomySystem`:

| Was | Heute | Ziel | Stand |
|---|---|---|---|
| Feldreserve | 2.000.000 AE (≈ 14 h) | Manifestwerte 9.000 / 15.000 AE — ein Startfeld geht im Match-Korridor sichtbar zur Neige | **offen** — von D-077 nicht berührt |
| Harvester-Startposition | in Reichweite von Feld **und** Raffinerie | echte Fahrstrecke, damit Konvois verwundbar und Standorte eine Entscheidung sind | **läuft** in D-077 |
| Feldanzahl | 2 | 5 (2 Start, 2 Expansion, 1 umkämpftes Zentrum) nach Manifest | **offen** |
| Ernterate | 2 AE/Tick, als Provisorium markiert | gegen die Zielkurve kalibriert (heute ≈ 4-faches Ziel) | **offen** |

Die Feldreserve ist von den drei offenen Punkten der wirksamste: Ohne sie bleibt
der Wirtschaftsbogen flach, egal wie gut die Eröffnung ist.

Das ist die narrativ wirksamste Änderung des ganzen Plans: Solange die Felder
unerschöpflich sind, **widerlegt** das Spiel die zentrale Aussage seiner eigenen
Fiktion (siehe [06_Narrative.md](06_Narrative.md)).

Nicht in dieser Phase: Feldanatomie, Nachwachsen, Überernte-Stufen,
Ausbreitung. Das ist G2-Umfang und in [../ScopeLedger.md](../ScopeLedger.md)
registriert.

### 1.4 · M · Die Runde bekommt ein Ende

`VictorySystem` rastet das Ergebnis ein — der Kernel tickt weiter, es gibt
keinen Ergebnisbildschirm, keinen Neustart, kein Beenden. Im gesamten Projekt
existiert weder `Application.Quit` noch `SceneManager`.

Umfang: Ergebnisanzeige für alle vier `MatchOutcome`-Codes, Neustart, Beenden.
Bewusst schlicht — die schöne Fassung kommt in Phase 2.4.

### 1.5 · S · Die erste gespielte Runde protokollieren

Governance-Tier 1 verlangt „grüne CI **plus eine gespielte und protokollierte
Runde**". Kein Mensch hat das Spiel je gespielt. Das ist der billigste offene
Punkt im Projekt und blockiert jeden Meilenstein.

**Abnahme:** Ein `GB`-Eintrag in [../GrayboxLog.md](../GrayboxLog.md) nach
[../DemoRunbook.md](../DemoRunbook.md), mit ehrlicher Rückmeldung.

---

## Phase 2 — Es wird bedienbar

**Ziel:** Jemand, der die Anleitung nicht gelesen hat, kann eine Runde spielen.

> **Reihenfolgewarnung:** 2.1 und 2.2 fassen dieselbe Datei an
> (`RtsDeviceInput`). Sie dürfen **nicht** als getrennte parallele Sprints
> laufen — die Hot-File-Regel verbietet überlappende Schreibbereiche, und der
> zweite Eingriff würde den ersten größtenteils überschreiben.

### 2.1 · L · Bauleiste **und** Fraktionswahl in einem Zug

Heute: dreizehn auswendig zu lernende Einzeltasten, fest verdrahtet auf die
Allianz-Ids 1–17. `SimDefinitions.ToDefinitionId(faction, role)` existiert
bereits — die Eingabeschicht nutzt sie nur nicht.

Ein Eingriff, zwei Ergebnisse: Die Def-Ids aus der Fraktion des lokalen Slots
ableiten **und** gleichzeitig eine klickbare Leiste bauen, die Kosten,
Voraussetzung und Verfügbarkeit zeigt.

Das verdoppelt den erlebbaren Content ohne eine einzige neue Definition und ohne
ein einziges neues Art-Asset — 17 fertige Legion-Rollen werden spielbar.

Dazu: Platzierungsvorschau (Ghost, grün/rot) und sichtbares Feedback für
abgelehnte Befehle. Fünf Ablehnungsgründe sind heute für den Spieler
ununterscheidbar von „kaputt".

**Systemwahl:** [../../research/Animation_Audio_UI.md](../../research/Animation_Audio_UI.md)
legt UI Toolkit als Primärsystem nahe. `DebugHud` erklärt sich selbst für
gate-untauglich. Das Input-System-Paket ist nicht installiert — ohne es ist
Rebinding später nicht nachrüstbar.

### 2.2 · M · Rückmeldung in der Welt

- **Auswahlmarker** — heute ist nach dem Loslassen der Maustaste unsichtbar, was
  ausgewählt ist
- **Lebensbalken** — heute nur Helligkeit des Fraktionstints in 16 Stufen; bei
  zwanzig Einheiten nicht ablesbar, und der Rot-Blend kollidiert mit der
  Legion-Grundfarbe
- **Befehlsmarker** — Move, Attack, Rally Point. Der Rally Point ist ohne Marker
  praktisch unbenutzbar
- **Kontextcursor** — Move / Attack / Harvest / Repair / Bau gültig / ungültig
- **Kontrollgruppen 1–9**, additive Auswahl mit Shift, Doppelklick-Typwahl

### 2.3 · M · Minimap

`MinimapRenderer` ist eine 24-zeilige Koordinatenformel ohne Aufrufer. Die
Simulation liefert bereits alles Nötige, einschließlich Radar-Pings. Auf einer
128×128-Karte mit Basen in gegenüberliegenden Ecken ist die Minimap das
Navigationsorgan.

### 2.4 · M · Hauptmenü, Pause, Ergebnisbildschirm

Die Build-Szenenliste enthält genau eine Szene. Der Spieler fällt ohne Rahmen in
ein laufendes Match. Umfang: Titel, Fraktionswahl, Start, Optionen (Lautstärke,
UI-Skalierung), Pausenmenü, Ergebnisbildschirm mit Neustart.

### 2.5 · S · Anzeigenamen sichtbar machen

`mvp-v1.json` führt für alle 34 Definitionen einen `displayName`
(Kommandozentrale / Gefechtsstand, Lynx / Räuber, Longbow / Donnerkanone). Die
Structs haben **kein Namensfeld** — die gesamte Fiktion ist im Produkt
unsichtbar.

**Kritisch:** Die Nachschlagetabelle gehört in die Präsentation, **nicht** in
`Nova.Simulation`. Anzeigenamen dürfen `DefinitionsHash64` nicht berühren, sonst
entwertet jede Textkorrektur alle aufgezeichneten Replays.

### 2.6 · M · Sichtbarer Nebel

Der Fog of War ist simulationsseitig fertig, am Bildschirm aber unsichtbar:
erkundetes und unerkundetes Gelände sehen identisch aus. Aufklärung hat keine
sichtbare Belohnung. Braucht einen Fullscreen-Pass als
`ScriptableRendererFeature` — im URP-Renderer sind heute null Features
registriert.

---

## Phase 3 — Gebäude wirken

**Ziel:** Kein Gebäude kostet Geld, ohne etwas zu tun. **Das ist die Antwort auf
„pro Fraktion fehlen zwei Gebäude".**

### 3.1 · M · Lager und Radar wirksam machen

**Lager:** AE-Obergrenze im `EconomySystem` — HQ 2.000 AE Basis, +2.000 je
Lager, Überschuss verfällt, 25 % Verlust bei Zerstörung (D-024). Heute addiert
`AddCredits` ungedeckelt; damit fehlt der Ausgabenanreiz vollständig.

**Radar:** Radar-Abdeckung vom Gebäude ableiten statt von jeder eigenen Einheit.
Heute erzeugt jede Einheit Pings — das Gebäude ist reine Kostenfalle.

### 3.2 · S · Low-Power vollständig durchziehen

Das Design nennt eine feste vierstufige Reihenfolge, bei der Radar und
Verteidigung **immer zuerst** fallen. Implementiert ist ausschließlich der
Tempo-Malus (−50 % Produktion und Bau).

Folge heute: Ein Angriff auf ein feindliches Kraftwerk hat keinerlei taktische
Wirkung außer langsamerem Bauen. Mit der vollständigen Regel wird das Kraftwerk
zum lohnenden Ziel — und damit erst zum interessanten Gebäude.

### 3.3 · M · Bauvoraussetzungs-Kette

`SimBuildingDefinition` hat genau **ein** Feld `PrerequisiteRole`. Das Design
nennt für sechs von neun Rollen Mehrfach- oder andere Voraussetzungen; acht
Abweichungen sind belegt. Eine Bitmaske über `UnitRole` reicht.

Macht die Baureihenfolge zur Entscheidung — und ist Voraussetzung dafür, dass
die KI-Bauliste aus 1.1 überhaupt sinnvoll erzwungen wird.

### 3.4 · L · Modulsystem der Verteidigungsplattform

Das Design ist eindeutig: „Basis ist ein unbewaffnetes Podest, Bewaffnung über
Module" — MG (250 AE, T1, Voraussetzung Kaserne) und Rakete (400 AE, T2,
Voraussetzung Forschungslabor). `mvp-v1.json` erklärt beide als aktiv, nur Flak
als deaktiviert.

Im Code ist die Plattform ab Werk fest bewaffnet, für beide Fraktionen
identisch. Das ist der einzige Verteidigungs-Entscheidungsraum des MVP.

Der Befehl `InstallDefenseModule` existiert bereits im Command-Schema und wird
heute deterministisch abgelehnt — die Registerstelle ist also frei gehalten.

### 3.5 · S · Platzierungsregeln und Reparaturkosten

- Bau-Einflussradius (8 Zellen um HQ / Lager / Kraftwerk), Mindestabstand zu
  Aetherium-Feldern, Gebäudeabstand. Heute prüft der Code nur „innerhalb der
  Karte" und „Zelle frei" — Basen lassen sich beliebig über die Karte streuen.
- Reparatur kostet heute **nichts**. Das Design verlangt 30 % des Neupreises.
  Kostenlose Reparatur entwertet Angriffe auf Gebäude fast vollständig, sobald
  ein Builder in der Basis steht.

### 3.6 · M · Legion-Waffenidentität

Der ScopeLedger registriert es als bewussten Konflikt: Salven- und
Flächenschaden sind der **einzige** Träger der Legion-Identität im Kampf (ein
generisches Fähigkeitssystem ist für MS-1 ausgeschlossen). Beide fehlen. Damit
spielen sich die Fraktionen im Gefecht faktisch gleich — die Allianz-Rolle
„Präzision, Einzelziel" ist nur durch Hitscan zufällig erfüllt.

---

## Phase 4 — Es klingt

**Ziel:** Jede Handlung hat eine hörbare Quittung.
Details, Quellen und Katalog: [04_Audioplan.md](04_Audioplan.md).

### 4.1 · S · **Blocker zuerst:** Lizenzrahmen um Audio erweitern

[../../assets/Licenses.md](../../assets/Licenses.md) kennt für Audio nur
Sonniss — und Sonniss darf nicht ins öffentliche Repository. Regel 6 desselben
Dokuments setzt „Default-Deny" für neue Anbieter. **Jede andere Audio-Quelle ist
damit aktuell gesperrt.**

Schritt 1 ist also nicht „Sounds suchen", sondern „Licenses.md §1 um
Audio-Zeilen ergänzen". Wer vorher importiert, verletzt die eigene Governance.

Gute Nachricht: Kenney ist bereits gedeckt — die CC0-Zeile nennt die **Quelle**,
nicht die Asset-Kategorie.

### 4.2 · M · Sim-zu-View-Ereigniskanal

Die technische Vorbedingung, die die Audio-Architektur nicht löst. Die
Präsentation liest heute ausschließlich **pollend**. Für Zustände (Low Power,
Match beendet) reicht das. Für **Ereignisse** (Schuss, Treffer, Tod, Bau fertig)
nicht — die passieren innerhalb eines Ticks und sind im nächsten Frame nicht
mehr am Zustand ablesbar.

Regelkonforme Lösung: ein Zustands-Differ in der Präsentation (voriger Frame
gegen aktuellen). Audio darf laut Architektur nicht in die Simulation
zurückwirken; der naheliegende Pfusch — die Sim feuert Events — würde den
Determinismus gefährden.

### 4.3 · M · `IAudioService`, Mixer, Import-Regeln

Backend ist mit D-039 entschieden (Unity Audio hinter stabiler Abstraktion,
FMOD erst ab Alpha). Es braucht keine Recherche, nur Umsetzung. Dazu der
Mixer-Baum, die Ordner- und Namenskonvention und — leicht zu übersehen — die
**Mono-Regel**: Unity spatialisiert Stereo-Clips nicht sinnvoll, und viele
CC0-Packs liefern Stereo.

### 4.4 · M · Tier-0-Katalog: die zwölf Sounds, ohne die es kaputt klingt

`UI_Click`, `UI_Select`, `UI_Ack`, `UI_Deny`, `WPN_Kinetic_Light`,
`WPN_Kinetic_Heavy`, `WPN_Explosive`, `IMP_Kinetic`, `IMP_Explosive`,
`DTH_Unit`, `DTH_Building`, `PRD_UnitReady`.

`UI_Deny` ist der wertvollste davon: er bindet an den Ablehnungsgrund und macht
die heute unsichtbaren Ablehnungen endlich lesbar.

---

## Phase 5 — Es sieht aus *(läuft parallel ab Tag eins)*

Vollständige Bestellliste mit Dateinamen, Specs und Zielordner:
[03_Bestellliste_Grafik.md](03_Bestellliste_Grafik.md).

Der Grafiker arbeitet extern und blockiert niemanden. Die einzige Abhängigkeit
in die andere Richtung: **P0-0 (Naming- und Ordnerkonvention für alles, was
keine der 34 Rollen ist) muss vor der ersten Lieferung festgelegt sein** — sonst
kann der Grafiker die Hälfte der Liste nicht benennen und nichts landet
reproduzierbar am richtigen Ort.

| Stufe | Inhalt | Warum |
|---|---|---|
| **P0** | Aetherium-Kristall, Baustellen-Meshes, Bau-Ghost, 34 Teammasken | Ohne diese ist eine Runde nicht lesbar |
| **P1** | Icons, Auswahlmarker, Lebensbalken, Cursor, Minimap-Grafik, Normal Maps, Bodentexturen, Legion-Emissive, Mesh-Nacharbeit | Macht das Spiel verständlich |
| **P2** | Kampf-VFX, Zerstörungs-VFX, Wirtschafts-VFX, Skybox, Gelände-Props, bewegte Teile, Infanterie-Rigs | Macht es gut |
| **P3** | Fraktionslogos, Menü-Artwork, Portraits, Ergebnisbildschirme, App-Icon | Politur |

---

## Phase 6 — Es heißt Hashkrieg

Vollständiger Ablauf und Risikoanalyse: [05_Umbenennung.md](05_Umbenennung.md).

Bewusst **zweistufig**, weil beide Stufen völlig verschiedene Risiken tragen:

### Stufe A · S · Die Marke — jederzeit sofort machbar

`productName`, `companyName`, Fenstertitel, README, Repository-Beschreibung,
Doku-Titel. Kein Build-Risiko. Das ist der „Umzug", der sich unmittelbar
anfühlt — und er kostet einen Nachmittag.

Ausnahmen, die **nicht** angefasst werden: `CHANGELOG.md` ist Historie (nur ein
neuer Eintrag), und der Build-Ausgabepfad steht doppelt — auch im Gate-Prüfskript.

### Stufe B · L · Die Code-Identität — als isolierter Sprint

17 Assemblies, 226 Namespaces, 560 using-Zeilen, drei hart validierte
Repository-Konstanten. Der asmdef-Rename ist **nur atomar sicher**: die
Referenzen sind Klartext-Namen, kein GUID. Ein einzelner geänderter `name`-Wert
ohne die referenzierenden Dateien legt die gesamte Unity-Kompilation lahm.

Voraussetzung: kein anderer offener Branch, keine parallele Phase.

---

## Phase 7 — Es erzählt

Vorschläge im Detail: [06_Narrative.md](06_Narrative.md).

### 7.1 · S · Neun Strings, die die Fraktionen hörbar trennen

Aus dem heutigen Namens-Wildwuchs eine Fraktionsdoktrin machen. Kostet neun
Stringänderungen in einer JSON-Datei und wirkt ab dem ersten HUD-Text. Löst
nebenbei einen echten Defekt: „Aegis" ist doppelt vergeben — Gebäude **und**
Einheit derselben Fraktion.

Hängt an 2.5 (ohne sichtbare Anzeigenamen zahlt jede Namensarbeit null aus).

### 7.2 · S · Sieben Erzählzeilen, die auf **heute vorhandener** Mechanik sitzen

[../../vision/Lore.md](../../vision/Lore.md) §7 hat die richtige Denkweise, aber
alle fünf Beispiele setzen Hashkrieg-Systeme voraus, die es nicht gibt. Ersatz:
Low-Power-Einbruch, Radar-Pings, Verkaufserstattung, sichtbarer Countdown zum
Zeitlimit, ein einmaliger Bark beim ersten Verlust, fraktionsabhängige Sieg- und
Niederlagentexte, sichtbar leerlaufende Felder.

Acht Textzeilen für den größten Ton-Gewinn im ganzen Plan.

### 7.3 · L · Minimal-Kampagne „Erster Feldzug" — fünf Missionen, nur Allianz

Das Kampagnendokument fordert drei Akte und zwölf bis fünfzehn Missionen. Das
ist bei zwei Entwicklern nicht baubar, und ein Drittel davon spielt die
Evolvierten, die nicht im Umfang sind.

Der Kniff: **skriptete Angriffswellen sind ungleich billiger als eine
reagierende KI**, und vier von fünf Missionen brauchen überhaupt keinen
denkenden Gegner. Diese Reihenfolge macht das Spiel vorzeigbar, **bevor** die
KI-Arbeit fertig ist.

Zwei Widersprüche sind vorher aufzulösen: Das Kampagnendokument baut auf einer
anderen Weltgenese als die Lore (Aetherium als Ursache statt als Folge), und es
plant ab Akt III eine Fraktion ein, die es nicht gibt.

**Präventive Vertragszeile:** Missions-Skripte müssen durch die
`CommandIngress`. Wenn ein Trigger Einheiten direkt spawnt, ist jede Mission
nicht aufzeichenbar und beim Replay desynchron — derselbe Fehler, den die KI
heute schon macht. Jetzt festzulegen kostet nichts; nach fünf gebauten Missionen
ist es eine Umbauaktion.

---

## Der kürzeste Weg zu „das fühlt sich wie ein Spiel an"

Wenn nur begrenzte Zeit da ist, ist das die Reihenfolge mit der höchsten
Rendite — sie ist eine Teilmenge des Plans, keine Abkürzung daran vorbei:

| # | Paket | Klasse | Wirkung |
|---|---|---|---|
| 1 | 0.1–0.3 Sicherung | S–M | verhindert, dass die 34 Assets verloren gehen |
| 2 | 1.1 KI registrieren und ausbauen | L | aus Sandkasten wird Spiel |
| 3 | 1.2 Zielerfassung und Feuererwiderung | M | aus Buchhaltung wird Gefecht |
| 4 | 2.1 Bauleiste plus Fraktionswahl | L | bedienbar, und der Content verdoppelt sich |
| 5 | 1.3 Wirtschaftsdruck | M | die Runde bekommt einen Bogen |
| 6 | 1.4 + 2.4 Menü und Ergebnis | M | die Runde bekommt einen Rahmen |
| 7 | 4.1–4.4 Audio Tier 0 | M | jede Handlung bekommt eine Quittung |
| 8 | 3.1 + 3.2 Lager, Radar, Low-Power | M | kein Gebäude ist mehr eine Attrappe |

Phase 5 P0/P1 läuft die ganze Zeit extern mit. Phase 6 Stufe A passt in jede
Lücke.

## Offene Punkte

- Die vier Inhaberentscheidungen aus [README.md](README.md).
- Aufwandsklassen sind geschätzt, nicht gemessen.
- Fünf Steuerdokumente stehen noch auf dem mit D-076 abgeschafften Gate-Regime
  und müssten nachgezogen werden, bevor jemand sie als Ist-Stand liest.
- Ob die MS-1-Abgrenzung (D-056) unverändert gilt, ist nicht ausdrücklich
  bestätigt.

## Nächste Schritte

1. E-1 bis E-4 entscheiden.
2. Phase 0 abschließen.
3. Parallel: [03_Bestellliste_Grafik.md](03_Bestellliste_Grafik.md) P0 an den
   Grafiker geben und Phase 6 Stufe A machen.
4. Phase 1 als ersten Sprint anlegen, mit 0.5 als Abnahmekriterium.
