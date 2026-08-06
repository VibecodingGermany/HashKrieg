# Sprint: Gefecht und Rundenrahmen

**Status:** vorgeschlagen, nicht begonnen | **Vorgänger:** [10_Sprint_Baubarkeit_und_Kartenbild.md](10_Sprint_Baubarkeit_und_Kartenbild.md) (umgesetzt), davor HUD-Sprint (D-084) und Hauptmenü-Sprint (D-083) | **Leitsatz:** aus der Demo wird eine Runde

## 1. Wo wir stehen

Beide Vorgängersprints sind inhaltlich da. Vom HUD-Sprint sind sieben der neun Punkte vollständig, zwei teilweise. Das Hauptmenü ist seit dem Generatorlauf vollständig verdrahtet: `AutoStart: 0`, MainMenu-Objekt mit Controller, Musik und AudioSource, AudioListener an der Kamera, PanelSettings vorhanden.

Die reale Spielschleife heute: Play → Menü → Neues Spiel → HQ, Builder, 3.000 AE → Raffinerie über die Bauleiste setzen → Harvester produzieren → Kraftwerk, Kaserne, Fahrzeugfabrik → Truppen bauen → zur Gegnerbasis schicken → **jeden Gegner einzeln anklicken, bis das feindliche HQ fällt** → „VICTORY" erscheint als Wort in der Statuszeile, die Simulation tickt weiter, und der einzige Ausweg ist das Beenden der Anwendung.

Genau diese beiden hervorgehobenen Stellen sind der Unterschied zwischen einer Demo, die man einmal vorführt, und einem Spiel, das man zweimal spielt. Sie sind der Inhalt dieses Sprints.

## 2. Voraussetzung — kein Sprintinhalt, aber Blocker

> **Stand 2026-08-06 abends:** Der HUD- und der Hauptmenü-Sprint sind committet (`706a394`, `6f03280`), Sprint 10 ist umgesetzt und wird gerade committet. Der Absatz unten bleibt als **Dauerregel** stehen: diese Dateien gehören in keinen Commit dieser Sprintreihe.

Der Arbeitsbaum wird vor jedem neuen Feature aufgeräumt. Drei Änderungen dürfen dabei nie mitkommen:

| Datei | Warum raus |
|---|---|
| `Assets/_Project/Data/Registries/AssetMappingRegistry.asset` | +72 Zeilen GUID-Verweise auf **gitignorierte** Prefabs. In jedem frischen Clone zeigen sie ins Leere. Der Inhaber hat ausdrücklich entschieden, dass die Datei leer im Repo bleibt. |
| `Packages/manifest.json` + `packages-lock.json` | Zwei Unity-AI-Editor-Pakete, eines davon Prerelease. Gehören in keinen der beiden Sprints und würden für jeden Klon und jeden CI-Lauf verbindlich. |
| `Assets/DefaultVolumeProfile.asset` | 785 Zeilen reine Editor-Re-Serialisierung, kein einziger Effekt aktiviert. Diff-Rauschen, das jedes künftige Blame verdeckt. |

Zwei Änderungen sind dagegen echt und gehören dokumentiert statt zurückgenommen: `QualitySettings antiAliasing 2 → 4` zusammen mit `NovaUrp m_MSAA 1 → 4` (gleichgerichtet, MSAA wird angehoben) und `GraphicsSettings m_LightsUseColorTemperature 0 → 1`.

## 2.1 Blocker aus der zweiten Spielsitzung — der Loop ist noch nicht geschlossen

**Stand 2026-08-06 abends, nach Sprint 10.** Bauen funktioniert, die Kaserne steht. Zwei Stellen weiter reißt die Kette trotzdem: **aus der Kaserne kommen keine Soldaten, und der Harvester erntet nicht.** Damit ist der Kernloop — Rohstoff ernten, Einheiten bauen, kämpfen — nach wie vor offen, und der Rest dieses Sprints lässt sich gar nicht erst spielen. Diese beiden Punkte gehen deshalb **vor** allem anderen.

### Der Harvester — bewiesene Ursache, gleicher Fehler wie beim Builder

`EconomySystem.ExecuteHarvestOrder` bricht ab, solange der Harvester nicht am Feld steht:

```
EconomySystem.cs — if (!IsInReach(in unit, field.GridPos)) return; // held, not dropped
```

Dasselbe gilt für die Rückfahrt: `ExecuteReturnOrder` zahlt nur aus, wenn der Harvester in Reichweite einer eigenen Raffinerie steht. Der Ernteauftrag wird also angenommen, das Fahrzeug bleibt stehen, und es passiert nie etwas.

**Und wieder hat die KI genau die Verdrahtung, die dem Spieler fehlt.** `SkirmishAiSystem` Abschnitt (4), im Kommentar wörtlich: *„send every idle own harvester to the own field and **WALK harvesters into reach with explicit Move intents**"* — „micro the harvesters into the economy's reach rule". Der Gegner erntet, der Spieler nicht.

