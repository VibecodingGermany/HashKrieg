# Scribe-Report: Sprint Hauptmenü + Einstellungen

Erstellt: 2026-08-06 · @scribe (Agent unter Inhaber-Delegation) · Branch: `feat/playable-core-loop`
Sprint-Spezifikation: `docs/production/hashkrieg/08_Sprint_Hauptmenue.md`
Schreibumfang: `docs/assets/Licenses.md`, `docs/production/DemoRunbook.md`,
`docs/production/DecisionLog.md`, `CHANGELOG.md`, Statuszeile und §8 von
`docs/production/hashkrieg/08_Sprint_Hauptmenue.md`. Kein Code, keine Tests,
keine weiteren Doku-Dateien angefasst.

---

## 1. Gewählte Entscheidungs-ID: D-083

**Begründung (im Eintrag selbst dokumentiert, damit sie niemand rekonstruieren muss):**

- Letzte im **Dokumentkörper** belegte ID ist **D-077** (`DecisionLog.md`,
  Abschnitt „D-077 | verbindlich | Spielbarer RTS-Core-Loop"). Der
  Änderungsverlauf bestätigt das mit Zeile 1.21.0.
- `docs/production/hashkrieg/00_Entscheidungen.md` reserviert den anschließenden
  Block für die Übertragung der Inhaberentscheidungen. Die dortige
  „Offene Punkte"-Zeile nennt **D-078 bis D-081**, die Stand-Tabelle derselben
  Datei führt aber inzwischen **fünf** Entscheidungen (E-1 bis E-5) — der
  reservierte Bereich ist real **D-078 bis D-082**.
- Erste kollisionsfreie Nummer außerhalb der Reservierung ist damit **D-083**.
  D-078 wäre falsch (reserviert), D-082 riskant (E-5 braucht sie).
- Kostenabwägung: Dieses Protokoll hat eine ID-Kollision bereits einmal teuer
  bezahlt (D-066–D-070 doppelt vergeben, Auflösung steht in „Offene Punkte").
  Eine übersprungene Nummer ist billig, eine Kollision nicht.

**Delegationslage** (Muster von D-074/D-075 übernommen): Punkte 1–4 und 6
(Overlay statt zweiter Szene, UI Toolkit als UI-Stack, `AutoStart = false`,
JSON-Einstellungen ohne `PlayerPrefs`/`AudioMixer`, „Laden" ausgegraut) sind
**vom Agenten unter ausdrücklicher Inhaber-Delegation entschieden und
überstimmbar**. Punkt 5 (Assetherkunft Suno/OpenAI, Schrift Rajdhani/OFL-1.1,
Menütitel „HASHKRIEG") hat der **Inhaber am 2026-08-06 selbst entschieden** und
ist entsprechend gekennzeichnet.

---

## 2. Änderungen je Datei

### `docs/production/DecisionLog.md` — 1.19.0 → 1.22.0

- Neuer Eintrag **D-083 | verbindlich | Hauptmenü als Overlay, UI Toolkit als
  UI-Standard**, eingefügt zwischen D-077 und `## Offene Punkte`. Felder nach
  dem D-077-Vorbild: Status / Nummernwahl / Kontext / Entscheidung (6 Punkte) /
  Verworfen (a–e) / Konsequenzen. Kein Feld „Alternativen" (≥3-Pflicht ruht in
  Tier 1, D-076) und kein „Sprint N"-Segment in der Überschrift.
- „Offene Punkte" um zwei Bullets ergänzt: D-083 in der Delegationslage samt
  fehlender `PROVENANCE.json`-Datensätze; Reservierung D-078–D-082 für E-1..E-5
  mit Hinweis auf die zu kurze und veraltete Zeile in `00_Entscheidungen.md`.
- Kopfversion von **1.19.0 auf 1.22.0** gezogen — der Kopf hing zwei Minor
  hinter der eigenen Verlaufstabelle (1.20.0, 1.21.0); der Rückstand ist mit
  diesem Bump aufgeholt statt fortgeschrieben.
- Neue Verlaufszeile 1.22.0 am Ende (Tabelle ist aufsteigend sortiert).

### `docs/assets/Licenses.md` — 1.3.0 → 1.4.0 (die eigentliche Sperre)

Ohne diese Datei wäre der Import der drei Assets nach der **eigenen** Projektregel
(§2 Regel 6, Default-Deny) unzulässig gewesen.

- **§1, neue Zeile „SIL Open Font License 1.1 (Schriften, z. B. Rajdhani)"** —
  direkt hinter die CC0-Zeile gesetzt, damit der offene Block zusammenbleibt und
  der KI-Block (Hunyuan3D/OpenAI) nicht zerschnitten wird. Nennt beide Auflagen:
  Lizenztext muss beiliegen, Verkauf der Schrift für sich allein untersagt,
  „Reserved Font Name" nicht für Ableitungen.
- **§1, neue Zeile „Suno (Bezahltarif)"** — hinter die OpenAI-Image-API-Zeile,
  vor die Audio-Zeile (Sonniss). Lizenzspalte sagt ausdrücklich: *„nach Auskunft
  des Inhabers zu seinem Tarif, ohne eigene AGB-Prüfung"*.
- **Neue Fußnote `[^3]`** nach dem Hausmuster von `[^1]`/`[^2]`: Datum,
  Entscheidungsträger, welcher Regelkonflikt aufgelöst wird, fehlender
  Recherchebeleg, Abschluss „Kein Rechtsrat – bei Zweifel im Einzelfall
  menschliche Entscheidung einholen."
- **§2 Regel 5** um die benannte, zweckgebundene **Suno-Ausnahme** ergänzt
  (kein Präzedenzrecht, jede weitere bezahlte Quelle braucht eine eigene
  Ausnahme). **Ohne diesen Schritt widerspräche das Dokument sich selbst** —
  Regel 3 und 5 verbieten den Bezahltarif wörtlich.
- **§2 Regel 6** (Whitelist) um Suno und OFL-1.1-Schriften erweitert.
- **§3 Ledger:** drei Zeilen mit Datum 2026-08-06 und **konkreten Repo-Pfaden**
  (`Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg`,
  `Assets/_Project/UI/UI_KeyArt_MainMenu.jpg`,
  `Assets/_Project/UI/Fonts/Rajdhani-{Regular,Bold}.ttf`), damit jede Zeile
  prüfbar ist.
- **„Offene Punkte":** die bestehende `CREDITS.md`-Zeile bleibt unverändert
  (weiterhin korrekt), dazu drei neue Bullets — die **begründete Entscheidung,
  dass dieser Import keine `CREDITS.md` auslöst**, die OFL-Beilagepflicht und
  die fehlenden `PROVENANCE.json`-Datensätze.
- Kopfversion 1.4.0; Verlaufszeile **angehängt**, nicht oben eingefügt: die
  Tabelle ist unsortiert (1.3.0 vor 1.0.0–1.2.0), Anhängen ist die einzige
  Variante, die keine bestehende Zeile bewegt und keine dritte Sortierlogik
  erzeugt. Die Anomalie wurde bewusst **nicht** nebenbei „repariert".

**CREDITS.md-Prüfung, Ergebnis: fällt nicht an.** §2 Regel 2 bindet die
Attributionspflicht an CC-BY. Suno und OpenAI verlangen keine Namensnennung;
OFL-1.1 verlangt die Mitlieferung des Lizenztexts, was etwas anderes ist als
Attribution; der Rajdhani-Copyright-Header (`OFL.txt`, Zeile 1) nennt **keinen**
„Reserved Font Name", die Umbenennungsklausel greift also ebenfalls nicht.
Entschieden und hingeschrieben statt offen gelassen.

### `docs/production/DemoRunbook.md` — 0.3.0 → 0.4.0

- **Abhängigkeiten:** DecisionLog-Zeile um D-083 erweitert, Verweis auf
  `hashkrieg/08_Sprint_Hauptmenue.md` aufgenommen (relativer Link, von
  `docs-checks` geprüft).
- **§1:** die einzige Auto-Start-Behauptung des Dokuments ersetzt. Neu: Play
  zeigt das Hauptmenü mit Musik, `AutoStart` steht im Generator auf `false`,
  „Neues Spiel" ruft das idempotente `StartGrayboxMatch()`, „Beenden" verlässt
  das Spiel (im Editor den Play-Modus). Dazu ein Hinweis auf den
  `AudioListener` an der Kamera und den häufigsten Stille-Grund.
- **§2:** Überschrift auf „(Spielstand GB-005, D-077 + Hauptmenü, D-083)"
  gezogen; neuer **erster** Aufzählungspunkt zum Menü (vier Einträge, „Laden"
  ausgegraut, Einstellungen überleben den Neustart, erstes UI-Toolkit-UI).
- **§4:** Einleitungssatz, dass die Zeitmarken **ab Matchstart** zählen; neuer
  Schritt 1 „Menü (vor 0:00)"; der bisherige Basis-Blick ist Schritt 2; die
  Schritte 3–8 durchnummeriert. Schlussschritt um „kein Rückweg ins Menü"
  ergänzt.
- **§5 „Bekannte Grenzen":** fünf neue ehrliche Zeilen vor dem Historien-Bullet
  — „Laden" ohne Funktion, wirkungsloser SFX-Regler, gemeinsames URP-Asset über
  alle sechs Render-Detail-Stufen, kein Pause-Menü/Restart/Fraktions- und
  Kartenwahl/Tastenbelegung; die bestehende Zeile zum fehlenden
  Ergebnisbildschirm um „und keinen Rückweg ins Hauptmenü" ergänzt. Der
  Historien-Bullet blieb unangetastet.
- **Neues §7 „Einstellungen – was gespeichert wird und wie man es zurücksetzt":**
  Pfad `<Application.persistentDataPath>/settings.json` mit den
  plattformabhängigen Ausprägungen, Inhalt, Anwendungszeitpunkt
  (`RuntimeInitializeOnLoadMethod(BeforeSceneLoad)`), **Zurücksetzen durch
  Löschen der Datei**, Verhalten bei kaputter oder nicht schreibbarer Datei,
  Hinweis auf Vorgabelautstärke 0,4 (Spur mit −11,8 LUFS laut gemastert).
  Als **§7 hinter §6 angehängt**, nicht eingeschoben — jede Einschiebung hätte
  die im Dokument und potenziell außerhalb referenzierten Nummern §4/§5/§6
  verschoben.
- **§3 Steuerungstabelle unverändert** — der Sprint hat weder Pause-Menü noch
  ESC-Binding im Umfang; keine erfundene Taste.
- Kopfversion 0.4.0, Verlaufszeile angehängt (Tabelle ist aufsteigend).

### `CHANGELOG.md` — ein Eintrag unter `[Unreleased]`

- Eingefügt **nach** dem Blockquote „Dokumentationsstand 0.12.0
  (unveröffentlicht)" und **vor** dem D-077-Block; der Blockquote ist
  unangetastet, es wurde **keine datierte Version** erzeugt und **`VERSION`
  nicht angefasst**.
- Eigener PR-Block mit `### Hinzugefügt` und `### Geändert`; bestehende
  gleichnamige Überschriften wurden **nicht** zusammengeführt (die Datei trägt
  sie mehrfach, einer pro PR-Block).
- `Hinzugefügt`: ein Top-Level-Lemma „Hauptmenü, Menümusik und Einstellungen
  (D-083)" mit sechs Unterpunkten — vier Menüeinträge und Overlay-Architektur,
  „Laden" ausgegraut samt Begründung, Einstellungen mit Persistenz (kein
  `PlayerPrefs`, kein `AudioMixer`), **ehrlicher Unterpunkt zu wirkungslosem
  SFX-Regler und geteiltem URP-Asset**, UI Toolkit als neuer Standard, Assets
  und Lizenzfreigabe.
- `Geändert`: `AutoStart = false` als eigenständiger, ausdrücklich benannter
  Verhaltenswechsel, mit Folge für Demo-Ablauf und PlayMode-Tests.
- Formattreue zum D-077-Block: Fett-Lemma mit D-ID, 2-Leerzeichen-Fortsetzung
  auf Top-Level, 4 auf Unterpunkten, ~78 Zeichen Zeilenbreite, Codebezeichner
  in Backticks.
- **Kein `### Verifikation`-Abschnitt** — es liegen mir keine Testergebnisse für
  diesen Sprint vor, und erfundene Zahlen wären genau der Fehler, den dieses
  Changelog sonst vermeidet. Wer die Zahlen hat, hängt den Abschnitt nach dem
  Vorbild „Verifikation (GB-005)" an.

### `docs/production/hashkrieg/08_Sprint_Hauptmenue.md` — Statuszeile und §8

- Statuszeile: `vorbereitet, nicht begonnen` → **`umgesetzt (2026-08-06)`**,
  dazu Verweis auf D-083, den geklärten Assetstand und die nachgezogene Doku
  (Licenses 1.4.0, DemoRunbook 0.4.0, CHANGELOG `[Unreleased]`).
- §8 von „Offene Punkte — brauchen eine Entscheidung des Owners" zu
  „Ehemals offene Punkte — vom Inhaber entschieden (2026-08-06)". Alle drei
  Fragen **stehen weiter da** (kursiv, als Frage erkennbar), darunter jeweils
  die Entscheidung: Herkunft (Suno-Bezahltarif / OpenAI gpt-image-1, inkl.
  Restpunkt `PROVENANCE.json`), Schrift (Rajdhani, OFL-1.1, keine
  `CREDITS.md`-Folge), Titel („HASHKRIEG" als Vollzug von E-3, Code-Identität
  `Nova.*` bleibt).

---

## 3. Prüfung

- `python3 .github/scripts/check_docs.py` → **OK: 151 Markdown-Dateien und 5
  Quality-JSONs geprüft.** Keine toten internen Links, UTF-8 und JSON-Parsing
  sauber. Der einzige `::notice::` (fehlende Status-Kopfzeile in
  `08_Sprint_Hauptmenue.md`) ist **vorbestehend** und laut
  `DocumentationStandard` 2.0.0 freiwillig — vor der Bearbeitung mit
  identischem Wortlaut vorhanden, also nicht durch diese Änderung entstanden.
- Alle neuen relativen Links gegen ihren Speicherort geprüft (DemoRunbook →
  `hashkrieg/08_Sprint_Hauptmenue.md`; DecisionLog → `hashkrieg/00_Entscheidungen.md`,
  `../assets/Licenses.md`, `../assets/Provenance.md`; Sprintdatei →
  `../DecisionLog.md`, `00_Entscheidungen.md`; CHANGELOG → `docs/...` ab
  Repo-Wurzel).

---

## 4. Nachzieharbeiten (außerhalb dieses Schreibumfangs — nicht stillschweigend erledigt)

1. **`PROVENANCE.json` für drei Assets fehlt.** `docs/assets/Provenance.md`
   verlangt vor der Repo-Aufnahme je Asset einen Sidecar plus Eintrag in
   `docs/assets/provenance-ledger.json` — ausdrücklich auch für **Audio und
   Fonts**. Key Art, Menümusik und die beiden Rajdhani-TTFs liegen ohne diese
   Datensätze im Arbeitsbaum. Die Ledger-Zeilen in `Licenses.md` §3 decken die
   Lizenzlage, **nicht** den Herkunftsnachweis je Datei. Pflichtfelder bei den
   KI-Quellen: `promptText`, `providerTermsUrl`, `providerTermsRetrievedAt`,
   wörtliches `outputOwnership`-Zitat — die kennt nur der Inhaber.
2. **`docs/production/hashkrieg/00_Entscheidungen.md` ist an zwei Stellen
   veraltet** (Datei lag außerhalb meines Schreibumfangs): „Offene Punkte" nennt
   den zu kurzen Bereich „D-078 bis D-081" (real D-078 bis D-082, weil E-5
   dazugekommen ist), und die Begründung im Zweck-Abschnitt („D-077 ist im
   DecisionLog noch nicht eingetragen") stimmt seit Commit `6f03280` nicht mehr.
3. **`Provenance.md` verweist zweimal auf ein „Licenses.md §4"**, das es nicht
   gibt (Licenses hat §1–§3 plus „Offene Punkte"). Der Verweis war bereits vor
   dieser Änderung falsch; ich habe **bewusst keinen neuen Abschnitt** in
   `Licenses.md` angelegt, damit der falsche Verweis nicht plötzlich auf etwas
   Unbeabsichtigtes zeigt.
4. **`Assets/Tests/PlayMode/GrayboxDemoProofTests.cs`** muss das Match explizit
   über `StartGrayboxMatch()` starten — mit `AutoStart = false` reißen die
   15-Sekunden-Assertions auf `IsMatchReady`. In D-083 als Konsequenz
   protokolliert; die Umsetzung gehört in die Code-Spur.
5. **Verifikationszahlen im CHANGELOG nachtragen**, sobald sie vorliegen (eigener
   Abschnitt nach dem Vorbild „Verifikation (GB-005)").

---

## 5. Vorbehalt zum Stand der Umsetzung

Zum Zeitpunkt dieses Reports lagen im Arbeitsbaum die **Assets**
(`Assets/_Project/UI/UI_KeyArt_MainMenu.jpg`,
`Assets/_Project/Audio/Music/MUS_MainMenu_Hashkrieg.ogg`, die Rajdhani-Fonts)
und `Assets/_Project/Scripts/Presentation/UI/GameSettings.cs` vor. **Nicht**
vorhanden waren das Menü-Overlay selbst, der `AudioListener`/`AutoStart = false`
im `BootstrapSceneGenerator` und ein `Application.Quit`-Aufruf. Die Doku
beschreibt damit den **Sprint-Sollstand**, den die parallel laufende Code-Spur
herstellt. Landet die Code-Spur nicht oder anders, sind vor allem
`DemoRunbook.md` §1/§2/§4 und der CHANGELOG-Eintrag entsprechend
nachzuziehen — das ist der einzige Punkt, an dem diese Doku eine Zusage macht,
die sie nicht selbst einlösen kann.
