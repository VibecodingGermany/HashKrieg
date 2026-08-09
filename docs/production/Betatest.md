# Betatest — das Spiel jemandem in die Hand geben

**Version:** 1.0.0 | **Status:** aktiv | **Verantwortungsbereich:** Netzstrang / Producer | **Sprint:** 13 | **Leitsatz:** ein Tester, der nicht weiß, was bewusst fehlt, meldet uns unsere eigene Roadmap zurück

## Zweck

Hashkrieg ist seit Sprint 12 eine vollständige Runde lang spielbar — aber
gespielt hat sie fast nur, wer sie gebaut hat. Dieses Dokument macht aus dem
Testpaket eine Testrunde: **Teil A** geht an die Testenden, **Teil B** bleibt
bei uns.

Der Engpass, den das löst, steht in
[Sprint 13](hashkrieg/13_Sprint_Netzpartie.md) unter „Der zweite Mensch": die
Abnahmestufen 3 und 4 sind keine Technik-, sondern eine Verfügbarkeitsfrage.

---

# Teil A — Für die Testenden

*Dieser Teil ist zum Weitergeben gedacht. Alles darunter ist intern.*

## Was du da vor dir hast

Hashkrieg ist ein Echtzeit-Strategiespiel im Stil der klassischen
Command-&-Conquer-Schule: Basis bauen, Rohstoff ernten, Armee aufstellen, die
gegnerische Zentrale zerstören. Zwei Fraktionen — Allianz gegen Legion.

Was du bekommst, ist ein **Testbuild, kein Spiel**. Der Kern läuft von vorne bis
hinten durch, aber Oberflächen sind roh, Zahlen sind ungewuchtet und ein paar
Gebäude kosten Geld und tun nichts. Das ist bekannt und steht unten aufgelistet.

**Eine Runde dauert 15 bis 30 Minuten.** Plane für den ersten Durchgang lieber
eine Stunde ein — der Reiz liegt darin, was dir dabei auffällt.

## Installieren

### macOS

1. `ProjectNova-<commit>.dmg` öffnen.
2. `ProjectNova.app` in den Ordner `Applications` ziehen.
3. Starten.

Der Build ist signiert und von Apple notarisiert — es kommt **keine** Warnung.
Wenn doch eine kommt, ist das ein Befund: bitte melden.

### Windows

