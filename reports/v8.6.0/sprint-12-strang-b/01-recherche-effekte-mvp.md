# Entscheidungsvorlage — Sichtbare und hörbare Gefechtseffekte (Strang B)

## 1. Die kurze Antwort

**Es gibt keine Library, die angezapft werden muss.** Alles Sichtbare — Mündungsfeuer, Leuchtspur, Einschlag, Explosion, Absacken — baut auf Modulen, die bereits im Manifest stehen: `com.unity.modules.particlesystem`, `LineRenderer` (im selben Modul, in `RallyFlagView` erprobt) und `UnityEngine.Pool.ObjectPool<T>` (Engine-Bestandteil seit 2021.1). Die einzige externe Quelle, die überhaupt etwas beiträgt, was die Engine nicht hat, ist **Kenney** — und zwar für **Audio** (Engines liefern keine Klänge) sowie später für eine echte Explosionswolke als Flipbook.

Der schnellste Weg zu sichtbarer Wirkung ist deshalb: **Zustands-Differ in der bestehenden `UnitViewManager`-Schleife + gepoolte Shuriken-Effekte auf Bordmitteln + Bloom einschalten.** Danach Kenney-SFX an dieselben zwei Flanken. Kein neues Paket, keine neue Lizenzzeile in §1, keine Simulationsdatei.

Wichtiger Befund vorab: Die Frage „welche Online-Library" führt in die Irre, und die Vorgabe des Sprintdokuments („prozedurale Partikeltextur erste Wahl") ebenfalls — **beide sind von Null verschieden**. Unity 6000.5.4f1 liefert `Default-Particle` mit, URP 17.5.0 hängt sie in `ParticlesUnlit.mat` bereits als `_BaseMap` ein (Recherchebefund, lokal an Editor- und PackageCache-Dateien verifiziert). Ein frisch angelegtes ParticleSystem hat in diesem Projekt ohne Import und ohne Codezeile bereits einen weichen Blob. Ein prozeduraler Texturgenerator wäre Arbeit für ein Ergebnis, das der Editor gratis danebenlegt.

## 2. Der Lizenzbefund

### Handwerk (keine Entscheidung nötig)

**BELEGT — Kenney ist bereits freigegeben.** `docs/assets/Licenses.md` §1 führt die Zeile „**Quaternius / Kenney / Poly Haven / ambientCG** | CC0 (Public Domain) | unbegrenzt | ja | Attribution: nein | **Vollständig öffentlich im GitHub-Repo erlaubt**" — ohne Kategorie-Qualifier, während dasselbe Dokument nachweislich kategoriescharf schreibt, wo es das will („Hunyuan3D 2.1 – generierte Meshes" vs. „– vortrainierte Modellgewichte", „SIL OFL 1.1 (Schriften …)"). Regel 6 nennt Kenney namentlich in der Erlaubt-Liste; §3-Ledger führt seit 2026-07-24 „Quaternius Sci-Fi & Kenney Kits | CC0 | 0 € | nein | Ja (öffentliches Repo)".

