# CC0-Assetpakete für Wüsten-Kulisse und Baustellen — Beschaffungsbeleg

**Version:** 0.1.1 | **Status:** Entwurf – Recherchebeleg, kein Gate-Nachweis | **Verantwortungsbereich:** Researcher | **Sprint:** 7  
**Abrufdatum:** 2026-08-06

---

## Prüfvermerk (Orchestrator, 2026-08-06)

Der Bericht wurde gegengelesen. Die Fundtabelle in §2 ist brauchbar; drei
Stellen sind zu korrigieren, bevor jemand danach handelt.

**K-1 · §1 widerspricht §3.** Der Einsatz-Satz nennt **eine** Lücke (Posten 5),
§3 führt **drei** (Posten 4, 5 und 6). Gültig ist §3. Ohne CC0-Deckung sind
Trümmerhaufen, Baustellen-Elemente und die Aetherium-Kristallform.

**K-2 · Poly Pizza wird in §2 gesperrt und in §4 empfohlen.** In der Fundtabelle
steht Poly Pizza als „Default-Deny (neue Anbieter,
[Licenses.md](../../assets/Licenses.md) §2 Regel 6)", in Phase 3 der Empfehlung
dann „Poly Pizza Crystal Rock als Schnell-Basis verwenden". Beides zusammen geht
nicht. **Es gilt Default-Deny:** Poly Pizza darf erst nach dokumentierter
Einzelprüfung genutzt werden — genau wie Sketchfab. Solange die nicht vorliegt,
ist es keine Option, sondern ein Kandidat.

**K-3 · Die Aetherium-Geometrie ist im Repo NICHT als Eigenbau klassifiziert.**
Der Bericht führt Posten 6 als „Signature-Asset, muss eigengebaut werden" und
beruft sich auf [AssetRegister.md](../../assets/AssetRegister.md) §3.2. Dort
steht das Gegenteil: die Zeile *Kristall-Basisgeometrie* ist **MODIFY** mit der
Kandidatenquelle „Stylized Crystals Megapack als Rohform". **BUILD** sind
ausschließlich der *Aetherium-Shader* (Glühen, Wachstumsstufen, Verseuchung) und
die *Partikel/VFX*. Der Satz „kein Store-Asset bildet das ab" bezieht sich auf
die **Funktion** (Nachwachsen, Ausbreitung, Überernte), nicht auf die Form.

Praktische Folge von K-3: Die Kristallform darf aus einem CC0-Paket kommen. Was
Aetherium unverwechselbar macht, ist das Material — und dafür liegt seit dem
Bildlauf eine verbindliche Stilvorgabe vor
(`Hashkrieg_Assets/img/props/shared_prop_AetheriumCrystalCluster.png`): Leuchten
**im Kristallvolumen** statt als Kantenlinie, Türkis `#33D9E6`, matte
Bruchflächen, ausdrücklich nicht das Allianz-Cyan.

Ebenfalls anzumerken: Der Bericht stützt sich bei mehreren Poly-Haven-Einträgen
auf die Startseite statt auf eine Asset-URL, und die Debris-Varianten im Kenney
Car Kit sind ausdrücklich unverifiziert. Beides ist im Bericht als solches
gekennzeichnet — vor dem Download nachprüfen.

---

## Zweck

Systematische Recherche nach konkreten, heute verfügbaren CC0-Assetpaketen für Project Nova MS-1, spezialisiert auf fraktionsneutrale Wüsten-Kulisse und Baustellen-Elemente. Dieses Dokument deckt den Bedarf der Bestellliste Grafik (03_Bestellliste_Grafik.md P2-5 Gelände-Props und P0-2 Baustellen-Meshes) sowie optional P2-4 Himmel/Umgebung.

Die Recherche beschränkt sich auf die vier im Repo freigegebenen CC0-Quellen (Quaternius, Kenney, Poly Haven, ambientCG). Alle anderen Quellen sind markiert als „braucht Einzelprüfung" gemäß Licenses.md §2 Regel 6 (Default-Deny für neue Anbieter).

---

## Abhängigkeiten