1. `ProjectNova-win-x64-<commit>.zip` **komplett auspacken** (Rechtsklick → „Alle
   extrahieren"). Nicht direkt aus dem ZIP starten, das Spiel findet dann seine
   Daten nicht.
2. `ProjectNova.exe` doppelklicken.
3. Windows meldet **„Der Computer wurde durch Windows geschützt"**. Das ist
   erwartet: der Build ist nicht signiert (ein Windows-Zertifikat kostet
   dreistellig im Jahr, das lohnt für zwei Testende nicht).
   → **„Weitere Informationen"** → **„Trotzdem ausführen"**.

Wenn dein Virenscanner die Datei wegräumt, ist das ebenfalls erwartbar und
ebenfalls ein Befund — sag uns welcher Scanner.

## Welchen Build hast du?

Bitte nenne diesen Wert bei jeder Rückmeldung. Er ist die einzige Möglichkeit,
deine Beobachtung einem Stand zuzuordnen.

```bash
# macOS
defaults read /Applications/ProjectNova.app/Contents/Info.plist NovaBuildCommit
```

```bat
REM Windows — im entpackten Ordner
type ProjectNova_Data\NovaBuildCommit.txt
```

Er steht auch im Dateinamen des Pakets. Steht ein `-dirty` dahinter, war beim
Bauen etwas nicht sauber — dann sag uns Bescheid, statt zu testen.

## Deine erste Runde

Alles Nötige ist anklickbar. Die Bauleiste liegt unten, die Kommandokarte des
ausgewählten Objekts rechts, die Minimap links unten. Tastenkürzel gibt es
zusätzlich, du brauchst sie nicht.

1. **Neues Spiel** im Hauptmenü. Du bist die Allianz, der Computer die Legion.
2. **Raffinerie bauen.** In der Bauleiste anklicken, dann mit der Maus platzieren
   — der Baugeist ist grün, wo es geht, und rot, wo nicht. Linksklick setzt,
   Rechtsklick bricht ab.
3. **Harvester produzieren.** Die kommen aus der **Raffinerie**, nicht aus dem
   Hauptquartier. Sie fahren danach von allein zum Aetherium-Feld und wieder
   zurück; da musst du nichts mikromanagen.
4. **Kraftwerk und Kaserne**, sobald der Strom knapp wird.
5. **Truppen bauen**, zusammenstellen, mit Rechtsklick zur Gegnerbasis schicken.
6. **Das feindliche Hauptquartier zerstören.** Dann kommt der
   Ergebnisbildschirm.

`P` pausiert. `F3` blendet ein Debug-Panel ein — interessant, wenn du wissen
willst, was die Simulation gerade denkt.

## Was bewusst noch fehlt

Das hier ist bekannt. Melde es nicht — außer du hast etwas Neues dazu.

| | |
|---|---|
| **Kein Attack-Move** | Truppen schießen zwar von selbst, halten unterwegs aber nicht an, um zu kämpfen. Sie laufen ins Ziel und feuern nebenbei |
| **Kein Speichern und Laden** | Der Menüeintrag „Laden" ist sichtbar und ausgegraut |
| **Lager und Radar** | kosten Geld und tun nichts. Zwei von neun Gebäuden warten noch auf ihre Wirkung |
| **Kein Wirtschaftsdruck** | Strommangel bremst noch nicht, Aetherium geht nicht aus. Die Runde ist dadurch entspannter, als sie sein soll |
| **Mehrspieler** | Der Verbindungsdialog existiert, aber eine Partie zu zweit hat noch nie jemand gespielt. Das machen wir gemeinsam mit Termin — nicht allein ausprobieren |
| **Grafik** | Je nach Paket siehst du fertige Modelle oder graue Platzhalterklötze. Beides ist vollständig spielbar |

## Was uns wirklich hilft

Nicht „das Spiel ist gut". Sondern das hier:

1. **Die ersten fünf Minuten.** Wo hast du gestockt? Was hast du gesucht und
   nicht gefunden? Was hast du angeklickt, das nichts getan hat?
2. **Wo du rausgefallen bist.** Der Punkt, an dem du das Spiel weggelegt oder
   ins Menü zurückgegangen bist — und warum.
3. **Was sich falsch angefühlt hat.** Zu langsam, zu schnell, zu unfair, zu
   zäh. Gefühl reicht völlig, du musst es nicht begründen.
4. **Was du erwartet hast, das nicht passiert ist.** Das ist die wertvollste
   Kategorie überhaupt. Erwartungen verraten Designfehler zuverlässiger als
   Meinungen.
5. **Ob es flüssig lief.** Ruckler, Standbilder, laute Lüfter — und ab wann
   (viele Einheiten? großes Gefecht?).
6. **Ton.** Zu laut, zu leise, nervig, fehlt.
7. **Lesbarkeit.** Erkennst du auf der Karte, was dir gehört und was nicht? Wo
   der Gegner ist? Wann ein Gebäude fertig wird?

**Eine Bitte:** Sag, was du **gesehen** hast, nicht was wir ändern sollen.
„Ich hab dreimal auf die Kaserne geklickt und nichts passierte" ist Gold.
„Ihr solltet das UI überarbeiten" können wir nicht nachbauen.

## Wie du zurückmeldest

**Sprachnachricht ist ausdrücklich erwünscht** — so lang du willst, ungeschnitten,
gerne beim Spielen mitgesprochen. Wir transkribieren das hier.

Sag am Anfang einmal:

- Welchen Build (siehe oben) und welches Betriebssystem
- Ob es die erste Runde war oder die dritte

Danach einfach erzählen. Wenn du lieber tippst: ein GitHub-Issue über
**Fehler melden** tut es genauso.

Screenshots und kurze Videos helfen enorm — vor allem bei allem, was mit
„das sah komisch aus" anfängt.

## Wenn es abstürzt

Das Logfile bitte mitschicken:

```bash
# macOS
~/Library/Logs/Project Nova/Project Nova/Player.log
```

```bat
REM Windows
%USERPROFILE%\AppData\LocalLow\Project Nova\Project Nova\Player.log
```

## Rechtliches in zwei Sätzen

Der Build ist dir zum Testen überlassen, nicht zur Weitergabe (PolyForm
Noncommercial 1.0.0). Bitte lade ihn nirgends hoch und gib ihn nicht weiter —
nicht aus Geheimniskrämerei, sondern weil ein Build ohne unseren Commit-Stempel
im Umlauf jede Fehlermeldung wertlos macht.

---

# Teil B — Für uns

## Wer bekommt was

| | |
|---|---|
| **Kanal** | Paket über den Drive-Ordner, Anleitung als Kopie von Teil A dazu |
| **Empfängerkreis** | namentlich bekannt, ein bis zwei Personen. Kein offener Link |
| **Kein CLA nötig** | Testende tragen keinen Code bei. Schickt jemand später Code, greift der normale Weg aus [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| **Kein Repo-Zugang** | Ein Tester braucht weder Fork noch Collaborator-Eintrag. Das Zugangsmodell aus [13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md) bleibt unberührt |

## Vor dem Verschicken — vier Prüfungen

1. **Sauberer Arbeitsbaum.** Ein `-dirty`-Paket wird nicht verschickt. Die
   Skripte hängen das Suffix an, aber sie hindern niemanden.
2. **Welcher Art-Stand steckt drin?** Das Art-Paket liegt außerhalb von Git. Ein
   Build aus einem frischen Klon zeigt Graubox-Klötze, ein Build von der
   Arbeitsmaschine zeigt Modelle. Beides ist legitim — aber wir müssen wissen,
   welches wir verschickt haben, sonst ist „sieht komisch aus" nicht zuordenbar.
3. **Einmal selbst gestartet.** Mindestens die Plattform, auf der wir das können.
   Der Windows-Player entsteht auf einem Mac und wurde bis heute nie ausgeführt.
4. **Commit notiert.** Wer welchen Stempel hat, gehört in den
   [GrayboxLog](GrayboxLog.md) — sonst raten wir später.

## Kadenz — wann ein Build ungültig wird

Der Relay sperrt Matches zwischen ungleichen Builds (Fingerprint-Sperre,
Sprint 12 A4). Daraus folgt die Regel aus
[13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md):

> Nach jedem Merge-Fenster: Build für **jede** Plattform, an der jemand testet,
> neuer Build an alle Testenden, alter Build ist ungültig.

Mit Testenden im Umlauf bekommt diese Regel Kosten. Zwei Konsequenzen:

- **Simulationsändernde Merges werden gesammelt**, nicht einzeln durchgereicht.
  Jeder einzelne kostet sonst eine Verteilrunde.
- **Solo-Feedback altert langsamer als Netz-Feedback.** Wer nur gegen die KI
  spielt, kann einen älteren Build behalten, solange wir wissen welchen. Für eine
  gemeinsame Partie müssen alle Seiten exakt gleich sein — dort ist der Rebuild
  Pflicht, nicht Komfort.

## Wohin die Befunde gehen

| Art des Befunds | Ziel |
|---|---|
| Reproduzierbarer Fehler | GitHub-Issue (`Fehler melden`) |
| Gefühl, Fluss, Balance, „fühlte sich falsch an" | [GrayboxLog](GrayboxLog.md) als Sitzungseintrag mit Commit |
| Fehlender Umfang | [ScopeLedger](ScopeLedger.md), nicht als Bug |
| Alles aus Sprachnachrichten | erst transkribieren, dann auf die drei Zeilen oben verteilen. Die Rohtranskription ist kein Ablageort |

Ein Befund ohne Commit ist eine Anekdote. Das gilt für Testende genauso wie für
uns.

## Offene Punkte

- Windows-Build-Modul (`windows-mono`) ist auf der Arbeitsmaschine **nicht
  installiert** — ohne das gibt es kein Windows-Paket. Siehe
  [tools/packaging/README.md](../../tools/packaging/README.md).
- Der Windows-Player wurde nie gestartet. Der erste Testende ist gleichzeitig
  der erste Beweis, dass er läuft.
- Ob das Betatest-Programm eine eigene D-ID braucht, ist offen. Es verteilt
  Binaries an Externe, berührt aber weder Verträge noch Simulation.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Erstfassung: Anleitung für Testende, Verteil- und Rückmeldeweg, Kadenzregel mit Testenden im Umlauf | Producer / Agent (Umsetzung) |
