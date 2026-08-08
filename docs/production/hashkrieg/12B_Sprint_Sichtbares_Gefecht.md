# Sprint 12 · Strang B — Man sieht und hört, dass geschossen wird

**Status:** technisch umgesetzt (2026-08-08) – manuelle 60-Einheiten-Sicht-/Gegenhörabnahme offen | **Übergeordnet:** [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md) Strang B | **Vorgänger:** [11_Sprint_Truppenfuehrung.md](11_Sprint_Truppenfuehrung.md) (umgesetzt, D-088) | **Entscheidung:** [D-090](../DecisionLog.md) | **Leitsatz:** ein Gefecht muss als Gefecht erkennbar sein

Dieses Dokument enthält zuerst den ausgeführten Stand und danach den
ursprünglichen Ausführungsplan vom 2026-08-07. Der Plan bleibt als historische
Begründung erhalten; bei Widerspruch führen D-090, der Ergebnisblock und der
[Umsetzungsreport](../../../reports/v8.6.0/sprint-12-strang-b/02-umsetzungsreport.md).
Abweichungen wurden nicht still bereinigt, sondern vollständig in D-090 und im
[ScopeLedger](../ScopeLedger.md) registriert.

---

## Ausführungsergebnis

| Stufe | Ausgeführter Stand |
|---|---|
| **1 · Bloom** | Im freigegebenen `DefaultVolumeProfile` wurde ausschließlich Bloom-Intensität 0 → 0,8 geändert. |
| **2 · Schuss** | `VisibleCombatFrameDiffer` liest nur fog-sichtbare `TryGetUnit`-Snapshots. `CombatEffectController` zeigt Mündungsstoß, maximal 0,1-s-Hitscan-Spur und Trefferstoß; aktive Effekte sind auf 64, Mündungslichter auf 8 gedeckelt. |
| **3 · Ton** | `IAudioService`/`UnityAudioService`, `MIX_Master`, zwölf `SND_*`-Events und `SfxSettingsBridge` sind verdrahtet. Der One-Shot-Pool hält 30 der projektweit 32 realen Stimmen und höchstens 24 räumliche; je Schlüssel gelten 3–4 Instanzen, atomare Layer und Prioritäts-Stealing. |
| **4 · Tod** | Bestätigte Tode halten den View 0,8 s, lösen Picking/Collider und geben danach die exakte Poolidentität frei. Gebäude erhalten Rauch, aber keine persistente Trümmerfläche. Die Heuristik ist fog-sicher und bewusst unvollständig. |
| **5 · Flipbook** | Optional und ausgelassen; Unity-Bordmittel genügen für diesen Durchgang. |
| **Assets** | Genau 35 unveränderte Kenney-OGGs: Sci-Fi 11, Impact 11, Interface 13. Pack-first-Ablage, drei `PROVENANCE.json` mit `files[]`, keine `desktop.ini`, keine Konvertierung oder Umbenennung. |
| **Musikprovenienz** | Ein Sidecar erfasst alle vier Suno-Tracks. Jeder Datensatz bleibt `incomplete`, weil mindestens ein echter Ursprungs- oder Konvertierungsbeleg fehlt; es wurde nichts erfunden. |
| **Determinismuswache** | Der nicht ausführbare A/B-Effektschaltertest wurde gemäß E9/D-090 durch `PresentationSourceBoundaryTests` ersetzt. Der Guard scannt Produktionsquellen außerhalb `Simulation/**`; der Differ selbst bleibt auf Fog-Sicht und `TryGetUnit`. |

### Nachweise

- `.dotnet/dotnet test tools/Nova.SimRunner.Tests/Nova.SimRunner.Tests.csproj -c Release --no-restore`:
  **549/549 grün**, 0 übersprungen. Das lokale SDK ist **8.0.318**.
- Unity EditMode: **521/521 grün**.
- Unity PlayMode: **8/9**. Der neue Slot-Reuse-Test ist grün; nur der
  bestehende headless `BarracksSpawnDiagnosisTests` scheitert an
  `RenderTexture.Create` und ist kein 12B-Logik- oder Compilefehler.
- Ein unabhängiger Abschlussaudit fand keine P0/P1-Befunde und regte zwei
  P2-Härtungen an. Vier zusätzliche Audio-Laufzeitverträge sowie der komplette
  Death-Hold-Abschluss (0,8-s-Grenze, Material-/Collider-Restoration und exakte
  Pool-Rückkehr) sind daraufhin als Tests ergänzt. Beide Testassemblies
  kompilieren mit dem Unity-Roslyn-Befehlsvertrag fehlerfrei. Ein erneuter
  Unity-Testlauf wurde vom Desktop-Freigabelimit vor dem Prozessstart abgewiesen;
  diese neuen Assertions sind deshalb ehrlich **nicht** in 521/521 bzw. 8/9
  eingerechnet.
- Das Editor-Authoring validierte 35 Importer, den Mixer, zwölf Events und die
  Bootstrap-Verdrahtung. Seine Nutzung reflektierter interner Mixer-APIs ist
  versionssensitiv und bricht bei Signaturdrift hart ab.
- Ein frischer universeller macOS-Build wurde erfolgreich erzeugt. Eine
  tatsächlich gespielte Abnahme mit ungefähr sechzig feuernden Einheiten ist
  noch offen; deshalb wird keine auditive oder visuelle Feinabstimmung als
  bestanden behauptet.

### Abweichungsprotokoll

D-090 und das ScopeLedger führen vollständig: dedizierter Differ statt zweier
Slot-Arrays; sichere statt vollständiger Todeserkennung; keine persistente
Trümmerfläche; D-039-Service statt direktem `PlayOneShot`; 35 pack-first-OGGs
statt semantisch umbenannter WAVs; `ALR_BaseUnderAttack` und Flipbook-Stufe 5
ausgelassen; Quellcode-Guard statt A/B-Schaltertest; kein eigener
Effektschalter; Kamera- statt Fokuspunkt-Listener; mögliche Cue-Verluste bei
Tick-Sprüngen; Tier-1-Vier-Augen-Ausnahme; unvollständige Musikbelege und das
versionssensitive Mixer-Authoring. Konservative Gain-/Cooldown-/Prioritätswerte
sind Startwerte bis zur Gegenhörabnahme.

---

# Ursprünglicher Ausführungsplan (historisch)

Die folgenden Aussagen beschreiben den Stand und die Annahmen vom 2026-08-07.
Insbesondere die damalige Parallelkoordination, SDK-Sperre, Mixerlosigkeit,
direkte-`PlayOneShot`-Vorgabe, vollständige Todesannahme, Trümmerfläche,
`ALR_BaseUnderAttack` und der Effekt-Schalter sind durch den Ergebnisblock und
D-090 ersetzt.

## 0. Parallelbetrieb — zwei Agenten, ein Repository

> **Dieser Strang wird ausgeführt, während GPT gleichzeitig an Strang A arbeitet.**
> Der Arbeitsbaum ist damit eine **bewegliche Grundlinie, kein Snapshot.**

Stand 2026-08-07, 21:26 Uhr: GPT arbeitet auf Branch **`chore/relay-publish`**.
Innerhalb einer halben Stunde sind dort sieben Dateien neu geschrieben und vier
weitere angelegt worden. Die Liste unten ist **eine Momentaufnahme und wächst** —
sie ersetzt nicht, dass vor der ersten Zeile `git status` frisch geprüft wird.

### GPTs Schreibbereich — nicht anfassen

