# Naming Convention – Benennungsregeln für Hashkrieg

**Version:** 0.5.0 | **Status:** Entwurf – MS-1 rebaselined | **Verantwortungsbereich:** Lead Technical Director | **Sprint:** 4

## Zweck

Dieses Dokument legt die verbindlichen Benennungsregeln für Code (Namespaces,
Typen, Member), Definitions-Assets, Ordner, Tests, Events und Datei-Header
fest. MS-1 aktiviert nur das Manifest aus
[MVPContentManifest.md](../production/MVPContentManifest.md); Namen für den
größeren Vollspielentwurf sind reserviert, aber keine Implementierungspflicht.

## Abhängigkeiten

- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056 (MS-1-Umfang), D-057 (kanonischer State/Commands), D-061 (Abnahme)
- [./Commands.md](./Commands.md) – führendes Command-Schema
- [../research/Unity_BestPractices.md](../research/Unity_BestPractices.md) – §3 (Registry-Pattern, stabile IDs)
- [./FolderStructure.md](./FolderStructure.md) – Ordner-/Assembly-Struktur, der die Namespaces folgen
- [./CodingGuidelines.md](./CodingGuidelines.md) – Regeln, deren Rollen hier benannt werden
- [../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md) – Art-Ebene der Namenskonvention (Mesh/Textur/Material/Prefab-Präfixe, §4.1)

## 1. Allgemeine C#-Regeln

| Element | Konvention | Beispiel |
|---|---|---|
| Typen, Methoden, Properties, Events | `PascalCase` | `MoveCommand`, `Execute`, `CurrentTick` |
| Interfaces | `I` + `PascalCase`, Rollenname | `ISimSystem`, `ISimRandom` |
| Parameter, lokale Variablen | `camelCase` | `targetTile`, `elapsedTicks` |
| Private/protected Felder | `_camelCase` | `_unitStates`, `_logger` |
| `const` und `static readonly` | `PascalCase` | `TicksPerSecond`, `MaxEntities` |
| Enums + Werte | `PascalCase` | `FactionId.Allianz` |
| Boolesche Member | Präfix `Is/Has/Can/Should` | `IsVisible`, `CanCapture` |

- Abkürzungen werden wie Wörter behandelt (`FogOfWarSystem`, nicht `FOWSystem`); Ausnahme etablierter Kurzformen: `UI`, `ID`/`Id`, `AI` (als `Ai` in PascalCase-Typen, z. B. `AiPlayer` in `Nova.AI`).
- Keine ungarische Notation, keine Unterstrich-Suffixe, keine kryptischen Kurznamen (`mgr`, `hndl`).
- Sprache aller Identifier: Englisch (DocumentationStandard §Sprache). Fraktionsnamen bilden die dokumentierte Ausnahme (§4).

## 2. Namespaces

Namespaces spiegeln exakt die Ordnerstruktur ([./FolderStructure.md](./FolderStructure.md) §3). Neue Feature-Namespaces werden nur auf Ebene unterhalb der Schicht-Roots angelegt.

| Namespace | Schicht | Inhalt (Beispiele) |
|---|---|---|
| `Nova.Core` | Core | `EntityId`, `Tick`, `INovaLogger`, `SimMath` |
| `Nova.Simulation` | Sim (Unity-frei) | `SimulationHost`, `SimulationConfig`, `CommandRecord`, `CommandKind` |
| `Nova.Simulation.Commands` | Sim | `CommandIntent`, `CommandIngress`, `CommandBatch` |
| `Nova.Simulation.State` | Sim | `UnitState`, `MatchState`, `PlayerState` |
| `Nova.Simulation.Definitions` | Sim | `UnitDefinition`, `WeaponDefinition` (Unity-freie Snapshots) |
| `Nova.Simulation.Economy` / `.Combat` / `.Movement` / `.Pathfinding` / `.FogOfWar` | Sim | je ein `ISimSystem` + zugehörige Typen |
| `Nova.Simulation.Burst` | Post-MVP reserviert | erst nach bewiesener exakter Feld-/Hash-/Byteparität aktivierbar (D-057) |
| `Nova.AI` / `Nova.AI.Strategy` / `.Tactics` / `.Squads` | KI (Unity-frei, D-043) | `AiPlayer`, `StrategicDirector`, `SquadBehavior` |
| `Nova.AI.Data` | Data (SO) | `DifficultyProfileSO`, `StrategyOptionSO`, `AiRegistrySO` |
| `Nova.Data` | Data (SO) | `UnitDefinitionSO`, `UnitRegistrySO`, `GameDatabaseMasterSO` |
| `Nova.Gameplay` / `Nova.Gameplay.<Feature>` | Gameplay | `MatchRunner`, `SimBridge`, Pools, Command-Eingang |
| `Nova.Presentation` / `Nova.Presentation.<Feature>` | Presentation | `UnitView`, `FogOfWarRenderFeature`, `AudioService` |
| `Nova.Editor` | Editor | Inspectors, Validatoren |
| `Nova.Simulation.Tests` / `Nova.Gameplay.Tests` / `Nova.PlayMode.Tests` | Tests | spiegeln die getestete Schicht |