Daraus folgt, **belegt**:
- Kenney ist kein „neu aufkommender Anbieter" → Default-Deny greift nicht, keine Einzelprüfung nach Regel 6.
- CC0 → **Regel 2 wird nicht ausgelöst**, `CREDITS.md` bleibt unangelegt. Licenses.md „Offene Punkte" hat diese Prüfung bereits einmal explizit durchgeführt und festgehalten: Auslöser ist die Lizenz (CC-BY), nicht die Zahl der Quellen.
- Regel 5 (0 € hart) ist mit den kostenlosen Einzeldownloads erfüllt. Der kostenpflichtige All-in-1-Bundle-Zugang bleibt gesperrt — unabhängig vom Preis und ohne inhaltlichen Verlust, weil die Einzelpakete dieselben Dateien unter identischer CC0-Lizenz liefern.
- Pflicht bleibt: **eine neue Zeile in Licenses.md §3** (Ledger, „Jede freigegebene CC0-/KI-Quelle erhält hier eine Zeile") und ein Provenienz-Datensatz. Die verbreitete Verkürzung „keine neue Lizenzzeile" ist falsch — sie gilt nur für §1.

### Inhaberentscheidung (blockiert sonst)

**(a) Widerspruch im eigenen Repo.** `docs/production/hashkrieg/12_Sprint_Zu_Zweit.md` sagt unter „Bewusst nicht in diesem Sprint" wörtlich: „**Audio Tier 0** … Der Katalog selbst wartet auf die Lizenzerweiterung in `Licenses.md` §1 um Audioquellen — eine Inhaberentscheidung, kein Handwerk." Das steht gegen den §1-Befund oben. Beide Dokumente sind verbindlich. Ein Satz des Inhabers löst das: entweder „Kenney-Audio ist von §1 Zeile 21 gedeckt, die Sprintzeile meint Nicht-Whitelist-Quellen", oder §1 bekommt eine ausdrückliche Audio-Klarstellung. **Ohne diesen Satz ist jeder SFX-Import angreifbar.**

**(b) Provenienz-Schema.** `docs/assets/Provenance.md` §2 verlangt je Sidecar genau einen Datensatz mit genau einem `sourceFileHash`. Ein Kenney-Audiopaket sind 50–130 Dateien. Die gelebte Praxis ist bereits ordnergranular (**belegt**: 37 getrackte `PROVENANCE.json` bei 34 Modellen plus Texturen). Entweder wird §2 um einen Batch-Sidecar (`files[]`-Array mit Einzel-Hashes) präzisiert, oder der Import ist formal unvollständig.

**(c) Vier-Augen.** Provenance.md §3 Schritt 7 verlangt eine zweite prüfende Person. Im Solo-Setup existiert sie nicht — **belegt**: alle bestehenden Datensätze haben `verifiedBy` leer, der Ledger-Kopf hält die Provenienzpflicht selbst als „NICHT erfüllt" fest. Entscheidung: Agent zählt als zweite Person, oder das Feld wird als bewusste Tier-1-Ausnahme protokolliert.

**(d) Altlast.** **BELEGT**: `git ls-files Assets/_Project/Audio` liefert ausschließlich vier `MUS_*.ogg` plus `.meta` — kein `PROVENANCE.json`. Diese vier Dateien werden über das öffentliche Repo bereits weiterverbreitet; Licenses.md „Offene Punkte" protokolliert die Lücke selbst. Frage: mit dem SFX-Import nachziehen oder ausdrücklich als Rückstand stehen lassen?

## 3. Der MVP in Stufen

Zeitangaben sind **Schätzungen**, nicht gemessen.

### Stufe 1 — Bloom (Minuten, Schätzung)
**Danach sichtbar:** nichts Neues für sich allein — aber jeder additive Effekt der Stufe 2 überstrahlt statt flach zu liegen.
**Dateien:** `Assets/DefaultVolumeProfile.asset`.
**BELEGT:** Bloom-Override steht auf `active: 1`, `threshold: 0.9`, `intensity: 0` (Zeile 566–578) — verkabelt und aus. HDR ist aktiv (`NovaUrp.asset` `m_SupportsHDR: 1`). Die Datei ist git-getrackt.
**Vorbedingung / Konflikt:** Der Kimi-Sprintprompt verbietet ausdrücklich, `DefaultVolumeProfile.asset` mitzucommitten. Ohne Aufhebung dieser Anweisung ist der Effekt nur lokal und auf jedem anderen Klon weg. **Inhaberentscheidung.**

### Stufe 2 — Man sieht, dass geschossen wird (halber bis ganzer Tag, Schätzung)
**Danach sichtbar:** Mündungsfeuer am Schützen, Leuchtspur zum Ziel über ~0,1 s, Funken am Einschlag.
**Dateien:** `Assets/_Project/Scripts/Gameplay/Match/UnitViewManager.cs` (Differ) plus eine neue Effekt-Komponente in `Assets/_Project/Scripts/Gameplay/…`.
**BELEGT, warum das klein ist:** `UnitViewManager.LateUpdate` (Zeile 236 ff.) hält pro sichtbarer Einheit bereits die vollständige `UnitState` in der Hand (`TryGetUnit`, Zeile 270) und pflegt bereits gecachte Vergleichswerte je Slot (`_boundIds`, Zeile 117/280). Der Differ sind zwei zusätzliche `int`-Arrays (letzter Cooldown, letzte Gesundheit) **innerhalb derselben Schleife**. Das Schusssignal trägt: `CombatSystem.cs:161-163` dekrementiert `WeaponCooldownTicks` pro Tick, Zeile 252 setzt beim Feuern auf den Profilwert — die steigende Flanke ist eindeutig. Zeile 242 im Code: „MS-1 hitscan: damage lands in the same tick, no projectile."
**Widerlegt, deshalb nicht im Plan:**
- *Kein* eigener Ereignis-Hook in `MatchRunner`. Der Differ lebt in der bestehenden Schleife.
- *Kein* prozeduraler Texturgenerator (siehe §1).
- *Kein* `ObjectPool.maxSize: 64` als Deckel — `maxSize` begrenzt die **ruhenden** Instanzen, nicht die aktiven. Der „max 64 gleichzeitig" braucht einen eigenen Aktivzähler, der beim Überlauf verwirft.
**Geschenkter Vorteil:** Die Schleife speist sich aus `fog.GetVisibleEntities` (Zeile 263, Kommentar Zeile 261: „Nothing here touches EntityManager.RawUnits"). Effekte erben die Fog-of-War-Korrektheit und können keinen verborgenen Schützen verraten. Wer den Differ stattdessen über `EntityManager.RawUnits` baut, verliert genau das.
**Bekannte Falle:** `MatchRunner.cs:247-253` hat eine **ungedeckelte** Aufholschleife (`while (_timeAccumulator >= TickDeltaTime)`). Nach einem Netzwerk-Stall laufen viele Ticks in einem Frame. Der frame-basierte Differ mit Slot-Cache meldet dann nicht doppelt, aber er **verschluckt** Zwischenereignisse. Das ist die mildere Fehlerart als 14 Tick-Kontingente Effekte in einer Millisekunde — aber es ist eine Eigenschaft, die man kennen muss, und sie hängt daran, was Strang A mit dem Akkumulator macht.

### Stufe 3 — Man hört es (Schätzung: kleiner als Stufe 2, aber größter wahrgenommener Sprung)
**Danach hörbar:** Schuss, Einschlag, Explosion.
**Dateien:** dieselbe Effekt-Komponente (`AudioSource.PlayOneShot` an denselben zwei Flanken), plus Importordner.
**BELEGT, warum die Verkabelung halb steht:** `GameSettings.cs` trägt bereits `sfxEnabled` (Z. 35), `sfxVolume` (Z. 36), `DefaultSfxVolume = 0.8f` (Z. 30) und `EffectiveSfxVolume` (Z. 54) — ungenutzt, mit dem Kommentar „applied to future SFX sources only" (Z. 15). Ein `AudioListener` existiert (`Bootstrap.unity:862`); die Behauptung, er fehle, ist **widerlegt**.
**Der eigentliche Aufwand liegt woanders:** `ProjectSettings/AudioManager.asset` hat `m_RealVoiceCount: 32` (Z. 14). Die Kamera steht laut Recherche bei ~42 m Höhe — bodennahe Quellen brauchen eine explizite Rolloff-Kurve oder einen Listener-Proxy. Es gibt kein AudioMixer-Asset. Das Abmischen, damit sechzig feuernde Einheiten nicht zu Brei werden, ist der Flaschenhals — nicht die Beschaffung. Dieser Posten steht in keiner Recherche und in keinem Sprintabschnitt.
**Vorbedingung:** Inhaberentscheidung 2a–2c.

### Stufe 4 — Tod wird lesbar (Schätzung: kleiner Tagesanteil)
**Danach sichtbar:** Einheiten sacken ab und blenden über ~0,8 s aus, statt schlagartig zu verschwinden.
**Dateien:** `UnitViewManager` (View-Freigabe erst nach Ablauf der Sterbeanimation).
**BELEGT:** Kein Rig, kein `.anim`, kein `.controller` im Projekt — Absacken plus Alpha ist die einzige ehrliche Option. Die Warnung des Sprintdokuments ist berechtigt: `UnitViewManager` recycelt Views über Slot-Index (`_boundIds`, `ReleaseView`, Z. 550/594). Wer den View nicht bis zum Ende festhält, zeigt die Leiche der vorigen Einheit im wiederverwendeten Slot.

### Stufe 5 — Explosionswolke (optional, erst nach 1–4)
**Danach sichtbar:** echte Detonationssilhouette statt überlappender Blobs.
**Weg:** Kenney *Smoke Particles*, `Explosion00..08` (9 Frames) zu einem 3×3-Atlas gepackt, `Texture Sheet Animation` + `_FlipbookBlending`. Das ist die eine Stelle, an der Bordmittel klar verlieren — ein Radialverlauf ergibt keine Pilzsilhouette.
**Ablage-Konflikt, BELEGT:** `.gitignore:99-104` schließt unter `Assets/_Project/Art/**` **png, mat, prefab samt .meta** aus. Unter `Art/VFX/` abgelegt = 0 MB Repo-Zuwachs, aber **auch die Effektmaterialien und -Prefabs verschwinden**, und ein frischer Klon ohne Asset-Paket hat keine Effekte. Materialien und Prefabs gehören deshalb **nicht** unter `Art/`. Inhaberentscheidung, siehe §7.

## 4. Die Projektil-Frage

**Die Simulation ist Hitscan — belegt.** `CombatSystem.cs:251-252`: Schaden und Cooldown fallen im selben Tick, Kommentar Zeile 242 „no projectile". Es existiert kein Projektil-Entity und keine Flugbahn.

**Die Absolutformel des Sprintdokuments („ein fliegendes Projektil wäre eine Lüge") ist nicht haltbar** — sie verbietet im selben Absatz, was sie erlaubt. Eine LineRenderer-Leuchtspur, die 0,1 s zwischen Schütze und Ziel steht, belegt denselben Raum zur selben Zeit wie ein gestrecktes Quad. Der Unterschied ist Rendering, nicht Ehrlichkeit.

**Empfehlung:** Ja, sichtbares Projektil — unter drei harten Bedingungen:
1. **Flugzeit ≤ 0,1 s** (ein Tick). Alles Langsamere lässt den Lebensbalken sichtbar *vor* dem Einschlag sinken; das ist die Lüge.
2. **Zielposition wird im Erkennungsmoment einmal kopiert.** Kein Nachführen auf ein lebendes Entity — sonst behauptet das Projektil Ballistik, die es nicht gibt, und fliegt ins Leere, wenn das Ziel im selben Tick stirbt.
3. **Es darf nie verfehlen und nie abgefangen werden.**

In dieser Form ist es ein Vector3 und ein Lerp auf dem fertigen Tracer (**Schätzung: knappe Stunde obendrauf**), fasst keine Simulationsdatei an und ist vom gesegneten Tracer wahrnehmungstechnisch nicht unterscheidbar. Der Inhaber hat danach gefragt, es kostet fast nichts — bauen.

## 5. Sound — konkreter Beschaffungsweg

Alle CC0, alle direkt von kenney.nl (nicht vom Spiegel — Provenance.md verlangt die Ursprungs-URL, und der Hash muss aus *dieser* Datei stammen):

| Paket | URL | Deckt ab |
|---|---|---|
| **Sci-fi Sounds** (70 Dateien) | https://kenney.nl/assets/sci-fi-sounds | Waffenschuss (`laserSmall_*`, `laserLarge_*`), **Explosion** (`explosionCrunch_000-004`, `lowFrequency_explosion_000/001`) — das einzige Explosionsmaterial im gesamten Kenney-Audiobestand |
| **Impact Sounds** (130 Dateien) | https://kenney.nl/assets/impact-sounds | Einschlag (`impactMetal_light/medium/heavy` je 5, `impactPlate_*` je 5) — drei Gewichtsstufen bilden Schadensklassen ab |
| **Interface Sounds** (100 Dateien) | https://kenney.nl/assets/interface-sounds | HUD: `select_001-008`, `confirmation_001-004`, `error_001-008`, `click_001-005` |

**Die Download-URLs enthalten einen inhaltsabhängigen Hash-Timestamp und dürfen nicht hart verdrahtet werden** — ein Beschaffungsskript muss sie von der Paketseite lesen.

**Ehrlicher Befund zur Deckung:** Kenney hat im gesamten Audiobestand **kein einziges kinetisches Feuerwaffengeräusch** — kein Gewehr, kein MG, keine Kanone. Alles Waffenmaterial ist Laser/Energie. Für ein Sci-Fi-RTS mit Aetherium halte ich das für stimmig; die Folgerung „das ist ein Blocker" ist **widerlegt**. Wer den schweren Panzerschuss trotzdem kinetisch will: `ffmpeg` liegt lokal (8.1.2), die Erzeugung per Rauschschichtung ist Eigenwerk, berührt Regel 6 gar nicht, und die Kommandozeile *ist* der vollständige, bit-genau reproduzierbare Provenienznachweis. Das ist die richtige **zweite** Runde, nicht der schnellste erste Schuss.

**Format-Kollision, muss entschieden werden:** Kenney liefert `.ogg`. Der Audioplan §5 schreibt für SFX `.wav` vor. Konvertieren bricht den SHA-256 gegen die Quelldatei und damit die Provenienzkette. **Empfehlung: Konvention auf `.ogg` öffnen, Originaldateien unverändert lassen.** Mono/Stereo ist irrelevant — Unitys Import-Schalter „Force To Mono" überschreibt die Kanalzahl; als Pflichtzeile ins Import-Preset.

**Provenienz-Aufwand real:** eine §3-Ledgerzeile plus ein Sidecar je Paket (**Schätzung: unter einer halben Stunde**), sobald §2 den Batch-Sidecar erlaubt.

## 6. Was NICHT zu tun ist

| Sackgasse | Grund |
|---|---|
| **Unity Asset Store** (auch kostenlose VFX-Pakete) | Doppelt gesperrt: der Store steht nicht in Regel 6 → Default-Deny; und die EULA erlaubt Verbreitung nur „as incorporated and embedded in that Licensed Product" — ein öffentliches Repo ist genau der ausgeschlossene Fall. Der Umweg „Ordner in .gitignore" zerstört die Klonbarkeit. |
| **VFX Graph** | Nicht im Manifest. Laut Unity-Doku unter URP nur auf einem Teil der Plattformen unterstützt, braucht Compute-Shader/SSBOs, kein Gamma-Farbraum. Ausgelegt für Millionen Partikel; hier fallen 64 Effekte à 20–50 Partikel an. Neues Assetformat, neuer Editor, neue Kompetenz — für drei Kleineffekte. |
| **uPools / kPooling / XPool / NightPool / RecyclerKit** | Nachbauten von `UnityEngine.Pool.ObjectPool<T>`, das in der Engine liegt. Keiner gegen Unity 6 getestet. Jeder kostet eine Regel-6-Einzelprüfung für null Mehrwert. |
| **usfxr** | Tot seit 2021, Laufzeit-Synthese bringt nichts, was gebackene WAVs nicht bringen. |
| **Effekseer** | MIT und aktiv, aber kauft native Binaries, einen zweiten Renderpfad am URP vorbei und einen eigenen Editor ein. Für später aufheben. |
| **ambientCG / Poly Haven** | Stehen in der Whitelist, liefern aber für Effekte nachweislich nichts. ambientCGs gesamte Decal-Kategorie sind 126 Assets aus zwei Familien (Wandschlieren, Fahrbahnmarkierungen); Poly Havens API kennt strukturell nur `hdris`, `textures`, `models`. Dort zu suchen ist verlorene Zeit. |
| **Sonniss / ZapSplat / Pixabay** | Alle drei verbieten die Weitergabe der Rohdatei außerhalb eines eingebetteten Werks — derselbe Konflikt, den Licenses.md Fußnote [^2] für Sonniss bereits einmal aufgelöst hat. Pixabay ist zusätzlich seit 2019 nicht mehr CC0; „Pixabay = CC0" wäre eine falsche Provenienzangabe. |
| **Freesound / OpenGameArt** | Technisch möglich, governance-teuer: neue §1-Zeile plus Prüfung **je Datei**, weil die Lizenz pro Upload variiert und der Uploader sie selbst setzt. Ein übersehener CC-BY-Treffer erzwingt `CREDITS.md` — genau die Pflegelast, die Regel 2 vermeidet. Notreserve, nicht dieser Sprint. |
| **ChipTone / bfxr** | Die Output-Freigabezitate sind **unbelegt** (itch.io und bfxr.net liefern bei Direktabruf die Sätze nicht bzw. HTTP 403). Provenance.md §3 sperrt genau diesen Fall: ohne archivierbaren Lizenzbeleg darf das Asset nicht ins Repository. Außerdem sind beide gehostete Dienste → Regel-6-Einzelprüfung. Der `ffmpeg`-Weg umgeht beides vollständig. |
| **Stable Audio Open** | Lizenzangabe unbelegt (keine `providerTermsUrl`, kein wörtliches `outputOwnership`-Zitat, Umsatzschwelle als nutzergebundene Bedingung ohne Präzedenz in §1). Für MS-1 ohnehin gegenstandslos. |
| **Kenney All-in-1-Bundle** | Kostenpflichtig → Regel 5. Und überflüssig: dieselben Dateien, dieselbe CC0-Lizenz, einzeln kostenlos. |

## 7. Offene Fragen an den Inhaber

1. **Sprintdokument gegen Licenses.md §1:** Gilt „Der Katalog wartet auf die Lizenzerweiterung in `Licenses.md` §1 um Audioquellen" auch für Kenney, oder ist Kenney-Audio durch die quellenbezogene §1-Zeile gedeckt? *Ein Satz — ohne ihn ist Stufe 3 blockiert.*
2. **Bloom:** Darf `Assets/DefaultVolumeProfile.asset` mitcommittet werden (Sprintprompt verbietet es ausdrücklich), oder bleibt die Wirkung lokal?
3. **Batch-Sidecar:** Wird `Provenance.md` §2 um „ein Pack = ein Ordner = ein Datensatz plus `files[]`-Array" präzisiert? Ohne das ist ein 50–130-Dateien-Import formal nicht abbildbar.
4. **Vier-Augen:** Zählt ein Agent als zweite Person nach §3 Schritt 7, oder wird das Feld als bewusste Tier-1-Ausnahme protokolliert?
5. **Altlast:** Werden die vier getrackten `MUS_*.ogg` mit `PROVENANCE.json` nachgezogen, wenn ohnehin Audio angefasst wird?
6. **Waffenklang:** Kenney-Laser für den ersten Durchgang akzeptiert, oder muss der schwere Schuss von Anfang an `ffmpeg`-Eigenbau sein?
7. **Fog of War beim Tod:** Dürfen Todeseffekte für gegnerische Einheiten spielen, die im Nebel sterben? Der Differ könnte den Tod über EntityId-Gültigkeit feststellen — das wäre ein Informationsleck, das `UnitViewManager` heute bewusst vermeidet.
8. **Ablage (nur relevant ab Stufe 5):** Kenney-PNGs unter `Assets/_Project/Art/VFX/` (0 MB Repo, aber ein harter Fallback auf `Default-Particle` ist Zusatzarbeit) oder unter einem getrackten Pfad (≈1 MB dauerhafte History, dafür verhält sich jeder Klon gleich)? **Effektmaterialien und -Prefabs gehören in jedem Fall außerhalb `Art/`** — `.gitignore:101-104` würde sie sonst mitentfernen.
9. **Determinismus-Nachweis:** Der im Sprint geforderte A/B-Hash-Test ist so **nicht baubar** — `GameSettings.cs` liegt in `Nova.Presentation.UI` (Rang 4), und `quality/scripts/run_gate_check.py` verbietet jeder Test-Assembly Referenzen auf Rang ≥ 4. Effektschicht und Schalter gehören nach **`Nova.Gameplay`** (Rang 3, dort liegt `UnitViewManager`, und `Nova.PlayMode.Tests` referenziert es bereits). Zusätzlich: `Nova.Simulation` hat `noEngineReferences: true` und referenziert nur `Nova.Core` — der Compiler erzwingt die Einbahnstraße bereits. Das reale Restrisiko ist ein Schreibzugriff über `EntityManager.GetUnitRef`/`RawUnits` aus der Präsentation, und der wird von einem Hash-Test nur gefangen, wenn er läuft — Unity-Tests laufen laut `.github/workflows/tests.yml` mangels CI-Lizenz **nicht**. **Empfehlung:** statt des A/B-Harness ein CI-fähiger Quellcode-Guard in `Nova.SimRunner.Tests` nach dem Muster von `NoFloatInSimulationTests` („kein `GetUnitRef` und kein Zugriff auf `SimulationKernel.Random` außerhalb `Simulation/`"). Braucht deine Zustimmung, weil es vom Sprintdokument abweicht.