**Lösung, nach dem Präzedenzfall D-085:** ein Harvester-Dispatch auf der Client-Seite, der beide Beine des Kreislaufs fährt — zum Feld, wenn ein Ernteauftrag steht und das Fahrzeug außer Reichweite ist; zur nächsten eigenen Raffinerie, sobald `IsReturningCargo` gesetzt ist. Move-Befehle über den normalen Command-Pfad, keine Regeländerung, kein Hash-Bruch. Die Automatik im Kreislauf selbst existiert bereits: die Simulation behält die `HarvestFieldId` über die Rückfahrt hinweg, es fehlt ausschließlich die Fahrt. Die Alternative — Harvester in der Simulation selbst fahren lassen — wäre eine Verhaltensänderung mit neuen Baselines und wird bewusst nicht gewählt.

### Die Kaserne — Ursache offen, Diagnose zuerst

Hier ist **keine** Ursache bewiesen, und geraten wird nicht. Der Befund: Der Wartebalken der Command Card läuft (er liest den echten Zustand über `ProductionSystem.TryGetProducer`, ist also kein Schein), aber es erscheint keine Einheit. Die Simulationstests decken genau diesen Pfad ab und sind grün — `ProductionSystemTests.Production_SpawnsAtDefaultRally_AfterExactBuildTicks` beweist, dass eine Kaserne bei Standard-Rally nach exakt `BuildTicks` spawnt. Der Defekt liegt also woanders als in der reinen Spawn-Logik.

**Diagnose in dieser Reihenfolge — das F3-Panel beantwortet die erste Frage sofort:**

1. **Steigt die Einheitenzahl im F3-Panel** (`Forces: slot 0 Nu/Nb`), wenn der Balken durchläuft? Das trennt Simulation und Darstellung in einem Blick.
2. **Ja, sie steigt** → die Einheit existiert und ist nur nicht zu sehen. Dann liegt es an der Darstellung: `UnitViewManager.ResolveViewPrefab` löst lokal auf die 34 Art-Prefabs aus der `AssetMappingRegistry` auf — ein fehlendes, falsch skaliertes oder unter den Boden gesetztes Infanterie-Prefab sieht exakt so aus wie „spawnt nicht". Gegenprobe: Registry-Eintrag der Infanterierolle leeren, dann muss das Graybox-Primitiv erscheinen. **Kein Test deckt diesen Pfad ab**, weil Tests keine Prefabs laden.
3. **Nein, sie steigt nicht** → einer der beiden **stillen Pausenpfade** in `ProductionSystem.ExecuteTick` greift: Entity-Store voll, oder `TryFindSpawnCell` findet in acht Ringen keine freie Zelle. Beide setzen den Fortschritt zurück auf die Schwelle und schweigen — der Balken steht dann voll und nichts passiert. Beide Kapazitäten sprechen dagegen (1024 Entities, Ring 8), aber der Pfad ist da und muss geprüft werden.

**Unabhängig vom Ergebnis:** beide stillen Pausen bekommen eine Rückmeldung, genau wie die Baustelle in Sprint 10 eine bekommen hat. Eine Produktion, die aus einem dieser Gründe hängt, muss das auf der Karte sagen — „kein Platz zum Ausrücken" ist eine Nachricht, kein Schweigen.

**Verbindlich:** Erst ein Test, der den Defekt reproduziert, dann die Behebung. Wenn der Defekt in der Darstellung liegt, ist es ein PlayMode-Test; liegt er in der Simulation, ein EditMode-/SimRunner-Test.

## 3. Drei Eingabedefekte — zuerst, weil sie das neue HUD vergiften

1. **Rechtsklick kennt die HUD-Sperre nicht.** `IsPointerOverHud` wird an drei Stellen geprüft (`RtsDeviceInput.cs:493`, `:525`, `:739`), aber der Rechtsklick-Zweig bei `:562` prüft sie nicht. Wer mit selektierter Armee auf die Bauleiste, die Minimap oder die Command Card rechtsklickt, schickt seine Truppen an den Punkt dahinter. Genau die Sorte „das Spiel macht etwas, das ich nicht wollte", gegen die der ganze HUD-Sprint angetreten ist.

2. **Roter Baugeist platziert trotzdem.** Der Klick prüft `_placementHasCell`, nicht `_placementValid` (`RtsDeviceInput.cs:493-499`). Zusätzlich klemmt `ToGridCoordinate` negative Footprint-Ursprünge auf 0 — am linken und unteren Kartenrand entsteht das Gebäude also an einer anderen Stelle als der Geist zeigte.

3. **Command Card wird unten abgeschnitten.** `EstimateHeight` rechnet die GUILayout-Margins und das Panel-Padding nicht mit (~40 px bei einer Fahrzeugfabrik mit vier produzierbaren Einheiten). Die unteren Zeilen samt Abbruch-Buttons liegen außerhalb der `BeginArea` und sind nicht mehr klickbar.

