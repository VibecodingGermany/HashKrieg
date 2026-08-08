# Audioplan — von null auf funktional, lizenzsauber

**Version:** 0.2.0 | **Status:** Tier 0 technisch umgesetzt (D-090), manuelle Gegenhörabnahme offen | **Verantwortungsbereich:** Technical Art / Audio | **Sprint:** 12

## Zweck

Dieses Dokument hält den ausgeführten Tier-0-Stand und darunter die historische
Beschaffungsplanung fest. Der Kampf- und HUD-Pfad besitzt jetzt zwölf
Soundereignisse, einen D-039-konformen Unity-Service, Mixer, SFX-Regler und 35
lizenzsaubere Kenney-CC0-Dateien. Die manuelle Gegenhörabnahme bleibt offen.

## Abhängigkeiten

- [../../tech/AudioArchitecture.md](../../tech/AudioArchitecture.md) – Service-Schnittstelle, Mixer-Topologie, Stimmen-Budgets, 3D-Setup
- [../../assets/Licenses.md](../../assets/Licenses.md) – zulässige Quellen, Default-Deny-Regel
- [../../assets/Provenance.md](../../assets/Provenance.md) – Provenienzpflicht, gilt ausdrücklich auch für Audio
- [../../assets/AssetRegister.md](../../assets/AssetRegister.md) – Beschaffungsstrategie (enthält veraltete Audio-Zeilen, siehe §5)

---

## Ausgeführter Tier-0-Stand (D-090)

- **Ereignisse:** `UI_Click`, `UI_Select`, `UI_Ack`, `UI_Deny`,
  `WPN_Kinetic_Light`, `WPN_Kinetic_Heavy`, `WPN_Explosive`, `IMP_Kinetic`,
  `IMP_Explosive`, `DTH_Unit`, `DTH_Building`, `PRD_UnitReady`.
  `ALR_BaseUnderAttack` bleibt Tier 1 und wurde nicht vorgezogen.
- **Auslösung:** `VisibleCombatFrameDiffer` liefert fog-sichere Kampf-Cues;
  Menü/HUD/Input rufen UI-Cues direkt über `AudioServiceLocator` auf. Kein
  Audioaufruf mutiert die Simulation.
- **Backend:** `Nova.Gameplay.Audio.IAudioService` und `UnityAudioService` sind
  der einzige neue One-Shot-Pfad. Die zwölf `SoundEventSO`-Assets tragen
  Kategorie, Standardpriorität, Variationen, Cooldown, Gain, Distanz und
  Concurrency. `DTH_Building` startet Low-Frequency- und Impact-Layer atomar.
- **Mischung:** `MIX_Master` enthält `Music`, `SFX`, `Voice` und `Ambience`.
  Exponiert sind fünf dB-Parameter. Projektweit bleiben 32 reale Stimmen;
  zwei sind für bestehende Musikcontroller reserviert, 30 für One-Shots und
  höchstens 24 für räumliche Quellen. Je Schlüssel gelten 3–4 Instanzen,
  logarithmischer Rolloff 15–120 m, keine Warteschlange und Stealing nur von
  älteren Stimmen strikt niedrigerer Priorität. `EffectiveSfxVolume` wird mit
  0 → −80 dB auf den SFX-Bus abgebildet.
- **Ablage und Format:** Genau 35 unveränderte `.ogg`-Dateien liegen
  **pack-first** unter `Audio/Sfx/Kenney/{SciFi,Impact,Interface}`. Die
  Quelldateinamen bleiben erhalten; `.ogg` ist für unveränderte CC0-Quellen
  ausdrücklich zulässig. `Force To Mono` gilt für die 3D-Familien, nicht für
  UI. Kurze SFX laden dekomprimiert; Originalbytes und SHA-256 bleiben gleich.
- **Provenienz:** Je Kenney-Pack liegt ein Batch-Sidecar mit `files[]` vor.
  Genau drei §3-Lizenzzeilen reichen aus; keine neue §1-Zeile und kein
  `CREDITS.md`. Das Musik-Sidecar existiert, bleibt bei allen vier Tracks
  ehrlich `incomplete`.
- **Legacy-Grenze:** `MenuMusicPlayer` und `MusicDirector` bleiben eine
  ausdrücklich dokumentierte D-090-Übergangsausnahme mit Routing auf `Music`.
  Sie sind nicht das Ziel des neuen Tier-0-One-Shot-Vertrags.

