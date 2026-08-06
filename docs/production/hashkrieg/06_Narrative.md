# Narrative — Namen, Erzählung durch Mechanik, Minimal-Kampagne

**Version:** 0.1.0 | **Status:** Entwurf – Vorschläge zur Entscheidung, kein Gate-Nachweis | **Verantwortungsbereich:** Game Director / Narrative | **Sprint:** 7

## Zweck

Die Lore ist stark und praktisch fertig. Was fehlt, ist der Weg von der Lore ins
Produkt: Namen, die die Fraktionen hörbar trennen; Erzählung, die auf **heute
vorhandener** Mechanik sitzt; und eine Kampagne, die zwei Entwickler wirklich
bauen können.

Alle Vorschläge hier sind **Vorschläge**. Sie kosten zusammen weniger als einen
Tag Arbeit und entscheiden über den Ton des gesamten Spiels.

## Abhängigkeiten

- [../../vision/Lore.md](../../vision/Lore.md) – verbindlicher Weltentwurf
- [../../gamedesign/Campaign.md](../../gamedesign/Campaign.md) – Kampagnenrahmen (widersprüchlich, siehe §3)
- [../../gamedesign/Factions.md](../../gamedesign/Factions.md) – Fraktionsprofile
- [02_Masterplan.md](02_Masterplan.md) – Phase 7

## Ausgangslage

Die Lore trägt: Lange Liquidation, Großer Abschluss, die Kette
*Aetherium → Strom → Rechenleistung → Anteil → Alles*, beide Fraktionen mit je
einem eingebauten Widerspruch, und die beste Zeile des ganzen Dokuments — *„Warum
kein Frieden möglich ist: Aus Arithmetik."*

Beide Repositorien tragen praktisch identische Fassungen; ein Diff ergibt genau
eine abweichende Zeile. Es gibt **keine zwei divergierenden Lore-Stände**.

Was fehlt, ist die Brücke ins Spiel. Heute erreicht **kein einziger
Anzeigename** den Bildschirm.

---

## 1. Fraktionsdoktrin für Namen — neun Strings

### Der Ist-Zustand wirkt unsauber, trägt aber ein Muster

Die Allianz hat deutsche Gebäudenamen (Kommandozentrale, Fahrzeugwerk), aber
englische Einheitennamen (Rifleman, Rocket Soldier, Jackal, Lynx, Longbow) und
zwei Mischformen mit Anführungszeichen (Pionier „Atlas", Sammler „Demeter").
Die Legion ist durchgehend deutsch (Rekrut, Räuber, Koloss, Donnerkanone).

Das sieht nach Zufall aus. Es lässt sich aber exakt aus der Lore begründen — und
sollte deshalb **nicht verworfen, sondern zur Regel erhoben werden.**

### Vorschlag: aus dem Zufall wird Doktrin

**Allianz** — *„Es war ein Buchhaltungsfehler."* Baut für die Ewigkeit,
institutionell.
→ Gebäude tragen die nüchterne deutsche Funktionsbezeichnung einer Behörde.
→ Einheiten tragen **deutsche Gattung plus englischen Programmnamen in
Anführungszeichen** — so, wie eine Beschaffungsstelle Rüstungsprogramme benennt.

**Legion** — *„Genau so fängt es wieder an."* Baut für morgen früh,
zusammengelötet.
→ Ausschließlich deutscher Spitzname. Kein Programmname, keine
Anführungszeichen. Was die Werkstatt auf die Wanne malt.

### Konkretes Delta

| Heute (Allianz) | Vorschlag |
|---|---|
| Rifleman | Infanterist „Rifleman" |
| Rocket Soldier | Panzerabwehrschütze „Lance" |
| Jackal-Aufklärer | Aufklärer „Jackal" |
| Lynx | Leichter Panzer „Lynx" |
| Aegis | Kampfpanzer „Aegis" |
| Longbow | Artillerie „Longbow" |
| **Aegis-Plattform** | **Wachplattform „Bastion"** |
| Depot | Verwahrstelle |

Pionier „Atlas" und Sammler „Demeter" erfüllen die Regel bereits.

| Heute (Legion) | Vorschlag |
|---|---|
| Hyäne (Buggy) | Hyäne |

Die Klammer ist eine Entwicklernotiz, kein Name. Alle übrigen 16 Legion-Namen
bleiben unverändert.

### Ein echter Defekt wird dabei mitbehoben

