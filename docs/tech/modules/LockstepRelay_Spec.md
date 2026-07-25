# Modulspezifikation – Multiplayer Command-Relay (`Nova.Networking`)

**Version:** 1.1.0 | **Status:** historischer Prototyp-/Scaffolding-Stand gemäß D-055 – nicht verbindlich | **Verantwortungsbereich:** Network Architect / Lead Technical Director | **Sprint:** Phase 2 (Modul 18)

## Zweck

Dieses Dokument beschreibt das **Multiplayer Command-Relay System** von *Project Nova*. Das Modul serialisiert deterministische Befehlspakete (`CommandEnvelopeNetPacket`) in kompakte 37-Byte-Binärpuffer, puffert eingehende Befehle pro Turn-Tick in einem `LockstepRelayBuffer` und prüft `StateHash`-Übereinstimmungen zur Multiplayer-Desync-Erkennung.


## Abhängigkeiten

- [../../production/MVPRecoveryPlan.md](../../production/MVPRecoveryPlan.md) – aktiver Gate- und Statusvertrag
- [../../production/DecisionLog.md](../../production/DecisionLog.md) – D-055 bis D-061
- [../ModuleOverview.md](../ModuleOverview.md) – aktive Modul- und State-Hoheit
- [../SimulationCore.md](../SimulationCore.md) und [../Commands.md](../Commands.md) – führende Kernverträge

> **Recovery-Hinweis:** Der folgende Text konserviert den nach D-055 nicht
> abgenommenen Prototyp-/Scaffolding-Stand. Er ist keine Implementierungsfreigabe.
> Bei jedem Konflikt führen die oben verlinkten aktiven Verträge. Eine künftige
> Freigabe erfordert das zuständige Gate, neue Laufzeitevidenz und eine
> inhaltlich rebaselinede Spezifikation.

---

## 1. Modul-Architektur

* **Assembly:** `Nova.Networking.dll` (`noEngineReferences: true`)
* **Paketgröße:** Exakt **37 Bytes** per `CommandEnvelopeNetPacket`.
* **Desync-Erkennung:** Bit-exakter FNV-1a 64-Bit `StateHash`-Vergleich aller Clients pro Frame-Tick.

```text
[ Network Transport / UDP Socket ]
                 │
                 ▼
    [ CommandEnvelopeNetPacket ] (37-Byte Deserialisierung)
                 │
                 ▼
       [ LockstepRelayBuffer ] ──► Check IsTickReady()
                 │
                 ├── VerifyDesyncHashes() ──► Log Desync Warning if Hash Mismatch
                 └── Execute Frame Turn ──► CommandProcessorSystem
```

---

## 2. Qualitätssicherung & Tests

* **Unit Tests:** [`LockstepRelayBufferTests.cs`](../../../Assets/Tests/EditMode/Networking/LockstepRelayBufferTests.cs) (Binär-Serialisierung, Turn-Tick-Vollständigkeitsprüfung & Desync-Erkennung).

## Offene Punkte

- Welche Teile dieses Prototyps nach Abgleich mit D-056 bis D-061 wiederverwendet
  werden, entscheidet erst die Implementierung im zuständigen Gate.

## Nächste Schritte

1. Bestand gegen die aktiven Kern-, Inhalts- und Gate-Verträge prüfen.
2. Widersprechende APIs und Werte nicht übernehmen.
3. Erst nach bestandener Gate-Evidenz eine neue verbindliche Revision erstellen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Historischen Prototyp-/Scaffolding-Stand dokumentiert | Modulverantwortliche |
| 1.1.0 | 2026-07-24 | Freigabe gemäß D-055 entzogen und aktive Recovery-Verträge als führend verankert | Lead Technical Director |