```
Assets/_Project/Scripts/Networking/**            (gesamtes Verzeichnis)
Assets/_Project/Scripts/Gameplay/Match/MatchRunner.cs
Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs
Assets/_Project/Scripts/Gameplay/Match/MatchConfig.cs
Assets/_Project/Scripts/Gameplay/Nova.Gameplay.asmdef
Assets/Tests/EditMode/Gameplay/Nova.Gameplay.Tests.asmdef
Assets/Tests/EditMode/Gameplay/MatchConfigTests.cs
tools/Nova.SimRunner.Tests/LockstepNetworkTests.cs
tools/packaging/
ProjectSettings/ProjectSettings.asset
Assets/_Project/Settings/NovaUrp.asset
Assets/UniversalRenderPipelineGlobalSettings.asset
```

### Die vier Berührungspunkte

| Punkt | Regel |
|---|---|
| **`Nova.Gameplay.asmdef`** | GPT ändert sie gerade (fügt `Nova.Networking` hinzu). Strang B braucht sie voraussichtlich nicht — die Effektkomponente liegt in derselben Assembly, `UnityEngine.Audio` ist ein Modul und kein Assembly-Verweis. **Wenn sie doch nötig wird: melden, nicht editieren.** |
| **`tools/Nova.SimRunner.Tests`** | Verschiedene Dateien, **ein Testprojekt**. Ein rotes `LockstepNetworkTests` ist GPTs laufende Arbeit, **kein Befund von Strang B** — nicht reparieren, nicht anfassen, im Report vermerken |
| **Hot Files** (`CHANGELOG.md`, `DecisionLog.md`, `ScopeLedger.md`) | Beide Stränge brauchen sie bei der Integration, vor allem eine **D-Nummer**. Nummer als **vorläufig** kennzeichnen; wer zweitens integriert, nummeriert um |
| **`MatchRunner.cs`** | Strang B *schreibt* sie nicht, aber der Differ hängt an der Tick-Semantik, die GPT gerade umbaut (Input-Delay 1→3, Lockstep-Barrier, Stall). **Nicht gegen das heutige Verhalten bauen, als wäre es endgültig** |

### Was ohne jede Berührung läuft

Stufe 1 und der vordere Teil von Stufe 3 sind zu GPTs Schreibbereich **vollständig
disjunkt** und hängen weder an `MatchRunner` noch am Differ:

- **Stufe 1** — nur `Assets/DefaultVolumeProfile.asset`. GPT fasst `NovaUrp.asset`
  und `UniversalRenderPipelineGlobalSettings.asset` an; das sind andere Dateien.
- **Stufe 3, vorderer Teil** — Governance, Audio-Import, Mixer-Bus,
  `EffectiveSfxVolume` verdrahten **und die UI-Sounds**. Letztere brauchen den
  Differ nicht: `UI_Click`, `UI_Select`, `UI_Ack` und `UI_Deny` hängen an der
  Eingabe, nicht am Tick — [04_Audioplan.md](04_Audioplan.md) §3 hält ausdrücklich
  fest, dass der Ablehnungsgrund direkt abfragbar ist.

**Die bindende Reihenfolge aus §3 bleibt bestehen.** Wer sie unter Parallelbetrieb
umstellen will, holt dafür die Zustimmung des Inhabers ein und begründet es im
Report — eine stille Umstellung ist ein Defekt.

### Verifikationslage, für beide Stränge gleich

`dotnet --list-sdks` liefert auf dieser Maschine nur `10.0.302`; `global.json`
pinnt `8.0.318` mit `rollForward: disable`. **`dotnet test tools/Nova.SimRunner.Tests`
läuft hier nicht.** Das ist der Blocker aus [12_Sprint_Zu_Zweit.md](12_Sprint_Zu_Zweit.md)
§0.2 und ist offen. Bis er behoben ist, ist „grün" für beide Stränge eine
Behauptung — entweder das SDK installieren oder den Nachweis über die CI im PR führen.

---

## 1. Was seit Sprint 12 belegt ist

Alles hier ist am Code oder an der Datei nachgesehen, nicht aus dem Masterplan
übernommen. Codestellen sind über **Symbolnamen** verankert, nicht über
Zeilennummern — der Arbeitsbaum bewegt sich.

| Befund | Beleg |
|---|---|
| **Es braucht keine externe Grafik-Library** | `com.unity.modules.particlesystem` ist im Manifest, `LineRenderer` liegt im selben Modul und ist in `RallyFlagView` erprobt, `UnityEngine.Pool.ObjectPool<T>` ist seit 2021.1 Engine-Bestandteil |
| **Unity liefert die Partikeltextur mit** | `Default-Particle` ist Editor-Ressource; URP 17.5.0 hängt sie in `ParticlesUnlit.mat` bereits als `_BaseMap` ein. Ein frisch angelegtes ParticleSystem hat ohne Import und ohne Codezeile einen weichen Blob |
| **Bloom ist verkabelt und ausgeschaltet** | `Assets/DefaultVolumeProfile.asset`: Bloom-Override `active: 1`, `threshold: 0.9`, **`intensity: 0`**. HDR ist aktiv (`NovaUrp.asset` `m_SupportsHDR: 1`) |
| **Die SFX-Verkabelung steht zur Hälfte** | `GameSettings` trägt `sfxEnabled`, `sfxVolume`, `DefaultSfxVolume = 0.8f`, `EffectiveSfxVolume` — sämtlich ungenutzt, mit dem Kommentar „applied to future SFX sources only" |
| **Ein `AudioListener` existiert** | genau einer, in `Bootstrap.unity`. Die Annahme „fehlt" ist widerlegt |
| **Die Waffen sind Hitscan** | `CombatSystem`, Klassenkommentar: „instantly (MS-1 hitscan — no projectiles, no flight time, no splash)"; im Feuercode: „MS-1 hitscan: damage lands in the same tick, no projectile." |
| **Die steigende Cooldown-Flanke trägt** | `CombatSystem` dekrementiert `WeaponCooldownTicks` je Tick um 1 und setzt beim Feuern auf `weapon.AttackCooldownTicks`. `WeaponProfiles.FallbackAttackCooldownTicks = 5` (0,5 s bei 10 Hz) |
| **Der Differ braucht keinen neuen Hook** | `UnitViewManager.LateUpdate` hält je sichtbarer Einheit bereits die vollständige `UnitState` (`TryGetUnit`) und pflegt bereits einen Slot-Cache (`_boundIds`) |
| **Die View-Schleife ist fog-korrekt** | sie speist sich aus `fog.GetVisibleEntities`; im Code steht ausdrücklich „Nothing here touches EntityManager.RawUnits" |
| **`MatchRunner` hat eine ungedeckelte Aufholschleife** | `while (_timeAccumulator >= TickDeltaTime)` — nach einem Netzwerk-Stall laufen mehrere Ticks in einem Frame |
| **`.gitignore` würde Effektmaterial unter `Art/` verschlucken** | der Art-Block schließt unter `Assets/_Project/Art/**` `png`, `mat`, `prefab` **samt `.meta`** aus |
| **Die Kenney-Pakete liegen gesichert vor** | siehe §5 — heruntergeladen, SHA-256 erfasst, Lizenztext archiviert |

---

## 2. Entscheidungen des Inhabers (2026-08-07)

Der Inhaber hat den MVP-Plan als Ganzes angenommen. Daraus folgen diese
Festlegungen; sie sind für diesen Sprint bindend.