> **„Aegis" ist doppelt vergeben** — die Allianz-Verteidigungsplattform heißt
> „Aegis-Plattform", der Allianz-Kampfpanzer heißt „Aegis". Zwei verschiedene
> Objekte derselben Fraktion mit demselben Eigennamen.

Das ist kein Geschmacksthema. Sobald Anzeigenamen im HUD, in Tooltips, in
Produktionsschlangen oder in Alarmen erscheinen (*„Aegis unter Beschuss"* —
welches?), ist die Meldung mehrdeutig. Die Einheit behält „Aegis" (stärkerer
Name am stärkeren Objekt, und Aegis als Schild passt zum schwersten Panzer), die
Plattform wird zur „Wachplattform Bastion".

**Aufwand: neun Stringänderungen in einer JSON-Datei.** Jetzt billig — nach der
Namens-Verdrahtung teurer.

### Ohne Masterplan 2.5 zahlt das null aus

Die Structs haben kein Namensfeld; eine Suche nach „Kommandozentrale" oder
„Donnerkanone" im Code trifft nur Kommentare. **Solange kein Anzeigename das
Bildschirmbild erreicht, ist jede Namensarbeit wirkungslos.**

Die Nachschlagetabelle gehört in die Präsentation, nicht in die Simulation —
Anzeigenamen dürfen den Definitions-Hash nicht berühren, sonst entwertet jede
Textkorrektur alle aufgezeichneten Replays.

---

## 2. Mechanik als Erzählung — sieben Zeilen, die heute schon einlösbar sind

[../../vision/Lore.md](../../vision/Lore.md) §7 hat die **richtige Denkweise**:
links, was der Spieler tut — rechts, was die Welt dabei sagt.

Aber alle fünf Zeilen setzen Hashkrieg-Systeme voraus, die es nicht gibt
(Echtzeit-Einnahmen aller Gegner, Umschalten Rechnen/Kämpfen, Halbierung der
Ausschüttung, Übertakten mit Hardwareverlust, Thermalradar). Keines existiert,
keines ist beschlossen. **Der Narrative-Strang hat damit ein Prinzip ohne einen
einzigen einlösbaren Fall.**

### Vorschlag: sieben Ersatzzeilen auf vorhandener Mechanik

| Was der Spieler erlebt | Was die Welt dabei sagt | Vorhanden? |
|---|---|---|
| Low Power halbiert Bau- und Produktionstempo | Der Allianz-Reaktor sackt hörbar ab, ihre Basis verliert Licht. Die Legion hat denselben Malus — aber ihr Generator klang ohnehin nie gesund. *„Die Allianz baut für die Ewigkeit, und die Ewigkeit braucht Strom."* | ja |
| Radar-Pings zeigen Gegner ohne Zielerlaubnis | *„Verstecken kannst du deine Stellungen, nicht dein Konto."* — die einzige Zeile der Originaltabelle, die heute schon einlösbar ist | ja |
| Verkauf erstattet 50 %, Abbruch 75 % | Für die Legion ist Einschmelzen Normalbetrieb, für die Allianz ein Verlust. Reine Text- und Bark-Frage, null Mechanik | ja |
| Das Zeitlimit wird ab Minute 40 zum sichtbaren Countdown | Fraktionsverschieden beschriftet: Allianz *„Abrechnungsfrist"*, Legion *„Feierabend"*. Macht *„Alle Kriege laufen gegen diese Uhr"* spürbar | ja (Anzeige fehlt) |
| Der erste eigene Verlust einer Runde | Löst genau **einen** Bark aus, danach nie wieder. Einmaligkeit erzeugt Bedeutung, Wiederholung zerstört sie | braucht 4.2 |
| Sieg und Niederlage | Fraktionsabhängig nach Lore §8: Allianz-Sieg *„Konsolidierung abgeschlossen"*, Legion-Sieg *„Der Hashkrieg geht weiter"* | ja |
| Das Aetherium-Feld läuft sichtbar leer | siehe §2.1 | braucht 1.3 |

**Vier Ergebniscodes mal zwei Fraktionen sind acht Textzeilen — für den größten
Ton-Gewinn im ganzen Vorschlag.**

Die Hashkrieg-Tabelle bleibt als Post-MVP-Reserve stehen und wird als solche
markiert.

### 2.1 · Die eine Zahl, die die eigene Fiktion widerlegt

`MatchBootstrap` setzt **2.000.000 AE pro Feld** ein. Bei zwei Harvestern reicht
das rund vierzehn Stunden — kein Feld geht je zur Neige.

Die Lore macht aber genau das Gegenteil zum Weltkern:

