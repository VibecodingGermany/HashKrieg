# Audio-Architektur

**Version:** 0.3.0 | **Status:** Tier-0-Implementierung verbindlich (D-090), Vollspielabschnitte als Zukunftsbild | **Verantwortungsbereich:** Lead Audio Designer | **Sprint:** 12

## Zweck

Technisches Design der Audio-Architektur von Project Nova: `AudioService`-Abstraktion mit Unity-Audio-Backend, Bus-/Kategoriestruktur, Voice-Management, adaptive Musik aus Sim-Daten, 3D-Sound-Setup für die isometrische Kamera, datengetriebene Sound-Zuweisung via ScriptableObjects sowie Lokalisierung und Performance-Budgets. Die FMOD-Zielarchitektur ist Post-MVP und keine MS-1-Verpflichtung.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-039 (Audio-Service), D-056 (MS-1-Umfang), D-057 (Sim/View-Trennung), D-058 (Lastkorridore), D-060 (Unity-Pin), D-061 (Abnahme), D-090 (Tier-0-Ist-Vertrag)
- [../research/Animation_Audio_UI.md](../research/Animation_Audio_UI.md) §2 – historische Audio-Einschätzung (Unity Audio, spätere FMOD-Evaluierung, Wwise verworfen)
- [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md) – ausschließliches Post-MVP-Zielbild für Voice-Line-Kategorien, Spam-Regel und Lokalisierungs-Staffelung
- [../gamedesign/Biomes.md](../gamedesign/Biomes.md) – Wetter-/Hazard-Events als Ambience- und Bark-Auslöser
- [../gamedesign/Weapons.md](../gamedesign/Weapons.md), [../gamedesign/Units](../gamedesign/Factions.md) – Träger der Sound-Referenzen

## MS-1-Override (D-056/D-058/D-060/D-061)

MS-1 verlangt funktionales Audiofeedback für den geschlossenen
Allianz-/Legion-Core-Loop, aber keine finale Musik, Vertonung, Sprecheraufnahmen,
Wetter-Ambience oder FMOD-Migration. Das produktive Szenario umfasst 100
Einheiten; 500 Audioemitter sind nur ein synthetischer Lasttest. Editor und
Backend-Validierung verwenden Unity `6000.5.4f1`, Revision `d550df8bd089`, URP.

Die mit „Post-MVP" markierten Anteile bewahren das frühere Vollspiel-/Alpha-
Zielbild als Designreserve. Sie autorisieren weder Commander-/Voice-Code noch
FMOD, adaptive Musik, Wetter-Ambience oder 500 produktive Audioemitter. Für den
heutigen Stand führen D-039 und D-090.

## Tier-0-Iststand (D-090)

- Namespace und Assembly: `Nova.Gameplay.Audio` in `Nova.Gameplay`.
- `IAudioService` bietet One-Shots 2D/3D, Stop und lineare Buslautstärke; Loops,
  adaptive Musik, Ducking und Commander-Voice gehören noch nicht zur API.
- `UnityAudioService` besitzt 30 One-Shot-Sources. Zusammen mit zwei explizit
  reservierten Legacy-Musikstimmen bleibt das Projektlimit 32; maximal 24
  One-Shot-Stimmen sind räumlich.
- Zwölf `SoundEventSO`-Assets definieren Kategorie, Standardpriorität,
  2–4 Variationen, atomare Layer, Concurrency, Cooldown, Gain und 15–120-m-
  Distanzen. Aufrufer können Kategorie/Priorität gezielt überschreiben; sonst
  gelten die authorisierten Assetwerte.
- `MIX_Master` führt `Master > Music / SFX / Voice / Ambience`, darunter
  `SFX_Weapons`, `SFX_Units`, `UI`, `Voice_Commander` und `Voice_Barks`.
  Master/Music/SFX/Voice/Ambience-dB sind exponiert.
- `VisibleCombatFrameDiffer` liefert fog-sichere Kampf-Cues. UI/HUD/Input
  melden ihre Cues direkt. Kein Audiopfad schreibt in die Simulation.
- `MenuMusicPlayer` und `MusicDirector` bleiben eine ausdrücklich begrenzte
  D-090-Übergangsausnahme: Sie besitzen historische `AudioSource`-Lebenszyklen,
  sind aber auf den Music-Bus geroutet und durch zwei reservierte Stimmen im
  Gesamtbudget berücksichtigt.

## 1. Grundprinzipien

