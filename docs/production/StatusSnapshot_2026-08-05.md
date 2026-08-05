# Projektstand-Snapshot 2026-08-05

**Version:** 0.1.0 | **Status:** datierter Ist-Stand – kein Gate-Nachweis | **Verantwortungsbereich:** Orchestrator / Technical Writer | **Sprint:** 7

## Zweck

Dieser Snapshot hält den Projektstand **vor dem Eintreffen der ersten
3D-Assets** fest: Repository-, Gate-, Test- und Asset-Status, der spielbare
Umfang und die in derselben Sitzung (GB-003) geschaffene Asset-Bereitschaft.
Er ist ein Lagebild, kein Fortschrittsnachweis – der Gate-Status steht
ausschließlich in [MVPRecoveryPlan.md](MVPRecoveryPlan.md) und entsteht nur
aus autorisierter Evidence.

## Abhängigkeiten

- [GrayboxLog.md](GrayboxLog.md) – Sitzungsprotokolle GB-001 bis GB-003
- [ScopeLedger.md](ScopeLedger.md) – registrierte Zurückstellungen
- [DemoRunbook.md](DemoRunbook.md) – Demo-Ablauf und Asset-Ablage
- [SprintPlanning.md](SprintPlanning.md) – Sprint-7-Arbeitsvertrag
- [DecisionLog.md](DecisionLog.md) – D-055 bis D-075

## 1. Repository-Stand

| Punkt | Stand |
|---|---|
| Branch zu Sitzungsbeginn | `main` @ `72ef550`, Arbeitsbaum sauber |
| Session-Arbeit (GB-003) | uncommittet im Arbeitsbaum auf `main`; Commit/PR steht als Inhaberentscheidung aus |
| Verlustgefährdete Alt-Arbeit aus GB-002 | bereinigt (früherer Befund; Arbeitsbaum war zu Sitzungsbeginn sauber) |

## 2. Gate- und Sprint-Status (unverändert, ohne neuen Nachweis)

| Stufe | Status |
|---|---|
| G0 | G0-A1 Mergekandidat; **G0-A2 offen/blockierend**; G0-B offen |
| G1–G5 | nicht begonnen (Reihenfolgebindung) |
| MS-0 / MS-1 | nicht erreicht |
| D-067 / D-068 | Entwürfe, Inhaber-Ratifizierung ausstehend |
| D-074 | in Kraft (Agent unter Delegation), Inhaber-Bestätigung ausstehend |

## 3. Verifikations-Stand (in dieser Sitzung ausgeführt)

| Prüfung | Ergebnis |
|---|---|
| .NET-Tests `tools/Nova.SimRunner.Tests` (Release) | **406/406 grün** (Baseline dieser Sitzung) |
| Unity EditMode-Tests, Batchmode 6000.5.4f1 | **410/410 grün** (405 + 5 neue Namenskonventions-Tests) |
| Szenen-Regenerierung (`BootstrapSceneGenerator`, headless) | Exit 0; Szene enthält neu das `Map`-Objekt samt Blockout-Verdrahtung |
| Determinismus | von dieser Sitzung **nicht berührt** (keine Sim-/Core-Änderung); letzter bekannter Fingerprint aus GB-002: `0xAF9FB211B6C9CACE` |
| Player-Builds | aus GB-002 vorhanden (`Builds/MacOSArm64`, `Builds/Windows64`), in dieser Sitzung **nicht** neu gebaut und nicht ausgeführt |

## 4. Asset-Stand

**Es liegt noch kein einziges 3D-Asset vor** – weder im Repository noch in
den geprüften Ablageorten des Arbeitsrechners (Downloads/Desktop/Dokumente,
Stand 2026-08-05). Vorhandenes Bildmaterial:

- `docs/assets/concept-art/full/` – 33 KI-generierte Konzeptbilder aller
  Allianz-/Legion-Einheiten und -Gebäude (Entwürfe, keine Produktionsassets)
- `docs/assets/reference/` – 4 orthografische Referenzbilder (Allianz-/Legion-
  HQ, Allianz-/Legion-LightTank) als Image-to-3D-Inputs des Vertical Slice
- `docs/assets/ArtManifest_MS1.md` – 34 Assets spezifiziert, alle
  `status: specified` (keines produziert)

## 5. In dieser Sitzung geschaffene Bereitschaft (GB-003)

- **Art-Ablage:** `Assets/_Project/Art/` mit der vollständigen
  ArtAssetStandard-Ordnerstruktur (2 Fraktionen × 9 Gebäude- und
  8 Einheiten-Rollen, `Shared/`, `Source/`).
- **Drop-in-Pipeline:** `ArtAssetNaming` (Namensparser, Nova.Data),
  `ArtAssetAutoSync` (Editor): registriert jedes konventionkonforme
  `PF_*`-Prefab beim Import automatisch in
  `Assets/_Project/Data/Registries/AssetMappingRegistry.asset` und stempelt
  die Standard-Import-Settings auf FBX/Texturen unter `Art/`.
- **Darstellung:** `UnitViewManager` rendert registrierte Definitions-Ids als
  Prefab (Fraktion/ Rolle aufgelöst), alles andere bleibt Graybox-Primitiv –
  Mischbetrieb ab dem ersten Asset.
- **Erste Karte:** Glutrinne-Blockout (`GlutrinneBlockoutView`) – Wüstentönung,
  Kartenrand, Kristallmarker auf den zwei real registrierten Feldern – plus
  Datenasset `Assets/_Project/Data/Maps/MAP_Glutrinne.asset` (Graybox-Teilmenge
  des Manifest-Layouts; 5-Felder-Ausbau ist G4).
- **Demo:** [DemoRunbook.md](DemoRunbook.md) mit Ablauf, Steuerung und
  ehrlichen Grenzen.

## 6. Spielbarer Stand in einem Satz

Lokales 1v1 auf der Glutrinne-Graybox: Allianz (Mensch) gegen Legion
(**untätig**, G3) mit Wirtschaftskreislauf, Bau/Produktion (4 von 17 Rollen
belegt), Fog of War, Schadensmatrix und Siegauswertung – Steuerung und
bekannte Lücken siehe [DemoRunbook.md](DemoRunbook.md) §5.

## Offene Punkte

- Inhaberentscheidungen: D-067/D-068 ratifizieren, D-074 bestätigen, sowie
  Commit/PR dieser Session-Arbeit.
- Erster menschlicher Play-Durchlauf (Look & Feel bis dahin unverifiziert).
- Provenienzpflicht vor Repo-Aufnahme jedes Assets (docs/assets/Provenance.md).
- Bekannte Spiellücken (KI-Slot untätig, keine Zielerfassung, Tastenbelegung,
  Pause ungebunden u. a.) – registriert in [ScopeLedger.md](ScopeLedger.md)
  und [GrayboxLog.md](GrayboxLog.md) GB-002.

## Nächste Schritte

1. Demo-Runde nach [DemoRunbook.md](DemoRunbook.md) durchlaufen, Rückmeldung
   als GB-Eintrag protokollieren.
2. Erste `PF_*`-Assets in Vertical-Slice-Reihenfolge ablegen (Allianz-/Legion-
   HQ und LightTank zuerst).
3. Gate-Pfad unverändert: G0-A2 implementieren, dann G0-B, dann G1 – diese
   Spur beansprucht keinen Gate-Status.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-08-05 | Erstfassung: datierter Stand vor Eintreffen der ersten 3D-Assets, inkl. Verifikationszahlen und Asset-Inventur der Sitzung GB-003 | Technical Writer |
