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
    // ── Raw deserialization fields ───────────────────────────────────────────
    // JsonUtility reliably deserializes public fields in plain C# classes.
    // These are never accessed directly outside this class — all reads go
    // through the validated properties below (Encapsulation).
    public float speed;
    public float rpm;
    public float battery;
    public float temp;

    // ── Validated Properties (Encapsulation) ────────────────────────────────
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
    /// Hides deserialization complexity from the caller.
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
            VehicleDataModel model = JsonUtility.FromJson<VehicleDataModel>(json);

            // Run values through property setters to apply clamping validation
            model.Speed       = model.speed;
            model.RPM         = model.rpm;
            model.Battery     = model.battery;
            model.Temperature = model.temp;

            return model;
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