## 3. Typnamen nach Rolle

Suffixe sind verbindlich – sie machen die Schichtzugehörigkeit am Namen erkennbar:

| Rolle | Muster | Beispiel |
|---|---|---|
| Command-Intent | `<Action>CommandIntent` | `MoveCommandIntent` |
| Kanonischer Command-Record | `CommandRecord` + `CommandKind` + versionierter Payload | `MoveCommandPayloadV1` |
| Sim-System | `<Domäne>System : ISimSystem` | `EconomySystem`, `FogOfWarSystem` |
| State-Struct (Sim) | `<Entität>State` | `UnitState`, `BuildingState` |
| Sim-Definition (Unity-frei) | `<Entität>Definition` | `UnitDefinition`, `TechDefinition` |
| SO-Schema (Unity) | `<Entität>DefinitionSO : ScriptableObject` | `UnitDefinitionSO`, `WeaponDefinitionSO` |
| Sub-Registry-SO (pro Kategorie, D-049) | `<Kategorie>RegistrySO : ScriptableObject` (Instanz: `<Kategorie>Registry.asset`) | `UnitRegistrySO`, `WeaponRegistrySO` |
| Master-Index-SO (generiert, D-049) | `GameDatabaseMasterSO` (Instanz: `GameDatabaseMaster.asset` – generiert, nie händisch) | – |
| View (MonoBehaviour, Presentation) | `<Gegenstand>View` | `UnitView`, `MinimapView`, `HealthbarOverlay` |
| Gameplay-Brücke/Runner | `<Zweck>Bridge` / `<Zweck>Runner` | `SimBridge`, `MatchRunner` |
| Service (Gameplay/Presentation) | `<Domäne>Service` | `AudioService`, `InputService` |
| Sim-Event-Record (Struct, Event-Puffer) | `<Ereignis>Event` | `DamageEvent`, `UnitDiedEvent` |
| Editor-Tool | `<Gegenstand>Validator` / `<Gegenstand>Inspector` | `GameDatabaseValidator` |

Die Paarung `UnitDefinition` (Sim) ↔ `UnitDefinitionSO` (Unity-Asset) ist bewusst gewählt: gleiche Domäne, klar getrennte Schicht – das SO ist die Editier-Quelle, die Definition der Sim-Snapshot.

## 4. SO-Asset-Dateinamen

Muster: `<PREFIX>_<Fraktion>_<Name>.asset`

- **Fraktions-Token:** `Allianz`, `Legion`, `Evolvierte` (deutsche GDD-Namen, dokumentierte Ausnahme zu §1), `Neutral` für neutrale Objekte (D-016), `Shared` für fraktionsübergreifendes.
- **Name:** `PascalCase` ohne Trennzeichen (`Rifleman`, `TitanMech`).

| Präfix | Asset-Typ | Beispiel |
|---|---|---|
| `UNIT_` | Einheiten (Infanterie/Fahrzeug/Luft/Drohne) | `UNIT_Allianz_Rifleman.asset` |
| `BLDG_` | Gebäude | `BLDG_Legion_WarFactory.asset` |
| `WPN_` | Waffen | `WPN_Shared_Railgun.asset` |
| `TECH_` | Forschung | `TECH_Evolvierte_SporeCloud.asset` |
| `FACT_` | Fraktions-Definition | `FACT_Allianz.asset` |
| `AIDIFF_` | KI-DifficultyProfile | `AIDIFF_Shared_Hard.asset` |
| `MAP_` | Karten-Definition | `MAP_Shared_DustBasin.asset` |
| `BIOME_` | Biom-Profil | `BIOME_Shared_Moon.asset` |
| `DB_` | Registry-Assets | Dokumentierte Ausnahme: Registry-Dateien tragen **kein** `DB_`-Präfix, sondern liegen in `Data/Registries/` als `<Kategorie>Registry.asset` plus generiertem `GameDatabaseMaster.asset` (D-049, s. FolderStructure §4) |