| # | Entscheidung |
|---|---|
| E1 | **Kenney-Audio ist durch `Licenses.md` §1 gedeckt.** Die CC0-Zeile nennt die *Quelle* („Quaternius / Kenney / Poly Haven / ambientCG"), nicht die Asset-Kategorie — dasselbe Dokument schreibt kategoriescharf, wo es das will (Hunyuan3D-Meshes vs. Modellgewichte). Kenney steht namentlich in der Erlaubt-Liste von Regel 6 und ist damit kein „neu aufkommender Anbieter". **Der Satz in Sprint 12 („Audio Tier 0 wartet auf die Lizenzerweiterung in §1") meint Nicht-Whitelist-Quellen und gilt für Kenney nicht.** Sprint 12 ist entsprechend zu präzisieren. Eine neue §1-Zeile entfällt; eine **§3-Ledgerzeile ist Pflicht** |
| E2 | **`Assets/DefaultVolumeProfile.asset` darf für die Bloom-Zeile committet werden.** Die Dauerregel aus Sprint 09 §2 bleibt für jede *andere* Änderung an dieser Datei bestehen — freigegeben ist ausschließlich `intensity` |
| E3 | **`Provenance.md` §2 wird um den Batch-Sidecar präzisiert:** ein Pack = ein Ordner = ein Datensatz, erweitert um ein `files[]`-Array mit Dateiname und Einzel-Hash. Ohne das ist ein 300-Dateien-Import formal nicht abbildbar |
| E4 | **Das Vier-Augen-Feld (`Provenance.md` §3 Schritt 7) wird als bewusste Tier-1-Ausnahme protokolliert.** Im Solo-Setup existiert die zweite Person nicht; das Feld bleibt leer, mit Begründung im Datensatz |
| E5 | **Die vier `MUS_*.ogg` bekommen ihre fehlenden Provenienzdatensätze nachgezogen**, wenn ohnehin Audio angefasst wird |
| E6 | **Kenney-Laserklang wird für den ersten Durchgang akzeptiert.** Kinetische Eigenerzeugung per `ffmpeg` ist die zweite Runde, nicht dieser Sprint |
| E7 | **Sichtbare Projektile werden gebaut** — rein visuell, unter den drei Bedingungen aus §4 |
| E8 | **Keine Todeseffekte für Einheiten außerhalb der eigenen Sicht.** Der Differ folgt der Fog-of-War-Grenze von `UnitViewManager` und leitet Tode niemals aus `EntityManager.RawUnits` oder EntityId-Gültigkeit ab |
| E9 | **Der A/B-Hash-Test aus Sprint 12 B5 wird durch einen Quellcode-Guard ersetzt** (§8, Abweichung 2) |

---

## 3. Der MVP in fünf Stufen

Jede Stufe liefert für sich etwas Sichtbares oder Hörbares und ist einzeln
abnehmbar. **Die Reihenfolge ist bindend** — Stufe 2 ohne Stufe 1 sieht flach
aus, und Stufe 3 hängt am Differ aus Stufe 2.

Aufwandsangaben sind **Schätzungen**, nicht gemessen.

### Stufe 1 · Bloom (Minuten)

**Danach:** für sich allein nichts — aber jeder additive Effekt der Stufe 2
überstrahlt, statt flach zu liegen.

**Schreibbereich:** `Assets/DefaultVolumeProfile.asset` (nur `intensity`).

Von `0` auf einen Wert im Bereich 0,6–1,2 anheben, im Spiel gegensehen. Kein
Code. Freigegeben durch E2.

### Stufe 2 · Der Schuss (Schätzung: halber bis ganzer Tag)

**Danach sichtbar:** Mündungsfeuer am Schützen, Leuchtspur zum Ziel über ~0,1 s,
Funkenstoß am Einschlag, sichtbares Projektil (§4).

**Schreibbereich:**
- `Assets/_Project/Scripts/Gameplay/Match/UnitViewManager.cs` (Differ)
- neue Effekt-Komponente unter `Assets/_Project/Scripts/Gameplay/` — **nicht**
  unter `Presentation/UI/` (Assembly-Rang, siehe §8 Abweichung 2)

**Der Differ gehört in die bestehende Schleife.** `UnitViewManager.LateUpdate`
hält je sichtbarer Einheit bereits die vollständige `UnitState` und pflegt schon
einen Slot-Cache. Der Differ sind zwei zusätzliche `int`-Arrays parallel zu
`_boundIds`: letzter `WeaponCooldownTicks`, letzte `CurrentHealth`.

| Ereignis | Ableitung |
|---|---|
| Schuss | `WeaponCooldownTicks` steigt gegenüber dem Vorwert **und** ein gültiges `AttackTarget` liegt an |
| Treffer | `CurrentHealth` gesunken |
| Tod | Slot verliert seine Bindung bzw. `IsActive` fällt — **nur innerhalb der Sichtbarkeitsmenge** (E8) |

**Vor der ersten Zeile prüfen:** Kein Waffenprofil darf `AttackCooldownTicks <= 1`
haben — bei 1 oder 0 ist die steigende Flanke nicht detektierbar und der Schuss
bleibt unsichtbar. Die Werte kommen aus den Definitionen, nicht aus dem Fallback.
Wenn ein Profil betroffen ist: **melden, nicht die Simulation anpassen** (das wäre
Strang C).

**Pooling:** `UnityEngine.Pool.ObjectPool<T>`. **Achtung:** `maxSize` begrenzt die
*ruhenden* Instanzen, **nicht die aktiven** — der geforderte Deckel von 64
gleichzeitigen Effekten braucht einen eigenen Aktivzähler, der bei Überlauf
**verwirft statt aufzustauen**.

**Bekannte Eigenschaft, kein Defekt:** `MatchRunner` holt nach einem Stall
mehrere Ticks in einem Frame nach. Ein frame-basierter Differ mit Slot-Cache
meldet dann nicht doppelt, **verschluckt aber Zwischenereignisse**. Das ist die
mildere Fehlerart als ein Effektgewitter und wird bewusst akzeptiert. Im Report
festhalten, damit Strang A es kennt.

### Stufe 3 · Der Ton (Schätzung: kleiner als Stufe 2, größter wahrgenommener Sprung)

**Danach hörbar:** Schuss, Einschlag, Explosion, HUD-Rückmeldung.

**Schreibbereich:** dieselbe Effekt-Komponente (`AudioSource.PlayOneShot` an
denselben Flanken), `Assets/_Project/Audio/Sfx/**`, `Assets/_Project/Audio/Mixer/`,
`docs/assets/Licenses.md` §3, `docs/assets/Provenance.md` §2,
`docs/assets/provenance-ledger.json`.

Beschaffung, Ablage und Zuordnung stehen vollständig in §5.

**Der Flaschenhals ist das Abmischen, nicht die Beschaffung.**
`ProjectSettings/AudioManager.asset` steht auf `m_RealVoiceCount: 32`, es gibt
kein AudioMixer-Asset, und die Kamera steht hoch über dem Geschehen. Ohne
Mixer-Bus, Rolloff-Kurve und Gleichzeitigkeitsdeckel je Ereignisschlüssel werden
sechzig feuernde Einheiten zu Brei. Das ist der eigentliche Aufwand dieser Stufe
und steht in keinem Abschnitt des Ursprungssprints.

Mindestens umzusetzen:
- Mixer `MIX_Master.mixer` mit `Master > Music / SFX / Ambience`; die
  Voice-Gruppe **leer anlegen** (späterer FMOD-Umstieg, `AudioArchitecture.md`).
- `EffectiveSfxVolume` aus `GameSettings` tatsächlich anwenden — das Feld
  existiert seit Sprint 10 ungenutzt.
- Pro Ereignisschlüssel 2–4 Variationen, zufällig gewählt (`UnityEngine.Random`
  ist in der Präsentation erlaubt), und ein Deckel von 3–4 gleichzeitigen
  Instanzen desselben Schlüssels.
- `ALR_BaseUnderAttack` mit ~20 s Cooldown, sonst Dauerfeuer.

### Stufe 4 · Der Tod (Schätzung: kleiner Tagesanteil)

**Danach sichtbar:** Einheiten sacken ab und blenden über ~0,8 s aus, statt
schlagartig zu verschwinden. Gebäude bekommen Rauchsäule und Absacken.

**Schreibbereich:** `UnitViewManager` (View-Freigabe erst nach Ablauf der
Sterbedarstellung).

**Kein Rig, keine Skelettanimation** — das Projekt enthält kein `.anim` und
keinen `.controller`, die Modelle sind statische Meshes. Absacken plus Alpha ist
die einzige ehrliche Option; nichts anderes versprechen.

**Die Falle ist real:** `UnitViewManager` recycelt Views über den Slot-Index. Wer
den View nicht bis zum Ende der Sterbedarstellung festhält, zeigt im
wiederverwendeten Slot die Leiche der vorigen Einheit. Ein Testfall, kein
Nebensatz.

### Stufe 5 · Die Explosionswolke (optional, erst nach 1–4)

**Danach sichtbar:** echte Detonationssilhouette statt überlappender Blobs.

**Weg:** Kenney *Smoke Particles*, `Explosion00..08` (9 Frames) zu einem
3×3-Atlas gepackt, `Texture Sheet Animation` mit `_FlipbookBlending`. Das ist die
eine Stelle, an der Bordmittel klar verlieren — ein Radialverlauf ergibt keine
Pilzsilhouette.

**Ablageregel, nicht verhandelbar:** Effekt-**Materialien** und -**Prefabs**
gehören **nicht** unter `Assets/_Project/Art/` — der `.gitignore`-Art-Block würde
sie samt `.meta` mitentfernen und ein frischer Clone hätte unsichtbare Effekte
statt eines sauberen Fallbacks. Ablage unter
`Assets/_Project/Scripts/Gameplay/…/Resources/` oder einem eigenen,
nicht-ignorierten VFX-Pfad. Die PNG-Texturen selbst dürfen unter `Art/VFX/`
liegen (0 MB Repo-Zuwachs), **dann muss aber ein Fallback auf `Default-Particle`
existieren**, sonst ist der Clone kaputt.

---

## 4. Die Projektil-Frage — entschieden

**Die Simulation ist Hitscan.** `CombatSystem` sagt es zweimal im Klartext:
Schaden und Cooldown fallen im selben Tick, es gibt kein Projektil-Entity und
keine Flugbahn.

**Die Absolutformel aus Sprint 12 („ein fliegendes Projektil wäre eine Lüge über
das, was die Simulation tut") ist nicht haltbar** — sie verbietet im selben
Absatz, was sie erlaubt. Eine LineRenderer-Leuchtspur, die 0,1 s zwischen
Schütze und Ziel steht, belegt denselben Raum zur selben Zeit wie ein gestrecktes
Quad, das dieselbe Strecke in 0,1 s durchläuft. Der Unterschied ist Rendering,
nicht Ehrlichkeit.

**Entschieden (E7): sichtbare Projektile werden gebaut**, unter drei harten
Bedingungen:

1. **Flugzeit ≤ 0,1 s** (ein Tick). Alles Langsamere lässt den Lebensbalken
   sichtbar *vor* dem Einschlag sinken — das wäre die Lüge.
2. **Die Zielposition wird im Erkennungsmoment einmal kopiert.** Kein Nachführen
   auf ein lebendes Entity — sonst behauptet das Projektil eine Ballistik, die es
   nicht gibt, und fliegt ins Leere, wenn das Ziel im selben Tick stirbt.
3. **Es darf nie verfehlen und nie abgefangen werden.** Ein Projektil, das
   danebengeht, widerspricht dem bereits gefallenen Schaden.

Umfang: ein `Vector3` und ein `Lerp` auf dem ohnehin gebauten Tracer.
**Schätzung: knappe Stunde obendrauf.** Fasst keine Simulationsdatei an.

---

## 5. Der Sound — beschafft, gesichert, zugeordnet

### 5.1 Ist-Stand der Beschaffung

Die drei Pakete sind **heruntergeladen und gesichert** (2026-08-07). Ablage
außerhalb des Repos, nach dem Muster von [AssetPackage.md](../../assets/AssetPackage.md):

```
/Volumes/2TB_CodingProjekte/Coding_Projekte/Hashkrieg_Assets/audio/sfx_source/kenney/
  kenney_sci-fi-sounds.zip        5 875 104 B
  kenney_impact-sounds.zip          800 850 B
  kenney_interface-sounds.zip       834 536 B
  SHA256SUMS.txt
  extracted/<paketname>/Audio/…    (entpackt zur Prüfung)
  extracted/<paketname>/License.txt
```

**SHA-256 der Originaldateien** — bitgenau gegen den Download geprüft, das ist
der Provenienznachweis und darf nie neu berechnet werden:

| Datei | SHA-256 |
|---|---|
| `kenney_sci-fi-sounds.zip` | `119340f351a5098ad814f78719438c0da355a9ce8a4c8a3af6a8d48aa3d49e04` |
| `kenney_impact-sounds.zip` | `029d734af1582474edf3a694d1b0cebc97c1c152f2f39fa34d4c2bafc5de77f8` |
| `kenney_interface-sounds.zip` | `f2193d072726d6758a5f7871b2dcc54dcce0d5c35c6f0a62f92549b327c81232` |

**Lizenztext, wörtlich aus `License.txt` des Pakets** (in allen drei identisch im
Aufbau, hier Sci-Fi Sounds 1.0):

> License: (Creative Commons Zero, CC0)
> http://creativecommons.org/publicdomain/zero/1.0/
> This content is free to use in personal, educational and commercial projects.
> Support us by crediting Kenney or www.kenney.nl (this is not mandatory)

Damit ist E1 nicht mehr Auslegung, sondern Beleg: **CC0, kommerziell erlaubt,
Namensnennung ausdrücklich nicht verpflichtend** → `CREDITS.md` wird nicht
ausgelöst (`Licenses.md` Regel 2 bindet die Attributionspflicht an CC-BY).

### 5.2 Paketinhalt

| Paket | Dateien | Format |
|---|---|---|
| Sci-Fi Sounds | 73 `.ogg` **+ 1 `desktop.ini`** | OGG Vorbis |
| Impact Sounds | 130 `.ogg` | OGG Vorbis |
| Interface Sounds | 100 `.ogg` | OGG Vorbis |

> **Die `desktop.ini` im Sci-Fi-Paket ist ein Windows-Artefakt und darf nicht
> importiert werden.** Sie liegt zwischen den OGGs und wird von einem
> Sammelimport sonst mitgenommen.

### 5.3 Zuordnung Tier 0 → Quelldatei

Aus dem Sound-Katalog in [04_Audioplan.md](04_Audioplan.md) §4.

| Schlüssel | Quelle | Dateien |
|---|---|---|
| `WPN_Kinetic_Light` | Sci-Fi | `laserSmall_000..004` |
| `WPN_Kinetic_Heavy` | Sci-Fi | `laserLarge_000..004` |
| `WPN_Explosive` | Sci-Fi | `explosionCrunch_000..004` |
| `IMP_Kinetic` | Impact | `impactMetal_light_000..004`, `impactPlate_light_*` |
| `IMP_Explosive` | Impact | `impactMetal_heavy_*`, `impactPlate_heavy_*` |
| `DTH_Unit` | Impact | `impactMetal_medium_*` |
| `DTH_Building` | Sci-Fi | `lowFrequency_explosion_000/001` + Impact `impactPlate_heavy_*` |
| `UI_Click` | Interface | `click_001..005` |
| `UI_Select` | Interface | `select_001..008` |
| `UI_Ack` | Interface | `confirmation_001..004` |
| `UI_Deny` | Interface | `error_001..008` |
| `PRD_UnitReady` | Interface | `confirmation_*` (andere Variante als `UI_Ack`) |

> **Nummerierung ist nicht einheitlich:** Sci-Fi und Impact zählen ab `000`, das
> Interface-Paket ab `001`. Wer die Namen generiert statt sie zu lesen, greift ins
> Leere.

**Ehrlicher Befund zur Deckung:** Kenney hat im gesamten Audiobestand **kein
kinetisches Feuerwaffengeräusch** — kein Gewehr, kein MG, keine Kanone. Alles
Waffenmaterial ist Laser/Energie. Für ein Aetherium-Sci-Fi-RTS ist das stimmig;
E6 akzeptiert es für den ersten Durchgang. Die Schlüsselnamen behalten trotzdem
`Kinetic`, weil sie an `DamageType` hängen, nicht an den Klang.

**`explosionCrunch_*` und `lowFrequency_explosion_*` sind das einzige
Explosionsmaterial im gesamten Kenney-Audiobestand.** Wenn es nicht trägt, ist
`ffmpeg`-Eigenerzeugung der Weg — nicht eine neue Fremdquelle (§7).

### 5.4 Format und Import

**Kenney liefert `.ogg`, der Audioplan §5 fordert `.wav` für SFX.** Konvertieren
bricht den SHA-256 gegen die Quelldatei und damit die Provenienzkette.

**Festlegung: die Konvention wird auf `.ogg` geöffnet, die Originaldateien
bleiben unverändert.** `04_Audioplan.md` §5 ist entsprechend zu präzisieren.

Mono/Stereo ist kein Thema: Unitys Import-Schalter **Force To Mono** überschreibt
die Kanalzahl. Er ist für alle 3D-Quellen (Waffen, Impacts, Einheiten, Gebäude)
Pflicht — ohne ihn fällt die Links-Rechts-Ortung aus, die im RTS die
Aufmerksamkeit lenkt. Für UI und Musik bleibt er aus.

### 5.5 Governance-Schritte für den Import

Abzuarbeiten **bevor** die erste `.ogg` unter `Assets/` liegt:

1. `docs/assets/Provenance.md` §2 um den Batch-Sidecar erweitern (E3): ein Pack =
   ein Ordner = ein Datensatz plus `files[]`-Array mit Dateiname und Einzel-Hash.
2. `docs/assets/Licenses.md` §3 um **eine Ledgerzeile je Paket** ergänzen — Datum,
   Paket/Quelle, `CC0 1.0`, unbegrenzt, 0 €, Attribution nein, Repo-Freigabe ja.
   **Keine neue §1-Zeile** (E1).
3. Je Paket ein `PROVENANCE.json` neben dem Zielordner, mit der Ursprungs-URL
   (`https://kenney.nl/assets/…`, **nicht** der Download-Link — der trägt einen
   inhaltsabhängigen Timestamp), Abrufdatum, ZIP-Hash aus §5.1 und `files[]`.
   `verifiedBy` bleibt leer, mit Begründung nach E4.
4. `provenance-ledger.json` um die drei Datensätze ergänzen.
5. Die vier `MUS_*.ogg` nachziehen (E5).

---

## 6. Bewusst nicht in diesem Sprint

| Punkt | Warum |
|---|---|
| **VFX Graph** | Nicht im Manifest. Braucht Compute-Shader, ist für Millionen Partikel ausgelegt; hier fallen 64 Effekte à 20–50 Partikel an. Neues Assetformat, neuer Editor, neue Kompetenz für drei Kleineffekte |
| **Unity Asset Store**, auch kostenlose Pakete | Doppelt gesperrt: der Store steht nicht in `Licenses.md` Regel 6 → Default-Deny; und die Store-EULA erlaubt Verbreitung nur „as incorporated and embedded in that Licensed Product" — ein öffentliches Repo ist genau der ausgeschlossene Fall |
| **Fremde Pooling-Bibliotheken** (uPools, kPooling, NightPool …) | Nachbauten von `UnityEngine.Pool.ObjectPool<T>`, das in der Engine liegt. Keiner gegen Unity 6 getestet, jeder kostet eine Regel-6-Einzelprüfung für null Mehrwert |
| **Freesound / OpenGameArt** | Technisch möglich, governance-teuer: neue §1-Zeile plus Prüfung **je Datei**, weil die Lizenz pro Upload variiert. Ein übersehener CC-BY-Treffer erzwingt `CREDITS.md`. Notreserve |
| **ChipTone / bfxr / usfxr** | Die Output-Freigaben sind nicht belegbar abrufbar; `Provenance.md` §3 sperrt genau diesen Fall. `ffmpeg` umgeht das vollständig |
| **Sonniss / ZapSplat / Pixabay** | Alle drei verbieten die Weitergabe der Rohdatei außerhalb eines eingebetteten Werks — derselbe Konflikt, den `Licenses.md` Fußnote [^2] für Sonniss bereits aufgelöst hat. Pixabay ist zusätzlich seit 2019 nicht mehr CC0 |
| **ambientCG / Poly Haven für Effekte** | Stehen in der Whitelist, liefern für Effekte aber nachweislich nichts (Decals sind Wandschlieren und Fahrbahnmarkierungen; Poly Havens API kennt nur `hdris`, `textures`, `models`) |
| **Prozeduraler Texturgenerator** | Widerlegt — Unity liefert `Default-Particle` mit, URP hängt sie bereits ein (§8 Abweichung 1) |
| **Skelettanimation** | Kein Rig, kein `.anim`, kein `.controller` im Projekt |
| **Kinetische Eigenerzeugung per `ffmpeg`** | Zweite Runde (E6) |
| **Audio Tier 1 und 2** | Dieser Sprint liefert Tier 0. Wirtschafts- und Rahmen-Sounds folgen, sobald Tier 0 im Spiel gegengehört ist |

---

## 7. Wenn ein Sound nicht trägt

Nicht neue Fremdquellen suchen — das kostet jedes Mal eine Regel-6-Einzelprüfung.
Der gedeckte Weg ist **Eigenerzeugung**: `ffmpeg` liegt lokal, Rauschschichtung
plus Hüllkurve ergibt Gewehr-, Kanonen- und Einschlagklänge, das Ergebnis läuft
als `original-work`, berührt Regel 6 gar nicht, und **die Kommandozeile selbst ist
der vollständige, bit-genau reproduzierbare Provenienznachweis.**

---

## 8. Abweichungen von Sprint 12 Strang B — einzeln begründet

Sprint 12 bleibt das übergeordnete Dokument. Diese drei Punkte weichen ab; sie
gehören mit dieser Begründung in den ScopeLedger.

**Abweichung 1 — „Partikeltexturen erste Wahl prozedural" wird verworfen.**
Sprint 12 B4 empfiehlt einen zur Laufzeit erzeugten Radialverlauf, um nichts
importieren zu müssen. Das ist von Null verschieden: Unity 6000.5.4 liefert
`Default-Particle` mit, URP 17.5.0 hängt sie in `ParticlesUnlit.mat` bereits als
`_BaseMap` ein. Ein prozeduraler Generator wäre Code, der geschrieben, getestet
und abgestimmt werden muss, für ein Ergebnis, das der Editor gratis danebenlegt.
**Stattdessen: Bordmittel, kein Generator, kein Import** — bis Stufe 5, wo ein
Flipbook nachweislich mehr kann.

**Abweichung 2 — der Determinismus-Nachweis B5 ist so nicht baubar.**
Sprint 12 fordert einen Test „State-Hash mit Effekten == State-Hash ohne
Effekte", mit dem Schalter in `GameSettings`. Drei Gründe, warum das nicht trägt:

1. `GameSettings` liegt in `Nova.Presentation.UI` (Rang 4);
   `quality/scripts/run_gate_check.py` verbietet jeder Test-Assembly Referenzen
   auf Rang ≥ 4.
2. Unity-Tests laufen laut `.github/workflows/tests.yml` mangels CI-Lizenz
   **überhaupt nicht** — der Test wäre grün, weil er nie läuft.
3. Die Einbahnstraße erzwingt der Compiler bereits: `Nova.Simulation` hat
   `noEngineReferences: true` und referenziert nur `Nova.Core`.

Das reale Restrisiko ist ein **Schreibzugriff aus der Präsentation** über
`EntityManager.GetUnitRef` oder `RawUnits`. **Ersatz (E9): ein CI-fähiger
Quellcode-Guard in `Nova.SimRunner.Tests`** nach dem Muster des bestehenden
`NoFloatInSimulationTests` — „kein `GetUnitRef` und kein Zugriff auf
`SimulationKernel.Random` außerhalb von `Simulation/`". Der läuft ohne Unity und
ohne Lizenz und fängt genau den Fehler, um den es geht.
Der Effekt-Schalter kommt trotzdem in `GameSettings` (er ist ohnehin nützlich,
wenn die Grafikkarte schwächelt), aber **die Effektschicht selbst gehört nach
`Nova.Gameplay`** (Rang 3, dort liegt `UnitViewManager`, und `Nova.PlayMode.Tests`
referenziert es bereits).

**Abweichung 3 — sichtbare Projektile werden erlaubt.** Begründung in §4.

---

## 9. Der ehrliche Preis

**Strang B kostet nichts am Simulationsvertrag** — keine Baseline wird rot, kein
Fingerprint ändert sich, kein Replay wird ungültig. Das ist die Eigenschaft, die
ihn neben Strang A laufen lässt, und sie muss durch den Guard aus Abweichung 2
belegt werden, nicht behauptet.

**Der Repo-Zuwachs ist real, aber klein.** Nur die tatsächlich verwendeten OGGs
wandern ins Repo, nicht die vollen 303 Dateien. Bei 2–4 Variationen je Schlüssel
und rund 12 Tier-0-Schlüsseln sind das grob 30–50 Dateien im niedrigen
einstelligen MB-Bereich. Dauerhaft, weil Git-Historie Binärdaten nie vergisst —
deshalb **kein Sammelimport aller drei Pakete**.

**Die Governance-Arbeit ist echte Arbeit**, aber einmalig: eine
`Provenance.md`-Präzisierung, drei Ledgerzeilen, drei Sidecars. **Schätzung:
unter einer halben Stunde**, sobald E3 steht.

---

## 10. Fertig wenn

1. Meine Panzer schießen, und **ich sehe es** — Mündungsfeuer am Rohr, ein
   sichtbares Geschoss auf dem Weg zum Ziel, ein Funkenstoß am Einschlag.
2. **Ich höre es** — Schuss, Einschlag, Explosion; und wenn sechzig Einheiten
   gleichzeitig feuern, ist es ein Gefecht und kein Brei.
3. Was stirbt, **sackt zusammen und blendet aus**, statt zu verschwinden. Der
   wiederverwendete View zeigt nie die Leiche der vorigen Einheit.
4. Ich klicke im HUD und **bekomme eine Rückmeldung**; ein abgelehnter Befehl
   sagt es mir hörbar.
5. Ein Gefecht am Rand meiner Sicht verrät mir **keine Einheit, die ich nicht
   sehen dürfte**.
6. `dotnet test tools/Nova.SimRunner.Tests` ist grün, **einschließlich des neuen
   Quellcode-Guards**, und keine Baseline hat sich bewegt.

---

## 11. Prompt für Kimi / GPT

```text
AUFGABE: Sichtbares und hoerbares Gefecht (Hashkrieg, Sprint 12 Strang B)

============================================================================
ACHTUNG — DU ARBEITEST NICHT ALLEIN IN DIESEM REPOSITORY
============================================================================
GPT arbeitet ZEITGLEICH an Strang A (Netzwerk/Lockstep/Relay) im SELBEN
Arbeitsbaum, auf Branch chore/relay-publish. Innerhalb einer halben Stunde sind
dort sieben Dateien neu geschrieben worden. Der Arbeitsbaum ist eine BEWEGLICHE
GRUNDLINIE, kein Snapshot.

GPTs SCHREIBBEREICH — DU FASST DAVON NICHTS AN:
  Assets/_Project/Scripts/Networking/**            (gesamtes Verzeichnis)
  Assets/_Project/Scripts/Gameplay/Match/MatchRunner.cs
  Assets/_Project/Scripts/Gameplay/Match/MatchBootstrap.cs
  Assets/_Project/Scripts/Gameplay/Match/MatchConfig.cs
  Assets/_Project/Scripts/Gameplay/Nova.Gameplay.asmdef
  Assets/Tests/EditMode/Gameplay/Nova.Gameplay.Tests.asmdef
  Assets/Tests/EditMode/Gameplay/MatchConfigTests.cs
  tools/Nova.SimRunner.Tests/LockstepNetworkTests.cs
  tools/packaging/
  ProjectSettings/ProjectSettings.asset
  Assets/_Project/Settings/NovaUrp.asset
  Assets/UniversalRenderPipelineGlobalSettings.asset
Diese Liste ist eine MOMENTAUFNAHME und waechst. Pruefe git status frisch, bevor
du anfaengst, und halte die Zeitstempel gegen deinen Startzeitpunkt.

HARTE REGELN IM PARALLELBETRIEB:
1. NIEMALS git checkout, git stash, git restore, git clean oder git reset auf
   Dateien, die du nicht selbst geschrieben hast. Fremde uncommittete Arbeit zu
   "bereinigen" vernichtet Stunden. Das ist der schlimmste Fehler, den du hier
   machen kannst.
2. NIEMALS git add -A oder git add . — du committest sonst GPTs halbfertige
   Arbeit in deinen Commit. Jede Datei EINZELN per Pfad adden.
3. Fremde Aenderungen in git status sind NICHT deine Baustelle. Nicht reparieren,
   nicht aufraeumen, nicht kommentieren — ignorieren.
4. Ein rotes LockstepNetworkTests ist GPTs LAUFENDE ARBEIT, kein Befund von dir.
   Nicht reparieren, nicht anfassen. Im Report vermerken, dass es rot war und warum.
5. Nova.Gameplay.asmdef aendert GPT gerade. Du brauchst sie voraussichtlich NICHT
   (deine Effektkomponente liegt in derselben Assembly, UnityEngine.Audio ist ein
   Modul und kein Assembly-Verweis). Wenn du sie doch brauchst: MELDEN, NICHT EDITIEREN.
6. MatchRunner.cs schreibst du nicht — aber dein Differ haengt an der Tick-Semantik,
   die GPT gerade umbaut (Input-Delay 1->3, Lockstep-Barrier, Stall). Baue NICHT
   gegen das heutige Verhalten, als waere es endgueltig. Wenn du an den Punkt kommst
   und Strang A noch nicht gelandet ist: MELDEN statt raten.
7. Hot Files (CHANGELOG.md, DecisionLog.md, ScopeLedger.md) brauchen beide Straenge
   bei der Integration. Nimm dir eine D-Nummer, aber KENNZEICHNE SIE ALS VORLAEUFIG —
   wer zweitens integriert, nummeriert um.

WAS OHNE JEDE BERUEHRUNG MIT GPT LAEUFT:
  - Stufe 1 (Bloom): nur Assets/DefaultVolumeProfile.asset. GPT fasst NovaUrp.asset
    und UniversalRenderPipelineGlobalSettings.asset an — andere Dateien.
  - Stufe 3, vorderer Teil: Governance, Audio-Import, Mixer-Bus, EffectiveSfxVolume
    verdrahten UND die UI-Sounds. Letztere brauchen den Differ nicht — UI_Click,
    UI_Select, UI_Ack und UI_Deny haengen an der Eingabe, nicht am Tick.
Die bindende Reihenfolge aus 12B §3 bleibt trotzdem bestehen. Wenn du sie wegen des
Parallelbetriebs umstellen willst: FRAG DEN INHABER und begruende es im Report.
Eine stille Umstellung ist ein Defekt.
============================================================================

Lies zuerst docs/production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md vollstaendig.
Dieser Prompt ist die Kurzfassung; das Dokument ist verbindlich. Das uebergeordnete
Dokument ist 12_Sprint_Zu_Zweit.md — wo beide sich widersprechen, gilt 12B, und die
Begruendung steht dort in §8.

VORBEDINGUNGEN
1. git status frisch pruefen. Siehe den Parallelbetrieb-Block oben — das ist keine
   Formalie, sondern die wichtigste Regel dieses Auftrags.
2. EIGENER BRANCH, und zwar NICHT chore/relay-publish (das ist GPTs). main ist
   PR-only. NICHT pushen ohne ausdrueckliche Freigabe des Inhabers.
3. Niemals mitcommitten: AssetMappingRegistry.asset, Packages/manifest.json,
   Packages/packages-lock.json, die Screenshots im Repo-Wurzelverzeichnis,
   .playwright-mcp/. AUSNAHME: Assets/DefaultVolumeProfile.asset DARF committet
   werden, aber AUSSCHLIESSLICH fuer die Bloom-intensity-Zeile (Inhaberentscheidung E2).
4. Du fasst KEINE Datei in Nova.Simulation oder Nova.Networking an. Wenn du glaubst,
   du muesstest — melden statt tun. Das ist die Bedingung, unter der dieser Strang
   parallel zu Strang A laufen darf.
5. Das .NET-8-SDK 8.0.318 fehlt (installiert ist nur 10.0.302, global.json pinnt mit
   rollForward: disable). dotnet test tools/Nova.SimRunner.Tests LAEUFT HIER NICHT.
   Entweder das SDK installieren oder den Nachweis ueber die CI im PR fuehren —
   und in beiden Faellen im Report festhalten, was tatsaechlich gelaufen ist.
   Kein "gruen" behaupten, das niemand gesehen hat.

AUSGANGSLAGE — verifiziert am 2026-08-07, nicht neu diagnostizieren
- Unity 6000.5.4f1, URP 17.5.0. particlesystem-Modul ist im Manifest, VFX Graph NICHT.
- Es gibt im Produktionscode KEIN ParticleSystem, KEIN LineRenderer (ausser
  RallyFlagView), KEIN .anim, KEINEN .controller. Die Modelle sind statische Meshes.
- Unity liefert Default-Particle mit; URP haengt sie in ParticlesUnlit.mat bereits
  als _BaseMap ein. BAUE KEINEN prozeduralen Texturgenerator — das ist gestrichen.
- Bloom steht in Assets/DefaultVolumeProfile.asset auf active:1, intensity:0. HDR ist an.
- GameSettings hat bereits sfxEnabled, sfxVolume, DefaultSfxVolume=0.8f,
  EffectiveSfxVolume — alles ungenutzt. Ein AudioListener existiert in Bootstrap.unity.
- CombatSystem ist HITSCAN: "damage lands in the same tick, no projectile".
  WeaponCooldownTicks wird je Tick um 1 dekrementiert und beim Feuern auf
  weapon.AttackCooldownTicks gesetzt. Fallback ist 5 Ticks.
- UnitViewManager.LateUpdate haelt je sichtbarer Einheit bereits die volle UnitState
  (TryGetUnit) und pflegt einen Slot-Cache (_boundIds). Die Schleife speist sich aus
  fog.GetVisibleEntities.
- MatchRunner hat eine ungedeckelte Aufholschleife (while _timeAccumulator >= TickDeltaTime).

STUFE 1 — BLOOM (Minuten)
Assets/DefaultVolumeProfile.asset: Bloom-intensity von 0 auf 0,6–1,2 anheben, im Spiel
gegensehen. Kein Code. Ohne das liegt jeder additive Effekt flach.

STUFE 2 — DER SCHUSS
Zustands-Differ IN DER BESTEHENDEN SCHLEIFE von UnitViewManager.LateUpdate. KEINEN
neuen Hook in MatchRunner bauen. Zwei zusaetzliche int-Arrays parallel zu _boundIds:
letzter WeaponCooldownTicks, letzte CurrentHealth.
  Schuss   = WeaponCooldownTicks STEIGT gegenueber dem Vorwert UND gueltiges AttackTarget
  Treffer  = CurrentHealth gesunken
  Tod      = Slot verliert Bindung / IsActive faellt — NUR innerhalb der Sichtbarkeitsmenge
PRUEFE VOR DER ERSTEN ZEILE: kein Waffenprofil darf AttackCooldownTicks <= 1 haben,
sonst ist die steigende Flanke nicht detektierbar. Werte kommen aus den Definitionen,
nicht aus dem Fallback. Betroffenes Profil MELDEN, nicht die Simulation anpassen.
Effekte: Muendungsfeuer (Partikelstoss + kurzer Lichtimpuls), Leuchtspur per
LineRenderer mit ~0,1 s Ausblenden, Einschlagfunken je DamageType.
SICHTBARES PROJEKTIL IST ERWUENSCHT (Inhaberentscheidung E7) unter drei Bedingungen:
Flugzeit <= 0,1 s; Zielposition wird im Erkennungsmoment EINMAL kopiert, kein
Nachfuehren auf ein lebendes Entity; es darf nie verfehlen. Das ist ein Vector3 und
ein Lerp auf dem Tracer.
POOLING mit UnityEngine.Pool.ObjectPool<T>. ACHTUNG: maxSize begrenzt die RUHENDEN
Instanzen, NICHT die aktiven. Der Deckel von 64 gleichzeitigen Effekten braucht einen
EIGENEN Aktivzaehler, der bei Ueberlauf VERWIRFT statt aufzustauen.
FOG OF WAR: den Differ ausschliesslich ueber fog.GetVisibleEntities fuehren. NIEMALS
ueber EntityManager.RawUnits oder EntityId-Gueltigkeit — das waere ein Informationsleck.
BEKANNTE EIGENSCHAFT, kein Defekt: nach einem Stall holt MatchRunner mehrere Ticks in
einem Frame nach; der Differ verschluckt dann Zwischenereignisse. Bewusst akzeptiert,
im Report festhalten.
ABLAGE: die Effektkomponente gehoert nach Nova.Gameplay (Rang 3, dort liegt
UnitViewManager), NICHT nach Presentation/UI (Rang 4) — sonst kann keine Test-Assembly
sie referenzieren (quality/scripts/run_gate_check.py).

STUFE 3 — DER TON
Die drei Kenney-Pakete sind BEREITS BESCHAFFT und liegen ausserhalb des Repos unter
/Volumes/2TB_CodingProjekte/Coding_Projekte/Hashkrieg_Assets/audio/sfx_source/kenney/
(ZIPs, SHA256SUMS.txt und entpackt unter extracted/). NICHT NEU HERUNTERLADEN — die
Hashes in 12B §5.1 sind der Provenienznachweis und muessen die der dortigen Dateien bleiben.
Lizenz ist CC0 1.0, Namensnennung ausdruecklich "not mandatory" (License.txt im Paket).
CREDITS.md wird NICHT ausgeloest.
GOVERNANCE VOR DEM IMPORT, in dieser Reihenfolge:
  1. docs/assets/Provenance.md §2 um den Batch-Sidecar erweitern: ein Pack = ein Ordner
     = ein Datensatz plus files[]-Array mit Dateiname und Einzel-Hash (Inhaberentscheidung E3).
  2. docs/assets/Licenses.md §3: eine Ledgerzeile JE PAKET. KEINE neue §1-Zeile — Kenney
     ist durch die bestehende quellenbezogene CC0-Zeile gedeckt (E1).
  3. Je Paket ein PROVENANCE.json mit der Ursprungs-URL (https://kenney.nl/assets/…,
     NICHT der Download-Link — der traegt einen inhaltsabhaengigen Timestamp),
     Abrufdatum, ZIP-Hash und files[]. verifiedBy bleibt leer mit Begruendung (E4).
  4. provenance-ledger.json ergaenzen.
  5. Die vier bestehenden MUS_*.ogg bekommen ihre fehlenden Datensaetze nachgezogen (E5).
IMPORT: NUR die tatsaechlich verwendeten Dateien, kein Sammelimport aller 303 — die
Git-Historie vergisst Binaerdaten nie. Zuordnung Tier-0-Schluessel -> Quelldatei steht
in 12B §5.3. Die desktop.ini im Sci-Fi-Paket ist ein Windows-Artefakt und darf NICHT mit.
FORMAT: Kenney liefert .ogg, der Audioplan §5 fordert .wav. NICHT KONVERTIEREN — das
braeche den SHA-256. Die Konvention wird auf .ogg geoeffnet; 04_Audioplan.md §5
entsprechend praezisieren. Force To Mono ist Pflicht fuer alle 3D-Quellen, aus fuer UI.
DER EIGENTLICHE AUFWAND IST DAS ABMISCHEN, nicht die Beschaffung:
AudioManager.asset steht auf m_RealVoiceCount: 32, es gibt kein AudioMixer-Asset, und
die Kamera steht hoch. Anlegen: MIX_Master.mixer mit Master > Music / SFX / Ambience
(Voice-Gruppe LEER anlegen fuer den spaeteren FMOD-Umstieg); EffectiveSfxVolume aus
GameSettings tatsaechlich anwenden; 2–4 Variationen je Schluessel mit Zufallswahl
(UnityEngine.Random ist in der Praesentation erlaubt); Deckel von 3–4 gleichzeitigen
Instanzen je Schluessel; ALR_BaseUnderAttack mit ~20 s Cooldown.

STUFE 4 — DER TOD
Absacken und Ausblenden ueber ~0,8 s mit Zerlegungsstoss; Gebaeude mit Rauchsaeule und
liegenbleibender Truemmerflaeche. KEINE Skelettanimation versprechen — es gibt kein Rig.
DIE FALLE: UnitViewManager recycelt Views ueber den Slot-Index. Der Effekt MUSS den View
halten, solange er laeuft, und ihn danach sicher freigeben — sonst zeigt der recycelte
View die Leiche der vorigen Einheit. Das ist ein Testfall, kein Nebensatz.

STUFE 5 — EXPLOSIONSWOLKE (optional, erst nach 1–4)
Kenney Smoke Particles, Explosion00..08 als 3x3-Atlas, Texture Sheet Animation mit
_FlipbookBlending. ABLAGEREGEL: Effekt-MATERIALIEN und -PREFABS gehoeren NICHT unter
Assets/_Project/Art/ — der .gitignore-Art-Block entfernt dort png, mat und prefab samt
.meta, und ein frischer Clone haette unsichtbare Effekte. PNG-Texturen duerfen unter
Art/VFX/ liegen, DANN aber mit Fallback auf Default-Particle.

DER DETERMINISMUS-NACHWEIS — ABWEICHUNG VOM URSPRUNGSSPRINT
Der dort geforderte A/B-Hash-Test ("State-Hash mit Effekten == ohne") ist NICHT baubar:
GameSettings liegt in Rang 4 und darf von keiner Test-Assembly referenziert werden, und
Unity-Tests laufen laut .github/workflows/tests.yml mangels CI-Lizenz ueberhaupt nicht —
der Test waere gruen, weil er nie laeuft. BAUE STATTDESSEN einen Quellcode-Guard in
Nova.SimRunner.Tests nach dem Muster des bestehenden NoFloatInSimulationTests:
"kein GetUnitRef und kein Zugriff auf SimulationKernel.Random ausserhalb von Simulation/".
Der laeuft headless ohne Lizenz und faengt genau den Fehler, um den es geht.

NICHT IN DIESEM SPRINT
VFX Graph. Unity Asset Store (auch kostenlos — die EULA erlaubt Verbreitung nur
eingebettet, ein oeffentliches Repo ist der ausgeschlossene Fall). Fremde
Pooling-Bibliotheken. Freesound/OpenGameArt (Lizenz variiert je Datei, CC-BY-Risiko).
ChipTone/bfxr/usfxr (Output-Lizenz nicht belegbar abrufbar). Sonniss/ZapSplat/Pixabay
(Rohdatei-Weitergabe untersagt bzw. seit 2019 nicht mehr CC0). Prozeduraler
Texturgenerator (gestrichen, siehe 12B §8). Skelettanimation. Kinetische
ffmpeg-Eigenerzeugung (zweite Runde). Audio Tier 1 und 2.

WENN EIN SOUND NICHT TRAEGT
Keine neue Fremdquelle suchen — jede kostet eine Regel-6-Einzelpruefung. Der gedeckte
Weg ist Eigenerzeugung per ffmpeg (liegt lokal): Rauschschichtung plus Huellkurve. Das
laeuft als original-work, beruehrt Regel 6 gar nicht, und die Kommandozeile selbst ist
der vollstaendige, reproduzierbare Provenienznachweis. Im Report festhalten.

VERIFIKATION
dotnet test tools/Nova.SimRunner.Tests muss gruen sein, einschliesslich des neuen
Quellcode-Guards. KEINE Baseline darf sich bewegen — dieser Strang aendert die
Simulation nicht. Wenn ein Fingerprint, ein Replay-Hash oder ein Snapshot-Hash rot
wird, hast du die Simulation angefasst: SOFORT MELDEN, nicht die Baseline neu setzen.
Unity-EditMode/PlayMode auf der Arbeitsmaschine nachziehen, soweit die Lizenz es zulaesst.

FERTIG WENN
Meine Panzer schiessen und ich sehe es — Muendungsfeuer, sichtbares Geschoss,
Einschlagfunken. Ich hoere es, und sechzig feuernde Einheiten sind ein Gefecht und kein
Brei. Was stirbt, sackt zusammen und blendet aus, und der wiederverwendete View zeigt
nie die Leiche der vorigen Einheit. Das HUD antwortet hoerbar, ein abgelehnter Befehl
sagt es mir. Ein Gefecht am Rand meiner Sicht verraet mir keine Einheit, die ich nicht
sehen duerfte.

ABSCHLUSS
- CHANGELOG.md: Eintrag unter [Unreleased]
- docs/production/DecisionLog.md: neue D-Nummer fuer den Effekt- und Audio-Einstieg
  (Kenney-CC0-Deckung, .ogg-Konventionsoeffnung, Batch-Sidecar, Ersatz des B5-Tests
  durch den Quellcode-Guard, sichtbare Projektile)
- docs/production/ScopeLedger.md: die drei Abweichungen aus 12B §8
- docs/assets/Licenses.md §3, docs/assets/Provenance.md §2, provenance-ledger.json
- docs/production/hashkrieg/04_Audioplan.md §5: .ogg-Praezisierung
- docs/production/hashkrieg/12B_Sprint_Sichtbares_Gefecht.md: Status auf "umgesetzt"
  plus Ergebnisblock nach dem Muster von Sprint 11
- docs/production/hashkrieg/12_Sprint_Zu_Zweit.md: Strang B als ausgelagert markieren
- reports/v8.6.0/sprint-12-strang-b/: Umsetzungsreport mit Files-Changed-Liste
- Eigener Branch. NICHT pushen ohne ausdrueckliche Freigabe des Inhabers.
```
