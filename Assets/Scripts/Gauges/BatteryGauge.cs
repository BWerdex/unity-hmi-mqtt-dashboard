using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battery / State of Charge gauge — horizontal fill bar with color coding.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay and OnAlert).
/// </summary>
public class BatteryGauge : BaseGauge
{
    [Header("Battery Config")]
    [SerializeField] private Image fillBarImage;
    [SerializeField] private Image batteryIconImage;

    private static readonly Color HighColor    = new Color(0f,    1f,    0.8f, 1f);  // #00FFCC cyan
    private static readonly Color MediumColor  = new Color(1f,    0.67f, 0f,   1f);  // #FFAA00 amber
    private static readonly Color LowColor     = new Color(1f,    0.27f, 0.27f, 1f); // #FF4444 red

    protected override void Awake()
    {
        base.Awake();
        Label = "BATTERY";
        MinValue = 0f;
        MaxValue = 100f;
        AlertThresholdMin = 15f;  // alert below 15%
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.UpdateDisplay — fills bar and picks color by SoC level.
    /// </summary>
    protected override void UpdateDisplay(float value)
    {
        float normalized = GetNormalizedValue(value);

        if (fillBarImage != null)
        {
            fillBarImage.fillAmount = normalized;
            fillBarImage.color = GetColorForLevel(value);
        }

        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(value)}%";
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.OnAlert — pulses red icon on low battery.
    /// </summary>
    protected override void OnAlert(float value)
    {
        if (batteryIconImage != null) batteryIconImage.color = LowColor;
        if (valueText != null) valueText.color = LowColor;
    }

    protected override void OnAlertCleared()
    {
        if (batteryIconImage != null) batteryIconImage.color = Color.white;
        if (valueText != null) valueText.color = Color.white;
    }

    // Private helper — color logic is hidden inside the class (Encapsulation)
    private Color GetColorForLevel(float value)
    {
        if (value >= 50f) return HighColor;
        if (value >= 20f) return MediumColor;
        return LowColor;
    }
}
