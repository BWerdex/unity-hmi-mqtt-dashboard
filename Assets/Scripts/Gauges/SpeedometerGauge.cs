using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Speedometer — circular arc gauge matching Figma Modern Car Gauge Cluster design.
/// Blue arc fill on dark grey ring track, large white number in center.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay, OnAlert).
/// </summary>
public class SpeedometerGauge : BaseGauge
{
    [Header("Arc References")]
    [SerializeField] private Image trackRing;
    [SerializeField] private Image arcFill;
    [SerializeField] private float arcFillRange = 0.75f; // 270 degrees of 360

    private static readonly Color ArcColor   = new Color(0.294f, 0.620f, 0.973f, 1f); // #4B9EF8 blue
    private static readonly Color AlertColor = new Color(1f,     0.267f, 0.267f, 1f); // #FF4444
    private static readonly Color TrackColor = new Color(0.173f, 0.192f, 0.247f, 1f); // #2C3140

    protected override void Awake()
    {
        base.Awake();
        Label = "SPEED";
        MinValue = 0f;
        MaxValue = 260f;
        AlertThresholdMax = 200f;
        if (trackRing != null) trackRing.color = TrackColor;
        if (arcFill   != null) arcFill.color   = ArcColor;
    }

    /// <summary>Polymorphism: fills the arc proportionally to speed value.</summary>
    protected override void UpdateDisplay(float value)
    {
        if (arcFill != null)
            arcFill.fillAmount = GetNormalizedValue(value) * arcFillRange;
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>Polymorphism: turns arc red above 200 km/h.</summary>
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