## Offene Abnahme

Die Budget- und Strukturtests sind automatisiert. Noch nicht durch eine
gespielte Runde belegt sind Klangbalance, Verständlichkeit bei ungefähr
sechzig feuernden Einheiten und die Entscheidung Kamera- versus
Fokuspunkt-Listener. Bis dahin sind Gain, Cooldown und Priorität konservative
Startwerte.

---

# Ursprüngliche Beschaffungsplanung (historisch)

Die folgenden Abschnitte erklären die Ausgangslage. Aussagen wie „vollständig
stumm", „kein Mixer" oder „Import blockiert" sind durch den obigen Stand und
D-090 ersetzt.

## 1. Der Blocker, der vor allem anderen steht

> **Es darf heute keine einzige Audiodatei importiert werden.**

[../../assets/Licenses.md](../../assets/Licenses.md) führt für Audio genau eine
Quelle: **Sonniss GDC Bundle** — royalty-free zur Verwendung *in* Spielen, aber
mit ausdrücklichem Verbot, die Rohdateien in ein öffentliches Repository zu
legen. `Project_Nova` ist ein öffentliches GitHub-Repository. Die einzige
dokumentierte Audio-Quelle des Projekts ist damit für den gewählten Arbeitsmodus
unbrauchbar.

Verschärfend: Regel 6 desselben Dokuments setzt **Default-Deny** — *„Neu
aufkommende Anbieter gelten bis zur dokumentierten Einzelprüfung als gesperrt."*
Freesound, OpenGameArt und jede andere Quelle sind damit aktuell gesperrt.

**Schritt 1 ist also nicht „Sounds suchen", sondern
[../../assets/Licenses.md](../../assets/Licenses.md) §1 um Audio-Zeilen
ergänzen.** Wer vorher importiert, verletzt die eigene Governance.

**Die gute Nachricht:** Kenney ist bereits gedeckt. Die CC0-Zeile in
`Licenses.md` nennt die **Quelle** (`Quaternius / Kenney / Poly Haven /
ambientCG`), nicht die Asset-Kategorie. Kenney-Audio-Packs fallen damit ohne
neue Lizenzzeile unter den bestehenden Rahmen: CC0-1.0, kommerziell erlaubt,
**keine Namensnennungspflicht**, öffentliches Repository erlaubt.

---

## 2. Quellenempfehlung für `Licenses.md` §1

Sortiert nach absteigender Repo-Tauglichkeit.

### Stufe 1 — sofort nutzbar, keine neue Zeile nötig

**Kenney** (`kenney.nl`) · CC0-1.0 · kommerziell ja · Namensnennung **nein** ·
öffentliches Repo ja.

Relevante Packs: *Sci-Fi Sounds*, *Impact Sounds*, *UI Audio*, *Interface
Sounds*, *Digital Audio*. Deckt UI-Klicks, Impacts, Alerts und einen Großteil
der Waffenbasis ab.

**Empfehlung: MS-1 zu rund 80 % aus Kenney bauen** — dann wird eine
`CREDITS.md` nie nötig, und die Pflegelast entfällt dauerhaft.

### Stufe 2 — neue Zeile nötig, nur mit CC0-Filter

| Quelle | Lizenzlage | Vorbehalt |
|---|---|---|
| **OpenGameArt.org** | gemischt: CC0-1.0 / CC-BY-3.0 / CC-BY-SA / GPL | Einzelprüfung je Datei zwingend, weil die Lizenz pro Einreichung variiert. **CC-BY-SA und GPL für Spiel-Assets meiden** (Copyleft-Ansteckungsrisiko). |
| **Freesound.org** mit Filter „Creative Commons 0" | CC0-1.0, keine Namensnennung | **Ohne gesetzten Filter ist die Quelle eine Falle** — Freesound enthält daneben CC-BY-4.0, CC-BY-NC (NC = absolutes Importverbot) und Sampling+. |

### Stufe 3 — nur mit Attribution, für MS-1 bewusst vermeiden

CC-BY-4.0-Material jeder Quelle (Freesound CC-BY, Kevin MacLeod / incompetech
für Musik). Kommerziell erlaubt, aber Namensnennung **Pflicht** — das löst die
Anlage von `CREDITS.md` aus und erzeugt dauerhafte Pflegelast.

