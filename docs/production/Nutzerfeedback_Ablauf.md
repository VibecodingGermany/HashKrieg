# Ablauf für Nutzerfeedback aus der Datenbank

**Version:** 1.0.0 | **Status:** verbindlich ab 2026-08-09 | **Gilt für:** jeden Testbericht aus `public.testberichte` (Supabase `hashkrieg-lobby`) | **Leitsatz:** der Befund gehört ins Repository, die Person nicht

## Die Grundregel

**Kein personenbeziehbares Datum verlässt jemals die Datenbank.**

Das Repository ist öffentlich. GitHub-Issues sind öffentlich. Ein Testbericht
kommt von einer realen Person, die sich mit ihrem Namen angemeldet hat und
dabei über Fehler in einem Spiel geschrieben hat — nicht darüber, öffentlich
genannt zu werden. Zwischen „der Befund ist wichtig" und „der Name darf
mitgehen" besteht kein Zusammenhang.

Diese Regel gilt **auch dann**, wenn die betroffene Person der Projektinhaber
selbst ist. Wer bei sich selbst eine Ausnahme macht, baut den Ablauf, der bei
allen anderen versagt.

## Was personenbeziehbar ist

Die Tabelle `public.testberichte` führt diese Felder. Die rechte Spalte
entscheidet, ob das Feld das Repository je erreicht:

| Feld | Darf ins Repo? |
|---|---|
| `bericht` | **ja** — der Befundtext, unverändert |
| `build` | **ja** — ohne ihn ist ein Befund nicht zuordenbar |
| `id` | **ja** — die Supabase-UUID ist der Rückweg zum Original |
| `eingegangen_am` | **ja**, aber nur als Datum, nicht als Zeitstempel |
| `plattform` | **ja**, aber nur grob (`macOS`, `Windows`) — keine Versionen |
| `tester` | **nein** — Klarname |
| `ip` | **nein** — personenbezogenes Datum, ausdrücklich so kommentiert |
| `user_agent` | **nein** — Fingerabdruck |
| `browser_kennung` | **nein** — Wiedererkennung desselben Browsers |
| `tester_token` | **nein** — Kennung aus dem persönlichen Einladungslink |
| `sprache`, `zeitzone`, `bildschirm` | **nein** — zusammen ein Fingerabdruck |

Auflösung, Sprache und Zeitzone wirken harmlos. In Kombination mit Plattform
und Zeitpunkt sind sie es nicht — bei einem kleinen Testerkreis genügen sie,
um eine Person zu isolieren. Sie bleiben deshalb draußen, auch wenn sie
technisch nützlich wären. Wer eine dieser Angaben für einen Befund wirklich
braucht, schreibt sie in das betreffende Issue hinein, nicht in den Bericht.

### Ausdrücklich nicht von dieser Regel erfasst

Namen von **Inhaber und Maintainern** in Governance-Dokumenten
(`DecisionLog.md`, `13-15_Parallelbetrieb.md`, CLA, Branch-Protection). Dort
steht der Name als **Entscheider oder Rechteinhaber**, nicht als
Feedbackgeber. Diese Nennungen sind gewollt und bleiben. Die Regel schützt
Testende, sie führt keine allgemeine Namenslöschung ein.

## Die Kennungen

Jeder Testende bekommt eine stabile Kennung `T-01`, `T-02`, … — stabil
bedeutet: **dieselbe Person behält über alle Berichte hinweg dieselbe
Kennung**. Nur so lässt sich später sagen „T-03 meldet das zum dritten Mal",
ohne zu wissen, wer T-03 ist.

Die Zuordnung steht in `public.testberichte.tester_pseudonym` und **nirgendwo
sonst**. Sie wird nicht ins Repository gespiegelt, nicht in ein Issue
geschrieben und nicht in einen Commit.

Vergabe beim ersten Bericht einer Person:

```sql
-- naechste freie Kennung ermitteln, dann setzen
select coalesce(max(substring(tester_pseudonym from 3)::int), 0) + 1
from public.testberichte where tester_pseudonym is not null;
```

## Der Ablauf in sechs Schritten

### 1 · Abholen

Offene Berichte aus der Datenbank ziehen:

```sql
select id, eingegangen_am, tester, build, plattform, status
from public.testberichte
where status = 'neu'
order by eingegangen_am;
```

Das Original bleibt in Supabase. Es wird **nicht** unverändert ins Repository
kopiert — auch nicht „vorläufig", auch nicht in einen ignorierten Ordner.

### 2 · Anonymisieren

Die Fassung fürs Repository entsteht als **neue Datei**, nicht als Bearbeitung
des Originals:

- Ablage: `docs/production/hashkrieg/Testberichte/`
- Dateiname: `JJJJ-MM-TT_<build>_<kennung>.md` — **nie** ein Name im Dateinamen
- Kopf: nur die Felder aus der Ja-Spalte oben
- Befundtext: **unverändert**. Nicht kürzen, nicht glätten, nicht in
  Hochdeutsch übersetzen. Der Ton eines Testers ist ein Befund für sich
- Wenn im Befundtext selbst ein Name, ein Ort oder ein Nutzerkonto auftaucht:
  durch `[entfernt]` ersetzen und den Eingriff im Kopf vermerken

Danach die Gegenprobe — sie ist Pflicht, nicht Kür:

```bash
grep -rniE "<klarname>|<mailadresse>|<token>" docs/
```

### 3 · Zerlegen

**Ein Befund, ein Issue.** Ein Bericht wird nie als Ganzes zu einem Issue —
genau dafür existiert die Tabelle.