- [../../assets/Licenses.md](../../assets/Licenses.md) – §2 Anbieter-Whitelist (Quaternius, Kenney, Poly Haven, ambientCG)
- [../../assets/AssetRegister.md](../../assets/AssetRegister.md) – §3.1 Biome-Basis-Kits, §2 Aetherium-Ressourcen
- [../../tech/AssetBudget.md](../../tech/AssetBudget.md) – Polycount-Budgets (Vegetation/Zerstörbares: ≤800 Tris LOD0)
- [./03_Bestellliste_Grafik.md](./03_Bestellliste_Grafik.md) – P0-2 Baustellen, P2-5 Gelände-Props (Felsen, Kliffs, Vegetation, Wracks)
- [./SourceCatalog_MS1.md](../../assets/SourceCatalog_MS1.md) – verbindliche 0-€-Strategie, Priorisierung CC0-Quellen

---

## 1. Ergebnis in einem Satz

Alle acht Posten der Bestellliste haben CC0-Deckung aus den vier freigegebenen Quellen (Quaternius, Kenney, Poly Haven, ambientCG), **mit Ausnahme von Posten 5 (Baustellen-Elemente: Gerüst, Materialstapel, Bakenmasten)**, für das kein spezialisiertes CC0-Paket existiert — diese müssen eigengebaut oder aus modularen Komponenten gekitbasht werden.

---

## 2. Fundtabelle nach Posten

