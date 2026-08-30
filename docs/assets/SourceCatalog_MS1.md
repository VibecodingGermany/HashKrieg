# Beschaffungskatalog MS-1 (0-€-Strategie)

**Version:** 0.2.0 | **Status:** Entwurf – MS-1 Beschaffungspfad entschieden, kein Gate-Nachweis | **Verantwortungsbereich:** Producer / Technical Art | **Sprint:** 7

## Zweck

Dieses Dokument recherchiert und belegt konkrete, kostenlose Bezugsquellen für die 34 MS-1-Art-Assets (17 Rollen × bis zu 2 Fraktionen) unter der 0-€-Strategie (CC0 + KI-Generierung, Entscheidung D-054) und legt den **verbindlichen Beschaffungspfad** fest (Projektinhaber-Entscheidung, 2026-07-25). Es prüft für jede Quelle Stil-Passung, Lizenzlage, Repo-Tauglichkeit und Budget-Realismus und liefert eine Rollen-zu-Quelle-Zuordnung mit je einer entschiedenen Primärstrategie als Arbeitsgrundlage für die Beschaffung. Es ersetzt nicht das [Licenses.md](Licenses.md)-Register (Ledger-Pflicht bleibt dort), sondern liefert die recherchierte Vorstufe dazu.

## Abhängigkeiten

- [Licenses.md](Licenses.md) – Lizenz-Rahmen je Quelle, Ledger-Pflicht bei Import
- [ProcurementStrategy.md](ProcurementStrategy.md) – Strategie B-Zero (D-054), BUY/MODIFY/BUILD-Rubrik
- [../tech/AssetBudget.md](../tech/AssetBudget.md) – Polycount-/Textur-Budgets je Asset-Klasse
- [../vision/Vision.md](../vision/Vision.md) – Stilziel „Stylized Military Sci-Fi", Silhouette > Detail
- [AssetRegister.md](AssetRegister.md) – Ledger der tatsächlich eingecheckten Assets je Rolle (Folgearbeit dieses Katalogs)
- [Provenance.md](Provenance.md) – Herkunftsnachweis je Asset (Folgearbeit dieses Katalogs)

## 0. Entschiedener Beschaffungspfad (0-€, verbindlich)

**Entscheidung des Projektinhabers, 2026-07-25:** 0 € ist ein hartes Budget-Limit – kein bezahlter Tier, keine Ausnahme. Begründung: kein Budget vorhanden, MVP hat Priorität vor Repo-Politur. Der in Version 0.1.0 dieses Dokuments offen gelassene Zielkonflikt („für klare Eigentumsrechte wäre ein bezahlter Tier nötig", siehe §2 „Wichtig"-Hinweis der Vorversion) ist damit **entschieden**: Statt eines bezahlten Tiers gilt der folgende Beschaffungspfad in fester Priorität. Der Zielkonflikt selbst bleibt als **Risiko** bestehen (siehe §9) – er ist nicht verschwunden, nur nicht mehr offen.

**Verbindliche Prioritätsreihenfolge je Rolle:**

1. **CC0-Kitbash** aus Quaternius, Kenney, Poly Haven und ambientCG, zusammengesetzt und angepasst in Blender. Erste Wahl überall, wo eine Rolle damit erreichbar ist – siehe §1 für die geprüften Quellen.
2. **Hunyuan3D 2.1, lokal/self-hosted**, für alles, was CC0-Kitbash nicht hergibt – insbesondere fraktionsspezifische Fahrzeuge und Gebäude, die eine eigenständige Silhouette statt einer geteilten Kitbash-Basis brauchen. Dies ist laut der in §2 dokumentierten Recherche der **einzige** geprüfte KI-Pfad, der 0 € Kosten, kommerzielle Nutzung **und** Output-Eigentum gleichzeitig erfüllt (Tencent Hunyuan 3D 2.1 Community License). Wichtig und hier explizit festgehalten: Die vortrainierten Modellgewichte selbst dürfen nicht weiterverteilt werden – die damit erzeugten Meshes (Outputs) dürfen es, das Repo führt also nur Outputs, keine Modellgewichte.
3. **OpenAI Image API** für 2D-Referenzblätter (Concept-Sheets), die als Input für den Image-to-3D-Schritt von Hunyuan3D 2.1 dienen. Das Output-Eigentum an den generierten 2D-Referenzen liegt beim Nutzer.
4. **Sketchfab**, ausschließlich nach dokumentierter Einzelfallprüfung je Modell (CC0 oder CC-BY mit Attribution gemäß Licenses.md §2.2), nur als letzte Stufe, wenn 1–3 eine Rolle nicht abdecken.

**Gesperrt für eingecheckte Assets:** Meshy Free-Tier (CC-BY-4.0-Pflicht ohne belegte Repo-Freigabe-Klärung, siehe §2), Tripo3D Free-Tier (laut Recherche widersprüchlich/teils nicht-kommerziell, siehe §2), sowie jeder weitere Anbieter ohne belegbare kommerzielle Nutzung und ohne Output-Eigentum im kostenlosen Tier. Diese Quellen sind ausschließlich für nicht eingecheckte Ideenreferenz zulässig (Moodboard, interne Konzeptfindung), nicht für Repo-Assets. Für jeden neuen, hier nicht gelisteten Anbieter gilt Default-Deny, bis eine Einzelfallprüfung nach demselben Maßstab dokumentiert ist.

**Bezahlte Tiers (Meshy Pro/Studio, Tripo3D Pro, Rodin AI) sind damit als Empfehlung gestrichen** und erscheinen in diesem Dokument nur noch als recherchierte, ausgeschlossene Optionen (§2, §7) mit Begründung des Ausschlusses – nicht als Handlungsoption für MS-1.

## 1. CC0-3D-Quellen: Recherchestand 2026-07-25

### Quaternius (quaternius.com / quaternius.itch.io)

Quaternius stellt seine Modelle unter CC0 (Public Domain) bereit; laut Pack-Beschreibungen frei für persönliche, edukative und kommerzielle Projekte nutzbar. Relevante Packs:

