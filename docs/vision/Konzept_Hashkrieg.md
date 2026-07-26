# Konzept-Variante B: „Hashkrieg" – Compute-Kriegswirtschaft für Project Nova

**Version:** 0.1.1 | **Status:** Brainstorm – NICHT verbindlich, Konzept-Variante | **Verantwortungsbereich:** Game Director (Brainstorm-Session) | **Sprint:** –

## Zweck

Dieses Dokument hält eine Konzept-Variante aus einer Brainstorming-Session vom
2026-07-25 fest (Samstagnacht-Session Dennis + Claude). Es ist **ausdrücklich
kein verbindliches Design**: Es erzeugt keine Entscheidungen im
[DecisionLog](../production/DecisionLog.md), ändert keine bestehenden Dokumente
und tastet den autorisierten MS-1-Pfad (D-056,
[MVPContentManifest.md](../production/MVPContentManifest.md)) nicht an. Es
dient als Diskussionsgrundlage für das Team – Pitch-Deck siehe Abschnitt
„Begleitmaterial".

## Abhängigkeiten

Dieses Dokument liest die folgenden Quellen und ändert keine davon:

- [GameLoop.md](./GameLoop.md) – Kernloop und Anti-Stall-Tabelle
- [../gamedesign/Economy.md](../gamedesign/Economy.md) – Sammler-Loop, Low-Power-Regel
- [../gamedesign/Resources.md](../gamedesign/Resources.md) – Überernte-Stufenmodell
- [../gamedesign/Biomes.md](../gamedesign/Biomes.md) – Biom-Modifier
- [../gamedesign/ResearchTree.md](../gamedesign/ResearchTree.md) – Forschung Tier 1–3
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-010, D-011,
  D-016, D-023, D-024, D-030, D-056 als unveränderter Bezugsrahmen
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md) –
  autorisierter MS-1-Inhalt, von dieser Variante unberührt

## Elevator Pitch

> In jedem RTS fließen Ressourcen nach innen: Harvester raus, sammeln, heim.
> Hashkrieg dreht die Richtung um. Du erzeugst Strom im Zentrum deiner Basis
> und pumpst ihn nach außen in GPU-Farmen, die Kryptowährung schürfen und
> KI-Modelle trainieren. Mit den Coins kaufst du Waffen; mit dem Compute
> erforschst du Technologie. Jedes Watt ist eine Entscheidung: rechnet es –
> oder schießt es? Und weil die Blockchain öffentlich ist, sieht jeder Spieler
> jederzeit, *was* die anderen verdienen. Nur nicht, *wo* es steht.

## 1. Die Kern-Inversion: Energie fließt nach außen

**Kette:** Kraftwerk (Zentrum) → Akku-Konvois → GPU-Farmen (Peripherie) → Block-Reward.

- **Akku-Trucks statt Harvester:** Der bestehende Sammler-Loop
  ([Economy.md](../gamedesign/Economy.md): 300er-Ladung, ~8 s Laden, ~3 s
  Abladen, 1 Dock, verwundbare Konvois) bleibt mechanisch identisch – nur die
  Logistik-Richtung dreht sich: Energie wird vom Zentrum an die Peripherie
  *geliefert* statt von dort geholt.
- **Kühlung als Standortfrage:** GPU-Farmen brauchen kalte Standorte (Seen,
  Eisfelder, Fallwind-Schluchten). Der bestehende `AetheriumSpreadModifier`
  pro Biom ([Biomes.md](../gamedesign/Biomes.md), −25 % bis +50 %) wird zum
  `CoolingModifier`: Schneekarten sind Mining-Paradiese, Vulkankarten liefern
  Geothermie-Strom im Überfluss bei miserabler Kühlung. Biome erhalten damit
  eine wirtschaftliche Identität, nicht nur eine optische.
