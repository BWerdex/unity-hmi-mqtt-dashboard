using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Fuel gauge — horizontal bar matching Figma design.
/// Amber/orange bar with icon background, shows fuel level as percentage.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay, OnAlert).
/// </summary>
public class FuelGauge : BaseGauge
{
    [Header("Bar References")]
    [SerializeField] private Image fillBar;
    [SerializeField] private Image iconBg;

    private static readonly Color BarColor   = new Color(0.961f, 0.651f, 0.137f, 1f); // #F5A623 amber
    private static readonly Color AlertColor = new Color(1f,     0.267f, 0.267f, 1f); // #FF4444

    protected override void Awake()
    {
        base.Awake();
        Label = "FUEL";
        MinValue = 0f;
        MaxValue = 100f;
        AlertThresholdMin = 15f;
        if (fillBar != null) fillBar.color = BarColor;
    }

    /// <summary>Polymorphism: fills amber bar proportionally to fuel level.</summary>
    protected override void UpdateDisplay(float value)
    {
        if (fillBar != null)
            fillBar.fillAmount = GetNormalizedValue(value);
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>Polymorphism: turns red on low fuel.</summary>
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
        if (iconBg    != null) iconBg.color     = new Color(0.353f, 0.196f, 0f, 1f); // dark amber
    }
}
