using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Orchestrates the HMI — wires MQTTManager events to gauge updates.
/// Demonstrates: Polymorphism (List of BaseGauge, each UpdateDisplay() override fires transparently),
/// Abstraction (RefreshAll hides per-gauge logic behind one call).
/// </summary>
public class HMIController : MonoBehaviour
{
    [Header("MQTT")]
    [SerializeField] private string vehicleTopic = "hmi/vehicle";

    [Header("Gauges — assign in Inspector")]
    [SerializeField] private SpeedometerGauge  speedGauge;
    [SerializeField] private RPMGauge          rpmGauge;
    [SerializeField] private BatteryGauge      batteryGauge;
    [SerializeField] private TemperatureGauge  tempGauge;

    // Polymorphism: stored as base type — concrete overrides fire automatically
    private List<BaseGauge> _allGauges = new();

    private void Start()
    {
        // Register gauges into the polymorphic list
        if (speedGauge   != null) _allGauges.Add(speedGauge);
        if (rpmGauge     != null) _allGauges.Add(rpmGauge);
        if (batteryGauge != null) _allGauges.Add(batteryGauge);
        if (tempGauge    != null) _allGauges.Add(tempGauge);

        // Subscribe to MQTT events
        if (MQTTManager.Instance != null)
        {
            MQTTManager.Instance.OnMessageReceived += OnMQTTMessage;
            MQTTManager.Instance.Subscribe(vehicleTopic);
        }
        else
        {
            Debug.LogWarning("[HMIController] MQTTManager not found. Is it in the scene?");
        }
    }

    private void OnDestroy()
    {
        if (MQTTManager.Instance != null)
            MQTTManager.Instance.OnMessageReceived -= OnMQTTMessage;
    }

    // ── MQTT callback ────────────────────────────────────────────────────────
    private void OnMQTTMessage(string topic, string payload)
    {
        if (topic != vehicleTopic) return;

        VehicleDataModel data = VehicleDataModel.FromJson(payload);
        if (data == null) return;

        RefreshAll(data);
    }

    // ── Abstraction: single call updates all gauges ──────────────────────────
    /// <summary>
    /// High-level method: push new telemetry to every gauge.
    /// Polymorphism in action — each gauge's overridden UpdateDisplay() fires.
    /// </summary>
    private void RefreshAll(VehicleDataModel data)
    {
        foreach (BaseGauge gauge in _allGauges)
        {
            gauge.SetValue(data.GetValueForLabel(gauge.Label));
        }
    }

    // ── Editor testing helper ────────────────────────────────────────────────
    [ContextMenu("Test: Normal Values")]
    private void TestNormal()
    {
        string json = "{\"speed\":120,\"rpm\":3500,\"battery\":72,\"temp\":85}";
        VehicleDataModel data = VehicleDataModel.FromJson(json);
        if (data != null) RefreshAll(data);
    }

    [ContextMenu("Test: Alert Values")]
    private void TestAlert()
    {
        string json = "{\"speed\":220,\"rpm\":7000,\"battery\":10,\"temp\":105}";
        VehicleDataModel data = VehicleDataModel.FromJson(json);
        if (data != null) RefreshAll(data);
    }
}