### Stufe 4 — gesperrt für das öffentliche Repository

- **Sonniss GDC Bundle** — royalty-free, aber Weitergabe der Sammlung untersagt.
  Nur brauchbar, wenn Audio außerhalb des öffentlichen Git liegt. Für MS-1
  verwerfen.
- **Jede NC-Lizenz** — absolutes Importverbot nach der Provenienzregel.

### Vierte Säule: Eigenerzeugung

Selbst aufgenommene oder synthetisierte Sounds laufen als `original-work` und
sind lizenzrechtlich der sauberste Pfad. Für Motoren, Einschläge und Alarme oft
schneller als die Suche.

> **Vorbehalt:** Diese Einstufungen sind nach dem Provenienz-Workflow **zum
> Abrufzeitpunkt je Datei** zu verifizieren und zu archivieren. Das ist keine
> Rechtsberatung; Zweifelsfälle gehen an eine menschliche Entscheidung.

---

## 3. Die technische Vorbedingung, die sonst übersehen wird

> Ohne diese Schicht ist die halbe Sound-Liste **nicht auslösbar**.

Die Präsentation liest heute ausschließlich **pollend**: sie fragt jeden Frame
den aktuellen Zustand ab. Für **Zustände** genügt das (Low Power an/aus, Match
beendet). Für **Ereignisse** nicht — Schuss, Treffer, Tod, Bau fertig, Einheit
fertig passieren *innerhalb eines Ticks* und sind im nächsten Frame nicht mehr
am Zustand ablesbar.

Die Audio-Architektur verbietet ausdrücklich, dass Audio in die Simulation
zurückwirkt, und `Nova.Simulation` darf keine Unity-APIs kennen. Der
naheliegende Pfusch — die Simulation feuert Events — würde den Determinismus
gefährden.

**Die einzige regelkonforme Lösung ist ein Zustands-Differ in der
Präsentation:** Snapshot des vorigen Frames gegen den aktuellen.

| Beobachtung | Ereignis |
|---|---|
| Trefferpunkte gesunken | Treffer |
| Entität verschwunden | Tod |
| Baufortschritt hat 100 % erreicht | Bau fertig |
| Warteschlangenlänge gesunken | Einheit fertig |

Diese Schicht ist **Masterplan 4.2** und Voraussetzung für 4.4.

Was ohne Differ direkt abfragbar ist: die vier Ergebniscodes, der
Low-Power-Zustand, der Ablehnungsgrund eines Befehls, die Sichtbarkeit im Fog of
War. Damit sind die Zustands-Sounds ohne jeden Eingriff in die Simulation
auslösbar.

---

## 4. Der Sound-Katalog

Gestaffelt nach Wirkung pro Aufwand. Gesamtumfang MS-1: **rund 20 Ereignisse,
rund 60 Dateien** — vollständig aus Kenney-CC0 deckbar.

### Tier 0 — ohne diese zwölf fühlt sich das Spiel kaputt an

| Schlüssel | Auslöser | Anmerkung |
|---|---|---|
| `UI_Click` | jeder Menü- und HUD-Klick | 2D |
| `UI_Select` | Einheit selektiert | |
| `UI_Ack` | Befehl akzeptiert | **eine** Stimme pro Gruppenbefehl, nicht pro Einheit |
| `UI_Deny` | Befehl abgelehnt | **die wertvollste Position der Liste** — bindet an den Ablehnungsgrund und macht die heute unsichtbaren Ablehnungen endlich lesbar |
| `WPN_Kinetic_Light` | Infanterie, Späher | |
| `WPN_Kinetic_Heavy` | LightTank, BattleTank, Verteidigungsplattform | |
| `WPN_Explosive` | Panzerabwehr, Artillerie, Legion-BattleTank | |
| `IMP_Kinetic` | Einschlag kinetisch | getrennt vom Schuss, weil Kampf Hitscan ist und beides im selben Tick liegt |
| `IMP_Explosive` | Einschlag explosiv | |
| `DTH_Unit` | Einheit stirbt | |
| `DTH_Building` | Gebäude zerstört | |
| `PRD_UnitReady` | Einheit fertig produziert | |

### Tier 1 — macht die Wirtschaft lesbar

