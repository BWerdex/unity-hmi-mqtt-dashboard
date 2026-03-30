using System;
using UnityEngine;

/// <summary>
/// Encapsulated data model for vehicle telemetry received via MQTT.
/// Demonstrates: Encapsulation (private fields + validated properties),
/// Abstraction (FromJson hides deserialization detail).
/// </summary>
[Serializable]
public class VehicleDataModel
{
    // ── Private backing fields (Encapsulation) ───────────────────────────────
    // Note: JsonUtility requires these to be public or [SerializeField] for deserialization.
    // We use a two-step approach: deserialize into a raw DTO then copy with validation.
    [SerializeField] private float speed;
    [SerializeField] private float rpm;
    [SerializeField] private float battery;
    [SerializeField] private float temp;

    // ── Validated Properties ─────────────────────────────────────────────────
    public float Speed
    {
        get => speed;
        set => speed = Mathf.Clamp(value, 0f, 260f);
    }

    public float RPM
    {
        get => rpm;
        set => rpm = Mathf.Clamp(value, 0f, 8000f);
    }

    public float Battery
    {
        get => battery;
        set => battery = Mathf.Clamp(value, 0f, 100f);
    }

    public float Temperature
    {
        get => temp;
        set => temp = Mathf.Clamp(value, -40f, 150f);
    }

    // ── Factory method (Abstraction over JsonUtility) ────────────────────────
    /// <summary>
    /// Parse a JSON string into a validated VehicleDataModel.
    /// Hides JsonUtility complexity from the caller.
    /// </summary>
    public static VehicleDataModel FromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[VehicleDataModel] Received null or empty JSON.");
            return null;
        }

        try
        {
            VehicleDataModel raw = JsonUtility.FromJson<VehicleDataModel>(json);

            // Re-assign through properties to apply clamping validation
            raw.Speed       = raw.speed;
            raw.RPM         = raw.rpm;
            raw.Battery     = raw.battery;
            raw.Temperature = raw.temp;

            return raw;
        }
        catch (Exception e)
        {
            Debug.LogError($"[VehicleDataModel] JSON parse error: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Returns the appropriate value for a gauge identified by its label.
    /// Used by HMIController for polymorphic gauge updates.
    /// </summary>
    public float GetValueForLabel(string label)
    {
        return label.ToUpper() switch
        {
            "SPEED"       => Speed,
            "RPM"         => RPM,
            "BATTERY"     => Battery,
            "TEMPERATURE" => Temperature,
            _             => 0f
        };
    }

    public override string ToString()
        => $"Speed={Speed} RPM={RPM} Battery={Battery} Temp={Temperature}";
}
