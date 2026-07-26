# Hashkrieg — Concept-Art-Style-Guide

**Version:** 0.1.0 | **Status:** Entwurf – Concept-Art-Standard, kein Gate-Nachweis | **Verantwortungsbereich:** Art Direction | **Sprint:** 7

## Zweck

Dieses Dokument ist das verbindliche Style-Sheet für alle Concept-Art-Bilder von *Hashkrieg*. Es legt Bildformat, Lichtsetzung, Farbwelt, Renderstil und Formensprache je Fraktion fest und liefert eine wiederverwendbare Prompt-Vorlage samt Abnahmekriterien. Jedes generierte Bild wird gegen dieses Dokument geprüft — es ist die Referenz, nicht ein Beispiel unter mehreren.

## Abhängigkeiten

- [`../vision/Lore.md`](../vision/Lore.md) — Weltentwurf, insbesondere Abschnitt 5 („Warum Blau gegen Rot"), aus dem die Formensprache je Fraktion in diesem Dokument abgeleitet wird.

## 1. Bildformat und Rahmung

Für alle Assets identisch, ohne Ausnahme:

| Parameter | Wert |
|---|---|
| Auflösung | 1024 × 1024 Pixel, quadratisch |
| Perspektive | Frontale Ansicht (Vorderansicht), orthografisch anmutend — keine perspektivische Verzerrung, keine Drei-Viertel-Drehung |
| Kameraachse | Kamera auf halber Objekthöhe, Blickachse waagerecht |
| Objektposition | Exakt mittig im Bild |
| Objektgröße | Füllt rund 78 % der Bildhöhe, umlaufend Luft zum Bildrand |
| Hintergrund | Einfarbig sehr dunkles Blauschwarz `#0B1017` — ohne Umgebung, ohne Bodenfläche, ohne Horizont; dezente Vignette ist erlaubt |
| Bildzusätze | Kein Text, keine Logos, keine Bedienoberfläche, keine Wasserzeichen, kein Rahmen |

Diese Rahmung ist bewusst starr: Nur wenn Kamera, Objektgröße und Hintergrund über alle Assets hinweg konstant bleiben, werden Fraktion, Rolle und Maßstab allein aus Form, Farbe und Detaildichte lesbar (siehe Abschnitt 7).

## 2. Lichtsetzung

Für alle Assets identisch:

- **Führungslicht:** kalt, von vorn oben links.
- **Kantenlicht:** warm, von hinten rechts — hebt die Silhouette vom dunklen Hintergrund ab (Rim-Light).
- **Aufhellung:** weich, ohne schwarze Löcher in den Schattenpartien.
- Keine harten Schlagschatten auf eine Bodenfläche — es gibt ohnehin keine sichtbare Standfläche im Bild; Schattenzeichnung bleibt auf das Objektvolumen selbst beschränkt.

Diese zweipolige Beleuchtung (kalt führend, warm konturierend) spiegelt bewusst die Farbwelt selbst: Beide Fraktionen erscheinen unter demselben Licht, das Kantenlicht ist neutral warm und trägt keine Fraktionsfarbe — die Identität kommt ausschließlich aus dem Leuchtakzent (Abschnitt 3), nicht aus der Szenenbeleuchtung.

## 3. Farbwelt

| | Allianz | Legion |
|---|---|---|
| Körperfarbe | Stahlgrau `#8A9199` | Rostrot `#7A3524` |
| Flächen/Platten | Azur `#2C6E9E` | Ocker `#B08430` |
| Leuchtakzent | Cyan `#58D5E8` | Orange `#FF8A3D` |
| Tiefen | Blauschwarz `#0B1017` | Rußschwarz `#2B2018` |

**Regel zum Leuchtakzent:** Der Leuchtakzent trägt die Fraktionsidentität und sitzt ausschließlich an Kanten, Lüftungsschlitzen, Emittern und Fugen — er entspricht funktional der Teamfarben-Maske im späteren Spielasset (dem Bereich, der im Spiel pro Spieler eingefärbt wird). Zielkorridor: 5–12 % der sichtbaren Fläche. Er ist damit ein Akzent, kein Flächenanstrich — Körperfarbe und Flächenfarbe bleiben die dominanten Farbwerte, der Leuchtakzent markiert nur die funktional „aktiven" Stellen des Objekts (Energieführung, Sensorik, Warnmarkierung).

## 4. Renderstil

Stilisierte Militär-Science-Fiction, malerische Concept-Art, mit der Anmutung von *Tempest Rising* und *Command & Conquer 3*: kinematisch, hoher Kontrast, klare lesbare Silhouette. Materialität und Volumen werden über Licht und Form gelesen, nicht über fotografische Textur.

**Negativliste — ausdrücklich ausgeschlossen:**

- Kein Fotorealismus
- Kein Cartoon
- Kein Anime
- Kein Blueprint (keine technische Konstruktionszeichnung)
- Kein technisches Schaubild (kein Explosionsdiagramm, keine Bemaßungslinien, keine Beschriftung)

## 5. Formensprache je Fraktion

Die Formensprache folgt unmittelbar aus der Überzeugung jeder Fraktion (Lore Abschnitt 5), nicht aus einem beliebigen Fraktions-Look.

### Allianz — geschlossen, versiegelt, dauerhaft

Die Allianz hält den Großen Abschluss für ein technisches, kein moralisches Versagen: Eine Kette ohne verlässliche Instanz frisst ihre Teilnehmer, also muss wieder eine gebaut werden — geprüft, effizient, dauerhaft — bis „ein Kind erben kann". Diese Überzeugung ist eine Wette auf Bestand, und das muss die Form zeigen:

- Geschlossene, versiegelte Volumen — nichts liegt offen, weil offene Technik verwundbar und vergänglich wirkt.
- Saubere Kanten, keine sichtbaren Flickstellen — eine Naht wäre ein Eingeständnis von Vergänglichkeit.
- Vertikale Betonung — Aufwärtsstreben als Ausdruck von Dauer und Anspruch.
- Flüssigkühlung sichtbar als geführte, gefasste Leitungen — nie als loses Kabel, sondern als Teil der versiegelten Konstruktion.
- Symmetrie — Ordnung als Selbstzweck und Beleg von Kontrolle.
- Gesamtwirkung: teuer, gewartet, für die Ewigkeit gebaut.

### Legion — offen, gestapelt, ersetzbar

Die Legion hat den Großen Abschluss als Verlust erlebt und rechnet seither mit weiterem Verlust. Ihre Überzeugung — jede abrechnungsbefugte Instanz darf auch löschen, also darf niemand die Mehrheit halten — verlangt keine Dauerhaftigkeit, sondern jederzeitige Ersetzbarkeit: Sie bauen für morgen früh, nicht für die Ewigkeit, weil Bestand in ihrer Rechnung nur Angriffsfläche ist.

- Offene, gestapelte, angeschraubte Volumen — Bauteile bleiben einzeln austauschbar statt monolithisch versiegelt.
- Sichtbare Verkabelung, Schlote, Lüfterbänke aus geborgenen Fundstücken — Spielergrafikkarten, alte Racks, alles was Strom frisst und Wärme abgibt.
- Waagerechte Betonung — Ausbreitung und Deckungssuche statt Aufwärtsstreben.
- Nieten und Flickstellen offen sichtbar — Reparatur ist kein Makel, sondern der Beweis, dass es trotz allem läuft.
- Asymmetrisch — gewachsen aus dem, was gerade verfügbar war, nicht aus einem Plan.
- Gesamtwirkung: billig, laut, ersetzbar, in zwei Stunden wieder aufgebaut.

## 6. Silhouetten-Regel

Jedes Asset muss allein am Umriss — ohne Farbinformation, ohne Detailtextur — sowohl seiner Fraktion als auch seiner Funktion zuordenbar sein.

- **Fraktionszuordnung über Umriss:** Ein geschlossener, symmetrischer, vertikal betonter Umriss liest sich als Allianz; ein offener, asymmetrischer, waagerecht betonter Umriss mit vorspringenden Anbauteilen liest sich als Legion. Wird das Bild auf eine reine schwarze Silhouette vor hellem Grund reduziert, muss diese Zuordnung weiterhin eindeutig sein.
- **Funktionszuordnung über Umriss:** Die Rolle eines Assets (z. B. Angriff, Verteidigung, Aufklärung, Logistik) muss sich aus charakteristischen Umrissmerkmalen ablesen lassen — etwa Waffenrohre und Turmaufbauten bei Kampfeinheiten, Antennen und offene Sensorik bei Aufklärung, große geschlossene Laderaumvolumen bei Logistik. Zwei Assets derselben Fraktion mit unterschiedlicher Rolle dürfen sich im Umriss nicht verwechseln lassen.
- **Prüfmethode:** Silhouette in Graustufen bzw. als reine Schwarzform betrachten — Fraktion und Funktion müssen ohne Farbe und ohne Oberflächendetail erkennbar bleiben.

## 7. Maßstabs-Hinweise

Da Kamera, Objektfüllung (78 % der Bildhöhe) und Bildformat für jedes Asset identisch sind, kann Maßstab nicht über Kameraabstand oder Bildkomposition vermittelt werden. Er entsteht ausschließlich über Detaildichte und die Größe wiedererkennbarer Bauteile, deren reale Maße über alle Assets hinweg konstant bleiben — eine Tür bleibt eine Tür, unabhängig davon, wie groß das Objekt ist, an dem sie sitzt.

Feste Referenzgrößen für Bauteile (gelten für alle Assets gleich):

- Tür/Zugangsluke: ca. 2 m Höhe
- Leiter/Trittstufe: ca. 0,3 m Sprossenabstand
- Handgriff/Halterung: ca. 0,1 m
- Niete/Verschraubung: ca. 2 cm

Daraus folgt die Detaildichte je Größenklasse:

- **Gebäude (ca. 12 m):** ruhige, großflächige Grundform mit wenigen, aber großen Baugruppen. Türen und Zugänge wirken im Verhältnis zur Gesamtform klein — mehrere Personen passten nebeneinander hindurch. Leitern und Treppen sind schmale Nebenelemente an der Fassade. Nietenraster und Plattenfugen sind fein und in dichter Wiederholung über große Flächen verteilt.
- **Fahrzeug (ca. 6 m):** mittlere Detaildichte. Luken und Einstiege sind in plausibler Ein-Personen-Größe, Griffe und Trittstufen einzeln zählbar und deutlich sichtbar. Ketten- oder Radsegmente sind als einzelne, klar unterscheidbare Elemente erkennbar.
- **Infanterist (ca. 2 m):** höchste Detaildichte relativ zur Gesamtform. Ausrüstungsteile — Gürtel, Taschen, Visier, Handschuhe, Gelenkpanzerung — stehen in unmittelbarem, menschentypischem Größenverhältnis zum Körper. Jedes Bauteil ist einzeln lesbar; es gibt keine kleinteilige Wiederholung, weil dafür auf dieser Größe kein Platz ist.

Kurz: Nicht das Objekt wird verkleinert oder vergrößert dargestellt — die Anzahl und Dichte gleich großer Referenzbauteile im Bild verrät die tatsächliche Größe.

## 8. Wiederverwendbare Prompt-Vorlage

Platzhalter: `{FRAKTION}` (Alliance / Legion), `{ROLLE}` (z. B. main battle tank, forward scout, heavy siege walker), `{BESCHREIBUNG}` (kurze visuelle Beschreibung des konkreten Assets), `{SILHOUETTE}` (das eine prägende Umrissmerkmal, an dem die Rolle erkennbar sein soll).

```
Stylized military sci-fi concept art of a {FRAKTION} {ROLLE}, in the painterly style of Tempest Rising and Command & Conquer 3 — cinematic, high contrast, clear readable silhouette. {BESCHREIBUNG}. Silhouette-defining feature: {SILHOUETTE}.

Camera: strict frontal view, orthographic-looking, no perspective distortion, no three-quarter angle. Camera at half the object's height, eye line horizontal. Subject centered, filling about 78% of the frame height, even margin of air around it.

Background: solid dark blue-black #0B1017, no environment, no ground plane, no horizon, subtle vignette allowed only.

Lighting: cool key light from front upper-left; warm rim light from behind upper-right separating the silhouette from the background; soft fill, no crushed blacks, no hard cast shadows.

Faction color language for {FRAKTION}:
- Alliance: steel-gray body #8A9199, azure plating #2C6E9E, cyan glow accent #58D5E8, blue-black recesses #0B1017. Sealed, closed volumes, clean edges, vertical emphasis, coolant lines shown as enclosed conduits, symmetric, expensive and permanent-looking.
- Legion: rust-red body #7A3524, ochre plating #B08430, orange glow accent #FF8A3D, soot-black recesses #2B2018. Open, stacked, bolted-on volumes, exposed cabling, stacks and salvaged fan banks, horizontal emphasis, visible rivets and patch repairs, asymmetric, cheap and replaceable-looking.

The glow accent color carries faction identity only — apply it strictly to edges, vents, emitters, and seams, covering roughly 5-12% of the visible surface. Do not use it as a base color.

No text, no logos, no UI elements, no watermarks, no frame. No photorealism, no cartoon style, no anime style, no blueprint/schematic look, no exploded-view diagram, no dimension lines or labels.
```

## 9. Negativ-Hinweise

- Keine Umgebung, keine Bodenfläche, kein Horizont, keine Requisiten im Hintergrund.
- Kein Text, keine Logos, keine Bedienoberfläche, keine Wasserzeichen, kein Rahmen im Bild.
- Kein Fotorealismus, kein Cartoon, kein Anime, kein Blueprint, kein technisches Schaubild, keine Bemaßungslinien.
- Keine Drei-Viertel-Ansicht, keine Vogel- oder Froschperspektive, keine perspektivische Stauchung.
- Keine harten Schlagschatten, keine ausgefressenen (komplett schwarzen) Schattenpartien.
- Keine Vermischung der Leuchtakzentfarben zwischen den Fraktionen (kein Cyan an Legion-Assets, kein Orange an Allianz-Assets).
- Kein Leuchtakzent als Flächenfarbe — er bleibt auf Kanten, Schlitze, Emitter und Fugen begrenzt.
- Keine Formensprache-Vermischung: keine offenen, angeschraubten Fundstück-Elemente an Allianz-Assets; keine versiegelten, symmetrischen Reinformen an Legion-Assets.

## 10. Abnahmekriterien

Checkliste für die Prüfung eines generierten Bildes gegen dieses Style-Sheet:

1. Bild ist 1024 × 1024 Pixel, Objekt in strikt frontaler Ansicht ohne perspektivische Verzerrung.
2. Objekt ist zentriert, füllt ca. 78 % der Bildhöhe, mit umlaufender Luft zum Bildrand.
3. Hintergrund ist einfarbig `#0B1017`, ohne Umgebung, Bodenfläche oder Horizont.
4. Kaltes Führungslicht von vorne oben links und warmes Kantenlicht von hinten rechts sind erkennbar; keine schwarzen Löcher, keine harten Schlagschatten.
5. Körperfarbe und Flächenfarbe entsprechen den Hex-Werten der jeweiligen Fraktion aus Abschnitt 3.
6. Der Leuchtakzent sitzt an Kanten/Schlitzen/Emittern/Fugen und deckt schätzungsweise 5–12 % der sichtbaren Fläche, nicht mehr.
7. Die Silhouette bleibt in reiner Schwarzform ohne Farbinformation eindeutig der richtigen Fraktion und Funktion zuordenbar.
8. Die Formensprache stimmt mit der Fraktion überein (Allianz: geschlossen, symmetrisch, vertikal betont; Legion: offen, asymmetrisch, waagerecht betont, sichtbar repariert).
9. Detaildichte und Bauteilgrößen (Türen, Griffe, Nieten) entsprechen der beabsichtigten Objektgröße gemäß Abschnitt 7.
10. Es sind keine Elemente aus der Negativliste in Abschnitt 4 oder 9 im Bild vorhanden (kein Text, kein Fotorealismus, keine Drei-Viertel-Ansicht usw.).

## Offene Punkte

- Ob Einheitennamen (Lynx, Räuber, Aegis, Koloss) aus der bestehenden Fiktion übernommen oder an die neue Fiktion angepasst werden, ist laut Lore noch offen und wird hier nicht vorweggenommen — die Prompt-Vorlage arbeitet deshalb mit Rollenbezeichnungen statt Eigennamen.
- Ob es je Fraktion eine begrenzte Zahl erlaubter Sekundärfarben (z. B. Warnmarkierungen, Verschleißspuren) geben soll, ist noch nicht festgelegt.
- Verhalten des Leuchtakzents bei sehr kleinen Assets (Infanterie), bei denen 5–12 % Flächenanteil optisch kaum als Kantenlicht darstellbar sind, ist noch zu klären.

## Nächste Schritte

1. Freigabe dieses Style-Sheets durch die Art Direction.
2. Testgenerierung je eines Beispielbilds pro Fraktion und Größenklasse (Gebäude, Fahrzeug, Infanterist) zur Kalibrierung der Prompt-Vorlage.
3. Abgleich der Abnahmekriterien anhand der ersten Testbilder, bevor die Vorlage in die reguläre Bildproduktion übernommen wird.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-26 | Erstfassung: Bildformat, Lichtsetzung, Farbwelt, Renderstil, Formensprache je Fraktion, Silhouetten- und Maßstabsregeln, Prompt-Vorlage, Negativ-Hinweise, Abnahmekriterien | Art Direction |
