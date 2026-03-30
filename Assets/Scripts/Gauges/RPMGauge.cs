using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RPM gauge — circular arc gauge matching Figma Modern Car Gauge Cluster design.
/// Teal/green arc fill on dark grey ring track, shows value as x1000 RPM.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay, OnAlert).
/// </summary>
public class RPMGauge : BaseGauge
{
    [Header("Arc References")]
    [SerializeField] private Image trackRing;
    [SerializeField] private Image arcFill;
    [SerializeField] private float arcFillRange = 0.75f; // 270 degrees of 360

    private static readonly Color ArcColor   = new Color(0f,     0.831f, 0.627f, 1f); // #00D4A0 teal
    private static readonly Color AlertColor = new Color(1f,     0.267f, 0.267f, 1f); // #FF4444
    private static readonly Color TrackColor = new Color(0.173f, 0.192f, 0.247f, 1f); // #2C3140

    protected override void Awake()
    {
        base.Awake();
        Label = "RPM";
        MinValue = 0f;
        MaxValue = 8000f;
        AlertThresholdMax = 6500f;
        if (trackRing != null) trackRing.color = TrackColor;
        if (arcFill   != null) arcFill.color   = ArcColor;
    }

    /// <summary>Polymorphism: fills arc and shows value as x.x (thousands).</summary>
    protected override void UpdateDisplay(float value)
    {
        if (arcFill != null)
            arcFill.fillAmount = GetNormalizedValue(value) * arcFillRange;
        if (valueText != null)
            valueText.text = (value / 1000f).ToString("F1");
    }

    /// <summary>Polymorphism: turns arc red at redline.</summary>
    protected override void OnAlert(float value)
    {
        if (arcFill   != null) arcFill.color   = AlertColor;
        if (valueText != null) valueText.color  = AlertColor;
    }

    protected override void OnAlertCleared()
    {
        if (arcFill   != null) arcFill.color   = ArcColor;
        if (valueText != null) valueText.color  = Color.white;
    }
}