Ablage: `Assets/_Project/Data/<Typ>/<Fraktion>/` (vgl. FolderStructure §4).

### 4.1 Verhältnis zur Art-Ebene

Die Präfixe in §4 bezeichnen ausschließlich die **Daten-Ebene**
(ScriptableObject-Assets in `Assets/_Project/Data/`). Daneben existiert eine
separate **Art-Ebene** für Mesh-, Textur-, Material- und Prefab-Dateien in
`Assets/_Project/Art/`, spezifiziert in
[../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md). Die beiden
Ebenen sind bewusst getrennte Namensräume mit unterschiedlichen
Fraktions-Token (Daten-Ebene: deutsche GDD-Token `Allianz`/`Legion`/
`Evolvierte`/`Neutral`/`Shared`, s. o.; Art-Ebene: englische PascalCase-Token
`Alliance`/`Legion`) und referenzieren dieselbe `<Fraktion>/<Rolle>`-
Kombination nur über den Rollennamen, nicht über einen gemeinsamen Präfix:

| Präfix | Ebene | Asset-Typ |
|---|---|---|
| `SM_` | Art | Mesh (`SM_BLDG_<Faction>_<Role>.fbx`, LOD-Suffixe `_LOD0`/`_LOD1`/`_LOD2` in derselben FBX) |
| `T_` | Art | Textur (`T_UNIT_<Faction>_<Role>_BC.png`; Suffixe `_BC`, `_N`, `_MSK`) |
| `M_` | Art | Material (`M_BLDG_<Faction>_<Role>.mat`) |
| `PF_` | Art | Prefab (`PF_UNIT_<Faction>_<Role>.prefab`) |

Rollennamen (`<Role>`) auf der Art-Ebene entsprechen exakt der kanonischen
Rollenliste aus [`quality/content/mvp-v1.json`](../../quality/content/mvp-v1.json)
(`buildings[].role` / `units[].role`), nicht den hier in §4 verwendeten
GDD-Namen. Details, Ordnerstruktur und Begründung: siehe
[../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md) §1–§2.

## 5. Ordnernamen

- `PascalCase`, Plural für Sammlungen (`Units/`, `Commands/`), Singular für Singleton-artige Bereiche (`Editor/`, `Settings/`).
- Keine Sammelordner (`Misc/`, `Common/`, `Util/`) – gemeinsame Basistypen gehören nach `Nova.Core`.
- Ordner unterhalb von `Scripts/` entsprechen 1:1 den Namespaces (§2).

## 6. Tests

- Test-Klassen: `<GetesteterTyp>Tests` (`EconomySystemTests`, `MoveCommandValidationTests`).
- Test-Methoden: `<Methode>_<Bedingung>_<Erwartung>` (`Execute_InsufficientAetherium_CommandRejected`).
- Match-Fixtures/Replays für Sim-Tests: `FIX_<Szenario>` als Dateiname (`FIX_HarvesterLoop_500ticks.json`).
- Test-Assemblies/Namespaces nach §2; Tests benennen ihre Erwartung als Behavior, nicht als Implementierung.

## 7. Events und Delegates