- **Kriegsschalter statt Einzel-Akkus:** „Fahrzeuge brauchen Strom" wird nicht
  als Einzelfahrzeug-Akku gebaut (Micromanagement-Risiko), sondern als
  Grid-Umschalter: Mining ↔ Overclock Army. Im Kriegsmodus erhält die Armee
  einen Tempo-/Feuerraten-Buff, das Einkommen fällt auf null – und der Abfall
  ist im öffentlichen Ledger für alle sichtbar (siehe §2).

## 2. Killer-Mechanik: Der Ledger ist öffentlich

Krypto-Ledger sind öffentlich – also ist die gesamte Wirtschaft aller Spieler
öffentlich. **Fog of War gilt für die Karte, volle Transparenz für das Konto.**

- Ein globaler **Hashrate-Ticker** läuft wie ein Börsenband durchs Interface.
  Jeder sieht das Einkommen aller – aber nicht, wo die Farmen stehen.
- **Mindgames:** Fällt die Hashrate eines Gegners plötzlich, heißt das „er
  wird angegriffen" oder „er hat auf Kriegsmodus geschaltet und rollt gleich
  los". Profis spielen Fake-Dips als Finte.
- **Wärme-Signatur:** Große Farmen leuchten auf Thermal-Radar. Wer fett
  Wirtschaft spielt, ist aufklärbar – die bestehende „Sichtdruck"-Zeile der
  Anti-Stall-Tabelle ([GameLoop.md](./GameLoop.md)), thematisch geschärft.
- Designphilosophisch deckungsgleich mit der globalen Superwaffen-Bau-Ansage
  (D-023): große Investitionen sind sichtbar.

## 3. Anteils-Einkommen: Aggression zahlt sich mathematisch aus

Einkommen wird wie echtes Mining modelliert:

```
Einkommen = eigene Hashrate ÷ globale Hashrate × Block-Reward
```

Die Wirtschaft wird zum Nullsummenspiel um Anteile:

1. Die Farm des Gegners zu zerstören **erhöht direkt das eigene Einkommen**,
   weil der eigene Netzwerk-Anteil steigt.
2. Turteln ist selbstbestrafend: Wer nur bunkert und mint, treibt die globale
   Difficulty hoch – auch für sich selbst.