Dazu zwei kleinere aus derselben Familie: die Rally-Point-Geste kapert den Rechtsklick, sobald bei einer Rahmenauswahl ein Gebäude mit drin ist (das HQ hat spawnbedingt immer den niedrigsten Index und damit `selected[0]`) — Rally sollte nur greifen, wenn die Selektion **ausschließlich** Gebäude enthält. Und Bauleiste wie Minimap ragen 4 px in den 12 px breiten Randscroll-Streifen der Kamera: wer den Mauszeiger an den unteren Bildschirmrand zur Bauleiste führt, scrollt dabei die Karte.

## 4. Der Kern: Zielerfassung und Feuererwiderung

**Das ist der eine Eingriff, der das Spielgefühl umdreht.**

`CombatSystem.ExecuteTick` überspringt heute jede Einheit ohne gesetztes Ziel:

```
CombatSystem.cs:168 — if (!attacker.IsActive || !attacker.AttackTarget.IsValid) continue;
```

`AttackTarget` wird ausschließlich durch einen expliziten `AttackTarget`-Befehl gesetzt. Folge: **jeder einzelne Schuss braucht einen Klick.** Eine Armee, die man irgendwohin schickt, verteidigt sich nicht, verfolgt nicht und schießt auf nichts. Die vorhandene Schadens- und Panzerungsmatrix, die Waffenprofile und die Reichweiten liegen ungenutzt daneben.

Das Gleiche trifft die **Verteidigungsplattform** noch härter. Sie ist bewaffnet (20 Schaden, Reichweite 10, Cooldown 10 Ticks — `SimDefinitions.cs:385`), und der Kommentar bei `:301` sagt ausdrücklich „DefensePlatform is armed because buildings CAN shoot". Aber `SelectionManager.CopyMobileSelection` filtert Gebäude aus jeder Befehlsselektion — ein Gebäude kann also gar keinen Angriffsbefehl empfangen und damit nie ein `AttackTarget` bekommen. Das einzige bewaffnete Gebäude des Spiels ist heute eine 400-AE-Kostenfalle.

**Eine zusätzliche Phase in `CombatSystem.ExecuteTick` löst beides.** Für jede aktive Einheit ohne gültiges Ziel: nächstes feindliches, sichtbares Ziel in Waffenreichweite suchen und `AttackTarget` setzen. Gebäude eingeschlossen. Das braucht

- **kein** neues Feld in `UnitState` — `AttackTarget` liegt bereits im Entity-Store-Block v4,
- **keine** Snapshot-Versionserhöhung,
- **keinen** neuen `CommandKind` — das v1-Register bleibt eingefroren,
- und die Sichtprüfung existiert schon (`FogOfWarSystem.GetTeamView`, wird von `CombatSystem` bereits benutzt).

**Der ehrliche Preis:** das ist die erste Simulationsänderung dieser Sprintreihe. Sie verändert den kanonischen Zustandsverlauf, und damit werden mehrere Baselines in `tools/Nova.SimRunner.Tests` rot — `MatchFingerprintTests`, `ReplayTests`, `SnapshotHashSensitivityTests` und die Öffnungs-Hash-Tests. Das ist kein Defekt, sondern genau das, wofür diese Tests da sind: sie melden, dass sich das Spielverhalten geändert hat. Die Baselines werden bewusst und dokumentiert neu gesetzt, nicht stillschweigend.

**Wichtig für die Determinismus-Disziplin:** die Zielsuche muss eine **stabile, indexbasierte Reihenfolge** haben (kleinster Entity-Index gewinnt bei gleichem Abstand), damit zwei Hosts dieselbe Wahl treffen. Kein `float`, keine Distanzsortierung über Fließkomma — Abstandsvergleich im Quadrat über `SimFixed`.

**Bewusst nicht in diesem Sprint: Attack-Move.** Die Geste „geh dorthin und schieß auf alles unterwegs" braucht entweder einen neuen `CommandKind` 18 gegen das eingefrorene v1-Register samt Golden-Bytes-Test, oder ein neues Zustandsfeld mit `StateVersion` 4 → 5. Beides ist ein eigener Sprint. Zielerfassung und Feuererwiderung sind billig, Attack-Move ist es nicht — sie werden getrennt, damit die billige Hälfte nicht Geisel der teuren wird.

## 5. Lebensbalken — reine Präsentation

`UnitState.CurrentHealth` und `MaxHealth` existieren und werden serialisiert (`UnitState.cs:41-42`). Ein Balken über Einheiten und Gebäuden ist damit null Simulationsänderung. Ohne ihn sieht man dem Gefecht nicht an, dass es stattfindet — mit Zielerfassung wird das erst richtig spürbar.

Vorschlag: Balken nur bei Schaden oder bei Selektion zeigen, nicht permanent über allem.

## 6. Rundenrahmen

Der größte Einzelposten dieses Sprints, und der Grund, warum man das Spiel heute nur einmal spielt.

