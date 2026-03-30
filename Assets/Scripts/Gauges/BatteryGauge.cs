using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Battery gauge — horizontal bar matching Figma design.
/// Green bar with icon background, shows SoC percentage.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay, OnAlert).
/// </summary>
public class BatteryGauge : BaseGauge
{
    [Header("Bar References")]
    [SerializeField] private Image fillBar;
    [SerializeField] private Image iconBg;

    private static readonly Color BarColor   = new Color(0f,     0.784f, 0.325f, 1f); // #00C853 green
    private static readonly Color AlertColor = new Color(1f,     0.267f, 0.267f, 1f); // #FF4444
    private static readonly Color TrackColor = new Color(0.173f, 0.192f, 0.247f, 1f); // #2C3140

    protected override void Awake()
    {
        base.Awake();
        Label = "BATTERY";
        MinValue = 0f;
        MaxValue = 100f;
        AlertThresholdMin = 15f;
        if (fillBar != null) fillBar.color = BarColor;
    }

    /// <summary>Polymorphism: fills green bar proportionally to battery %.</summary>
    protected override void UpdateDisplay(float value)
    {
        if (fillBar != null)
            fillBar.fillAmount = GetNormalizedValue(value);
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>Polymorphism: turns red on low battery.</summary>
    protected override void OnAlert(float value)
    {
        if (fillBar   != null) fillBar.color   = AlertColor;
        if (valueText != null) valueText.color  = AlertColor;
        if (iconBg    != null) iconBg.color     = AlertColor;
    }

    protected override void OnAlertCleared()
    {
        if (fillBar   != null) fillBar.color   = BarColor;
        if (valueText != null) valueText.color  = Color.white;
        if (iconBg    != null) iconBg.color     = new Color(0f, 0.294f, 0.157f, 1f); // dark green
    }
}