| Schlüssel | Auslöser |
|---|---|
| `BLD_PlaceOk` | Bauauftrag angenommen |
| `BLD_Complete` | Gebäude fertiggestellt |
| `ECO_HarvestLoop` | Loop am erntenden Harvester |
| `ECO_Deposit` | Abgabe an der Raffinerie — die Belohnungsquittung des Kernloops |
| `ALR_LowPower` | Energiedefizit tritt ein |
| `ALR_BaseUnderAttack` | eigenes Gebäude nimmt Schaden — **Cooldown ~20 s**, sonst Dauerfeuer |

### Tier 2 — Rahmen

`MTC_Victory`, `MTC_Defeat`, `MTC_Draw` (decken die vier Ergebniscodes ab) ·
`AMB_Bed` (ein 2D-Ambience-Loop) · `MUS_Menu` und `MUS_Match` (zwei Tracks, kein
adaptives Layering — das ist per MS-1-Abgrenzung ausdrücklich draußen).

### Variationen sind Pflicht, nicht Kür

Je Ereignis **2–4 Variationen** anlegen. Die Ereignis-Definition führt ein
Variationen-Array, und der Gleichzeitigkeitsdeckel erlaubt 3–4 gleiche
Instanzen. Ohne Variationen wird Infanteriefeuer zum Maschinengewehrgeräusch aus
einer einzigen Datei.

---

## 5. Ordner, Namen, Import-Einstellungen

### Ordnerbaum unter `Assets/_Project/Audio/`

```
Sfx/Weapons/          Sfx/Impacts/
Sfx/Units/<Faction>/<Role>/
Sfx/Buildings/<Faction>/<Role>/
Sfx/Ui/               Alerts/
Ambience/             Music/
Mixer/                Events/
Source/               (unkomprimierte Arbeitsdateien, aus Builds ausgeschlossen)
```

Spiegelt die bestehende Art-Systematik, statt eine zweite zu erfinden.

### Dateinamen — neue Präfixe für die Namenskonvention

| Muster | Beispiel |
|---|---|
| `SFX_WPN_<Faction>_<Role>_<NN>.wav` | `SFX_WPN_Legion_BattleTank_01.wav` |
| `SFX_IMP_<DamageType>_<NN>.wav` | `SFX_IMP_Explosive_02.wav` |
| `SFX_UNIT_<Faction>_<Role>_<Event>_<NN>.wav` | |
| `SFX_BLDG_<Faction>_<Role>_<Event>_<NN>.wav` | |
| `SFX_UI_<Action>_<NN>.wav` · `SFX_ALR_<Key>.wav` | |
| `AMB_<Biome>_<Layer>.ogg` · `MUS_<State>[_<Faction>].ogg` | |
| `SND_<Key>.asset` (Ereignis-Definition) · `MIX_Master.mixer` | |

`<Faction>` ist PascalCase `Alliance` / `Legion`, `<Role>` exakt aus dem
Manifest, `<DamageType>` exakt aus dem Code-Enum (`Kinetic`, `Energy`,
`Explosive`, `Fire`, `Bio`, `Radiation`). Fraktionsübergreifende Sounds nutzen
das Token `Shared`.

**Das zweistellige `_<NN>` ist Pflicht, nicht optional** — es ist der
Variations-Index.

### Import-Einstellungen — die Mono-Regel entscheidet über die Ortung

Die Architektur fordert volle Spatialisierung für die Links-Rechts-Ortung am
Bildschirmrand, spezifiziert aber keine Import-Einstellungen. **Unity
spatialisiert Stereo-Clips nicht sinnvoll** — ein Stereo-Sample an einer
3D-Quelle wird nicht korrekt gepannt, und genau die Ortung, die im RTS die
Aufmerksamkeit lenkt, fällt aus. Viele CC0-Packs liefern Stereo.

| Einstellung | Wert |
|---|---|
| **Force To Mono** | **an für alle 3D-Quellen** (Waffen, Impacts, Einheiten, Gebäude) · aus für UI, Musik, Ambience |
| Load Type | Decompress On Load für kurze SFX · Streaming für Musik und Ambience |
| Compression | Vorbis, Quality ~70 für SFX · ADPCM als Alternative bei sehr kurzen Impacts |
| Preload Audio Data | **an für Alerts** — ein nachgeladener Alarm kommt zu spät |
| Samplerate-Override | 22050 Hz für UI-Klicks |