> *„Sie verzeiht Übernutzung nicht — ein ausgebluteter Mutterkristall kommt nicht
> zurück."*

und begründet damit überhaupt, **warum es eine Feldschlacht statt einer
Belagerung ist**.

Solange die Felder unerschöpflich sind, ist die zentrale Aussage der Fiktion im
Spiel nicht nur unerzählt, sondern **widerlegt**.

Der genaue Zielwert ist eine Balance-Frage und gehört gemessen, nicht geraten —
aber die Größenordnung 2 Mio ist erkennbar als Graybox-Platzhalter gesetzt und
nicht als Designaussage. Umgesetzt in Masterplan 1.3.

### 2.2 · Das billigste starke Erzählmittel überhaupt

Lore §8: *„Die Legion nennt es Hashkrieg. Die Allianz nennt es die
Konsolidierung."* Beide Begriffe sollen je nach gespielter Fraktion im Spiel
auftauchen.

**Reine Textvariable, null Mechanik — und noch nirgends umgesetzt.** Der Titel
des Spiels wird damit selbst zur Parteinahme, und das ist beabsichtigt.

---

## 3. Minimal-Kampagne „Erster Feldzug"

### Zwei Widersprüche müssen vorher aufgelöst werden

**Erstens die Prämisse.** [../../gamedesign/Campaign.md](../../gamedesign/Campaign.md)
baut auf „Aetherium-Fallout", dem Eintrag des Kristalls in die Welt als Auslöser
der Weltveränderung. Die Lore erzählt eine völlig andere Genese: eine
ökonomische Abrechnung über zwei Jahrzehnte, in der Aetherium erst **nach** dem
Zusammenbruch an den Bruchlinien toter Fusionsanlagen wächst. **Aetherium ist
dort Folge, nicht Ursache.**

**Zweitens die Struktur.** Das Kampagnendokument macht Akt III zur
Evolvierten-Perspektive und führt ab Mission 11 Evolvierten-Systeme ein. Die
Evolvierten sind nicht im Umfang und laut Lore ausdrücklich „die Wendung", also
späterer Stoff. **Ein Drittel der geplanten Kampagne ist nicht baubar.**

Solange dieser Widerspruch steht, produziert jede Missionsarbeit Material, das
später neu geschrieben werden muss.

### Der Vorschlag: fünf Missionen statt zwölf bis fünfzehn

Das Dokument fordert drei Akte, 12–15 Missionen, 30–50 Minuten je Mission,
8–12 Stunden gesamt. Das ist bei zwei Entwicklern und einem noch untätigen
Gegner nicht baubar.

**„Erster Feldzug": 5 Missionen, ausschließlich Allianz, je 10–15 Minuten,
gesamt rund eine Stunde.**

Der entscheidende Kniff: **Skriptete Angriffswellen sind ungleich billiger als
eine reagierende KI**, und vier der fünf Missionen brauchen überhaupt keinen
denkenden Gegner.

| # | Mission | Lehrt | Braucht KI? |
|---|---|---|---|
| **M1** | Kommandotrupp ohne Basis | Bewegen, Angreifen, Konterprinzip | nein |
| **M2** | Basis, Ernte, Low-Power-Regel gegen statische Ziele | Wirtschaft und Strom | nein |
| **M3** | Fahrzeugfabrik und T2 über das Forschungslabor | Techkette | nein |
| **M4** | Verteidigung gegen Wellen | Druck ohne Gegner-Intelligenz | nein |
| **M5** | Angriff auf eine befestigte Basis | Alles zusammen | teilweise |

**M1 ist mit dem heutigen Code sofort baubar** — kein Bau, keine Wirtschaft, kein
Gegneraufbau. Sie erzählt zugleich *„Die Allianz baut für die Ewigkeit"* aus der
Froschperspektive derer, die noch nichts haben.

**M4 umgeht den KI-Blocker vollständig** und erzeugt trotzdem Druck. Das ist
genau der Missionstyp, den das Kampagnendokument ohnehin vorsieht.

Gestrichen gegenüber dem Dokument: Luftfahrzeuge, Flugabwehr, Superwaffe, Mauern
und Drohnen — im Code nicht vorhanden.