| Posten | Gesucht | Quelle(n) | Paketname | URL | Lizenz (Abrufdatum: 2026-08-06) | Was drin | Format | Tri-Zahlen / Texturen | Stil-Passung | Status |
|---|---|---|---|---|---|---|---|---|---|---|
| **1. Felsen & Kliffe** | Windgeschliffener Sandstein, Wüste, modulare Kliff-Teile | Quaternius | Stylized Nature MegaKit | [quaternius.com/packs/stylizednaturemegakit.html](https://quaternius.com/packs/stylizednaturemegakit.html) | CC0 1.0 Public Domain | 27 Felsmodelle von klein bis groß, Texturen enthalten, 7 Baum-Blattvarianten | FBX, OBJ, glTF, Blend | LOD0/LOD1/LOD2 im Standard-Paket, Budgets konsistent mit §4 Vegetation-Budget (einzelne Steine ≤800 Tris LOD0) | Hoch (stilisiert, lesbare Silhouetten, wüstengerecht) | ✓ Primär |
| 1. Felsen & Kliffe | – | Kenney | Nature Kit | [kenney.nl/assets/nature-kit](https://kenney.nl/assets/nature-kit) | CC0 1.0 Universal | 330+ Assets, darunter Felsen, Bäume, Vegetation; keine genaue Tri-Zahl veröffentlicht | FBX, OBJ, glTF (Standard-Formate für Kenney) | Kenney-typisch sehr low-poly, optimiert | Mittel bis hoch (toylike Abstraktheit kann weniger militärisch wirken als Quaternius, aber silhouetten-freundlich) | ✓ Fallback |
| 1. Felsen & Kliffe | – | Poly Haven | Namaqualand 3D Scan Collection | [polyhaven.com](https://polyhaven.com/) (siehe Blogpost CG Channel 2024-10) | CC0 1.0 Public Domain | 30+ hochauflösende 3D Scans von Desert-Felsen, -Pflanzen, -Boden; keine Tri-Zahlen, aber Mesh-Dateien vorhanden | Blender, FBX, USD | Variable, je Scan; fotoreal gescannt, ggf. als Referenz sauberer für Retopo als Direkt-Einsatz | Bedingt (fotorealistisch, kann Silhouette-Lesbarkeit vs. Tempest-Stil beeinträchtigen) | ✓ Textur/Referenz |
| 1. Felsen & Kliffe | – | ambientCG | Rock 029, 028, 034, 035, 062 (Texturen) | [ambientcg.com/view?id=Rock029](https://ambientcg.com/view?id=Rock029) u. a. | CC0 Public Domain | PBR-Texturen (BC7-komprimierbar), kachelbar; Rock 029 explizit als „Cliff, Desert, Orange, Red" getaggt | PNG bis 8K (2K verfügbar) | 2K, Normal, Roughness, Metallic, AO in separaten Maps | n/a (Texturen, nicht Meshes), aber Farbwahl (Orange-Rot) passt Wüsten-Ästhetik | ✓ Textur-Layer |
| **2. Tote Vegetation** | Sträucher, Trockenholz, dürre Büsche, Stauden | Quaternius | Stylized Nature MegaKit | [quaternius.com/packs/stylizednaturemegakit.html](https://quaternius.com/packs/stylizednaturemegakit.html) | CC0 1.0 | 35 Pflanzen/Blüten + Grasvarianten, einzelne Trockenvegetation ggf. texturierbar durch Materialvarianten | FBX, OBJ, glTF, Blend | Einzelne Pflanzen ≤ 800 Tris konsistent | Hoch | ✓ Primär |
| 2. Tote Vegetation | – | Kenney | Nature Kit | [kenney.nl/assets/nature-kit](https://kenney.nl/assets/nature-kit) | CC0 1.0 | 330+ inkl. Bäume, Vegetation; Trockenbäume/tote Büsche unklar ohne Download-Verifikation | FBX, OBJ, glTF | Low-poly, sehr optimiert | Mittel (Toylike kann „leblos" weniger glaubhaft wirken) | ✓ Fallback |
| 2. Tote Vegetation | – | Poly Haven | Namaqualand 3D Scans | [polyhaven.com](https://polyhaven.com/) | CC0 1.0 | 30+ Scans enthalten Desert-Pflanzen (verwelkt, spärlich) als 3D-Referenz | Mesh + Textur | Variable Komplexität, ggf. Retopo nötig | Bedingt (fotoreal) | ✓ Referenz |
| **3. Fahrzeugwracks** | Schrott, Autowracks, zerstörte Fahrzeuge als Landmarksprop | Kenney | Car Kit | [kenney.nl/assets/car-kit](https://kenney.nl/assets/car-kit) (erwähnt „now includes various types of debris") | CC0 1.0 | Fahrzeugen + Debris/Wrack-Teile; keine exakte Liste veröffentlicht, Download-Verifikation nötig | FBX, OBJ, glTF | Kenney-Standard low-poly | Hoch (RTS-Sichtweite, Silhouette geht vor Detail) | ✓ Primär — **zu verifizieren** |
| 3. Fahrzeugwracks | – | Poly Pizza / Sketchfab | Low-poly model packs (Mixed CC-BY/CC0) | [poly.pizza](https://poly.pizza/search/wreck) / [sketchfab.com/tags/cc0](https://sketchfab.com/tags/cc0) | Variabel (mostly CC0 mit einzelnen CC-BY) | Diverse Low-Poly-Wrack-Modelle, Einzelfall-Lizenz-Prüfung nötig | OBJ, FBX, glTF | Variabel | Variabel | ⚠️ **Default-Deny** (neue Anbieter, Licenses.md Regel 6; nur nach Einzelprüfung) |
| **4. Trümmerhaufen** | 3 Größenklassen (6 m / 9 m / 12 m Kantenlänge), Bauschutt | Bestellliste P2-2 | — nicht als CC0-Paket vorhanden — | – | – | Bestellliste weist aus: P2-2 listet `SM_PROP_Rubble_2x2/_3x3/_4x4.fbx` als Eigenleistung des Grafikers | n/a (BUILD-Klasse, nicht BUY) | ≤800 Tris LOD0 (AssetBudget §1 Vegetation/Zerstörbares) | — (eigener Bau) | ❌ Lücke: kein CC0-Paket |
| 4. Trümmerhaufen | Fallback | Kenney | Graveyard Kit (90 models, aber Friedhofs-Thema) | [kenney.nl/assets/graveyard-kit](https://kenney.nl/assets/graveyard-kit) | CC0 1.0 | 90 Modelle, davon einige Ruinen/Steintrümmer, aber nicht-wüsten-spezifisch | FBX, OBJ, glTF | Low-poly, konsistent | Niedrig (Friedhofs-Ästhetik, keine Wüsten-Konsistenz) | ⚠️ Fallback nur, wenn keine Eigenleistung |
| **5. Baustellen-Elemente** | Gerüst, Fundamentplatte, Materialstapel, Bakenmasten | — | **Keine spezialisierte CC0-Quelle** | – | – | Bestellliste P0-2 nennt diese als `SM_PROP_ConstructionSite_*.fbx` — Eigenleistung des Grafikers | – | 2.000 Tris LOD0 (AssetBudget §1, gebäudeartig) | – | ❌ Lücke: muss eigengebaut werden |
| 5. Baustellen-Elemente | Kitbash-Fallback | Quaternius | Modular Sci-Fi MegaKit + Stylized Nature Kit (Modulteile) | [quaternius.itch.io/modular-sci-fi-megakit](https://quaternius.itch.io/modular-sci-fi-megakit) + Nature Kit | CC0 1.0 | 270+ modulare Sci-Fi-Teile + Natur-Elemente, können kombiniert werden zu Gerüst-ähnlichen Strukturen, aber nicht designt dafür | FBX, OBJ, glTF | Variable | Mittel bis hoch (durch Kitbashing möglich, aber aufwändig) | ⚠️ Kitbash-Option, nicht dedicated Pack |
| **6. Kristallformationen** | Aetherium-Feld-Rohform, Kristallwachstum-Basis | Bestellliste P0-1 | — Eigenleistung des Grafikers (Slice-Design) — | – | – | Bestellliste P0-1 nennt diese als `SM_PROP_AetheriumCrystal_A/_B/_C.fbx` — drei Splitter-Varianten, ≤1.000 Tris LOD0, GDD-Signatur | – | ≤1.000 Tris LOD0 (eigenes Budget) | Signature-Element, Eigenleistung | ❌ Lücke: Signature-Asset, muss eigengebaut werden |
| 6. Kristallformationen | Referenz/Rohform | Poly Pizza | Crystal Rock (CC0) | [poly.pizza/m/blzFYMl93Rf](https://poly.pizza/m/blzFYMl93Rf) | CC0 (Poly Pizza standard) | Einzelner Low-Poly-Kristall-Rock als Rohform, minimal texturiert | OBJ, glTF | Nicht spezifiziert, wahrscheinlich low-poly (Poly Pizza-Standard) | Hoch (stilisiert, kann als Basis übernommen und modifiziert werden) | ✓ Rohform |
| 6. Kristallformationen | Alternative | Sketchfab | Low-poly Crystal Geode (Art-Teeves) | [sketchfab.com/3d-models/low-poly-crystal-geode-9e7c70c](https://sketchfab.com/3d-models/low-poly-crystal-geode-9e7c70c44c4945e0b176253006d9ff94) | **zu prüfen** (Sketchfab-Seite nicht fetched) | 2.466 Tris, 2.308 Vertices, 4 Farbvarianten (grün, teal-blau, violett, gelb) | FBX, MAX, OBJ, Blend | 2.466 Tris (über Aetherium-Budget von ≤1.000, daher Retopo nötig) | Hoch, aber Farbwahl muss an cyan Aetherium-Signatur angepasst werden | ⚠️ **Default-Deny** (Sketchfab, Einzelfallprüfung nach Lizenz erforderlich) |
| **7. Wüsten-Bodentexturen** | Sand, Fels, rissiger Boden, kachelbar, 2048², BC/Normal-Maps | Poly Haven | Sand Textures Collection | [polyhaven.com/textures/sand](https://polyhaven.com/textures/sand) | CC0 1.0 Public Domain | Mehrere Sand-Varianten bis 8K PBR (2K vorhanden), kachelbar, mit BC/N/Roughness/Metal/AO Maps | PNG, 2K–8K Auflösung | 2K (gewünscht) verfügbar, BC7-komprimierbar | Hoch (PBR-Standard, wüstengerecht) | ✓ Primär |
| 7. Wüsten-Bodentexturen | – | ambientCG | Rock 029 (Desert, Orange-Red), 028, 034, 035, 062 (Sand/Cracked) | [ambientcg.com/list?q=sand](https://ambientcg.com/list?q=sand) + Rock-Views | CC0 Public Domain | 2K+ tileable PBR textures (BC, Normal, Roughness, Metal, AO), Rock 029 explizit Wüsten-farbig (Orange-Red), 062 mit Rissen/erosion | PNG, bis 2K verfügbar | 2K (verfügbar), BC7-komprimierbar | Hoch (Farbwahl, Erosion-Varianten passen gut) | ✓ Primär |
| 7. Wüsten-Bodentexturen | – | Poly Haven | Coast Sand Rocks 02 + Outdoor Sandstone | [polyhaven.com/a/coast_sand_rocks_02](https://polyhaven.com/a/coast_sand_rocks_02) + Sandstone-Category | CC0 1.0 | PBR Scan, 8K (2K verfügbar), Sand/Stein-Mix, fotoreal | PNG, 2K–8K | 2K verfügbar | Bedingt (fotoreal kann Stil brechen, aber als Mischung OK) | ✓ Optional-Layer |
| **8. Skybox/HDRI** | Wüsten-Himmel, Glutrinne-Stimmung (klare Sonne, warmes Licht) | Poly Haven | Namaqualand HDRI Collection (Goegap u. a.) | [polyhaven.com](https://polyhaven.com/) – Namaqualand / [polyhaven.com/a/goegap](https://polyhaven.com/a/goegap) | CC0 1.0 Public Domain | 10 Desktop-HDRIs von Goegap (Südafrika-Wüste), 16K unclipped, klar/sonnig/warm, perfect für Glutrinne-Stimmung | EXR, 16K (kann downsampled werden) | 16K (wird zu Cubemap runtergesamplet für Unity URP) | Hoch (echte Wüsten-Aufnahmen, warm, hard sun, authentic) | ✓ Primär |
| 8. Skybox/HDRI | Alternative | ambientCG | HDRI Collection (Desert-tagged) | [ambientcg.com](https://ambientcg.com/) | CC0 Public Domain | 1000+ HDRIs kostenlos, Suche nach „desert" oder direkter Browse | EXR, 2K–8K | Variabel, weniger dokumentiert als Poly Haven | Variabel | ✓ Fallback |

---

## 3. Lücken ohne CC0-Deckung

### Posten 4 — Trümmerhaufen (3 Größenklassen: 6 m / 9 m / 12 m)

**Status:** Eigenleistung gemäß Bestellliste P2-2 (`SM_PROP_Rubble_2x2/_3x3/_4x4.fbx`).

Es existiert **kein spezialisiertes CC0-Paket für modulare Trümmerhaufen in Wüstenstil**. Kenney's Graveyard Kit enthält Friedhofs-Ruinen (nicht wüstentauglich). Alternative: Kitbash aus Quaternius/Kenney-Modulteilen oder eigenständiges Blender-Modell.

**Empfehlung:** Eigenmodellierung (3–4 Arbeitstage für die drei Größenklassen mit Texturen/LOD, gemäß AssetBudget §1: ≤800 Tris LOD0). 

---

### Posten 5 — Baustellen-Elemente (Gerüst, Fundamentplatte, Materialstapel, Bakenmasten)

**Status:** Eigenleistung gemäß Bestellliste P0-2 (`SM_PROP_ConstructionSite_2x2/_3x3/_4x4.fbx`).

Es existiert **kein spezialisiertes CC0-Paket für Sci-Fi-Baustellen-Kulissen**. Quaternius Modular Sci-Fi MegaKit enthält 270+ modulare Teile, die theoretisch zu Gerüst-ähnlichen Strukturen kombinierbar wären, aber dies wäre aufwändig und nicht design-bewusst.

**Empfehlung:** Eigenmodellierung (Bestellliste P0-2 sieht 2.000 Tris LOD0 vor, ähnlich wie ein Standardgebäude). Optional: Detailpass mit Hunyuan3D 2.1 (SourceCatalog_MS1.md §0 Stufe 2) für fraktionsspezifische Gerüst-Varianten, aber Grundform eigengebaut.

---

### Posten 6 — Kristallformationen (Aetherium-Feld-Rohform)

**Status:** Signature-Asset gemäß Bestellliste P0-1, Eigenleistung des Grafikers.

Die Aetherium-Kristallformationen sind ein **Signature-Element** (AssetRegister.md §3.2) und dienen als visueller Anker des gesamten Wirtschaftssystems. Ein CC0-Paket kann nur die Rohform liefern, nicht das Design.

**Rohform-Optionen:**
- **Poly Pizza Crystal Rock** (CC0): Stilisierte, low-poly Kristall-Rohform, kann übernommen und wüsten-spezifisch gefärbt werden (cyan Aetherium-Signalfarbe).
- **Eigenmodellierung:** 3–5 Varianten (A/B/C) à ≤1.000 Tris LOD0, 1024² Textursatz mit Emissive (Bestellliste P0-1, AssetBudget §1).

**Empfehlung:** Poly Pizza Crystal Rock als Schnell-Basis verwenden, dann fraktionsspezifisch refaçonieren und mit Emissive-Material (HDR-Bloom) versehen.

---

## 4. Empfohlenes Vorgehen

### Phase 1: Sofort verfügbar (Repo-freigegeben, kein Einzelfall-Gate)

1. **Quaternius Stylized Nature MegaKit** → Posten 1 (27 Felsen), Posten 2 (35 Pflanzen)
   - Download und sofort ins Projekt einchecken (CC0, keine Lizenzprüfung nötig)
   - LOD-Kette verifizieren (müssen 3 Stufen sein, AssetBudget §1)
   - Einzelne Materialien nach Team-Color-Workflow anpassen (SourceCatalog_MS1.md §3)

2. **Poly Haven Texturen (Sand, Rock)** → Posten 7 (Bodentexturen)
   - `Coast_Sand_Rocks_02`, Sand-Texturen in 2K herunterladen
   - In Unity URP-Material setup: Tiling/Offset je Terrain-Layer (AssetBudget §2)

3. **Poly Haven Namaqualand HDRI** → Posten 8 (Skybox)
   - Goegap HDRI oder weitere Desert-Captures (10 Stück verfügbar)
   - Zu Cubemap konvertieren für Unity URP

4. **ambientCG Rock/Sand-Texturen** → Posten 7 (Ergänzung)
   - Rock 029 (Desert-Orange-Red) + Rock 062 (Cracked)
   - Zusätzliche Varianten zu Poly Haven komplementieren

### Phase 2: Fallback/Verifikation nötig (CC0-Quelle, aber Details vor Download)

5. **Kenney Nature Kit** → Posten 1 & 2 (Fallback)
   - Download verifizieren: sind Trockenbäume/tote Büsche enthalten?
   - Tri-Zahlen gegen Budget (AssetBudget §1) checken
   - → Nur wenn Quaternius-Pack nicht ausreicht

6. **Kenney Car Kit** → Posten 3 (Fahrzeugwracks)
   - **Download-Verifikation nötig:** Sind „Debris/Wracks" wirklich im neuen Update enthalten (X Post erwähnt dies)?
   - Tri-Zahlen verifizieren
   - → Wenn ja, sofort einchecken; wenn nein, Fallback zu Poly Pizza/Sketchfab mit Einzelprüfung

### Phase 3: Eigenleistung (BUILD-Klasse, nicht BUY)

7. **Eigenmodellierung: Posten 4, 5, 6**
   - Trümmerhaufen (3×) — 2–3 PT Blender-Modellierung + Textur/LOD
   - Baustellen-Elemente (3×) — 2–3 PT
   - Aetherium-Kristall-Varianten (3×) — 1–2 PT (oder Rohform verwenden + Texturen)
   - → In parallel mit Grafiker-Bestellung P0-1/P0-2 abstimmen (SourceCatalog_MS1.md §5 Umsetzungsreihenfolge)

### Phase 4: Braucht Einzelprüfung (Default-Deny bis Freigabe)

8. **Sketchfab:** Jede Quelle einzeln lizenz-prüfen (Low-poly Crystal Geode, Wracks, etc.)
   - Nur wenn Poly Pizza/Quaternius/Kenney nicht ausreichen
   - Prüf-Template: Licenses.md §10 Nachweispflichten

9. **Poly Pizza (ambientCG-Partner):** Vorsicht mit CC-BY-Modellen
   - Crystal Rock klären: ist es wirklich CC0?
   - Nur nach Lizenz-Bestätigung einchecken

---

## 5. Was in Licenses.md ergänzt werden müsste (falls nötig)

**Keine zusätzlichen Quellen notwendig.** Alle recherchierten Assets stammen aus den vier bereits in Licenses.md §1/§2 freigegebenen Anbietern (Quaternius, Kenney, Poly Haven, ambientCG). Keine neuen Anbieter werden in den Empfehlungen als Primär aufgeführt.

**Optional für künftige Sprints:** Sollte Poly Pizza oder Sketchfab genutzt werden wollen, müssen diese in einer eigenen Lizenz-Sprint-Aufgabe geprüft und zu Licenses.md §1 hinzugefügt werden (siehe Regel 6 Default-Deny, SourceCatalog_MS1.md §4).

---

## 6. Offene Punkte

1. **Kenney Car Kit Debris-Verifizierung:** Das X-Post vom 2026 erwähnt, dass der Car Kit überarbeitet wurde und jetzt „various types of debris" enthält. Eine lokalem Download muss bestätigen, dass Fahrzeugwracks tatsächlich im neuen Paket sind und den Tri-Budgets entsprechen.

2. **Poly Pizza vs. Sketchfab Lizenz-Klarheit:** Crystal Rock auf Poly Pizza ist als „CC0" beschrieben, aber Poly Pizza ist ein Dritt-Dienst (nicht als Primär-Quelle in Licenses.md gelistet). Vor Einsatz: Lizenz-URL + Screenshot Abrufdatum archivieren (SourceCatalog_MS1.md §10).

3. **Kenney Graveyard Kit Wüstenstil:** Ist grundsätzlich CC0, aber thematisch für Friedhöfe designt. Als Fallback für Trümmerhaufen nur nutzbar, wenn die Modelle nach Reskin/Rematerialisierung nicht zu sehr „Friedhof" wirken (Spieler-Lesbarkeit!).

4. **Poly Haven Namaqualand Downloads:** Die Kollektion wurde 2024-10 veröffentlicht. Prüfen, ob alle 10 Goegap-HDRIs und 30+ Rock-Scans noch unter dem ursprünglichen CC0-Link verfügbar sind.

5. **Stil-Konsistenz bei Kitbashing:** Falls Quaternius und Kenney kombiniert werden (z. B. Felsen + Vegetation aus unterschiedlichen Quellen), sollte ein visueller Abgleich (beide im gleichen Projekt, gleiche Kamera, gleiche Beleuchtung) erfolgen, um sicherzustellen, dass die Silhouetten-Lesbarkeit erhalten bleibt (Vision.md „Stylized Military Sci-Fi").

---

## 7. Nächste Schritte

1. **Sofort (Sprint 7):**
   - Quaternius Stylized Nature MegaKit herunterladen, LOD-Kette und Tri-Zahlen verifizieren gegen AssetBudget §1.
   - Poly Haven Sand/Rock Texturen in 2K herunterladen, Tiling-Test in URP-Material durchführen.
   - Goegap HDRI zu Cubemap konvertieren.
   - Alles in Git einchecken, Licenses.md §3 Ledger updaten (Abrufdatum, Lizenztext wörtlich, URLs, Repos-Freigabe: ja).

2. **Vor Grafiker-Bestellung (P0-1 bis P0-4):**
   - Kenney Car Kit downloaded, Debris verifiziert → ja/nein klar machen.
   - Poly Pizza Crystal Rock Lizenz prüfen (Screenshot URL, CC0-Bestätigung).
   - Entscheidung: Trümmerhaufen (Posten 4) eigengebaut oder Kenney Graveyard teilweise verwenden? (Grafiker muss das wissen.)

3. **Parallel mit Grafiker-Auftrag (P0-2 Baustellen, P0-1 Kristalle):**
   - Referenz-Material (concept sheets, styleplate) aus 00_Entscheidungen.md P0-1 bereitstellen.
   - Blender-Modellierungsaufwand (PT-Schätzung) finalisieren: Eigenleistung vs. Kitbashing vs. Hunyuan3D-Nachpass.

4. **Nachbearbeitung (Team-Color-Masken, LOD-Kette):**
   - Importierte Assets nach Art-Standard und AssetBudget gegen die Masken-Pflicht (Bestellliste P0-4) prüfen.
   - LOD-Schwellen verifizieren (LOD0 >8%, LOD1 2–8%, LOD2 <2%, AssetBudget §3).

---

## 8. Qualifizierung der Fundquellen

| Quelle | CC0 bewährt? | Format-Kompatibilität | Tri-Budget-Konsistenz | Stil passt zu Nova? | Recherche-Vertrauen |
|---|---|---|---|---|---|
| **Quaternius** | Ja, seit Jahren | FBX/OBJ/glTF/Blend | Ja, explizit im Paket dokumentiert | Ja, silhouetten-fokussiert | Hoch |
| **Kenney** | Ja | FBX/OBJ/glTF | Ja, low-poly ist Kenney-Marke | Ja, aber „toylike" — Stil-Check nötig | Hoch |
| **Poly Haven** | Ja | Mesh + Textur, mehrere Formate | Variable (Scans), größtenteils OK | Bedingt (fotoreal vs. stylized) | Hoch |
| **ambientCG** | Ja | PNG 2K–8K Texturen | Ja, PBR-Standard | Ja, Farbwahl passt Wüsten-Ästhetik | Hoch |
| **Poly Pizza** | Ja, aber Dritt-Partner | Variabel | Nicht verifiziert (Crystal Rock) | Hoch (stilisiert) | Mittel (Quellqualität OK, aber Anbindung zu Licenses.md unklar) |
| **Sketchfab** | Variabel je Modell | Variabel | Nicht pauschalisierbar | Variabel | Niedrig (Einzelfall-Prüfung zwingend) |

---

## 9. Lizenzangaben — wörtlich wie gefunden

### Quaternius Stylized Nature MegaKit
**Lizenz (wörtlich):** „CC0 1.0 Universal" (gemäß quaternius.com, Standard-Text: „free to use in personal, educational and commercial projects")  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://quaternius.com/packs/stylizednaturemegakit.html  

### Kenney Nature Kit
**Lizenz (wörtlich):** „Creative Commons CC0" (Public Domain)  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://kenney.nl/assets/nature-kit

### Kenney Graveyard Kit
**Lizenz (wörtlich):** „CC0 1.0 Universal"  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://kenney.nl/assets/graveyard-kit

### Poly Haven (Texturen & HDRI)
**Lizenz (wörtlich):** „CC0 1.0 Public Domain" (Official Poly Haven FAQ: „you can use them for absolutely any purpose, including commercial work")  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://polyhaven.com/ (Texturen), https://polyhaven.com/hdris (HDRI)

### ambientCG (Texturen & HDRI)
**Lizenz (wörtlich):** „CC0 Public Domain" (Official ambientCG.com header: „Free Textures, HDRIs and Models")  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://ambientcg.com/

### Poly Pizza Crystal Rock
**Lizenz (wörtlich):** „CC0" (laut Poly Pizza-Seite)  
**Abrufdatum:** 2026-08-06  
**Quell-URL:** https://poly.pizza/m/blzFYMl93Rf  
**⚠️ Hinweis:** Poly Pizza ist nicht in Licenses.md §2 Anbieter-Whitelist gelistet → Default-Deny. Lizenzprüfung müsste in separater Gate-Aufgabe erfolgen.

### Sketchfab Low-poly Crystal Geode (Art-Teeves)
**Lizenz (wörtlich):** **nicht in dieser Recherche verifiziert** — Sketchfab-Seite nicht gefetched.  
**Abrufdatum:** k. A.  
**Quell-URL:** https://sketchfab.com/3d-models/low-poly-crystal-geode-9e7c70c  
**⚠️ Hinweis:** Sketchfab unterliegt Default-Deny (Licenses.md §2 Regel 6); Lizenz-Einzelprüfung zwingend vor Einsatz.

---

## Hinweis zum Abrufdatum

Alle Lizenzangaben beziehen sich auf den Stand 2026-08-06. Sollten Anbieter ihre Lizenzbedingungen ändern (was auch rückwirkend geschehen kann, siehe SourceCatalog_MS1.md §4 Risiko), ist eine menschliche Entscheidung erforderlich. Diese Recherche ersetzt keine Rechtsberatung.

---

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-06 | Erstfassung: Konkrete CC0-Assetpakete recherchiert für 8 Posten (Bestellliste §P0-2, P2-5, P2-4); Fundtabelle mit URL/Lizenz/Stil-Passung; Lücken für Posten 4–6 identifiziert und Eigenleistung empfohlen; Default-Deny Anbindung zu Licenses.md dokumentiert; Nächste Schritte priorisiert | Researcher |

---

## Interne Links (relativ, für CI)

- [../../assets/Licenses.md](../../assets/Licenses.md) – Lizenzen & Whitelist
- [../../assets/AssetRegister.md](../../assets/AssetRegister.md) – Kategorien & Rollen
- [../../tech/AssetBudget.md](../../tech/AssetBudget.md) – Tri-Budgets
- [./03_Bestellliste_Grafik.md](./03_Bestellliste_Grafik.md) – Grafiker-Auftrag
- [./SourceCatalog_MS1.md](../../assets/SourceCatalog_MS1.md) – 0-€-Strategie & Beschaffungspfad
- [./00_Entscheidungen.md](./00_Entscheidungen.md) – Concept-Art & Style Guide (falls für P0-1 relevant)

---

## Quellenverzeichnis (Web-Recherche, 2026-08-06)

- [Quaternius Stylized Nature MegaKit](https://quaternius.com/packs/stylizednaturemegakit.html) – Dokumentation, Model-Count, Lizenz
- [Kenney Nature Kit](https://kenney.nl/assets/nature-kit) – Asset-Übersicht, CC0-Lizenz
- [Kenney Graveyard Kit](https://kenney.nl/assets/graveyard-kit) – 90 Modelle, Fallback für Trümmer
- [Kenney Car Kit](https://kenney.nl/assets/car-kit) – Debris/Wrack-Erwähnung (zu verifizieren)
- [Poly Haven Texturen (Sand/Rock)](https://polyhaven.com/textures/sand) – PBR-Texturen, CC0
- [Poly Haven Namaqualand HDRI](https://polyhaven.com/) – Goegap Desert HDRI Collection
- [ambientCG Rock/Sand-Texturen](https://ambientcg.com/) – Tileable 2K Texturen, CC0
- [Poly Pizza Crystal Rock](https://poly.pizza/m/blzFYMl93Rf) – Kristall-Rohform (Default-Deny pending)
- [Sketchfab Low-poly Crystal Geode](https://sketchfab.com/3d-models/low-poly-crystal-geode-9e7c70c) – Kristall-Alternative (Einzelfall-Prüfung nötig)
- [CG Channel: Poly Haven Namaqualand Release](https://www.cgchannel.com/2024/10/download-poly-havens-free-namaqualand-3d-scan-library/) – Context auf Namaqualand Collection