- **Ergebnisbildschirm.** `VictorySystem` kennt den Ausgang bereits, `DebugHud` schreibt ihn als Wort in die Statuszeile. Es braucht ein Panel „Sieg" / „Niederlage" mit zwei Knöpfen: *Neue Runde* und *Hauptmenü*. Billig.
- **Sichtbare Pause.** `MatchRunner.PauseMatch` existiert, aber ein pausiertes Spiel und ein kaputtes Spiel sehen identisch aus. Overlay plus gestoppte Simulation, Musik läuft weiter.
- **Neustart und Rückweg ins Menü.** Der teure Teil. `InitializeMatch` baut Kernel, EntityManager, alle acht Systeme, die KI-Session, beide `MatchSession`-Instanzen, beide Ingress-Instanzen und beide Transports neu auf — das ist als Reset richtig, aber die **Präsentationsschicht muss mitgezogen werden**: `UnitViewManager` (Views aus dem alten Match), `SelectionManager` (Ids aus dem alten Match), Kameraposition, Minimap und besonders `FogOfWarOverlayView`, das Textur und Pixelpuffer genau einmal anlegt und nie an eine geänderte Fog-Grid-Größe anpasst. Bei gleicher Kartengröße trägt das; bei einer anderen Karte gibt es eine `IndexOutOfRangeException`. Defensiv mitreparieren.

### 6.1 Musik im Gefecht

Der Rundenrahmen ist die richtige Stelle dafür: Musik hat dieselben Zustandsübergänge wie die Runde selbst. Heute verstummt das Spiel in der Sekunde, in der es losgeht — `MenuMusicPlayer` blendet beim Matchstart aus, und danach ist Stille.

**Das Material liegt vor:** `Hashkrieg_Assets/audio/Music_inGame/`, acht Suno-Dateien — real **drei Themen** mit Varianten (Thema 1 orchestral 3:49; Thema 2 in fünf Fassungen, 2:04 bis 3:47; Thema 3 in zwei Fassungen, 3:38 und 3:47). Vorschlag, überstimmbar: je Thema die längste Fassung, also `HashKrieggame 1_orc`, `HashKrieggame 2 (2)`, `HashKrieggame 3 (1)`.

**Ablage und Format.** Nach `.ogg` konvertieren (wie die Menümusik) und als `MUS_Ingame_Hashkrieg_01..03.ogg` neben `MUS_MainMenu_Hashkrieg.ogg` in `Assets/_Project/Audio/Music/` legen. Import-Settings: **Streaming**, Vorbis, Qualität ~70 %, „Load In Background" an, „Preload Audio Data" aus — sonst liegen elf Minuten Musik dekomprimiert im Speicher. Repo-Zuwachs: rund 10–13 MB. Das ist bewusst in Kauf genommen und konsistent mit der Menümusik, die ebenfalls im Repo liegt; Audio fällt **nicht** unter die Art-Paket-Regel.

**Verhalten.** Ein `MusicDirector` mit Playlist: Matchstart blendet ein, während `MenuMusicPlayer` ausblendet. Titel laufen nacheinander mit kurzer Pause dazwischen, kein Titel zweimal hintereinander. Pause-Overlay: Musik läuft weiter (schon oben festgelegt). Ergebnisbildschirm: ausblenden. „Hauptmenü": Menümusik kommt zurück. Lautstärke und An/Aus kommen aus derselben `GameSettings`-Quelle wie heute, live anwendbar. Die Ein-/Ausblendmechanik von `MenuMusicPlayer` wird **geteilt, nicht kopiert**.

