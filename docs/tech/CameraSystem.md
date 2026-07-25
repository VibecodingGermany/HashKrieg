# Kamerasystem

**Version:** 1.0.0 | **Status:** verbindlich für MS-1 – G0 aktiv | **Verantwortungsbereich:** Lead UI/UX Designer / Lead Graphics Engineer | **Sprint:** 7

## Zweck

Definiert die MS-1-Kamera als rein präsentationsseitigen Client-State. Die Kamera
darf keine Simulation mutieren und keine verborgenen Informationen aufdecken.

## Abhängigkeiten

- [InputSystem.md](InputSystem.md) – Kamera-Intents und Rebinding
- [FogOfWar.md](FogOfWar.md) – sichtbare Welt- und Minimapdaten
- [../vision/CoreGameplay.md](../vision/CoreGameplay.md) – RTS-Bedienbild
- [../production/MVPContentManifest.md](../production/MVPContentManifest.md) –
  Accessibility-Mindestumfang

## 1. Verantwortungsgrenze

`Nova.Presentation` besitzt Position, Zoom und lokale Übergänge der Kamera.
`Nova.Simulation` kennt keinen Kamera-State. Panning, Zoom, Edge Scroll und
Minimap-Navigation erzeugen keine Commands und erscheinen weder in Replays noch
in State-Hashes.

World Picking verwendet nur Entity-Proxies aus dem gefilterten
Player-Snapshot. Ein Raycast gegen verborgene Unity-Objekte darf niemals eine
Ziel-ID oder andere Information an Input oder UI zurückgeben.

## 2. MS-1-Bedienung

Die Kamera unterstützt:

- Tastatur-Panning,
- mittlere-Maustaste-Drag,
- konfigurierbares Edge Scrolling,
- Mausrad-Zoom,
- Sprung zu Kontrollgruppen und Ereignismarkern sowie
- Klick auf die Minimap innerhalb der Kartenbegrenzung.

Rotation ist für MS-1 nicht erforderlich. Schräge Top-Down-Perspektive,
Zoomgrenzen, Bewegungsgeschwindigkeit und Kartenrand-Padding werden
datengetrieben im Präsentationsprofil geführt, nicht im Sim-State.

## 3. Feedback und Accessibility

Ein gesendeter Befehl erhält innerhalb von höchstens 100 ms einen sicht- oder
hörbaren Marker, auch wenn der Ziel-Tick später ausgeführt wird. Der Marker ist
vorläufig und darf einen späteren deterministischen Fehler nicht als Erfolg
darstellen.

MS-1 bietet:

- UI-Skalierung 80–150 %,
- vollständiges Rebinding der Kameraaktionen,
- reduzierte Erschütterung,
- reduzierte Lichtblitze und
- Farb-/Formredundanz bei Kartenrand-, Ping- und Auswahlzuständen.

Kameraerschütterung ist standardmäßig begrenzt, unterbricht keine Eingabe und
kann vollständig reduziert werden.

## 4. FoW und Minimap

Kamera und Minimap konsumieren ausschließlich den committed Player-Snapshot aus
[FogOfWar.md](FogOfWar.md). Radar schaltet Minimap und Signatur-Pings frei, aber
keine verborgenen Entity-Proxies. Minimap-Klicks erzeugen nur einen lokalen
Navigations-Intent.

## 5. Tests

Pflichtprüfungen:

- keine Assembly-Kante von Simulation zu Presentation;
- keine Sim-Mutation durch jede Kameraaktion;
- Kartenbegrenzung bei allen Zoomstufen und UI-Skalierungen;
- Picking liefert für verborgene Entities keinen Treffer;
- Rebinding-Persistenz und Konflikterkennung;
- Shake-/Flash-Reduktion und
- Command-Feedback-Latenz ≤100 ms im G4-Referenzbuild.

## Offene Punkte

- Freie Rotation, Touch-Steuerung und zusätzliche Cinematic-Kameras sind
  Post-MVP.

## Nächste Schritte

1. Kamera-Intent-API mit [InputSystem.md](InputSystem.md) implementieren.
2. In G4 gegen Glutrinne, Minimap und alle UI-Skalen testen.

## Änderungsverlauf

| Version | Datum | Änderung | Autor |
|---|---|---|---|
| 1.0.0 | 2026-07-24 | Präsentationsgrenze, FoW-sicheres Picking und MS-1-Accessibility festgelegt | Lead UI/UX Designer / Lead Graphics Engineer |
