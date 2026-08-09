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

## Wie du zurückmeldest — laut denken, ChatGPT sortiert

Du sollst nichts aufschreiben und nichts sortieren. Du sollst reden.

1. **Handy neben die Tastatur.** ChatGPT öffnen, **neuen** Chat starten.
2. Den Text aus `PROMPT FUER CHATGPT.txt` einmal komplett hineinkopieren und
   abschicken. ChatGPT bestätigt kurz und wartet dann.
3. **Spielen und dabei reden.** Alles, was dir auffällt, als Sprachnachricht in
   genau diesen Chat. Ruhig zehn Minuten am Stück, halbe Sätze, Flüche,
   Gemurmel — der Prompt ist darauf ausgelegt.
4. Wenn du fertig bist: **FERTIG** in den Chat schreiben.
5. ChatGPT baut daraus einen sortierten Bericht. Den **komplett kopieren** und
   uns schicken.

Der Prompt hält ChatGPT dabei an drei Dinge, die uns die Arbeit abnehmen: Es
trennt, was du **gesehen** hast, von dem, was du **vermutest**; es erfindet
nichts dazu; und es schreibt am Ende ausdrücklich hin, wozu **nichts** kam,
statt die Lücke stillschweigend wegzulassen.

Wer lieber tippt, benutzt denselben Prompt mit getippten Nachrichten. Wer gar
kein ChatGPT will, schickt die rohe Sprachnachricht — wir transkribieren hier.

### Screenshots

| | Windows | macOS |
|---|---|---|
| Ganzer Bildschirm | `Win` + `Druck` → `Bilder\Screenshots` | `Cmd`+`Shift`+`3` → Schreibtisch |
| Ausschnitt | `Win`+`Umschalt`+`S` | `Cmd`+`Shift`+`4` |
| Video | `Win`+`Alt`+`R` → `Videos\Captures` | `Cmd`+`Shift`+`5` |

**Sag im selben Moment laut, worum es geht** — „Screenshot: hier hängt die
Bauleiste". Sonst liegen am Ende vierzig Bilder da, und bei der Hälfte weiß
niemand mehr, warum.

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
| **Kanal** | **Eine Adresse: `https://project-nova-pitch.vercel.app/beta`.** Statt jedem einzeln zu schreiben, bekommt jeder Testende diesen Link. Die Seite trägt Projektbeschreibung, Downloads, Installation, bewusste Lücken und den Rückmeldeweg |
| **Sichtbarkeit** | `noindex, nofollow`, nicht vom Investorenpapier verlinkt. Wer die Adresse hat, kommt rein — bewusst so, es sind ein bis zwei bekannte Personen |
| **Downloads** | drei Pakete unter `<kennung>/` im Vercel-Blob-Store `hashkrieg-beta`: macOS-DMG, Windows-ZIP und das kombinierte Paket. Sie liegen **nicht** im Vercel-Deploy — statische Dateien sind dort bei 100 MB gedeckelt |
| **Gebaut mit** | `build-mac.sh` → `build-windows.sh` → `build-testpaket.sh`, siehe [tools/packaging/README.md](../../tools/packaging/README.md) |
| **Kein CLA nötig** | Testende tragen keinen Code bei. Schickt jemand später Code, greift der normale Weg aus [CONTRIBUTING.md](../../CONTRIBUTING.md) |
| **Kein Repo-Zugang** | Ein Tester braucht weder Fork noch Collaborator-Eintrag. Das Zugangsmodell aus [13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md) bleibt unberührt |

**Warum Blob und nicht der VPS:** Auf dem VPS läuft der Relay. Ein 327-MB-Download
mitten in einer Netzpartie ist genau die Wechselwirkung, die wir nicht wollen.
Dasselbe Argument spricht gegen das Supabase-Projekt `hashkrieg-lobby`.

### Nach einem neuen Build