**Lizenzlage — verbindlicher Zwischenschritt.** D-083 hat den Suno-Bezahltarif ausdrücklich **zweckgebunden nur für die Menümusik** freigegeben („erzeugt kein Präzedenzrecht", `docs/assets/Licenses.md` §2 Regel 5). Ingame-Musik aus derselben Quelle braucht deshalb eine **Erweiterung der Ausnahme als eigene Inhaberentscheidung (D-086)** plus Einträge im Lizenz-Ledger §3. Ohne diesen Eintrag werden die Dateien nicht committet.

## 7. Billige Bedienbarkeit, die viel bringt

- **Kontrollgruppen 1–9** (Strg+Zahl setzen, Zahl abrufen) und **additive Auswahl mit Shift**. Reiner Auswahlzustand in `SelectionManager`, der bereits eine EditMode-Fixture hat. Berührt Simulation, Determinismus, Snapshot-Format und Befehlsregister mit null Zeilen — und ist die Geste, ohne die sich kein RTS richtig anfühlt.
- **Ablehnungsgründe sichtbar machen.** `CommandRejectReason` ist vollständig vorhanden, erreicht den Spieler aber nur im per Default ausgeblendeten F3-Panel. Ein abgelehnter Befehl, eine pausierte Simulation und eine stillstehende Baustelle sehen deshalb alle gleich aus wie „kaputt" — genau das Urteil „verwirrend". Kurze Einblendung im Dauer-HUD.
- **Ressourcenleisten-Chrome.** `DebugHud` erzeugt `_statusStyle` und benutzt ihn nie. Der Wert steht ohne Panel auf der hellen Wüstenebene. CHANGELOG und DecisionLog behaupten hier mehr, als der Code leistet — entweder verwenden oder die Doku korrigieren.
- **Steuerungslegende sichtbar machen.** Mittlere Maustaste für Kamera-Rotation und Space für Reset sind implementiert, aber nur hinter F3 dokumentiert. Der erklärte Zweck des HUD-Sprints — ohne Vorwissen bedienbar — ist für die Kamera damit verfehlt.

## 8. Bewusst nicht in diesem Sprint

| Punkt | Warum später |
|---|---|
| Attack-Move | Neuer `CommandKind` gegen eingefrorenes v1-Register oder `StateVersion`-Bump. Eigener Sprint. |
| Wirtschaftsbogen (erschöpfbare Felder) | `FieldReserveAE` geht in den kanonischen Hash ein und ist über sechs Dateien in beiden Testbahnen handgespiegelt. Richtige Diagnose, teuerste Umsetzung. |
| Fraktionswahl | Hängt an `InitialStateHash`; Spawn-Reihenfolge und Fraktionsbytes sind an zwei Stellen gespiegelt. Determinismus-Änderung, kein Menü-Feature. |
| KI-Ausbau | Der Gegner baut heute drei Gebäudetypen und eine Einheitensorte. Mit Feuererwiderung wird er automatisch gefährlicher — erst danach sinnvoll neu zu bewerten. |
| Lager und Radar mit Funktion | Zwei von neun Gebäuden kosten Geld und tun nichts. Ehrlicher wäre, sie bis dahin in der Bauleiste als „noch ohne Wirkung" zu kennzeichnen. |

## 9. Fertig wenn

Ich schicke den Harvester auf ein Feld — **er fährt hin, erntet, liefert bei der Raffinerie ab und fängt von allein wieder an**, mein Kontostand steigt ohne einen weiteren Klick. Ich baue in der Kaserne Soldaten, **und sie stehen danach vor der Kaserne**. Ich schicke sie zur Gegnerbasis — **und sie kämpfen von selbst**. Ich sehe an Lebensbalken, wer verliert. Meine Verteidigungsplattform schießt auf angreifende Gegner. Ich hole meine Armee mit der Taste 1 zurück. Wenn ein Befehl abgelehnt wird, sehe ich warum. Es läuft Musik, solange die Runde läuft, und sie verstummt mit dem Ergebnis. Und wenn die Runde vorbei ist, drücke ich auf *Neue Runde* statt auf *Programm beenden*.

Das ist der geschlossene Kernloop: ernten, bauen, kämpfen, gewinnen oder verlieren, neu anfangen.

---

## 10. Prompt für Kimi

```text
AUFGABE: Gefecht und Rundenrahmen (Hashkrieg, Branch feat/playable-core-loop)

AUSGANGSLAGE (Stand 2026-08-06 abends — gilt, nicht neu erheben)
Der Arbeitsbaum ist sauber. Branch feat/playable-core-loop steht auf 1f6607a und ist
gepusht; PR #23 gegen main ist offen und traegt vier Commits (D-077, D-083/D-084, D-085,
gitignore-Chore). Sprint 10 (Baubarkeit und Kartenbild) ist umgesetzt und committet.
AssetMappingRegistry.asset ist lokal auf skip-worktree gesetzt: die Datei bleibt lokal mit
ihren Art-Mappings bestehen, taucht nicht mehr im git status auf und kann nicht
versehentlich mitcommittet werden. NICHT anfassen, NICHT zuruecksetzen.
Diese drei Dateien gehoeren weiterhin in KEINEN Commit dieser Sprintreihe:
- Assets/_Project/Data/Registries/AssetMappingRegistry.asset (GUID-Verweise auf
  gitignorierte Prefabs — in jedem frischen Clone tot; Inhaberentscheidung)
- Packages/manifest.json + packages-lock.json (zwei Unity-AI-Editor-Pakete, eines
  Prerelease — kein reproduzierbarer Build)
- Assets/DefaultVolumeProfile.asset (Editor-Re-Serialisierung ohne Wirkung)

WARNUNG ZUR VERIFIKATION
Auf der Arbeitsmaschine fehlt das in global.json gepinnte .NET-8-SDK (rollForward:
disable, installiert ist nur 10.0.302) — "dotnet test tools/Nova.SimRunner.Tests" laeuft
dort NICHT. Wer die Simulation anfasst — und dieser Sprint tut das —, MUSS das SDK 8.0.318
installieren oder den Nachweis ueber die CI im PR fuehren. Eine Simulationsaenderung ohne
gelaufene Simulationstests wird nicht committet.

KONTEXT
Die Runde läuft heute so: Menü -> Neues Spiel -> Basis bauen -> Truppen bauen -> zur
Gegnerbasis schicken -> JEDEN GEGNER EINZELN ANKLICKEN -> "VICTORY" erscheint als Wort
in der Statuszeile, die Simulation tickt weiter, und der einzige Ausweg ist das Beenden
der Anwendung. Diese zwei Stellen sind der Inhalt dieses Sprints.

1. DER LOOP IST NICHT GESCHLOSSEN — VOR ALLEM ANDEREN
   Befund der zweiten Spielsitzung, nach Sprint 10: Bauen funktioniert, die Kaserne steht.
   Aber AUS DER KASERNE KOMMEN KEINE SOLDATEN, und DER HARVESTER ERNTET NICHT. Ohne diese
   beiden Punkte laesst sich der Rest dieses Sprints nicht einmal spielen.

   (a) HARVESTER — Ursache bewiesen, derselbe Fehler wie beim Builder in D-085.
       EconomySystem.ExecuteHarvestOrder bricht ab, solange das Fahrzeug nicht am Feld
       steht: "if (!IsInReach(in unit, field.GridPos)) return; // held, not dropped".
       Dasselbe bei der Rueckfahrt: ExecuteReturnOrder zahlt nur in Reichweite einer
       eigenen Raffinerie aus. Der Auftrag wird angenommen, das Fahrzeug bleibt stehen.
       Und wieder hat die KI die Verdrahtung, die dem Spieler fehlt — SkirmishAiSystem
       Abschnitt (4), Kommentar woertlich: "send every idle own harvester to the own field
       and WALK harvesters into reach with explicit Move intents".
       LOESUNG nach dem Praezedenzfall D-085: ein Harvester-Dispatch auf der Client-Seite,
       der beide Beine faehrt — zum Feld, wenn ein Ernteauftrag steht und das Fahrzeug
       ausser Reichweite ist; zur naechsten eigenen Raffinerie, sobald IsReturningCargo
       gesetzt ist. Move-Befehle ueber den normalen Command-Pfad. KEINE Regelaenderung,
       KEIN Hash-Bruch. Die Kreislauf-Automatik existiert schon: die Simulation behaelt die
       HarvestFieldId ueber die Rueckfahrt hinweg, es fehlt ausschliesslich die Fahrt.
       Harvester in der Simulation selbst fahren zu lassen ist die Alternative und wird
       BEWUSST NICHT gewaehlt — das waere eine Verhaltensaenderung mit neuen Baselines.

   (b) KASERNE — Ursache OFFEN. Nicht raten, erst diagnostizieren.
       Der Wartebalken laeuft und liest den echten Zustand (CommandCardHud ueber
       ProductionSystem.TryGetProducer), aber es erscheint keine Einheit. Die
       Simulationstests decken den Spawn-Pfad ab und sind gruen
       (ProductionSystemTests.Production_SpawnsAtDefaultRally_AfterExactBuildTicks).
       Der Defekt liegt also nicht in der reinen Spawn-Logik.
       DIAGNOSE IN DIESER REIHENFOLGE — das F3-Panel beantwortet Frage 1 sofort:
       1. Steigt die Einheitenzahl im F3-Panel (Forces: slot 0 Nu/Nb), waehrend der Balken
          durchlaeuft? Das trennt Simulation und Darstellung in einem Blick.
       2. JA -> die Einheit existiert und ist nur nicht sichtbar. Dann Darstellung:
          UnitViewManager.ResolveViewPrefab loest lokal auf die 34 Art-Prefabs aus der
          AssetMappingRegistry auf. Ein fehlendes, falsch skaliertes oder unter den Boden
          gesetztes Infanterie-Prefab sieht exakt aus wie "spawnt nicht". Gegenprobe:
          Registry-Eintrag der Infanterierolle leeren, dann MUSS das Graybox-Primitiv
          erscheinen. Kein Test deckt diesen Pfad ab — Tests laden keine Prefabs.
       3. NEIN -> einer der beiden STILLEN PAUSENPFADE in ProductionSystem.ExecuteTick:
          Entity-Store voll, oder TryFindSpawnCell findet in acht Ringen keine freie Zelle.
          Beide setzen den Fortschritt auf die Schwelle zurueck und schweigen. Beide
          Kapazitaeten sprechen dagegen (1024 Entities, Ring 8), der Pfad ist aber da.
       UNABHAENGIG VOM ERGEBNIS: beide stillen Pausen bekommen eine Rueckmeldung, genau wie
       die Baustelle in Sprint 10. "Kein Platz zum Ausruecken" ist eine Nachricht, kein
       Schweigen.
       VERBINDLICH: erst ein Test, der den Defekt reproduziert, dann die Behebung. Liegt er
       in der Darstellung, ist es ein PlayMode-Test; liegt er in der Simulation, ein
       EditMode-/SimRunner-Test.

2. DREI EINGABEDEFEKTE (sie vergiften sonst das neue HUD)
   a) Rechtsklick kennt die HUD-Sperre nicht: IsPointerOverHud wird bei RtsDeviceInput.cs
      :493, :525 und :739 geprüft, aber NICHT im Rechtsklick-Zweig bei :562. Wer mit
      selektierter Armee auf Bauleiste, Minimap oder Command Card rechtsklickt, schickt
      seine Truppen an den Punkt dahinter.
   b) Roter Baugeist platziert trotzdem: der Klick prüft _placementHasCell statt
      _placementValid (:493-499). Zusätzlich klemmt ToGridCoordinate negative Footprint-
      Ursprünge auf 0 — am linken/unteren Kartenrand entsteht das Gebäude woanders als
      der Geist zeigte.
   c) Command Card wird unten abgeschnitten: EstimateHeight rechnet GUILayout-Margins und
      Panel-Padding nicht mit (~40 px bei einer Fahrzeugfabrik mit 4 Einheiten). Untere
      Zeilen samt Abbruch-Buttons liegen außerhalb der BeginArea und sind nicht klickbar.
   Dazu zwei kleinere: die Rally-Point-Geste kapert den Rechtsklick, sobald bei einer
   Rahmenauswahl ein Gebäude mit drin ist (das HQ hat spawnbedingt immer selected[0]) —
   Rally darf nur greifen, wenn die Selektion AUSSCHLIESSLICH Gebäude enthält. Und
   Bauleiste wie Minimap ragen 4 px in den 12 px breiten Randscroll-Streifen der Kamera:
   der Weg zur Bauleiste scrollt die Karte.

3. ZIELERFASSUNG UND FEUERERWIDERUNG — der Kern
   CombatSystem.ExecuteTick überspringt heute jede Einheit ohne gesetztes Ziel
   (CombatSystem.cs:168). AttackTarget wird NUR durch einen expliziten Befehl gesetzt,
   also braucht jeder einzelne Schuss einen Klick.
   Dasselbe trifft die Verteidigungsplattform noch härter: sie IST bewaffnet (20 Schaden,
   Reichweite 10, SimDefinitions.cs:385), aber SelectionManager.CopyMobileSelection
   filtert Gebäude aus jeder Befehlsselektion — ein Gebäude kann also nie ein AttackTarget
   bekommen und nie feuern. 400 AE für eine Kostenfalle.
   LÖSUNG: eine zusätzliche Phase in CombatSystem.ExecuteTick. Für jede aktive Einheit
   OHNE gültiges Ziel das nächste feindliche, sichtbare Ziel in Waffenreichweite suchen
   und AttackTarget setzen. GEBÄUDE EINGESCHLOSSEN.
   Das braucht KEIN neues UnitState-Feld (AttackTarget liegt schon im Entity-Store-Block
   v4), KEINE Snapshot-Versionserhöhung, KEINEN neuen CommandKind. Die Sichtprüfung
   existiert bereits (FogOfWarSystem.GetTeamView, wird von CombatSystem schon benutzt).
   DETERMINISMUS: stabile, indexbasierte Reihenfolge (kleinster Entity-Index gewinnt bei
   gleichem Abstand). Kein float, keine Fließkomma-Distanzsortierung — Abstandsvergleich
   im Quadrat über SimFixed.
   ERWARTETE FOLGE: das ist die erste Simulationsänderung dieser Sprintreihe. Mehrere
   Baselines in tools/Nova.SimRunner.Tests werden rot (MatchFingerprintTests, ReplayTests,
   SnapshotHashSensitivityTests, Öffnungs-Hash). Das ist kein Defekt, sondern genau ihr
   Zweck. Baselines bewusst und dokumentiert neu setzen, nicht stillschweigend.
   NICHT in diesem Sprint: Attack-Move. Das bräuchte CommandKind 18 gegen das eingefrorene
   v1-Register oder StateVersion 4->5. Eigener Sprint. Nicht mit hineinbündeln.

4. LEBENSBALKEN (reine Präsentation, null Simulationsänderung)
   UnitState.CurrentHealth und MaxHealth existieren (UnitState.cs:41-42). Balken über
   Einheiten und Gebäuden. Vorschlag: nur bei Schaden oder Selektion zeigen, nicht
   permanent über allem.

5. RUNDENRAHMEN (der größte Posten)
   - Ergebnisbildschirm: VictorySystem kennt den Ausgang bereits, DebugHud schreibt ihn
     nur als Wort. Panel "Sieg"/"Niederlage" mit zwei Knöpfen: Neue Runde, Hauptmenü.
   - Sichtbare Pause: MatchRunner.PauseMatch existiert, aber pausiert und kaputt sehen
     identisch aus. Overlay plus gestoppte Simulation, Musik läuft weiter.
   - Neustart und Rückweg ins Menü: InitializeMatch baut Kernel, EntityManager, alle acht
     Systeme, KI-Session, beide MatchSessions, beide Ingress-Instanzen und beide
     Transports neu auf — als Reset richtig. Aber die PRÄSENTATIONSSCHICHT muss mit:
     UnitViewManager (Views des alten Matches), SelectionManager (alte Ids), Kamera,
     Minimap, und besonders FogOfWarOverlayView, das Textur und Pixelpuffer genau einmal
     anlegt und nie an eine geänderte Fog-Grid-Größe anpasst (IndexOutOfRangeException bei
     größerer Karte). Defensiv mitreparieren.

6. MUSIK IM GEFECHT (gehoert zum Rundenrahmen — gleiche Zustandsuebergaenge)
   Heute verstummt das Spiel in der Sekunde, in der es losgeht: MenuMusicPlayer blendet
   beim Matchstart aus, danach Stille.
   MATERIAL: Hashkrieg_Assets/audio/Music_inGame/ — acht Suno-Dateien, real DREI Themen
   mit Varianten. Vorschlag (ueberstimmbar): je Thema die laengste Fassung, also
   "HashKrieggame 1_orc" (3:49), "HashKrieggame 2 (2)" (3:47), "HashKrieggame 3 (1)"
   (3:47).
   ABLAGE: nach .ogg konvertieren wie die Menuemusik, als MUS_Ingame_Hashkrieg_01..03.ogg
   neben MUS_MainMenu_Hashkrieg.ogg in Assets/_Project/Audio/Music/.
   IMPORT-SETTINGS (wichtig): Load Type STREAMING, Vorbis, Qualitaet ~70 %, Load In
   Background AN, Preload Audio Data AUS — sonst liegen elf Minuten Musik dekomprimiert
   im Speicher. Repo-Zuwachs ~10-13 MB, bewusst in Kauf genommen; Audio faellt NICHT
   unter die Art-Paket-Regel, die Menuemusik liegt ebenfalls im Repo.
   VERHALTEN: ein MusicDirector mit Playlist. Matchstart blendet ein, waehrend
   MenuMusicPlayer ausblendet. Titel nacheinander mit kurzer Pause, kein Titel zweimal
   hintereinander. Pause-Overlay: Musik laeuft weiter. Ergebnisbildschirm: ausblenden.
   "Hauptmenue": Menuemusik kommt zurueck. Lautstaerke/An-Aus aus derselben
   GameSettings-Quelle wie heute, live anwendbar. Die Ein-/Ausblendmechanik von
   MenuMusicPlayer wird GETEILT, nicht kopiert. Verdrahtung im BootstrapSceneGenerator
   (die Szene ist Maschinenausgabe).
   LIZENZ — VERBINDLICHER ZWISCHENSCHRITT VOR DEM COMMIT: D-083 hat den Suno-Bezahltarif
   ausdruecklich ZWECKGEBUNDEN NUR fuer die Menuemusik freigegeben ("erzeugt kein
   Praezedenzrecht", docs/assets/Licenses.md §2 Regel 5). Ingame-Musik aus derselben
   Quelle braucht eine Erweiterung der Ausnahme als eigene Inhaberentscheidung (D-086)
   plus Eintraege im Lizenz-Ledger §3. Ohne diesen Eintrag werden die Dateien NICHT
   committet.

7. BILLIGE BEDIENBARKEIT
   - Kontrollgruppen 1-9 (Strg+Zahl setzen, Zahl abrufen) und additive Auswahl mit Shift.
     Reiner Auswahlzustand im SelectionManager, der schon eine EditMode-Fixture hat.
     Null Berührung von Simulation, Determinismus, Snapshot-Format, Befehlsregister.
   - Ablehnungsgründe sichtbar machen: CommandRejectReason ist vollständig da, erreicht
     den Spieler aber nur im per Default ausgeblendeten F3-Panel. Kurze Einblendung im
     Dauer-HUD. Heute sehen abgelehnter Befehl, pausierte Simulation und stillstehende
     Baustelle alle gleich aus wie "kaputt".
   - Ressourcenleisten-Chrome: DebugHud erzeugt _statusStyle und benutzt ihn nie. Entweder
     verwenden oder CHANGELOG und DecisionLog korrigieren, die hier mehr behaupten.
   - Steuerungslegende sichtbar machen: mittlere Maustaste (Kamera-Rotation) und Space
     (Reset) sind implementiert, aber nur hinter F3 dokumentiert.

REIHENFOLGE
1. Arbeitsbaum sauber, inkl. der drei Aussonderungen (Voraussetzung)
2. DER LOOP: Harvester faehrt und erntet, Kaserne spawnt — zuerst, alles andere baut
   darauf auf und laesst sich ohne das nicht einmal spielen
3. Die drei Eingabedefekte (klein, sofort spürbar)
4. Zielerfassung + Feuererwiderung + Lebensbalken (der Kern — zusammen testen)
5. Rundenrahmen
6. Musik (setzt den Rundenrahmen voraus: sie haengt an dessen Zustandsuebergaengen)
7. Kontrollgruppen, Ablehnungsgründe, Chrome, Legende

FERTIG WENN
Ich schicke den Harvester auf ein Feld, er faehrt hin, erntet, bringt die Ladung zur
Raffinerie und faengt von allein wieder an — mein Kontostand steigt, ohne dass ich klicke.
Ich baue in der Kaserne Soldaten, und sie stehen danach vor der Kaserne. Ich schicke sie
zur Gegnerbasis — und sie kämpfen von selbst. Ich sehe an Lebensbalken, wer verliert.
Meine Verteidigungsplattform schießt auf angreifende Gegner. Ich hole meine Armee mit
Taste 1 zurück. Wenn ein Befehl abgelehnt wird, sehe ich warum. Es läuft Musik, solange
die Runde läuft. Und wenn die Runde vorbei ist, drücke ich auf "Neue Runde" statt auf
"Programm beenden".
```
