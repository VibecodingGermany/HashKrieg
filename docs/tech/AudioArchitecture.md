# Audio-Architektur

**Version:** 0.2.2 | **Status:** Entwurf – MS-1-Override verbindlich | **Verantwortungsbereich:** Lead Audio Designer | **Sprint:** 3

## Zweck

Technisches Design der Audio-Architektur von Project Nova: `AudioService`-Abstraktion mit Unity-Audio-Backend, Bus-/Kategoriestruktur, Voice-Management, adaptive Musik aus Sim-Daten, 3D-Sound-Setup für die isometrische Kamera, datengetriebene Sound-Zuweisung via ScriptableObjects sowie Lokalisierung und Performance-Budgets. Die FMOD-Zielarchitektur ist Post-MVP und keine MS-1-Verpflichtung.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-039 (historisches Audio-Zielbild), D-056 (MS-1-Umfang), D-057 (Sim/View-Trennung), D-058 (Lastkorridore), D-060 (Unity-Pin), D-061 (Abnahme)
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

Die Abschnitte 2–8 bewahren das frühere Vollspiel-/Alpha-Zielbild als
Designreserve. Sie autorisieren weder Commander-/Voice-Code noch FMOD, adaptive
Musik, Wetter-Ambience oder 500 produktive Audioemitter in G0–G5. Für MS-1 sind
ausschließlich dieser Override, D-056 und die G4-/G5-Akzeptanzprofile führend.

## 1. Grundprinzipien