- **Modular Sci-Fi MegaKit** – 270+ modulare Environment-Teile im Raster; geeignet als Kitbashing-Basis für Gebäuderollen (HQ, Reaktor, Raffinerie, Kaserne, Fahrzeugfabrik, Radar, Geschützstellung). Formate FBX/OBJ/glTF/Blend. ([Quaternius Modular Sci-Fi Megakit](https://quaternius.com/packs/modularscifimegakit.html))
- **Sci-Fi Essentials Kit** – futuristische Requisiten, Kisten, Screens, einfache Robotermodelle; CC0. Eignet sich als Ergänzung für Detail-Props an Gebäuden, nicht als Hauptquelle für Einheiten. ([Quaternius Sci-Fi Essentials Kit](https://quaternius.itch.io/sci-fi-essentials-kit))
- **Ultimate Modular Sci-Fi Pack** – weitere modulare Struktur-Bausteine für Gebäude-Kitbashing. ([Quaternius Ultimate Modular Sci-Fi](https://quaternius.com/packs/ultimatemodularscifi.html))
- **Sci-Fi Modular Gun Pack / Sci-Fi Gun Pack** – Waffenmodule, geeignet als Anbauteile für Panzer/Geschützstellung, nicht als vollständige Fahrzeug- oder Infanterie-Meshes. ([Sci-Fi Modular Gun Pack](https://quaternius.com/packs/scifimodularguns.html), [Sci-Fi Gun Pack](https://quaternius.com/packs/scifigun.html))

**Bewertung:** Quaternius deckt Gebäuderollen über Kitbashing aus Modulteilen gut ab (MODIFY-Klasse gemäß ProcurementStrategy.md §3). Für komplette Fahrzeug- oder Infanterie-Einheiten liefert Quaternius primär Requisiten/Waffen, keine vollständigen Militär-Einheiten-Roster – hier schließt gemäß §0 Hunyuan3D 2.1 die Lücke. Stil-Passung zum Zielstil „Stylized Military Sci-Fi" (Tempest Rising/C&C3-Referenz) ist mittel bis hoch, da die Module klare, lesbare Silhouetten mit moderater Detailtiefe bieten. Polycount pro Modulteil liegt laut Pack-Beschreibungen im niedrigen bis mittleren Bereich und ist damit potenziell budget-konform ([../tech/AssetBudget.md](../tech/AssetBudget.md) §1) – eine exakte Tri-Zahl je Einzelteil konnte am 2026-07-25 nicht verifiziert werden (Pack-Seiten nennen keine Polycount-Tabelle).

### Kenney (kenney.nl)

Kenney-Assets sind durchgängig CC0 lizenziert.

- **Sci-Fi RTS** (`kenney.nl/assets/sci-fi-rts`) – 120+ Assets: Strukturen, Fahrzeuge, Umgebungsobjekte, Einheiten und Tiles, explizit für RTS-Spiele konzipiert. Dies ist die direkteste Rollen-Abdeckung aller geprüften Quellen: Gebäude- und Fahrzeug-Rollen (Kaserne, Fahrzeugfabrik, Radar-ähnliche Strukturen, leichte/schwere Fahrzeuge, Buggy-ähnliche Einheiten) sind potenziell direkt abgedeckt. ([Kenney Sci-Fi RTS](https://kenney.nl/assets/sci-fi-rts))
- **UI Pack – Sci-Fi** (`kenney.nl/assets/ui-pack-sci-fi`) – nicht 3D-relevant, aber für Team-/HUD-Konsistenz brauchbar. ([Kenney UI Pack Sci-Fi](https://kenney.nl/assets/ui-pack-sci-fi))

**Bewertung:** Kenneys Sci-Fi-RTS-Stil ist tendenziell sehr low-poly/geometrisch-abstrakt (Kenneys typischer „toylike" Baukasten-Look), was der Silhouette-Priorität entgegenkommt, aber möglicherweise weniger militärisch-realistisch wirkt als die Tempest-Rising-Referenz verlangt – Stil-Passung wird hier als **mittel** eingestuft, bis ein visueller Abgleich am realen Modell erfolgt ist. Die exakte Anzahl und Benennung der 120 Assets sowie ihre Zuordenbarkeit zu den 17 Projekt-Rollen konnte aus den Suchergebnissen nicht vollständig verifiziert werden; ein manueller Download-Review ist vor Festlegung als Primärquelle nötig.

### Poly Haven (polyhaven.com)

Poly Haven bietet alle Assets (Texturen, HDRIs, 3D-Modelle) unter CC0, kostenlos für private und kommerzielle Nutzung, keine Attribution nötig. Texturen in bis zu 8K mit vollständigen PBR-Map-Sets, u. a. Metal-Kategorie. ([Poly Haven](https://polyhaven.com/), [Poly Haven Textures: Metal](https://polyhaven.com/textures/metal)) Poly Haven ist primär eine **Textur-/Material-Quelle**, nicht für fertige Militär-Meshes relevant – wichtig für den Material-Standard (Metall-/Panel-Texturen für den Team-Color-Basislayer) gemäß ProcurementStrategy.md §2.3.

### ambientCG (ambientcg.com)

Über 1.000 hochauflösende Texturen (bis 8K) unter CC0, prozedural und photorealistisch gemischt. ([ambientCG](https://ambientcg.com/)) Ergänzt Poly Haven als zweite CC0-Textur-Quelle für den einheitlichen URP-Material-Standard (Panzerplatten, Beton, Industrie-Metall).

### OpenGameArt (opengameart.org)

In den Suchergebnissen bestätigt als Quelle mit CC0-Filtermöglichkeit; konkret gefunden: „Sci-fi RTS (120+ sprites)" – dies sind jedoch **2D-Sprites**, nicht 3D-Meshes, und daher für unsere 3D-Rollen nicht direkt nutzbar. ([OpenGameArt Sci-fi RTS Sprites](https://opengameart.org/content/sci-fi-rts-120-sprites)) Die allgemeine CC0-Filterfunktion von OpenGameArt für 3D-Modelle wurde am 2026-07-25 nicht im Detail durchsucht – **offener Punkt**, weitere Recherche vor Festlegung nötig. Für den in §0 entschiedenen Pfad ist OpenGameArt nicht Teil der Prioritätsliste, kann aber bei Bedarf ergänzend geprüft werden.

### Sketchfab (CC0-Filter und CC-BY, Stufe 4 im entschiedenen Pfad)

Sketchfab führt Tag-Seiten für „cc0" und „military-vehicle" sowie „mech-low-poly"; konkrete Funde:

- „Military Landvehicle Kit 1.2" in der Sammlung von Sky_Void, laut Suchergebnis unter CC0. ([Sky_Void Military Vehicles Collection](https://sketchfab.com/Sky_Void/collections/military-vehicles-c481c846861a496583f0797a07dd9c96))
- „Low Poly Mecha" von blaze71643 – **CC-Attribution**, nicht CC0 (8,6k Tris, 5,6k Vertices). ([Low Poly Mecha](https://sketchfab.com/3d-models/low-poly-mecha-20d5edff7d6f4b2fbc738ec1fa2aa821))
- Diverse Low-Poly-Militärfahrzeug-Packs (z. B. MedSamer, vkh3d, RgsDev) mit uneinheitlicher Lizenz – jeweils vor Nutzung einzeln prüfen. ([Low-poly Military Vehicles pack](https://sketchfab.com/3d-models/low-poly-military-vehicles-pack-48137988cfef4ff88f51b08b996ecc1f))

**Wichtig:** Sketchfab-Modelle sind lizenzrechtlich heterogen (CC0 bis CC-BY-NC bis All-Rights-Reserved) und müssen **pro Einzelmodell** verifiziert werden, bevor sie ins Repo aufgenommen werden – gemäß Licenses.md §2.2 löst jedes CC-BY-Modell eine `CREDITS.md`-Pflicht aus. Gemäß §0 ist Sketchfab nur die letzte Stufe des entschiedenen Pfads, nachdem CC0-Kitbash und Hunyuan3D 2.1 geprüft und für eine Rolle nicht ausreichend befunden wurden.

### BlenderKit / Base Mesh (nicht abschließend geprüft)

BlenderKit wurde am 2026-07-25 nicht mit belastbaren CC0-Suchtreffern für Sci-Fi-Militärgebäude/-fahrzeuge verifiziert – **offener Punkt**, konnte nicht bestätigt werden. Kein Bestandteil des entschiedenen Pfads in §0.

## 2. KI-3D-Generierung: Lizenz- und Nutzungslage 2026

### Meshy — recherchiert, für MS-1 gesperrt (§0)

- **Bezahlplan (Pro/Studio/Enterprise):** Kunden besitzen die auf der Plattform erstellten Assets vollständig, sofern keine urheberrechtsverletzenden Quellmaterialien verwendet wurden – volle kommerzielle Rechte, keine Attributionspflicht. ([Meshy: Can I use my generated assets for commercial projects?](https://help.meshy.ai/en/articles/9992001-can-i-use-my-generated-assets-for-commercial-projects)) **Ausschlussgrund für MS-1:** bezahlter Tier, verstößt gegen das harte 0-€-Limit aus §0/E1.
- **Free-Tier:** Assets stehen unter CC BY 4.0 – kommerzielle Nutzung ist erlaubt, aber **Attributionspflicht** gegenüber Meshy (Vorschlag: „Model created with Meshy – CC BY 4.0 License"). ([Meshy: Ownership of generated models](https://help.meshy.ai/en/articles/10137554-what-is-the-ownership-of-the-generated-models)) **Ausschlussgrund für MS-1:** die explizite Freigabe zur Weitergabe in einem öffentlichen Repo konnte nicht wörtlich verifiziert werden (siehe unten) – gemäß §0 „Gesperrt für eingecheckte Assets".
- **Weitergabe im öffentlichen Repo:** Die Meshy-Hilfeseiten adressieren öffentliche Code-Repository-Weitergabe nicht explizit. Diese Interpretation für den spezifischen Fall „öffentliches GitHub-Repo" **konnte am 2026-07-25 nicht wörtlich in den Meshy-Nutzungsbedingungen verifiziert werden**.
- Preise: Free 0 €, Pro 20 $/Monat, Studio 60 $/Monat, Enterprise custom. ([Meshy Pricing](https://www.meshy.ai/pricing))

### Tripo3D (Tripo AI) — recherchiert, für MS-1 gesperrt (§0)

- **Free-Tier:** Modelle werden öffentlich unter CC BY 4.0 veröffentlicht – **nicht kommerziell nutzbar** laut Rechercheergebnis (Attribution-only/nicht-kommerziell eingestuft). ([Tripo AI: 3D Assets License Guide](https://www.tripo3d.ai/game-development/3d-assets-license-game-development)) **Ausschlussgrund für MS-1:** widersprüchliche/nicht-kommerzielle Free-Tier-Lage, siehe unten.
- **Pro-Tier (bezahlt):** Volle kommerzielle Rechte, private Modelle, 3000 Credits/Monat in den Suchergebnissen genannt. Export als STL/OBJ ohne Einschränkung. ([Tripo AI Pricing/Guide](https://lorphic.com/tripo-ai-pricing-3d-models-full-guide-and-review/)) **Ausschlussgrund für MS-1:** bezahlter Tier, verstößt gegen das 0-€-Limit.
- **Widerspruch zu beachten:** Eine Quelle bezeichnet den Free-Tier-Output als „CC BY 4.0" (also grundsätzlich mit Attribution kommerziell nutzbar), eine andere als „nicht-kommerziell". Dieser Widerspruch **konnte am 2026-07-25 nicht auf der offiziellen Tripo3D-Terms-Seite abschließend aufgelöst werden** – da der 0-€-Pfad gemäß §0 ohnehin nicht auf Tripo3D setzt, ist diese Klärung für MS-1 nicht mehr blockierend, bleibt aber als offener Rechercheposten in §„Offene Punkte" stehen.

### Hunyuan3D (Tencent) — entschiedener KI-Pfad (§0, Stufe 2)

- **Hunyuan3D 1.0/2.0:** „TENCENT HUNYUAN NON-COMMERCIAL LICENSE AGREEMENT" – laut GitHub-Repo **nicht** für kommerzielle Nutzung freigegeben. ([Hunyuan3D-1 Notice](https://github.com/Tencent-Hunyuan/Hunyuan3D-1/blob/main/Notice), [Hunyuan3D-2 LICENSE](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE)) Diese Versionen sind **nicht** Teil des entschiedenen Pfads.
- **Hunyuan3D 2.1:** „Tencent Hunyuan 3D 2.1 Community License Agreement" – erlaubt kommerzielle Nutzung, solange keine Eigentumsansprüche an Original-Modellen/-Code erhoben und die Pretrained-Modelle selbst nicht weiterverteilt werden; Tencent beansprucht keine Rechte an den generierten Outputs, Nutzer tragen die Verantwortung für die Outputs. ([DeepWiki: Hunyuan3D-2.1 Licensing](https://deepwiki.com/Tencent-Hunyuan/Hunyuan3D-2.1/7-licensing-and-usage)) **Dies ist die Version, auf die sich der in §0 entschiedene Pfad stützt** – lokal/self-hosted betrieben, 0 € Lizenzkosten, kommerzielle Nutzung und Output-Eigentum beim Nutzer, solange nur Outputs (Meshes) und keine Modellgewichte weiterverteilt werden.
- **Wichtige Korrektur gegenüber Licenses.md:** Das bestehende Lizenz-Register (Licenses.md §1, Stand Sprint 5) führt Hunyuan3D pauschal als „Open Source / Public Domain" mit uneingeschränkter Repo-Freigabe. Diese Recherche zeigt eine **differenziertere Lage**: Nur die 2.1-Community-Lizenz erlaubt kommerzielle Nutzung, mit der Einschränkung, dass die Pretrained-Modelle selbst nicht weiterverteilt werden dürfen (generierte Assets/Outputs sind davon laut Tencents Aussage nicht betroffen, aber die Formulierung „keine Weiterverteilung der Pretrained-Modelle" ist zu prüfen, falls Modellgewichte statt nur Outputs ins Repo sollen – das ist bei diesem Projekt ohnehin nicht vorgesehen, da nur Outputs eingecheckt werden). **Empfehlung:** Licenses.md §1 in einer separaten Lizenz-Sprint-Aufgabe auf die konkrete Hunyuan3D-Version (2.1 Community License) präzisieren.

### Rodin AI (Hyper3D / Deemos) — recherchiert, für MS-1 gesperrt (§0)

- Alle Abo-Stufen gewähren laut Rechercheergebnis volle kommerzielle Nutzung der generierten Assets; kein kostenloser CC0-artiger Tier gefunden – Preismodell ist monatliches Abo. ([Rodin Gen-2 AI Wiki](https://aiwiki.ai/wiki/rodin_gen_2)) **Ausschlussgrund für MS-1:** kein verifizierter 0-€-Tier, damit außerhalb des harten Budget-Limits aus §0/E1.
- Deemos hat im Dezember 2025 eine kommerzielle Lizenz für Trainingsdaten von Shutterstock erworben, was laut Presseerklärung ein „rights-cleared" Trainingsdaten-Ökosystem stärken soll – ein positives Signal gegen Trainingsdaten-Rechtsrisiken, aber eine **Presseerklärung, keine unabhängig verifizierte Rechtsprüfung**. ([Hyper3D/Shutterstock Presseerklärung](https://markets.financialcontent.com/poteaudailynews/article/pressadvantage-2025-12-16-hyper3d-acquires-commercial-3d-asset-license-from-shutterstock-and-announces-cooperation-with-blender))
- **Kein Free-Tier mit 0-€-Kompatibilität verifiziert** – dies bleibt ein offener Rechercheposten, ist aber für die MS-1-Entscheidung nicht mehr blockierend, da Rodin AI ohnehin nicht Teil des in §0 entschiedenen Pfads ist.

### OpenAI Image API — entschiedener Pfad-Baustein (§0, Stufe 3)

Die OpenAI Image API dient in diesem Katalog ausschließlich zur Erzeugung von 2D-Referenzblättern (Concept-Sheets, Orthogonalansichten), die als Input für den Image-to-3D-Schritt von Hunyuan3D 2.1 verwendet werden. Das Output-Eigentum an den generierten 2D-Bildern liegt laut den zum Zeitpunkt der Nutzung geltenden OpenAI-Nutzungsbedingungen beim Nutzer; eine sitzungsspezifische Verifikation der aktuellen ToS-Fassung wurde im Rahmen dieser Dokument-Fortschreibung nicht erneut durchgeführt und sollte vor dem ersten produktiven Einsatz kurz gegengeprüft werden (Eintrag in „Offene Punkte").

## 2a. Hardware-Voraussetzungen: Hunyuan3D 2.1 lokal/self-hosted (E3)

Der entschiedene Pfad setzt in Stufe 2 (§0) auf lokal/self-hosted betriebenes Hunyuan3D 2.1. Das erfordert eine geeignete GPU. Folgende Angaben sind zu unterscheiden:

**Recherchiert/belegbar (Projekt-/Modell-Dokumentation, allgemein bekannt für die Hunyuan3D-2.x-Modellfamilie):**
- Hunyuan3D 2.1 ist eine Diffusion-basierte Shape- und PBR-Textur-Generierungs-Pipeline (zwei Teilmodelle: Shape-Generierung, Textur-Synthese), die lokal ausführbar ist und dafür eine dedizierte NVIDIA-GPU mit CUDA-Unterstützung voraussetzt. Reduzierte Modell-Varianten (Mini/Turbo-Konfigurationen innerhalb der 2.x-Familie) benötigen weniger Ressourcen als der volle Shape+Textur-Lauf.

**Geschätzt, am 2026-07-25 NICHT durch eine erneute Prüfung der offiziellen Repo-Dokumentation belegt (vor Produktivnutzung zu verifizieren):**
- **VRAM-Größenordnung:** für den vollen Shape+Textur-Lauf realistischerweise **ca. 16–24 GB VRAM** (Größenordnung High-End-Consumer-GPU, z. B. RTX 4080/4090-Klasse); für reduzierte Mini/Turbo-Konfigurationen ggf. weniger. Dies ist eine Einschätzung basierend auf vergleichbaren Diffusion-3D-Pipelines, keine wörtlich zitierte Herstellerangabe.
- **Laufzeit je Mesh:** grob geschätzt wenige Minuten für die Shape-Stufe zzgl. mehrerer weiterer Minuten für die Textur-Synthese auf einer High-End-Consumer-GPU – d. h. ein einzelnes Asset dürfte sich eher in Minuten als in Sekunden oder Stunden bewegen. Auch dies ist eine Schätzung, keine belegte Zahl.

**Rückfallebene, falls die vorhandene Hardware für den lokalen Hunyuan3D-2.1-Lauf nicht ausreicht:** In diesem Fall ist **nicht** auf einen bezahlten Cloud-Dienst auszuweichen (das würde §0/E1 verletzen), sondern der Umfang der KI-generierten Rollen zu reduzieren und stattdessen verstärkt auf den reinen CC0-Kitbash-Pfad (§1) zurückzugreifen, auch wenn das für fraktionsspezifische Rollen eine geringere visuelle Differenzierung bedeutet (siehe Risiko in §9).

## 3. Team-Farben-Masken-Workflows

1. **Blender Vertex-Paint → Textur-Bake.** Ein Farb-Attribut-Layer wird auf dem Mesh angelegt (Properties → Data → Color Attributes, Domain „Face Corner" für weiche Verläufe), die Team-Zone wird im Vertex-Paint-Modus markiert und anschließend über einen Bake-Pass in eine Textur-Maske gebacken (z. B. mit dem Blender-Addon „Bake to Vertex Color" bzw. dem umgekehrten Weg via „Vertex Color Master"-Addon für Kanal-Isolation R/G/B/A). **Aufwand:** gering bis mittel (~0,25–0,5 PT pro Asset), gut geeignet für Kitbashing-Assets aus CC0-Modulteilen, da die UV-Zuordnung oft bereits vorhanden ist. ([Bake to Vertex Color Addon](https://www.blendernation.com/2020/07/31/blender-addon-bake-to-vertex-color/), [Vertex Color Master](https://github.com/andyp123/blender_vertex_color_master))
2. **Material-Zonen → ID-Map (Blender + Substance-artiger Workflow).** Materialzuweisungen im Mesh werden in eine Vertex-Color-ID-Map konvertiert, die dann in einem Textur-Tool (z. B. Substance Painter oder einem OSS-Äquivalent) als Maskenbasis für Team-Color-Fills dient. **Aufwand:** mittel (~0,5–1 PT), sinnvoll bei Assets mit mehreren funktional getrennten Flächen (z. B. Panzerrumpf vs. Turm). ([Turning Material Zones into Vertex Colour](https://www.versluis.com/2021/11/turning-material-zones-into-vertex-colour-in-blender-and-id-maps-in-substance-painter/))
3. **Unity URP Channel-Packed-Mask-Textur (Runtime-Shader-Ansatz).** Statt Vertex-Farben wird ein Masken-Kanal (z. B. der Alpha- oder ein ungenutzter RGB-Kanal der Material-Textur, siehe AssetBudget.md §2 „Teamfarben über Mask-Channel + Shader") direkt im Textur-Atlas mitgeführt und im URP-Shader-Graph per Lerp-Node zur Laufzeit eingefärbt. Dies passt zur bereits in AssetBudget.md §2 verbindlich vorgegebenen Atlanten-Pflicht (1 Material/1 Textur-Set je Typ) und vermeidet zusätzliche Vertex-Color-Bakes. **Aufwand:** gering bei neu erstellten/KI-generierten Assets (Maske direkt im Textur-Pass mitgezeichnet), mittel bei CC0-Fremdmaterial (Nachbearbeitung des vorhandenen Materials nötig). ([Unity ColorMask Command](https://docs.unity3d.com/Manual//SL-ColorMask.html), [Unity: Channel-packed Texture in URP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/shaders-in-universalrp-channel-packed-texture.html), [Asset Color Customization with Shader Graph and Color Masks](https://4experience.co/asset-color-customization-with-shader-graph-and-color-masks/))

**Empfehlung:** Workflow 3 (Channel-Packed-Mask im URP-Shader) ist am kompatibelsten mit dem bestehenden Atlanten-/Ein-Material-Standard aus AssetBudget.md §2 und sollte als Standard-Pipeline für alle importierten CC0- und KI-Assets gelten; Workflow 1 dient als Fallback für Assets ohne sauber trennbare UV-Inseln.

## 4. Risiko: KI-Trainingsdaten- und Urheberrechtslage 2026

- **Konkreter 3D-Bezug:** Am 26. März 2026 wurden mehrere Klagen gegen Meta, Roblox, Microsoft und Nvidia eingereicht (Kläger: Beaulier), die sich auf die Entfernung von Autorennamen aus unter Creative-Commons-Lizenz veröffentlichten 3D-Modellen im Rahmen von KI-Modell-Training beziehen. Dies ist ein **direkt 3D-Asset-relevanter Präzedenzfall** und zeigt, dass CC-lizenziertes 3D-Trainingsmaterial rechtlich angreifbar sein kann, auch wenn der eigene Output nicht direkt betroffen ist. ([AI Copyright Training Data Lawsuits 2026](https://www.aivortex.io/legal/guides/ai-copyright-training-data-2026-landscape/))
- **Breiterer Rahmen:** Im Fall NYT v. OpenAI wurde ein Antrag auf Klageabweisung abgelehnt, da die Outputs plausibel mit dem Originalmaterial konkurrieren könnten; am 10. März 2026 verhandelte die Große Kammer des EuGH in „Like Company v. Google" erstmals direkt die Frage, ob LLM-Training EU-Urheberrecht verletzt. Diese Verfahren betreffen primär Text/LLMs, zeigen aber einen allgemeinen Trend zu strengerer gerichtlicher Prüfung von KI-Trainingsdaten, der auf 3D-Generatoren übertragbar sein könnte. ([AI Watch.dog Lawsuits](https://aiwatch.dog/lawsuits), [IAM: Fair Use in AI Training Disputes](https://www.iam-media.com/article/how-us-courts-are-addressing-fair-use-questions-in-ai-training-and-copyright-disputes))
- **Risikoeinschätzung für Hashkrieg (kein Rechtsrat):** Da unsere KI-generierten Assets nicht das Trainingsmaterial selbst reproduzieren, sondern neue Meshes erzeugen, ist das direkte Klagerisiko für das Projekt aktuell als **gering bis mittel** einzuschätzen. Das größere praktische Risiko liegt nicht in Klagen gegen uns, sondern darin, dass ein Anbieter (z. B. wegen eigener Rechtsstreitigkeiten) seine Lizenzbedingungen rückwirkend verschärft oder den Dienst einstellt.
- **Was passiert bei Lizenzänderung eines Anbieters:** Bereits generierte und ins Repo aufgenommene Assets bleiben unter der zum Generierungszeitpunkt geltenden Lizenz nutzbar, sofern diese das zusichert (wie bei Hunyuan3D 2.1 Community License: Nutzer behält Output-Eigentum) – **rückwirkende Verschärfungen sind aber nicht in jedem Fall ausgeschlossen und anbieterspezifisch zu prüfen**. Als Gegenmaßnahme empfiehlt sich: (a) Lizenztext und Abrufdatum bei jedem Import archivieren (siehe §10), (b) den in §0 entschiedenen, engen Anbieterkreis (CC0-Quellen + Hunyuan3D 2.1) beibehalten statt zusätzliche Anbieter unkontrolliert hinzuzufügen, (c) bei Anbieterwechsel betroffene Assets im Lizenz-Register (Licenses.md) markieren statt stillschweigend zu ersetzen.

## 5. Rollen-Zuordnung (17 Rollen, entschiedene Primärstrategie + Fallback)

Rollen-Bezeichner gemäß `quality/content/mvp-v1.json` (9 Gebäude-, 8 Einheiten-Rollen, je ×2 Fraktionen = 34 Assets). Jede Rolle trägt genau **eine** entschiedene Primärstrategie (`CC0-Base`, `AI-Generated` oder `Hybrid`) gemäß Prioritätspfad aus §0 – keine offenen „entweder/oder"-Zeilen mehr.

| Rolle | Primärquelle | Fallback | Strategie |
|---|---|---|---|
| HQ | Quaternius Modular Sci-Fi MegaKit (Kitbash-Basis) + Hunyuan3D 2.1 (Detailpass für fraktionsspezifische Silhouette) | Kenney Sci-Fi RTS + reiner CC0-Kitbash ohne Detailpass | Hybrid |
| Power | Quaternius Modular Sci-Fi MegaKit | Hunyuan3D 2.1 | CC0-Base |
| Refinery | Quaternius Modular Sci-Fi MegaKit | Hunyuan3D 2.1 | CC0-Base |
| Storage | Quaternius Modular Sci-Fi MegaKit | Hunyuan3D 2.1 | CC0-Base |
| Barracks | Kenney Sci-Fi RTS | Hunyuan3D 2.1 | CC0-Base |
| VehicleFactory | Kenney Sci-Fi RTS | Quaternius Ultimate Modular Sci-Fi | CC0-Base |
| ResearchLab | Quaternius Modular Sci-Fi MegaKit | Hunyuan3D 2.1 | CC0-Base |
| Radar | Quaternius Sci-Fi Essentials Kit (Basis) + Hunyuan3D 2.1 (Antennen-/Dish-Detail) | reiner CC0-Kitbash ohne Detailpass | Hybrid |
| DefensePlatform | Quaternius Sci-Fi Modular Gun Pack + MegaKit-Sockel (Basis) + Hunyuan3D 2.1 (Detailpass) | reiner CC0-Kitbash ohne Detailpass | Hybrid |
| Builder (Infanterie) | Hunyuan3D 2.1 (humanoides Mesh, kein vollständiger CC0-Humanoid-Bestand verifizierbar, siehe §1) | Sketchfab (einzeln lizenzgeprüft) | AI-Generated |
| Harvester (Fahrzeug) | Kenney Sci-Fi RTS | Hunyuan3D 2.1 | CC0-Base |
| BasicInfantry | Hunyuan3D 2.1 | Sketchfab (einzeln lizenzgeprüft) | AI-Generated |
| AntiArmorInfantry | Hunyuan3D 2.1 | Sketchfab (einzeln lizenzgeprüft) | AI-Generated |
| ScoutVehicle | Kenney Sci-Fi RTS (Buggy-Basis) + Hunyuan3D 2.1 (Fraktions-Skin) | reiner CC0-Kitbash ohne Fraktions-Skin | Hybrid |
| LightTank | Hunyuan3D 2.1 (fraktionsspezifisch: Lynx/Räuber, Vertical-Slice-Priorität gemäß §11) | Quaternius/Kenney-Kitbash generische Panzer-Chassis-Basis | AI-Generated |
| BattleTank | Hunyuan3D 2.1 (fraktionsspezifisch) | Quaternius/Kenney-Kitbash generische Panzer-Chassis-Basis | AI-Generated |
| Artillery | Quaternius Sci-Fi Modular Gun Pack + Fahrzeug-Basis (Basis) + Hunyuan3D 2.1 (Detailpass) | reiner CC0-Kitbash ohne Detailpass | Hybrid |

**Zusammenfassung:** 7 Rollen `CC0-Base`, 5 Rollen `Hybrid`, 5 Rollen `AI-Generated`. Bei allen Rollen gilt für beide Fraktionen dieselbe Primärquelle je Rolle; die fraktionsspezifische Differenzierung erfolgt entweder über die Team-Color-Maske (bei `CC0-Base`-Rollen, siehe §3) oder über einen fraktionsspezifischen Hunyuan3D-2.1-Prompt/-Detailpass (bei `Hybrid`- und `AI-Generated`-Rollen).

## 6. Umsetzungsreihenfolge MVP (E5)

Die Bearbeitung folgt dieser verbindlichen Reihenfolge, um die Beschaffungs-Pipeline (CC0-Kitbash + Hunyuan3D 2.1) zuerst an einem kleinen, repräsentativen Ausschnitt zu validieren, bevor Aufwand in die volle Rollenbreite fließt:

1. **Vertical-Slice-Assets zuerst:** Allianz-HQ, Lynx (Allianz LightTank), Legion-HQ, Räuber (Legion LightTank) – diese vier Assets decken je einen `Hybrid`- und einen `AI-Generated`-Fall pro Fraktion ab und validieren beide Pipeline-Zweige aus §0 gleichzeitig. Details zum Slice-Umfang selbst liegen in der parallel bearbeiteten Datei „VerticalSlice_MS1.md" (hier nur referenziert, nicht verlinkt, da zeitgleich in Bearbeitung).
2. **Danach die übrigen Gebäuderollen:** Power, Refinery, Storage, Barracks, VehicleFactory, ResearchLab, Radar, DefensePlatform – überwiegend `CC0-Base`, geringeres Risiko, da die Kitbash-Pipeline bereits am Slice erprobt wurde.
3. **Zuletzt die übrigen Einheiten-Rollen:** Builder, Harvester, BasicInfantry, AntiArmorInfantry, ScoutVehicle, BattleTank, Artillery – hier greift für die `AI-Generated`- und `Hybrid`-Fälle dieselbe Hunyuan3D-2.1-Pipeline, die am Slice bereits validiert wurde.

**Begründung:** Der Slice validiert die technische Pipeline (Kitbash-Workflow, Hunyuan3D-2.1-Setup inkl. Hardware-Realität aus §2a, Team-Color-Masken-Workflow aus §3) an vier Assets, bevor derselbe Aufwand auf die verbleibenden 30 Assets skaliert wird. Scheitert die Pipeline an einem der beiden Zweige, ist der Korrekturaufwand auf vier statt 34 Assets begrenzt.

## 7. Quellenmatrix

| Quelle | Lizenz | Kommerziell | Öffentliches Repo | Attribution | Abdeckung unserer Rollen | Stil-Passung | URL | geprüft am |
|---|---|---|---|---|---|---|---|---|
| Quaternius (MegaKit u. a.) | CC0 | ja | ja | nein | Gebäude (Kitbashing), Waffen-Module | hoch | [quaternius.com](https://quaternius.com/packs/modularscifimegakit.html) | 2026-07-25 |
| Kenney Sci-Fi RTS | CC0 | ja | ja | nein | Gebäude + Fahrzeuge (breit, ungeprüft im Detail) | mittel | [kenney.nl/assets/sci-fi-rts](https://kenney.nl/assets/sci-fi-rts) | 2026-07-25 |
| Poly Haven | CC0 | ja | ja | nein | nur Texturen/Material | n/a (Textur-Quelle) | [polyhaven.com](https://polyhaven.com/) | 2026-07-25 |
| ambientCG | CC0 | ja | ja | nein | nur Texturen/Material | n/a (Textur-Quelle) | [ambientcg.com](https://ambientcg.com/) | 2026-07-25 |
| OpenGameArt (Sci-fi RTS Sprites) | ungeklärt (2D, nicht 3D) | n/a | n/a | n/a | keine (2D-Sprites) | niedrig | [opengameart.org](https://opengameart.org/content/sci-fi-rts-120-sprites) | 2026-07-25 |
| Sketchfab (CC0-Tag, einzelmodell-abhängig) | uneinheitlich, pro Modell prüfen | teils | teils (nur CC0-Modelle) | teils (CC-BY-Modelle) | Fahrzeuge/Mechs punktuell, nur als Stufe 4 (§0) | mittel | [sketchfab.com/tags/cc0](https://sketchfab.com/tags/cc0) | 2026-07-25 |
| Meshy (Pro-Tier) | Nutzer-Eigentum (kommerziell) | ja | vermutlich ja, nicht wörtlich verifiziert | nein (Pro) | alle Rollen (Prompt-abhängig) | hoch (Prompt-Qualität-abhängig) | [meshy.ai/pricing](https://www.meshy.ai/pricing) | 2026-07-25 — **für MS-1 gesperrt, bezahlter Tier** |
| Meshy (Free-Tier) | CC BY 4.0 | ja | ja, mit Attribution | ja | alle Rollen (Prompt-abhängig) | hoch | [help.meshy.ai](https://help.meshy.ai/en/articles/10137554-what-is-the-ownership-of-the-generated-models) | 2026-07-25 — **für MS-1 gesperrt, siehe §0** |
| Tripo3D (Pro-Tier) | Nutzer-Eigentum (kommerziell) | ja | nicht abschließend verifiziert | nein | alle Rollen (Prompt-abhängig) | hoch (nicht getestet) | [tripo3d.ai](https://www.tripo3d.ai/game-development/3d-assets-license-game-development) | 2026-07-25 — **für MS-1 gesperrt, bezahlter Tier** |
| Tripo3D (Free-Tier) | CC BY 4.0, laut einer Quelle nicht-kommerziell | widersprüchlich, nicht verifiziert | nein (nicht kommerziell laut einer Quelle) | ja (falls kommerziell nutzbar) | – | – | [tripo3d.ai](https://www.tripo3d.ai/game-development/3d-assets-license-game-development) | 2026-07-25 — **für MS-1 gesperrt, siehe §0** |
| Hunyuan3D 1.0/2.0 | Non-Commercial License | **nein** | nein | n/a | – (nicht nutzbar) | – | [GitHub LICENSE](https://github.com/Tencent-Hunyuan/Hunyuan3D-2/blob/main/LICENSE) | 2026-07-25 |
| Hunyuan3D 2.1 | Tencent Hunyuan 3D 2.1 Community License | ja (mit Einschränkungen) | ja (Outputs), Pretrained-Modelle selbst nicht weiterverteilen | nein | alle Rollen (Prompt-abhängig) | hoch (nicht getestet) | [DeepWiki Lizenz](https://deepwiki.com/Tencent-Hunyuan/Hunyuan3D-2.1/7-licensing-and-usage) | 2026-07-25 — **entschiedener KI-Pfad, siehe §0** |
| Rodin AI (Hyper3D) | Abo-basiert, kein 0-€-Free-Tier verifiziert | ja (alle Bezahl-Tiers) | nicht verifiziert | nein | alle Rollen (Prompt-abhängig) | hoch (nicht getestet) | [Rodin Gen-2 AI Wiki](https://aiwiki.ai/wiki/rodin_gen_2) | 2026-07-25 — **für MS-1 gesperrt, kein 0-€-Tier** |

## 8. KI-Anbietervergleich

| Anbieter | Kommerzielle Nutzung | Eigentum am Output | Repo-Weitergabe | Free-Tier-Limits | Attribution | Exportformate/Qualität | MS-1-Status |
|---|---|---|---|---|---|---|---|
| Meshy | ja (Pro), ja mit Attribution (Free) | Nutzer (Pro), Meshy/CC-BY (Free) | vermutlich ja, nicht wörtlich verifiziert | begrenzte Credits, CC-BY-Pflicht | nein (Pro) / ja (Free) | FBX/OBJ/glTF, laut Marktbeschreibung produktionsnahe Topologie in v6 | gesperrt |
| Tripo3D | ja (Pro), widersprüchlich (Free) | Nutzer (Pro) | nicht verifiziert | 3000 Credits/Monat (Pro) | nein (Pro) / ja falls kommerziell nutzbar | STL/OBJ | gesperrt |
| Hunyuan3D 2.1 | ja, mit Einschränkung (keine Weiterverteilung der Pretrained-Modelle) | Nutzer (Tencent beansprucht keine Rechte an Outputs) | ja für Outputs | Open-Weight, lokal betreibbar (kein Credit-System bekannt) | nein | Community-License, Details modellabhängig | **entschiedener Pfad** |
| Rodin AI (Hyper3D) | ja (alle Bezahl-Tiers) | nicht abschließend verifiziert | nicht verifiziert | kein 0-€-Tier verifiziert | nicht verifiziert | bis zu 10 Mio. Polygone (Gen-2.5), Übermaß für unsere Budgets – Downsampling nötig | gesperrt |

## 9. Risiken

Siehe §4 oben (Trainingsdaten-Rechtsstreitigkeiten, Lizenzänderungsrisiko, Handlungsempfehlung). Zusätzlich, spezifisch zur 0-€-Entscheidung aus §0:

- **Der 0-€-Pfad erkauft geringere Eigentums-/Rechtssicherheit als ein bezahlter Tier gehabt hätte.** Ein bezahlter Meshy-/Tripo3D-Pro-Tier hätte eine explizit vertraglich zugesicherte, kommerziell abgesicherte Eigentümerschaft am Output geboten. Der stattdessen gewählte Hunyuan3D-2.1-Community-License-Pfad stützt sich auf eine Community-Lizenz statt auf einen individuell verhandelten kommerziellen Vertrag; die in §2 dokumentierte Unschärfe („Weiterverteilung der Pretrained-Modelle" vs. „Weitergabe der Outputs") ist zwar für den hier vorgesehenen Anwendungsfall (nur Outputs, keine Modellgewichte im Repo) nach aktuellem Rechercheergebnis unkritisch, wurde aber nicht durch eine unabhängige Rechtsprüfung bestätigt. Das ist der Preis der 0-€-Entscheidung: geringere vertragliche Absicherung im Tausch gegen keine laufenden Kosten.
- **Hardware-Abhängigkeit als zusätzliches Risiko:** Der KI-Pfad (Stufe 2, §0) setzt lokale GPU-Kapazität voraus (siehe §2a). Reicht die vorhandene Hardware nicht aus, verengt sich der praktisch nutzbare Pfad auf reines CC0-Kitbash, was die fraktionsspezifische visuelle Differenzierung einzelner Rollen (insbesondere `AI-Generated`-Rollen wie LightTank/BattleTank) schwächen kann.

## 10. Nachweispflichten je Quellenart

Für jedes importierte Asset ist Folgendes im Lizenz-Register ([Licenses.md](Licenses.md) §3-Ledger) zu dokumentieren:

**CC0-Quellen (Quaternius, Kenney, Poly Haven, ambientCG, Sketchfab-CC0):**
- Quellname, Pack-/Modell-Name, URL
- Lizenzversion/-typ (z. B. „CC0 1.0")
- Abrufdatum
- ggf. vorgenommene Änderungen (Retopo, Materialumbau, Kitbashing-Kombination)
- Repo-Freigabe ja/nein

**CC-BY-Quellen (z. B. einzelne Sketchfab-Modelle):**
- wie oben, zusätzlich: Autor/Urheber, exakter Attributionstext, Eintrag in `CREDITS.md`

**KI-generierte Assets (Hunyuan3D 2.1, ggf. OpenAI Image API für 2D-Referenzen):**
- verwendetes Modell/Version (z. B. „Hunyuan3D 2.1, lokal/self-hosted")
- vollständiger Prompt-Text bzw. verwendetes 2D-Referenzblatt
- Generierungsdatum
- Lizenztyp zum Generierungszeitpunkt (Community License) mit Beleg-URL der ToS-Version
- vorgenommene Nachbearbeitung (Retopo, Rigging, Team-Color-Maske)
- Repo-Freigabe ja/nein, inkl. Begründung bei „nein"

## Offene Punkte

- **Tripo3D Free-Tier-Lizenz widersprüchlich recherchiert** (CC BY 4.0 vs. „nicht-kommerziell") – für MS-1 nicht mehr blockierend, da Tripo3D gemäß §0 ohnehin gesperrt ist; die Klärung bleibt als allgemeiner Rechercheposten offen, falls der Anbieter später erneut relevant wird.
- **Meshy: explizite Aussage zur Weitergabe in einem öffentlichen GitHub-Repo** konnte in den Nutzungsbedingungen nicht wörtlich gefunden werden – für MS-1 nicht mehr blockierend, da Meshy gemäß §0 gesperrt ist.
- **Hunyuan3D-Version-Diskrepanz zu Licenses.md:** Das bestehende Lizenz-Register führt Hunyuan3D pauschal als unbeschränkt frei; diese Recherche zeigt, dass nur Version 2.1 (Community License) kommerziell nutzbar ist und mit Einschränkungen bei der Weiterverteilung der Pretrained-Modelle. Licenses.md §1 sollte in einem eigenen Lizenz-Sprint präzisiert werden – dies ist jetzt **dringlicher**, da Hunyuan3D 2.1 der entschiedene KI-Pfad ist.
- **Hunyuan3D-2.1-Hardware-Angaben (VRAM, Laufzeit) sind in §2a als Schätzung markiert** und müssen vor dem produktiven Einsatz an der tatsächlich vorhandenen Projekt-Hardware verifiziert werden (z. B. Testlauf mit einem einzelnen Vertical-Slice-Asset).
- **OpenAI Image API ToS für die konkrete Nutzung als Image-to-3D-Referenzquelle** wurden im Rahmen dieser Dokument-Fortschreibung nicht erneut gegengeprüft – vor erstem produktivem Einsatz kurz verifizieren.
- **Rodin AI 0-€-Tauglichkeit nicht verifiziert** – für MS-1 nicht mehr blockierend, da Rodin AI gemäß §0 gesperrt ist.
- **BlenderKit/Base-Mesh-CC0-Bestand für Sci-Fi-Militär** nicht verifiziert.
- **OpenGameArt 3D-CC0-Bestand** (jenseits der gefundenen 2D-Sprites) nicht im Detail durchsucht.
- **Kenney Sci-Fi-RTS-Detailinhalt** (welche der 120 Assets exakt welche der 17 Rollen abdecken) nicht auf Modell-Ebene verifiziert – manueller Download-Review vor Festlegung als Primärquelle nötig.
- **Bezahlquellen (außerhalb des 0-€-Pfads, nur falls die 0-€-Entscheidung später revidiert wird):** kommerzielle Asset-Store-Bundles (z. B. Synty-Stil-Pakete) wurden in dieser Recherche bewusst nicht evaluiert, da §0 sie ausschließt.

## Nächste Schritte

1. Sprint 7/8: Vertical-Slice-Assets zuerst umsetzen (Allianz-HQ, Lynx, Legion-HQ, Räuber) gemäß Reihenfolge in §6, um beide Pipeline-Zweige (CC0-Kitbash-Hybrid und Hunyuan3D-2.1-AI-Generated) an einem kleinen Ausschnitt zu validieren.
2. Sprint 7/8: Manueller Download-Review von Kenney Sci-Fi RTS und Quaternius MegaKit gegen die 17 Rollen und AssetBudget.md-Polycount-Grenzen; Ergebnis in AssetRegister.md eintragen.
3. Sprint 8: Testlauf von Hunyuan3D 2.1 auf der tatsächlich vorhandenen Projekt-Hardware, um die in §2a geschätzten VRAM-/Laufzeit-Angaben zu verifizieren oder zu korrigieren.
4. Sprint 8: Licenses.md §1 auf Hunyuan3D-2.1-Community-License präzisieren (separater Lizenz-Fix, kein Bestandteil dieses Dokuments).
5. Vor erstem Asset-Import: Nachweispflichten aus §10 als Pflichtfelder ins Import-PR-Template übernehmen (Abstimmung mit AssetBudget.md §6 Kauf-Prüfung).

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-25 | Erstfassung: Recherche zu CC0-3D-Quellen, KI-3D-Lizenzlage, Team-Color-Masken-Workflows und Trainingsdaten-Risiken für MS-1 unter D-054 | Producer |
| 0.2.0 | 2026-07-25 | Beschaffungspfad entschieden: 0-€-Limit hart (E1), verbindliche Prioritätsreihenfolge CC0-Kitbash → Hunyuan3D 2.1 lokal → OpenAI Image API → Sketchfab (E2), bezahlte Tiers als Empfehlung gestrichen; Hardware-Voraussetzungen Hunyuan3D 2.1 ergänzt (E3); alle 17 Rollen auf je eine entschiedene Primärstrategie festgelegt (E4); Umsetzungsreihenfolge MVP mit Vertical-Slice-Priorität ergänzt (E5) | Producer |
</content>