3. Kein Zufall, keine Influx-Events: Alles folgt aus Spielerhandlungen –
   kompatibel mit der expliziten Regel in [GameLoop.md](./GameLoop.md)
   („keine plötzlichen Ressourcen-Influx-Events").

Diese eine Formel ersetzt mehrere Zeilen der bestehenden Anti-Stall-Tabelle.

## 4. Mechanik-Mapping: bestehendes Design → neue Fiktion

| Bestehendes System | Neue Fiktion | Effekt |
|---|---|---|
| Überernte, 4-Stufen-Modell mit permanentem Mutterkristall-Schaden (D-010, [Resources.md](../gamedesign/Resources.md)) | **Overclocking:** GPUs übertakten = kurzfristig +Hashrate, permanenter Thermalschaden (Strapaziert → Ausgeblutet → Durchgebrannt) | Intuitiver – „Übertakten frisst Hardware" versteht jeder sofort |
| Endliche Felder als Match-Taktgeber, Wirtschaftsknick ~24:00 (D-010) | **Halving-Event ~Minute 20:** Block-Reward halbiert sich, Einkommen sinkt natürlich | Authentisch und planbar – der Knick wird zum angekündigten Countdown |
| Lager, 25 %-Verlust bei Zerstörung (D-024) | **Hot Wallet vs. Cold Storage:** Hot = sofort ausgebbar, aber durch Hacker-Einheiten bestehlbar; Cold = sicher, aber Abhebe-Verzögerung; Cold-Storage-Gebäude muss buchstäblich gekühlt werden | Aus passiver Silo-Regel wird aktive Risiko-Entscheidung |
| Low-Power-Regel (D-030, [Economy.md](../gamedesign/Economy.md)) | Bleibt **wörtlich identisch** | Rückt vom Randsystem ins Herz des Spiels |
| Superwaffe, Limit 1, globale Ansage (D-023) | **51 %-Attacke:** >51 % der globalen Hashrate über X Minuten halten → Chain-Reorg, Teil-Entwertung des Gegner-Kontos; globale Warnung „Spieler X nähert sich der Mehrheit" | Wirtschaftliche Superwaffe – King of the Hill auf dem Ledger |
| Forschung Tier 1–3 ([ResearchTree.md](../gamedesign/ResearchTree.md)) | **Modell-Training:** Tech wird trainiert, nicht erforscht – Compute rein, Checkpoints raus | Saubere Zwei-Währungs-Logik (Coins für Einheiten, AI-Token für Tech), Fortführung der D-010-Hybridwirtschaft |
| Sammler-Loop (300er-Ladung, Docks, Queues) | **Akku-Konvois**, Richtung invertiert | Mechanisch unverändert |
| Biom-Modifier (`AetheriumSpreadModifier`, [Biomes.md](../gamedesign/Biomes.md)) | `CoolingModifier` | Biome bekommen Wirtschafts-Identität |
| Neutrale/capturebare Gebäude (D-016) | Verlassene Rechenzentren; Legion kann Neutrale **infizieren** (Botnetz) | Bestehender Mechanik-Unterbau trägt das Fraktions-Gimmick |

## 5. Fraktionen

- **Allianz → Der Hyperscaler.** Big-Tech-Konzern: ASICs statt Consumer-GPUs,
  Flüssigkühlung, teuer und effizient. Kostenfaktor ×1,15 bleibt unverändert.
- **Legion → Das Botnetz.** Schrottplatz-Ökonomie: zusammengelötete
  Gaming-GPUs, Masse statt Klasse (×0,85 passt). Gimmick: neutrale Gebäude
  infizieren – die verlassene Stadt wird zur Zombie-Mining-Farm (D-016 als
  Unterbau).
- **Evolvierte → Die KI selbst.** Die dritte Fraktion mint nicht – **sie ist
  das, wofür alle anderen minen.** Ein emergentes Modell, das sich selbst
  optimiert: Regeneration = Self-Healing Code, Wachstum statt Bau (D-011
  bleibt), stiehlt Inference statt Felder zu ernten. Corp gegen Schrott-Punks
  gegen die entlaufene KI.

## 6. Match-Bogen (D-010-Korridor 20–35 min bleibt)

| Phase | Zeit | Hashkrieg-Zustand |
|---|---|---|
| Eröffnung | 0:00–4:00 | Erstes Kraftwerk als Pflicht-Frühinvestition (heute schon so); erste Farm am nächsten Kühl-Standort |
| Expansion | 4:00–10:00 | Zweite Farm weiter draußen, längere Konvoi-Wege, erste Scharmützel um Konvois |
| Midgame | 10:00–20:00 | Training Tier 2/3 läuft, Overclocking-Entscheidungen, Ticker-Mindgames |
| **Halving** | ~20:00 | Block-Reward halbiert – die Endgame-Uhr (ersetzt Wirtschaftsknick ~24:00) |
| Endgame | 20:00–35:00 | 51 %-Fenster, Elite („Founder-Modell"), Erstickungssieg über Netzwerk-Anteil oder Finalschlag |

## 7. Ton: zwei Richtungen

1. **Satire:** Announcer im Startup-Buzzword-Bingo, „Inference-Silo",
   „Liquidity Pool". Risiko: datiert schnell; Krypto-Ästhetik trägt Stigma.
2. **Post-Boom ernst (Empfehlung):** Nach dem großen KI-Rausch ist Compute die
   einzige harte Währung – „Compute is the new oil". **Aetherium bleibt** als
   einziger Brennstoff mit genug Dichte für die Kraftwerke. Dann ist Hashkrieg
   keine Ersetzung der Lore, sondern ihre Fortsetzung – und altert nicht mit
   der Krypto-Mode, weil das Spiel von Energie und Rechenleistung handelt.

## 8. Reality-Check und Optionen

- **Mechanisch** ist Hashkrieg ein Fiktionstausch plus drei neue Mechaniken
  (öffentlicher Ticker, Anteils-Einkommen/Difficulty, Hot/Cold Wallet). Der
  Kernloop `Sammeln → Bauen → Produzieren → Angreifen → Kontrollieren →
  Gewinnen` bleibt intakt; „Sammeln" wird zu „Erzeugen & Verteilen".
- **Produktionsseitig** wäre ein Pivot jetzt teuer: DecisionLog, MS-1-Override
  (D-056) und Asset-Manifeste sind auf Aetherium kalibriert.

| Option | Inhalt | Bewertung |
|---|---|---|
| A: Voll-Pivot jetzt | Hashkrieg ersetzt das Aetherium-Setting sofort | **Nicht empfohlen** – entwertet laufende MS-1-Arbeit |
| B: Prototyp nach MS-1 | MS-1 wie geplant fertigstellen (validiert die identische Mechanik), danach Hashkrieg als Prototype-Mode-Spike | **Empfehlung** |
| C: Cherry-Picks sofort | Öffentlicher Wirtschafts-Ticker („Markt-Transparenz durch Aetherium-Resonanz") und Anteils-Einkommen als Anti-Stall-Formel ins bestehende Setting übernehmen | Prüfenswert unabhängig von A/B – braucht keine Krypto-Fiktion |

## Begleitmaterial

- Interaktives Pitch-Deck (Artifact, Samstagnacht-Session 2026-07-25) – Link
  in der Session bzw. beim Game Director.
- Quell-Analyse: [GameLoop.md](./GameLoop.md),
  [Economy.md](../gamedesign/Economy.md),
  [Resources.md](../gamedesign/Resources.md) (Stand 2026-07-25).

## Offene Punkte

- Zwei-Währungs-Balance: Verhältnis Coins (Einheiten) zu AI-Token (Tech) –
  ein Konverter-Gebäude („Exchange")?
- 51 %-Attacke: Haltedauer, Wirkhöhe, Counterplay (Sabotage-Fenster analog
  D-023-Rückschlagregel)?
- Difficulty-Kurve: rein anteilsbasiert oder zusätzlich global steigend?
- Kühlung: eigener Sichtbarkeits-Layer (Thermal-Radar) oder Modifier im
  bestehenden Fog-of-War-System?
- Namensfrage: „Hashkrieg" ist Arbeitstitel der Variante, nicht des Spiels.

## Nächste Schritte

Diese Schritte geben ausschließlich den Stand der Session wieder; keiner von
ihnen ist entschieden, und keiner erzeugt eine D-ID.

1. Variante unverändert als Diskussionsgrundlage im Team lesen; MS-1 läuft
   nach D-056 und dem MVP-Inhaltsmanifest unverändert weiter.
2. Über Option A/B/C aus Abschnitt 8 entscheidet der Inhaber – erst eine
   solche Entscheidung erzeugt eine D-ID im
   [DecisionLog](../production/DecisionLog.md).
3. Option C (Cherry-Picks: öffentlicher Wirtschafts-Ticker,
   Anteils-Einkommen) unabhängig von A/B prüfen, weil sie ohne
   Krypto-Fiktion in das bestehende Setting passt.
4. Die offenen Punkte oben bleiben offen, bis eine Entscheidung nach 2. den
   Rahmen dafür schafft.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-25 | Erstfassung aus Brainstorming-Session (Dennis + Claude, Samstagnacht) | Game Director / Claude |
| 0.1.1 | 2026-07-26 | Struktur an den Dokumentationsstandard angeglichen (Pflichtabschnitte Zweck/Abhängigkeiten/Offene Punkte/Nächste Schritte, „Begleitmaterial" vor „Offene Punkte" gezogen); Fachinhalt der Variante unverändert | Technical Writer |