1. **Audio ist reiner Presentation-Layer.** Kein Audio-Ereignis wirkt in die Simulation zurück; Audio muss **nicht deterministisch** sein (D-033, Research §„Grundprinzip"). Die Unity-freie Assembly `Nova.Simulation` (D-035) kennt **keine** Audio-APIs – die Kopplung erfolgt ausschließlich über Sim-Events/Zustands-Snapshots, die der Presentation-Layer liest.
2. **Abstraktion von Tag 1.** Alle Sound-Aufrufe laufen über `IAudioService`. Kein Aufruf von `AudioSource.Play()`, `AudioMixer`- oder FMOD-APIs außerhalb der Backend-Implementierung. Damit ist der Middleware-Wechsel Alpha (Unity Audio → FMOD) ein Backend-Tausch, kein Refactoring.
3. **Vollständig datengetrieben.** Sound-Zuweisungen leben in Definitions-SOs über die GameDatabase (Definitions-only, kein Runtime-State in SOs – Vier-Säulen-Prämisse). Laufzeit-Zustände (Cooldowns, aktive Voices) liegen im AudioService, nicht in SOs.

## 2. AudioService-Abstraktion

Namespace `Nova.Presentation.Audio`. Die Schnittstelle ist backend-neutral; Begriffe wie „Event" werden bewusst FMOD-kompatibel gewählt (SoundEvent-ID statt Clip-Referenz an der API-Oberfläche).

```csharp
namespace Nova.Presentation.Audio
{
    /// <summary>Einzige Audio-Einstiegsstelle für Gameplay-, UI- und Commander-Code.</summary>
    public interface IAudioService
    {
        // One-Shots
        AudioHandle Play2D(SoundEventId id, AudioCategory category, VoicePriority priority);
        AudioHandle Play3D(SoundEventId id, SimPosition worldPos, AudioCategory category, VoicePriority priority);

        // Loops / bewegte Quellen (Fahrwerk, Harvester, Ambience-Betten)
        AudioHandle StartLoop(SoundEventId id, AudioCategory category, EntityViewRef followTarget);
        void StopLoop(AudioHandle handle);

        // Musik
        void SetMusicIntensity(float intensity01);   // aus Sim-Daten, siehe §5
        void SetMusicState(MusicState state);        // Menu | Match | Victory | Defeat

        // Bus-Steuerung (Optionen-Menü, Ducking)
        void SetBusVolume(AudioBus bus, float linear01);
        void Duck(AudioBus bus, float seconds);      // z. B. Commander-Voice duckt SFX

        // Commander-Event-Voiceovers (Spam-Regel intern, siehe §4.3)
        void RequestCommanderLine(CommanderEvent evt);
    }

    public enum AudioCategory { Music, SfxWeapons, SfxUnits, Ui, VoiceCommander, VoiceBarks, Ambience }
    public enum VoicePriority { Critical, High, Normal, Low }  // Stealing-Reihenfolge
    public readonly struct SoundEventId { /* stabiler String-Key, aus SO generiert */ }
}
```

**Backend-Implementierungen:**

- `UnityAudioService` (MVP): AudioSource-Pool, `AudioMixer` für Busse, selbstgebautes Mini-Voice-Limit (§4.2). Kein FMOD-Package im MVP-Build.
- `FmodAudioService` (ab Alpha, committed laut Research-Empfehlung): mappt `SoundEventId` auf FMOD-Event-Pfade, 1:1 hinter derselben Schnittstelle. Wechsel erfolgt per DI/Service-Registrierung, Aufrufer bleiben unverändert.

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

## 4. Voice-Management bei 500 Einheiten

### 4.1 Priorisierung

| Klasse | Priorität | Beispiel |
|---|---|---|
| Kritisch | Critical | Superwaffen-Warnung, HQ unter Beschuss, Commander-Events „hoch" |
| Wichtig | High | Basis unter Beschuss, Mauer durchbrochen, Sieg/Niederlage |
| Normal | Normal | Acknowledge/Selection-Barks, Waffenfeuer im Fokus |
| Niedrig | Low | Combat-Barks, Schritte/Fahrwerk entfernter Einheiten |

Stealing-Reihenfolge bei erschöpftem Voice-Budget: Low → Normal → …; Critical wird nie gestohlen.

### 4.2 Budgets und Culling (Richtwerte, tunbar per `AudioBudgetSO`)

- **Simultane Stimmen gesamt:** MVP 32 (Unity-Audio-Deckel), Alpha/FMOD 64 real / 256 virtuell.
- **Distanz-Culling:** 3D-Quellen jenseits der Max-Distanz (§6) werden gar nicht erst gestartet; zusätzlich Bildschirmrand-Culling – hörbar, aber außerhalb des Kamerafrustums + Puffer → Stimme nur, wenn Budget frei.
- **Bark-Budgets:** max. 1 Acknowledge-Bark pro Auswahl-Aktion (Gruppenbefehl an 40 Einheiten = 1 Stimme); globaler Bark-Cooldown ~1,5 s; Cooldown pro Einheiten**gruppe** statt pro Einheit (Research: „ein Nachmittag Arbeit" im MVP).
- **Concurrency-Deckel:** gleiche `SoundEventId` max. 3–4× gleichzeitig (Schuss-Variationen), danach Stealing des ältesten. MVP manuell im Service, Alpha nativ über FMOD Max-Instances/Cooldowns.

### 4.3 Commander-Event-Voiceovers (Post-MVP)

Post-MVP-Zielregel aus [../gamedesign/CommanderSystem.md](../gamedesign/CommanderSystem.md) §3: **max. 1 Event-Ansage alle ~8–12 s** (Richtwert); kritische Events (Superwaffe, HQ unter Beschuss) **dürfen unterbrechen**. Der Service führt dann eine priorisierte Warteschlange: Normal-Priorität wird bei vollem Cooldown verworfen (nicht gestaut), Critical preempted die laufende Ansage. Kategorien und Mengen (~42–55 Lines/Commander im historischen Vollspielentwurf) folgen CommanderSystem §4. Dieser Absatz ist keine MS-1-Anforderung.

## 5. Adaptive Musik

- **Intensitätsquelle ist die Simulation, gelesen nicht gekoppelt:** Ein `MusicIntensityProvider` (Presentation-Layer) aggregiert pro Sekunde aus Sim-Snapshots: aktive Kampf-Ereignisse nahe eigener Einheiten, eigene Verluste/min, Basis-Beschuss, Superwaffen-Countdown. Ergebnis: `intensity01` geglättet (Anstieg schnell ~2 s, Abfall langsam ~15–20 s) → `SetMusicIntensity`.
- **Post-MVP-Zielbild (Unity Audio):** 2–3 Musik-Stems (Ruhe / Spannung / Gefecht) per Crossfade über Mixer-Snapshots; Sieg/Niederlage als harte State-Übergänge.
- **Alpha (FMOD):** Parameter-gesteuertes vertikales Layering, gleiche `intensity01`-Semantik – Aufrufer-Code ändert sich nicht.
- Musik ist im Post-MVP-Zielbild fraktionsgebunden; MS-1 fordert keine
  Commander-Definition und keine adaptive Stem-Implementierung.

## 6. 3D-Sound-Setup (isometrische Kamera)

- **Listener-Position:** Ein AudioListener, auf die **Kamera-Fokuspunkt auf dem Terrain** projiziert (nicht an der Kamera selbst), mit minimalem Zoom-Höhen-Offset. Konsequenz: Zoomen ändert die Hörlautstärke von Weltquellen (Auszoomen = leiser + mehr Lowpass) – gewünschter RTS-Effekt; Feintuning über Distanzkurve.
- **Distanzmodell:** `Logarithmic Rolloff`, Min-Distanz ~15 m, Max-Distanz ~120 m (bei Karten 128/192/256 m: Welt bleibt hörbar „in der Nähe", Überlagerung aus dem halben Match wird vermieden). Kurve und Max-Distanz pro Kategorie im `AudioBudgetSO` überschreibbar (Waffen weiter als Schritte).
- **Panning:** 3D-Quellen spatialisiert (Spatial Blend 1.0) für Links/Rechts-Ortung am Bildschirmrand; Barks und UI bleiben 2D.
- **Ambience:** 2D-Bett pro Biom (Wind, Grundrauschen) + Wetter-Layer, die an die periodischen Events aus Biomes.md (Sandsturm, Schneesturm, Monsun, Sporenflug, Strahlungsfront, Staubsturm) gekoppelt sind – inkl. der dort definierten 15/20-s-Vorwarnung als Audio-Cue. Ortsfeste Zonen (Nebelbänke, Smog) erhalten keine eigenen Audio-Trigger, ggf. Position-Loops im MVP out of scope.

## 7. Datenmodelle (ScriptableObjects, Definitions-only)

```csharp
// GameDatabase-registriert; kein Runtime-State in diesen Assets.
public class SoundEventSO : ScriptableObject
{
    public string EventKey;            // -> SoundEventId (MVP: Clip-Set, Alpha: FMOD-Pfad)
    public AudioCategory Category;
    public VoicePriority DefaultPriority;
    public AudioClip[] Variations;     // MVP-Backend; Alpha: Referenz bleibt als Editor-Preview
    public int MaxConcurrent;          // Concurrency-Deckel §4.2
    public float CooldownSeconds;
}

public class UnitSoundSetSO : ScriptableObject   // Referenziert aus UnitDefinitionSO
{
    public SoundEventSO OnSelected, OnAcknowledge, OnAttack, OnDeath;
    public SoundEventSO MoveLoop, WeaponFire;
}

public class AudioBudgetSO : ScriptableObject    // ein Asset, global
{
    public int MaxVoicesTotal;         // 32 MVP / 64 Alpha
    public float BarkGroupCooldown;    // ~1.5 s
    public float CommanderMinInterval; // ~8–12 s Korridor (Min/Max)
    public float MusicIntensityRise, MusicIntensityFall;
}

public class CommanderVoiceSetSO : ScriptableObject  // aus CommanderDefinitionSO.VoiceProfileID
{
    public string Locale;              // Post-MVP-Lokalisierung
    public CommanderLineEntry[] Lines; // Event -> Clips/Keys + Priorität (Katalog = CommanderSystem §4)
}
```

Zuweisungsweg: `UnitDefinitionSO → UnitSoundSetSO → SoundEventSO`. Einheiten ohne eigenes Set fallen auf Fraktions-Defaults zurück. Untertitel der Commander-Lines sind UI-Sache; Audio liefert pro Line einen Lokalisierungsschlüssel mit.

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
| Reale Stimmen gesamt | ≤ 32 | ≤ 64 (virtuell 256) |
| 3D-Quellen aktiv | ≤ 24 | ≤ 48 |
| Loops (Fahrwerk/Harvester/Ambience) | ≤ 8 | ≤ 16 |
| CPU-Budget Audio-Thread | ≤ 1 ms/Frame (Update im Service, kein per-Einheit-Polling) | ≤ 1,5 ms inkl. FMOD-Update |
| Musik-Stems geladen | 3 | Bank-basiert, Streaming |

Begründung MVP-Deckel: zwei Fraktionen, eine Karte, Sound-Dichte weit unter
der Vollspielvision – native Grenzen reichen (Research §2). Alle Werte stehen
im `AudioBudgetSO`; spielkritische Alerts bleiben hörbar und untertitelt.
Commander-Voice gehört nicht zu MS-1.

## Offene Punkte

- **D-039 ist vorhanden, aber für MS-1 begrenzt:** Unity Audio bleibt das
  zulässige Backend; FMOD besitzt ohne neue Post-MVP-Entscheidung keinen
  Implementierungstermin.
- **FMOD-Budgetschwelle:** Erst bei einer Post-MVP-Reaktivierung gegen dann
  aktuelle Lizenz- und Finanzdaten prüfen.
- **Listener-Modell:** Fokuspunkt-Listener mit Zoom-Lautstärke-Kopplung ist eine Design-Annahme; Alternative (Listener starr an Kamera, manuelle Ducking-Kurve) im MVP-Prototyp per Gehör entscheiden.
- **Wetter-Vorwarn-Cues:** Biomes.md definiert 15/20-s-Vorwarnung „Audio" ohne konkrete Cue-Liste – Cue-Design (global vs. räumlich) mit Level Design abstimmen.
- **KI-Barks:** Ob KI-gesteuerte Einheiten (Command-only, 3-Schichten-KI) im Singleplayer hörbar acknolwedgen (Feedback für KI-Aktionen) oder stumm bleiben, ist gamedesign-seitig nicht festgelegt.

## Nächste Schritte

- Sprint 4: DecisionLog-Eintrag (D-ID) für die Audio-Entscheidung beantragen; dieses Dokument danach auf die D-ID umhängen.
- Sprint 5–6: `SoundEventSO`/`UnitSoundSetSO`-Schema mit Lead Programmer gegen die GameDatabase-Konventionen (Definitions-only, Registry) prüfen.
- Nach G0/G1 in G4 ausschließlich `IAudioService` +
  `UnityAudioService`, Source-Pool, Mixer, Alerts, Untertitel und die
  minimalen Allianz-/Legion-/Aetherium-Events integrieren.
- G4/G5-Audio gegen UI-only-Matches und Accessibility-Anforderungen prüfen.
- Commander-Casting, Commander-Spam-Queue, externe Musikproduktion und FMOD
  bis zu einer Post-MVP-D-ID sperren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Audio Designer |
| 0.2.0 | 2026-07-24 | MS-1-Audioumfang, synthetischen 500er-Lasttest, Post-MVP-FMOD und Unity-Pin gemäß D-056/D-058/D-060/D-061 abgegrenzt | Lead Audio Designer |
| 0.2.1 | 2026-07-24 | MS-1 auf zwei Fraktionen korrigiert und Commander-/FMOD-Arbeit aus Sprint 7 entfernt | Lead Audio Designer |
| 0.2.2 | 2026-07-24 | D-039-Anker korrigiert und Vollspiel-Audio, Commander-Voice sowie adaptive Musik ausdrücklich aus G0–G5 entfernt | Lead Audio Designer |