Vor dem Anlegen wird der Befund **gegen den aktuellen Code geprüft**. Ein
Issue, das die Ursache benennt und die Datei nennt, ist bearbeitbar; eines,
das nur die Beobachtung wiederholt, verschiebt die Arbeit nur.

In jedem Issue:

- Verweis auf die **anonymisierte** Datei, nie auf den Namen
- Zitate aus dem Bericht sind erlaubt und erwünscht — sie tragen die Dringlichkeit
- Build-Kennung, damit der Befund zuordenbar bleibt

### 4 · Abgleichen, bevor etwas angelegt wird

**Zuerst die bestehenden Issues lesen** — offene *und* geschlossene:

```bash
gh issue list --state all --limit 100
```

Drei Fälle, drei Reaktionen:

| Fall | Reaktion |
|---|---|
| Der Befund existiert schon als Issue | Kein neues Issue. Den Bericht als Kommentar anhängen |
| Das bestehende Issue ist längst erledigt | Prüfen, schließen mit Begründung und Belegstelle (Commit, Datei) |
| Der Befund ähnelt einem bestehenden, ist aber ein anderer Fehler | Neues Issue, und den Unterschied **im Issue benennen** |

Der dritte Fall ist der gefährliche. Beispiel aus dem ersten Durchlauf: #20
(„Kreislauf bricht nach der Rückkehr ab") war behoben, der neue Befund
(„Kreislauf startet nie") sah gleich aus und war ein anderer Fehler. Wer hier
zusammenlegt, verliert einen Fehler.

### 5 · Gruppieren und Sprints **vorschlagen**

Die Issues werden zu einem Vorschlag gebündelt — als eigenes Dokument unter
`docs/production/hashkrieg/`, mit dem Status **„Vorschlag zur Sprintbildung"**.

Der Schnitt läuft über die **Schreibhoheit**, nicht über das Thema. Ein
thematisch schlüssiges Paket, das die Grenze zwischen zwei Strängen
überschreitet, hebt die Trennung auf, die den Parallelbetrieb möglich macht
(siehe [hashkrieg/13-15_Parallelbetrieb.md](hashkrieg/13-15_Parallelbetrieb.md)).
Befunde, die auf der Naht liegen, werden als **Vertragsfläche** ausgewiesen,
statt still einem Strang zugeschlagen zu werden.

Was ein Vorschlag enthalten muss:

- die Einordnung jedes Issues mit Hoheit und Abhängigkeit
- die Reihenfolge **mit Begründung**, wo eine Abhängigkeit besteht
- alles, was eine **Inhaberentscheidung** braucht, ausdrücklich als solche

### Was hier ausdrücklich **nicht** passiert

**Kein Sprint wird festgeplant.** Keine Sprintdatei wird angelegt, keine
Nummer vergeben, kein Status auf `in-progress` gesetzt, keine bestehende
Sprintdatei um Pakete erweitert.

Ob aus einem Vorschlag ein Sprint wird, ob mehrere zusammengelegt werden oder
ob etwas ganz entfällt, **entscheidet der Inhaber**. Der Vorschlag ist eine
Vorlage zur Entscheidung, kein Plan im Wartezustand.

### 6 · Zurückschreiben

Erst wenn die Issues stehen:

```sql
update public.testberichte
set status = 'zerlegt',
    issues_erstellt_am = now(),
    notiz = '<Issue-Nummern und Ablageort der anonymisierten Fassung>'
where id = '<uuid>';
```

Statuswerte: `neu` → `gesichtet` (gelesen) → `zerlegt` (Issues erstellt), oder
`verworfen` (kein Handlungsbedarf, mit Begründung in `notiz`).

## Wenn doch etwas nach draußen gelangt ist

1. **Umfang feststellen, bevor irgendetwas geändert wird.** Issue-Bodies,
   Kommentare, Titel, Commits, Branches, Dateinamen. Ein Dateiname reist als
   Quellenangabe in jedes Issue mit — das ist der Weg, den der erste
   Durchlauf genommen hat
2. **Ist es gepusht?** Ein nicht gepushter Branch wird umgeschrieben. Ein
   gepushter braucht eine eigene Entscheidung
3. **Öffentliches bereinigen** — Issue-Bodies und Kommentare bearbeiten
4. **Ehrlich benennen, was bleibt:** GitHub führt für bearbeitete Issues und
   Kommentare einen **Bearbeitungsverlauf**, den jeder Leser aufrufen kann.
   Eine Bearbeitung entfernt den Text aus der aktuellen Fassung, nicht aus der
   Historie. Vollständige Entfernung heißt: Issue löschen und neu anlegen —
   und kostet Nummer und Querverweise
5. **Gegenprobe** über alle betroffenen Nummern, nicht nur die geänderten

## Warum es diese Regel gibt

Beim ersten Durchlauf am 09.08.2026 wanderte der Klarname des Testers in den
Dateinamen der Repo-Fassung — und von dort als Quellenangabe in alle 16
angelegten Issues eines **öffentlichen** Repositories. Das Repo selbst war
nicht gepusht, die Issues waren sofort öffentlich.

Der Fehler lag nicht im Bericht und nicht in der Datenbank. Beide waren
richtig gebaut: die Tabelle kommentiert `ip` ausdrücklich als
personenbezogenes Datum, und der Lesezugriff ist auf `service_role`
beschränkt. Der Fehler lag im Schritt dazwischen, den niemand definiert
hatte — und genau den definiert dieses Dokument.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-08-09 | Ablauf nach dem Namensvorfall im ersten Durchlauf festgelegt | Orchestrator / Inhaberauftrag |