Die Seite hat **eine** Stelle, an der Downloads stehen — den `RELEASE`-Block am
Ende von `beta/index.html` im Projekt `project-nova-pitch`. Dort die Kennung
setzen, dann hochladen und ausrollen:

```bash
vercel env pull .env.local
vercel blob put <datei> --pathname "<kennung>/<datei>" --force true
npx vercel@latest deploy --prod --yes
```

Bleibt eine URL im `RELEASE`-Block leer, zeigt die Seite dort „Link folgt"
statt eines toten Knopfes.

**Alte Stände löschen** — `vercel blob del "<alte-kennung>/<datei>"`. Ein
kompletter Satz sind rund 660 MB; ohne Aufräumen wächst der Store mit jedem
Build um denselben Betrag.

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

Der ChatGPT-Bericht kommt bereits sortiert an. Seine Abschnitte bilden auf
unsere Ablage ab:

| Abschnitt im Bericht | Ziel |
|---|---|
| 3 · Fehler und Merkwürdigkeiten | GitHub-Issue (`Fehler melden`), eine Zeile = ein Issue |
| 1, 2, 4, 5 · Einstieg, Hänger, Gefühl, Erwartung | [GrayboxLog](GrayboxLog.md) als Sitzungseintrag mit Commit |
| 6 · Technik | Issue, wenn reproduzierbar; sonst GrayboxLog |
| 7 · Vermutungen und Wünsche | [ScopeLedger](ScopeLedger.md), nicht als Bug. **Nie ungeprüft in ein Issue** |
| 9 · Wozu nichts kam | Hinweis für die nächste Runde, welche Frage offen blieb |

Abschnitt 7 ist der, an dem Disziplin nötig ist: Wünsche eines Testenden lesen
sich wie Befunde, sind aber Deutung. Genau dafür trennt der Prompt sie.

Ein Befund ohne Commit ist eine Anekdote. Das gilt für Testende genauso wie für
uns.

## Offene Punkte

- Der Windows-Player wurde nie gestartet. Der erste Testende ist gleichzeitig
  der erste Beweis, dass er läuft.
- Ob das Betatest-Programm eine eigene D-ID braucht, ist offen. Es verteilt
  Binaries an Externe, berührt aber weder Verträge noch Simulation.
- Das Rückmeldeverfahren ist ungetestet. Ob eine zehnminütige Sprachnachricht
  in ChatGPT sauber durchläuft und der Prompt hält, was er verspricht, weiß
  erst der erste Durchgang.
- Die Seite kennt keine Zugangskontrolle. Wer die Adresse weitergibt, gibt die
  Downloads weiter. Für zwei bekannte Personen ist das entschieden und in
  Ordnung; für einen größeren Kreis wäre es eine neue Entscheidung.
- Der Blob-Store hat noch keine Aufräumroutine. Solange es einen Stand gibt,
  ist das kein Problem — ab dem dritten wird es eins.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.2.0 | 2026-08-09 | Verteilung auf eine Beta-Seite umgestellt (`/beta` im Investorenpapier-Projekt) statt Einzelversand; Downloads liegen im Vercel-Blob-Store, Begründung gegen VPS und Supabase festgehalten; Ablauf für den nächsten Build und die Aufräumpflicht ergänzt | Producer / Agent (Umsetzung) |
| 1.1.0 | 2026-08-09 | Rückmeldeweg auf das ChatGPT-Verfahren umgestellt (Prompt zuerst, Sprachnachrichten beim Spielen, `FERTIG` löst den Bericht aus), Screenshot-Tastenkürzel für beide Systeme ergänzt, Befund-Ablage auf die Abschnitte des Berichts abgebildet; Verteilung auf das kombinierte Testpaket umgestellt | Producer / Agent (Umsetzung) |
| 1.0.0 | 2026-08-09 | Erstfassung: Anleitung für Testende, Verteil- und Rückmeldeweg, Kadenzregel mit Testenden im Umlauf | Producer / Agent (Umsetzung) |