- **View-/Gameplay-Events (C#-`event`):** Muster `On<Subjekt><Veränderung>`, ausgelöst nach der Änderung: `OnHealthChanged`, `OnMatchEnded`, `OnSelectionChanged`. Payload als Struct-Argument (`event Action<HealthChangedPayload>`), nie `EventHandler`/`EventArgs`-Klassen-Hierarchien.
- **Keine selbstdefinierten Delegate-Typen** – immer `Action<>`/`Func<>` (Research §6). Delegate-Namen mit Suffix `Handler` entfallen damit.
- **Sim-Event-Puffer-Records** heißen `<Ereignis>Event` (§3) und sind keine C#-Events; die Namensnähe ist gewollt (DamageEvent = Datensatz, OnDamageReceived = mögliches View-Event daraus).
- **Commands** werden nie `On…` benannt – Commands sind Absichten (Imperativ), Events sind Fakten (Vergangenheit).

## 8. Authoring-Keys und kanonische Definitions-IDs

SO-Definitionen besitzen außerhalb der Simulation einen stabilen
`DefinitionKey` im Format dot-lower:

```text
unit.allianz.rifleman      bldg.legion.war_factory      tech.evolvierte.spore_cloud
```

- Muster: `<typ>.<fraktion>.<name_snake_case>`; Fraktions-Token wie §4 in
  lower-case.
- Keys sind nach Vergabe unveränderlich; Asset-Umbenennung ändert den Key
  nicht.
- Der G1-Definitions-Build sortiert gültige Keys byteweise nach UTF-8,
  weist daraus `DefinitionId uint16` von 1 aufwärts zu und schreibt die
  Zuordnung in den kanonischen `DefinitionSnapshot`. 0 ist ungültig.
- Sim-State, Commands, Snapshots und Replays enthalten ausschließlich
  `DefinitionId`, niemals Strings.
- Jede geänderte Zuordnung ändert `DefinitionsHash64`. Replays werden dann
  abgelehnt; Savegames benötigen eine explizite Migration.

## 9. Datei-Header-Konvention

Jede handgeschriebene `.cs`-Datei beginnt mit:

```csharp
// -----------------------------------------------------------------------------
// Hashkrieg – <eine Zeile Zweck>
// Assembly: Nova.Simulation | Layer: Simulation (keine UnityEngine-Referenzen, D-057)
// Entscheidungen: D-057, D-061
// -----------------------------------------------------------------------------
```

- `Layer`-Zeile nennt die Schicht; im Sim-Kern inkl. des Hinweises auf die Unity-Freiheit (macht die härteste Regel in jeder Datei sichtbar).
- `Entscheidungen` listet nur die D-IDs, die die Datei unmittelbar binden (kein Sammelsurium).
- Header werden bei Schicht-Wechsel einer Datei angepasst; Header sind Pflicht, aber bewusst kurz – kein Lizenz-/Autorenblock.

## Offene Punkte

- **Präfix-Vollständigkeit:** Die Tabelle deckt den MS-1-Scope; Präfixe für
  Superwaffen, Commander und Hazards werden erst mit einem Post-MVP-Scope
  verbindlich.
- **Fraktions-Token:** `Allianz`/`Legion`/`Evolvierte` folgen den GDD-Namen; falls das GDD lokalisierte interne Namen ändert, ist §4 nachzuziehen (IDs nach §8 bleiben stabil).
- **AIDIFF_-Präfix:** Längeres Token wegen Eindeutigkeit gegenüber einem späteren `AI_`-Sammelpräfix gewählt; bei Einführung weiterer KI-Asset-Typen (Behavior-Trees etc.) Namespace/Präfix gemeinsam final festlegen.

## Nächste Schritte

1. Konsistenzreview gegen Architecture.md und die AI-/Data-Tech-Docs (Namespace-Liste §2 mit den dort geplanten Systemen abgleichen).
2. G1: Referenzdateien mit korrekten Suffixen anlegen
   (`MoveCommandIntent`, `CommandRecord`, `MoveCommandPayloadV1`).
3. In G1 den Definitions-Generator umsetzen: Er validiert und sortiert
   `DefinitionKey`, erzeugt den kanonischen `DefinitionSnapshot` samt
   `DefinitionId uint16` und optional typisierte Compile-Zeit-Konstanten. CI
   prüft Rebuild + Diff; nur Mapping und Hash, nicht Strings, erreichen
   Commands/State/Persistence.
4. Präfix-Tabelle nach Sprint-5-Asset-Audit vervollständigen und Version erhöhen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 0.2.0 | 2026-07-21 | Korrekturlauf Sprint 4 (D-043–D-052, Review-Findings): Nova.AI-Namespaces (D-043), Sub-Registry-/Master-Index-Benennung (D-049), Command-/CommandEnvelope-Benennung boxfrei (Review F-5), ID-Codegen als Sprint-7-Tooling-Aufgabe konkretisiert | Lead Technical Director |
| 0.2.1 | 2026-07-21 | Restfehler behoben: Altrest `Nova.Simulation.Ai` als Sim-Namespace entfernt (D-043 – KI ist eigene Assembly `Nova.AI`/`.Strategy`/`.Tactics`/`.Squads`, kein zweiter Namespace mehr in `Nova.Simulation`) | Lead Technical Director |
| 0.3.0 | 2026-07-24 | Benennungsumfang und Command-Typen auf D-056/D-057/D-061 rebaselined | Lead Technical Director |
| 0.4.0 | 2026-07-24 | Authoring-String-Keys sauber von kanonischem `DefinitionId uint16` und Persistence getrennt | Lead Technical Director |
| 0.5.0 | 2026-07-25 | §4.1 ergänzt: Verweis auf die Art-Ebene (`SM_`/`T_`/`M_`/`PF_`-Präfixe) in [../assets/ArtAssetStandard.md](../assets/ArtAssetStandard.md), Abgrenzung zur bestehenden Daten-Ebene (§4) klargestellt | Technical Art |