**Der Legion-Feldzug folgt als zweiter Schritt mit denselben Karten aus der
Gegenrichtung.** Das liefert die Lore-Doppelperspektive (*„Zwei Feldzüge, beide
wahr, keiner richtig"*) zum halben Preis.

### Das eigentliche Argument

> Diese Reihenfolge macht das Spiel **vorzeigbar, bevor** die KI-Arbeit fertig
> ist — während Skirmish ohne echte KI unspielbar bleibt.

### Präventive Vertragszeile, die jetzt nichts kostet

**Missions-Skripte müssen durch die `CommandIngress`.**

`MatchRunner` legt fest: *UI und KI erzeugen nur `CommandIntent`-Werte und
reichen sie an die Ingress.* Die heutige KI verletzt das bereits durch
Direktaufrufe — mit der Folge, dass ihre Aktionen nie im Record-Stream landen.

Skriptete Angriffswellen sind exakt derselbe Fall: Wenn ein Missions-Trigger
Einheiten direkt spawnt oder Befehle direkt setzt, ist jede Mission nicht
aufzeichenbar, nicht wiederholbar und beim späteren Replay- oder Netzbetrieb
desynchron.

**Jetzt festzulegen kostet nichts. Nach fünf gebauten Missionen ist es eine
Umbauaktion.** Das Kampagnendokument fordert bereits eine flache Trigger-Sprache
— dort gehört die Ingress-Pflicht als Vertragszeile hinein.

---

## 4. Vier offene Lore-Punkte, zwei davon entscheidungsreif

[../../vision/Lore.md](../../vision/Lore.md) führt sie seit der Erstfassung. Zwei
halten die Arbeit auf:

1. **Zeitabstand zwischen Großem Abschluss und Spielgegenwart.** Das Dokument
   macht selbst den Vorschlag: *rund 30 Jahre, damit eine Generation die alte
   Welt noch aus Erzählungen kennt, aber niemand mehr aus eigener Anschauung.*
   Der Vorschlag trägt und sollte schlicht bestätigt werden — er bestimmt jeden
   Missionstext, jede Bark-Formulierung und das Alter jeder Figur.
   → **Empfehlung: bestätigen.**

2. **Einheitennamen** — durch die Doktrin in §1 entscheidbar.
   → **Empfehlung: Doktrin annehmen.**

3. **Ist Aetherium natürlich entstanden oder ausgesät?** Die Empfehlung des
   Dokuments — *nie auflösen* — ist gut und sollte als Festlegung notiert
   werden, damit die Frage nicht in jeder Missionsbesprechung neu aufgemacht
   wird.
   → **Empfehlung: als bewusst offen festschreiben.**

4. **Geografische Verortung** — kann offen bleiben, kostet nichts.

Solange 1 und 2 offen sind, ist jeder geschriebene Missionstext vorläufig.

---

## 5. Was ausdrücklich Reserve bleibt

Die Mechanik-Inversion aus
[../../vision/Konzept_Hashkrieg.md](../../vision/Konzept_Hashkrieg.md) —
öffentlicher Hashrate-Ticker, Anteils-Einkommen, Halving, 51-Prozent-Attacke,
Hot/Cold Wallet — ist **nicht beschlossen** und existiert nirgends im Code. Das
Dokument empfiehlt selbst, MS-1 planmäßig fertigzustellen und Hashkrieg danach
als Prototyp zu erproben.

Dieser Plan folgt dieser Empfehlung: **Hashkrieg ist der Name und die Welt,
Aetherium bleibt die Ressource, die Mechanik-Inversion bleibt Reserve.**

Zwei Elemente aus dem Konzept sind allerdings unabhängig davon prüfenswert, weil
sie ohne Krypto-Fiktion ins bestehende Setting passen: der **öffentliche
Wirtschafts-Ticker** und das **Anteils-Einkommen** als Anti-Stall-Formel. Beides
ist eine eigene Designentscheidung, kein Teil dieses Plans.

## Offene Punkte

- §1 Doktrin annehmen oder verwerfen (neun Strings).
- §3 Widerspruch zwischen Kampagnendokument und Lore auflösen — das
  Kampagnendokument muss auf die Lore-Genese umgeschrieben werden.
- §4 Punkte 1 und 3 bestätigen.
- Ob die Minimal-Kampagne vor oder nach der Skirmish-KI kommt, ist eine
  Produktentscheidung mit Argumenten auf beiden Seiten.

## Nächste Schritte

1. §1 und §4 entscheiden — zusammen unter einer Stunde.
2. Masterplan 2.5 umsetzen, sonst zahlt §1 null aus.
3. Die acht Sieg- und Niederlagenzeilen aus §2 schreiben — größter Ton-Gewinn
   pro Aufwand im gesamten Plan.
4. Kampagnendokument auf die Lore-Genese und auf zwei Fraktionen korrigieren,
   bevor die erste Mission gebaut wird.
