# Savegames

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead Technical Director / Lead UI Engineer | **Sprint:** 7

## Zweck

Definiert MS-1-Speicherplätze, konsistente Capture-Punkte, atomisches Schreiben,
Backup-Recovery und getestete Fortsetzung. Savegames verwenden den kanonischen
Snapshot und den getrennten KI-Sidecar.

## Abhängigkeiten

- [Serialization.md](Serialization.md) – Snapshotbytes, Fingerprint und Limits
- [GameState.md](GameState.md) – Sim-State und KI-Sidecar-Grenze
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md)
- [../production/DecisionLog.md](../production/DecisionLog.md) – D-056/D-057

## 1. Slotmodell

| Typ | Anzahl | Rotation |
|---|---:|---|
| manuell | 10 | keine; expliziter Slot |
| Quicksave | 2 | A ↔ B |
| Autosave | 3 | ältester gültiger Slot wird ersetzt |

Autosaves werden alle fünf Minuten Matchzeit angefordert. Pause stoppt
Sim-Ticks und damit den Matchzeit-Timer. Ein manueller Save ist aus dem
Pause-/Matchmenü verfügbar.

## 2. Konsistenter Capture

Save-Anforderungen sind Session-Aktionen, keine frei serialisierten
Simulations-Commands. Der Host:

1. wartet auf eine abgeschlossene Tickgrenze,
2. friert den kanonischen `SimState`,
3. friert den zum selben committed View-Tick gehörenden `AiSidecar`,
4. serialisiert beide mit gemeinsamen Fingerprint- und Tickmetadaten und
5. setzt die Simulation erst nach sicherem Buffer-Capture fort.

UI, Renderer und Kamera werden nicht gespeichert. Pending Commands,
Sequence/Dedupe, Allocator und Deferred Queues sind Teil des Sim-Snapshots.

## 3. Dateiinhalte

Eine Save-Datei enthält:

- Formatversion 1.0 und Slottyp/-index,
- Fingerprint und Save-Tick,
- kanonischen Sim-Snapshot,
- versionierten AI-Sidecar,
- Block-/State-/File-Hashes und
- nicht autoritative Anzeige-Metadaten außerhalb des Sim-State.

Die unkomprimierte Zielgröße ist ≤4 MiB; Dateien über 64 MiB werden vor
Payload-Parse abgelehnt.

## 4. Atomisches Schreiben

Für jeden Slot existieren Primary und Backup:

1. neue Bytes vollständig in eine temporäre Datei desselben Verzeichnisses
   schreiben und flushen;
2. vorhandenes gültiges Primary als Backup erhalten;
3. temporäre Datei atomisch als Primary ersetzen;
4. temporäre Reste nach erfolgreichem Start bereinigen.

Ein Schreibfehler darf weder das letzte gültige Primary noch Backup zerstören.
Quicksave-/Autosave-Rotation wechselt erst nach erfolgreichem atomischem
Commit.

## 5. Laden und Recovery

Laden prüft in dieser Reihenfolge:

1. Hardcap und Envelope,
2. File-/Blockhashes,
3. Fingerprint beziehungsweise registrierte Save-Migration,
4. Sim-Snapshot und AI-Sidecar vollständig in temporären Objekten,
5. Roundtrip-/Invarianten.

Ist Primary beschädigt oder unvollständig, versucht der Host das Backup und
meldet den Recovery-Fall sichtbar. Sind beide ungültig, bleibt die aktuelle
Session unverändert und der Slot wird nicht als erfolgreich geladen
ausgegeben.

## 6. Kompatibilität

Pre-G1-Prototyp-Saves sind unsupported. Nach G1 darf nur eine explizite,
getestete Migration einen älteren Save-Fingerprint akzeptieren. Replays werden
nicht über Save-Migrationen geladen.

## 7. Pflichtprüfungen

- alle zehn manuellen Slots;
- Quicksave-Reihenfolge A/B über mindestens vier Saves;
- drei Autosaves über mindestens fünf Intervalle;
- Strom-/Prozessabbruch vor und während atomischem Replace;
- beschädigtes Primary mit erfolgreichem Backup-Recovery;
- beide Kopien beschädigt ohne Sessionmutation;
- Save/Load unmittelbar vor und nach FoW-Recompute;
- AI-Fortsetzung und gequeute Commands über mindestens 1.000 Ticks;
- Autosave-Punkte 5–45 Minuten in G5.

## Offene Punkte

- Cloud-Saves und Steam sind Post-MVP.
- Kompression wird erst nach G1-Größenmessung entschieden.

## Nächste Schritte

1. Slot-/Recovery-Tests vor UI-Anbindung implementieren.
2. G3 AI-Sidecar-Fortsetzung nachweisen.
3. G4 UI-only Save/Load/Recovery integrieren.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 0.1.0 | 2026-07-21 | Erstfassung | Lead Technical Director |
| 1.0.0 | 2026-07-24 | Zehn manuelle Slots, Quicksave A/B, drei Autosaves, kanonischen Snapshot, AI-Sidecar und Backup-Recovery festgelegt | Lead Technical Director / Lead UI Engineer |
