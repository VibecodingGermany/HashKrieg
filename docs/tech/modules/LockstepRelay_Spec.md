# Modulspezifikation – Multiplayer Command-Relay (`Nova.Networking`)

**Version:** 1.0.0 | **Status:** Freigegeben (Phase 2 / Modul 18) | **Verantwortungsbereich:** Network Architect / Lead Technical Director | **Sprint:** Phase 2 (Modul 18)

## Zweck

Dieses Dokument beschreibt das **Multiplayer Command-Relay System** von *Project Nova*. Das Modul serialisiert deterministische Befehlspakete (`CommandEnvelopeNetPacket`) in kompakte 37-Byte-Binärpuffer, puffert eingehende Befehle pro Turn-Tick in einem `LockstepRelayBuffer` und prüft `StateHash`-Übereinstimmungen zur Multiplayer-Desync-Erkennung.

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