1. **Audio ist reiner Presentation-Layer.** Kein Audio-Ereignis wirkt in die Simulation zurück; Audio muss **nicht deterministisch** sein (D-033, Research §„Grundprinzip"). Die Unity-freie Assembly `Nova.Simulation` (D-035) kennt **keine** Audio-APIs – die Kopplung erfolgt ausschließlich über Sim-Events/Zustands-Snapshots, die der Presentation-Layer liest.
2. **Abstraktion für neue One-Shots.** Alle neuen Tier-0-Sound-Aufrufe laufen über `IAudioService`; nur `UnityAudioService` besitzt dafür Sources und Mixerparameter. Die beiden benannten Legacy-Musikcontroller sind bis zu ihrer späteren Migration die einzige Ausnahme.
3. **Datengetriebene Ereignisse.** Sound-Zuweisungen leben in `SoundEventSO`; Laufzeit-Zustände wie Cooldowns und aktive Stimmen liegen ausschließlich im Service. Eine GameDatabase-/`UnitSoundSetSO`-Kopplung ist Zukunftsbild, nicht Ist-Stand.

## 2. AudioService-Abstraktion

Namespace `Nova.Gameplay.Audio`. Die aktuelle Schnittstelle ist klein und
backend-neutral; Aufrufer sehen stabile Event-IDs statt Clip-Referenzen.

```csharp
namespace Nova.Gameplay.Audio
{
    public interface IAudioService
    {
        AudioHandle Play2D(SoundEventId id, AudioCategory category, VoicePriority priority);
        AudioHandle Play3D(SoundEventId id, AudioPosition position,
                           AudioCategory category, VoicePriority priority);
        void Stop(AudioHandle handle);
        void SetBusVolume(AudioBus bus, float linear01);
    }
}
```

**Backend-Implementierungen:**

- `UnityAudioService` (MVP): AudioSource-Pool, `AudioMixer` für Busse, selbstgebautes Mini-Voice-Limit (§4.2). Kein FMOD-Package im MVP-Build.
- `FmodAudioService` ist ein Post-MVP-Zielbild und weder paketiert noch terminiert.

**Vertrag, der den Wechsel absichert:** Aufrufer kennen nur `SoundEventId`, Kategorie, Priorität und Position. Variation (Random-Pitch, Alternativtakes) und Mix sind Sache des Backends bzw. später des FMOD-Studio-Projekts.

## 3. Kategorien und Busse

Mixer-Struktur (MVP: `AudioMixer`-Gruppen; Alpha: FMOD-Busse mit gleicher Topologie, damit Mix-Einstellungen übertragbar bleiben):

```
Master
├── Music
├── SFX
│   ├── SFX_Weapons      (Schüsse, Treffer, Explosionen)
│   ├── SFX_Units        (Fahrwerk, Schritte, Bau, Harvester)
│   └── UI               (Klicks, Alerts, Minimap-Pings)
├── Voice
│   ├── Voice_Commander  (Event-Voiceovers, duckt SFX um ~6 dB)
│   └── Voice_Barks      (Einheiten-Acknowledge/Selection/Combat)
└── Ambience             (Biom-Bett, Wetter-Layer, siehe §6)
```

Regeln: Commander-Voice ist immer verständlich (Sidechain-Ducking auf SFX/Ambience). UI ist 2D, nie abstandsgedämpft. Barks laufen **nicht** über den 3D-Distanzpegel der Welt-Position, sondern über das Bark-Budget (§4.2) – sie sind Lesbarkeits-Feedback, keine Raumakustik.

## 4. Voice-Management

### 4.1 Priorisierung

| Klasse | Priorität | Beispiel |
|---|---|---|
| Kritisch | Critical | Superwaffen-Warnung, HQ unter Beschuss, Commander-Events „hoch" |
| Wichtig | High | Basis unter Beschuss, Mauer durchbrochen, Sieg/Niederlage |
| Normal | Normal | Acknowledge/Selection-Barks, Waffenfeuer im Fokus |
| Niedrig | Low | Combat-Barks, Schritte/Fahrwerk entfernter Einheiten |

Stealing-Reihenfolge bei erschöpftem Voice-Budget: Low → Normal → …; Critical wird nie gestohlen.

### 4.2 Budgets und Culling (Richtwerte, tunbar per `AudioBudgetSO`)

- **Simultane Stimmen gesamt:** Projekt 32; davon zwei reservierte Legacy-Musikstimmen und 30 Tier-0-One-Shots. Alpha/FMOD-Zielbild: 64 real / 256 virtuell.
- **Räumliche One-Shots:** höchstens 24.
- **Distanz-Culling:** 3D-Quellen jenseits der Max-Distanz (§6) werden gar nicht erst gestartet; zusätzlich Bildschirmrand-Culling – hörbar, aber außerhalb des Kamerafrustums + Puffer → Stimme nur, wenn Budget frei.
- **Bark-Budgets:** max. 1 Acknowledge-Bark pro Auswahl-Aktion (Gruppenbefehl an 40 Einheiten = 1 Stimme); globaler Bark-Cooldown ~1,5 s; Cooldown pro Einheiten**gruppe** statt pro Einheit (Research: „ein Nachmittag Arbeit" im MVP).
- **Concurrency-Deckel:** gleiche `SoundEventId` max. 3–4× gleichzeitig (Schuss-Variationen), danach Stealing des ältesten. MVP manuell im Service, Alpha nativ über FMOD Max-Instances/Cooldowns.

### 4.3 Commander-Event-Voiceovers (Post-MVP)

Post-MVP-Zielregel aus [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md) §3: **max. 1 Event-Ansage alle ~8–12 s** (Richtwert); kritische Events (Superwaffe, HQ unter Beschuss) **dürfen unterbrechen**. Der Service führt dann eine priorisierte Warteschlange: Normal-Priorität wird bei vollem Cooldown verworfen (nicht gestaut), Critical preempted die laufende Ansage. Kategorien und Mengen (~42–55 Lines/Commander im historischen Vollspielentwurf) folgen CommanderSystem §4. Dieser Absatz ist keine MS-1-Anforderung.

## 5. Adaptive Musik (Post-MVP-Zielbild)

- **Intensitätsquelle ist die Simulation, gelesen nicht gekoppelt:** Ein `MusicIntensityProvider` (Presentation-Layer) aggregiert pro Sekunde aus Sim-Snapshots: aktive Kampf-Ereignisse nahe eigener Einheiten, eigene Verluste/min, Basis-Beschuss, Superwaffen-Countdown. Ergebnis: `intensity01` geglättet (Anstieg schnell ~2 s, Abfall langsam ~15–20 s) → `SetMusicIntensity`.
- **Post-MVP-Zielbild (Unity Audio):** 2–3 Musik-Stems (Ruhe / Spannung / Gefecht) per Crossfade über Mixer-Snapshots; Sieg/Niederlage als harte State-Übergänge.
- **Alpha (FMOD):** Parameter-gesteuertes vertikales Layering, gleiche `intensity01`-Semantik – Aufrufer-Code ändert sich nicht.
- Musik ist im Post-MVP-Zielbild fraktionsgebunden; MS-1 fordert keine
  Commander-Definition und keine adaptive Stem-Implementierung.

## 6. 3D-Sound-Setup (isometrische Kamera)

- **Listener-Position heute:** genau ein `AudioListener` an der Kamera. Der Fokuspunkt auf dem Terrain ist eine offene Gegenhöralternative und keine implementierte Zusage.
- **Distanzmodell:** `Logarithmic Rolloff`, Min-Distanz ~15 m, Max-Distanz ~120 m (bei Karten 128/192/256 m: Welt bleibt hörbar „in der Nähe", Überlagerung aus dem halben Match wird vermieden). Kurve und Max-Distanz pro Kategorie im `AudioBudgetSO` überschreibbar (Waffen weiter als Schritte).
- **Panning:** 3D-Quellen spatialisiert (Spatial Blend 1.0) für Links/Rechts-Ortung am Bildschirmrand; Barks und UI bleiben 2D.
- **Ambience:** 2D-Bett pro Biom (Wind, Grundrauschen) + Wetter-Layer, die an die periodischen Events aus Biomes.md (Sandsturm, Schneesturm, Monsun, Sporenflug, Strahlungsfront, Staubsturm) gekoppelt sind – inkl. der dort definierten 15/20-s-Vorwarnung als Audio-Cue. Ortsfeste Zonen (Nebelbänke, Smog) erhalten keine eigenen Audio-Trigger, ggf. Position-Loops im MVP out of scope.

## 7. Datenmodelle

```csharp
public class SoundEventSO : ScriptableObject
{
    public SoundEventId EventId;
    public AudioCategory Category;
    public VoicePriority DefaultPriority;
    public SoundVariation[] Variations; // je Variation ein oder mehrere atomare Layer
    public int MaxConcurrent;
    public float CooldownSeconds;
    public bool Spatialized;
    public float Gain, MinDistance, MaxDistance;
}
```

Heute referenziert der Szenen-Service die zwölf `SoundEventSO`-Assets direkt.
`UnitSoundSetSO`, `AudioBudgetSO`, Commander-Sets, Loops und GameDatabase-
Registrierung bleiben mögliche spätere Ausbaustufen.

## 8. Lokalisierung der Commander-Voice

Historisches Post-MVP-Zielbild aus CommanderSystem: Englisch vertont und
deutsche Untertitel zuerst, Deutsch und Englisch später vollständig vertont.
Für MS-1 ist Commander-/Voice-Content gemäß D-056 deaktiviert.

- `SoundEventId` ist locale-neutral; die Locale-Auflösung passiert im Backend über `CommanderVoiceSetSO.Locale` (Asset-Ordner/Bank pro Sprache). Aufrufer-Code ist sprachagnostisch.
- Post-MVP-Budget: zunächst 1 Sprecher × 3 Fraktionen (EN). Diese
  Asset-Struktur ist keine MS-1-Implementierungspflicht.
- Rest-Abhängigkeit Q-018 (Preis-/Budget-Rahmen) betrifft nur den Umfang der Alternativtakes, nicht die Staffelung.

## 9. Performance-Budget

| Metrik | MVP (Unity Audio) | Alpha (FMOD) |
|---|---|---|
| Reale Stimmen gesamt | ≤ 32: 30 One-Shots + 2 reservierte Musikstimmen | ≤ 64 (virtuell 256) |
| 3D-One-Shots aktiv | ≤ 24 | ≤ 48 |
| Loops (Fahrwerk/Harvester/Ambience) | nicht im Tier-0-Service | ≤ 16 |
| CPU-Budget Audio-Thread | ≤ 1 ms/Frame (Update im Service, kein per-Einheit-Polling) | ≤ 1,5 ms inkl. FMOD-Update |
| Musik-Stems geladen | 3 | Bank-basiert, Streaming |

Begründung MVP-Deckel: zwei Fraktionen, eine Karte, Sound-Dichte weit unter
der Vollspielvision – native Grenzen reichen. Globale Deckel sind derzeit
Servicekonstanten; ereignisspezifische Werte stehen in `SoundEventSO`.
Commander-Voice gehört nicht zu MS-1.

## Offene Punkte

- **D-039 ist vorhanden, aber für MS-1 begrenzt:** Unity Audio bleibt das
  zulässige Backend; FMOD besitzt ohne neue Post-MVP-Entscheidung keinen
  Implementierungstermin.
- **FMOD-Budgetschwelle:** Erst bei einer Post-MVP-Reaktivierung gegen dann
  aktuelle Lizenz- und Finanzdaten prüfen.
- **Listener-Modell:** Kamera ist der Ist-Stand; Fokuspunkt und Zoom-Kopplung im Gegenhörtest vergleichen.
- **Legacy-Musik:** `MenuMusicPlayer` und `MusicDirector` hinter den Service migrieren, wenn die Musik-API entschieden wird.
- **Wetter-Vorwarn-Cues:** Biomes.md definiert 15/20-s-Vorwarnung „Audio" ohne konkrete Cue-Liste – Cue-Design (global vs. räumlich) mit Level Design abstimmen.
- **KI-Barks:** Ob KI-gesteuerte Einheiten (Command-only, 3-Schichten-KI) im Singleplayer hörbar acknolwedgen (Feedback für KI-Aktionen) oder stumm bleiben, ist gamedesign-seitig nicht festgelegt.

## Nächste Schritte

- Tier-0-Mix mit einem dichten Gefecht hören und Gain/Cooldowns/Prioritäten
  erst auf Basis dieses Befunds nachstimmen.
- Den Kamera-Listener gegen einen Fokuspunkt-Listener gegenhören.
- `ALR_BaseUnderAttack` und weitere Wirtschafts-/Match-Cues nur in einem
  getrennten Tier-1-Umfang mit ausgewählter Quelle ergänzen.
- Commander-Casting, Commander-Spam-Queue, externe Musikproduktion und FMOD
  bis zu einer Post-MVP-D-ID sperren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Audio Designer |
| 0.2.0 | 2026-07-24 | MS-1-Audioumfang, synthetischen 500er-Lasttest, Post-MVP-FMOD und Unity-Pin gemäß D-056/D-058/D-060/D-061 abgegrenzt | Lead Audio Designer |
| 0.2.1 | 2026-07-24 | MS-1 auf zwei Fraktionen korrigiert und Commander-/FMOD-Arbeit aus Sprint 7 entfernt | Lead Audio Designer |
| 0.2.2 | 2026-07-24 | D-039-Anker korrigiert und Vollspiel-Audio, Commander-Voice sowie adaptive Musik ausdrücklich aus G0–G5 entfernt | Lead Audio Designer |
| 0.3.0 | 2026-08-08 | D-090-Tier-0-Iststand mit tatsächlichem Namespace, kleiner Service-API, Eventdaten, Mixer, Stimmenbudgets, Kamera-Listener und Legacy-Musikausnahme dokumentiert; Vollspiel-/FMOD-Anteile als Zukunftsbild markiert | Lead Audio Designer / Agent (Umsetzung) |
