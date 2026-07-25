# Eingabesystem

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead UI/UX Designer | **Sprint:** 7

## Zweck

Definiert Device→Intent, Rebinding, Client-Feedback und die harte Grenze zur
autoritativen Command-Pipeline.

## Abhängigkeiten

- [Commands.md](Commands.md) – Ingress und Schema v1
- [CameraSystem.md](CameraSystem.md) – lokale Kamera-Intents
- [FogOfWar.md](FogOfWar.md) – gefiltertes Picking
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md)

## 1. Schichten

```text
Device → Input Action → Client Intent
                         ├─ Camera/Selection/UI (lokal)
                         └─ CommandIntent → MatchSession/Ingress
```

Nur `MatchSession`/`CommandIngress` bindet PlayerSlot, Sequence und TargetTick.
Input kennt keinen mutable Sim-State und erzeugt keinen versiegelten Command.

## 2. Lokale Intents

Keine Sim-Commands:

- Kamera-Panning/Zoom/Edge Scroll/Minimap-Navigation;
- Auswahl, Box Select und Kontrollgruppenverwaltung;
- UI-Navigation, Tooltips und Settings;
- Rebinding und UI-Skalierung.

Picking arbeitet ausschließlich auf dem gefilterten Player-Snapshot.
Verborgene Unity-Proxies dürfen kein Ziel liefern.

## 3. CommandIntents

MS-1 benötigt:

- Move, Stop, AttackTarget;
- Harvest und ReturnCargo;
- PlaceBuilding, CancelConstruction, Repair und Sell;
- QueueUnit und CancelProduction;
- SetRallyPoint;
- InstallDefenseModule.

Pause, Save und Load sind versionierte Session-Aktionen an einer Tickgrenze,
keine direkte State-Mutation. Zurückgestellte Ability-, Research-, Capture-,
Luft-, Mauer- und Superweapon-Aktionen sind nicht aktiv.

## 4. Client-Feedback

Spätestens 100 ms nach Annahme des Client-Intents erscheint sicht- oder
hörbares Feedback. Es ist als „gesendet“ markiert und darf keinen Sim-Erfolg
vortäuschen. Das spätere deterministische `CommandResult` bestätigt oder
verwirft die Aktion.

Bei Backpressure oder struktureller Ablehnung wird kein Erfolgsmarker gezeigt;
die Ursache ist lokal lesbar.

## 5. Rebinding

Alle Gameplay-, Kamera- und UI-Kernaktionen sind frei belegbar. Anforderungen:

- Maus/Tastatur getrennt abbildbar;
- Konflikte sichtbar und auflösbar;
- Reset pro Profil und global;
- Overrides in User Settings, niemals im Sim-State;
- laden vor Matchstart und ohne Definitionshash-Änderung.

Rohes Binding-JSON ist kein Replay-/Save-Payload.

## 6. Accessibility

- UI-Skalierung 80–150 %;
- Farb- und Formredundanz für Ziel-/Fehler-/Auswahlzustände;
- reduzierte Shake-/Flash-Settings;
- Bedienung von Pause, zehn manuellen Slots, Quicksave A/B,
  Autosave-Status, Load und Recovery über normale UI.

## 7. Tests

- jede aktive Action besitzt Defaultbinding und Rebind-Pfad;
- Konflikt-/Reset-/Persistenztests;
- keine State-Mutation durch lokale Intents;
- CommandIntent erreicht nur den Ingress;
- verborgenes Picking liefert keine Entity;
- Feedback-Latenz ≤100 ms;
- UI-only G5-Matches ohne Inspector/Console.

## Offene Punkte

- Touch, Controller-first und Kamera-Rotation sind Post-MVP.

## Nächste Schritte

1. Intenttypen gegen Command-Schema v1 in G1 einfrieren.
2. FoW-sicheres Picking in G2 integrieren.
3. Rebinding/Accessibility/Feedback in G4/G5 abnehmen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead UI/UX Designer |
| 1.0.0 | 2026-07-24 | Input auf CommandIntent-Vertrauensgrenze, ≤100-ms-Feedback, Rebinding und MS-1-Accessibility rebaselined | Lead UI/UX Designer |
