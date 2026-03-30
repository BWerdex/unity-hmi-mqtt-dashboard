# Unity HMI MQTT Dashboard

Real-time automotive HMI dashboard built with **Unity 6 (URP)** and **MQTT**, displaying four live gauges:

- Speedometer (km/h)
- RPM Gauge (with redline warning)
- Battery / State of Charge (%)
- Temperature (°C)

## Architecture

Demonstrates all four OOP pillars:

| Pillar | Where |
|--------|-------|
| **Encapsulation** | `BaseGauge` — private fields with validated getters/setters |
| **Abstraction** | `BaseGauge.SetValue()` and `MQTTManager` public API hide implementation details |
| **Inheritance** | `SpeedometerGauge`, `RPMGauge`, `BatteryGauge`, `TemperatureGauge` all extend `BaseGauge` |
| **Polymorphism** | `HMIController` iterates `List<BaseGauge>` — each gauge's `UpdateDisplay()` override runs at runtime |

## Setup

### Prerequisites
- Unity 6 (6000.x) with URP
- [Mosquitto MQTT broker](https://mosquitto.org/download/)
- M2Mqtt.dll in `Assets/Plugins/M2Mqtt/`

### Run locally

1. Install and start Mosquitto:
   ```bash
   mosquitto -v
   ```

2. Open the project in Unity 6, load `Assets/Scenes/MainHMI.unity`

3. Press Play

4. Publish test data:
   ```bash
   mosquitto_pub -h localhost -t "hmi/vehicle" -m "{\"speed\":120,\"rpm\":3500,\"battery\":72,\"temp\":85}"
   ```

5. Publish alert values to test warnings:
   ```bash
   mosquitto_pub -h localhost -t "hmi/vehicle" -m "{\"speed\":220,\"rpm\":7000,\"battery\":10,\"temp\":105}"
   ```

## MQTT Topic Schema

| Topic | Payload |
|-------|---------|
| `hmi/vehicle` | `{"speed": float, "rpm": float, "battery": float, "temp": float}` |

## Visual Design

Dark automotive HUD — background `#0A0A0A`, accent `#00FFCC`, 1920×1080.

## Branch Strategy

| Branch | Purpose |
|--------|---------|
| `main` | Stable demo-ready builds |
| `develop` | Integration branch |
| `feature/project-setup` | Project init and structure |
| `feature/oop-architecture` | Core OOP class hierarchy |
| `feature/mqtt-integration` | MQTT manager and data model |
| `feature/gauge-ui` | Visual gauge prefabs |