Ohne diese Zeilen fliegen die Speicher- und CPU-Zusagen der Architektur
(≤ 1 ms Audio-Thread, ≤ 32 Stimmen) ins Blaue.

### Mixer

Kein Mixer-Asset im Repository. Die Architektur definiert den Baum
`Master > Music / SFX (SFX_Weapons, SFX_Units, UI) / Voice / Ambience` mit
Sidechain-Ducking der Commander-Stimme auf SFX.

Für MS-1 genügt eine reduzierte Fassung ohne den Voice-Zweig (Commander-Voice
ist ausdrücklich außerhalb des Umfangs) — **die Voice-Gruppen sollten aber leer
angelegt werden**, damit der spätere FMOD-Umstieg die Mix-Einstellungen
übernehmen kann. Die Busse sind außerdem Voraussetzung für das Optionsmenü.

---

## 6. Provenienz — eine Präzisierung ist nötig

Die Provenienzregel verlangt eine Sidecar-Datei „direkt neben der Audiodatei"
mit „genau einem Datensatz". Bei 34 3D-Assets sind das 34 Datensätze — machbar.
Bei rund 60 Audiodateien wären es 60 Datensätze mit je 15 Pflichtfeldern und je
einem SHA-256, **obwohl alle aus einem CC0-Pack mit identischer Lizenz,
identischer URL und identischem Abrufdatum stammen**.

Das ist Buchhaltung ohne Erkenntnisgewinn und wird in der Praxis übersprungen —
womit die Provenienzkette faktisch reißt.

**Vorschlag:** Festschreiben, dass für Audio **ein Sidecar pro Import-Batch**
gilt (ein Pack = ein Ordner = ein Datensatz), erweitert um ein Feld `files` mit
Dateiname und Einzel-Hash je Sample. Das erhält die Hash-Prüfbarkeit und macht
den Import in Minuten statt Stunden erledigbar. Erfordert eine Präzisierung in
[../../assets/Provenance.md](../../assets/Provenance.md) §2.

### Zwei Dokumente widersprechen sich und müssen im selben Zug korrigiert werden

[../../assets/AssetRegister.md](../../assets/AssetRegister.md) §3.11 führt
weiterhin Sonniss als Haupt-SFX-Quelle (nicht repo-fähig) und Musik als
„Store-Tracks oder Composer-Auftrag, 20–100 USD/Track". Das kollidiert mit der
Kostenregel („0 Euro ist hart für MS-1") — und mit der eigenen Kostenschätzung
zwei Bildschirmseiten weiter unten, die 0 Euro nennt.

Wer §3.11 als Beschaffungsanweisung liest, läuft in eine Ausgabe, die die
Governance verbietet.

---

## 7. Prüflauf

Die Provenienzregel beschreibt ein künftiges Prüfskript und hält ausdrücklich
fest, dass es nicht existiert. Bei 34 Art-Assets ist Sichtprüfung noch machbar;
mit zusätzlich 60 Audiodateien aus mehreren Packs kippt das.

Ein Skript, das (a) jede Datei unter `Assets/_Project/Audio/` gegen einen
Provenienz-Datensatz hält, (b) `licenseId` gegen eine aus `Licenses.md` §1
abgeleitete Whitelist prüft und (c) Ledger und Sidecars gegeneinander abgleicht,
deckt den Großteil ab und passt in die bestehende `quality/scripts/`-Struktur.

## Offene Punkte

- `Licenses.md` §1 muss um Audio-Zeilen ergänzt werden — **blockiert jeden
  Import**.
- Die Provenienz-Granularität für Audio (§6) braucht eine Festlegung.
- `AssetRegister.md` §3.11 widerspricht der eigenen Kostenregel.
- Ob Musik überhaupt in MS-1 gehört, ist eine offene Umfangsfrage — zwei Tracks
  sind hier als Tier 2 vorgesehen, aber verzichtbar.

## Nächste Schritte

1. `Licenses.md` §1 um die Stufe-1- und Stufe-2-Zeilen ergänzen (Masterplan 4.1).
2. Zustands-Differ bauen (Masterplan 4.2) — ohne ihn ist Tier 0 zur Hälfte nicht
   auslösbar.
3. Service, Mixer und Import-Presets anlegen (Masterplan 4.3).
4. Tier 0 aus Kenney-CC0 beschaffen und einbauen (Masterplan 4.4).
