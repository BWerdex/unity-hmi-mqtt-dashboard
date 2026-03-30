using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Speedometer gauge — rotates a needle and shows km/h value.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay and OnAlert).
/// </summary>
public class SpeedometerGauge : BaseGauge
{
    [Header("Speedometer Config")]
    [SerializeField] private Transform needleTransform;
    [SerializeField] private float needleMinAngle = 135f;   // angle at 0 km/h (degrees, Z-axis)
    [SerializeField] private float needleMaxAngle = -135f;  // angle at 260 km/h
    [SerializeField] private Image needleImage;
    [SerializeField] private Image arcFillImage;

    // HUD colors
    private static readonly Color NormalColor  = new Color(0f,    1f,    0.8f, 1f);  // #00FFCC
    private static readonly Color AlertColor   = new Color(1f,    0.27f, 0.27f, 1f); // #FF4444

    protected override void Awake()
    {
        base.Awake();
        Label = "SPEED";
        MinValue = 0f;
        MaxValue = 260f;
        AlertThresholdMax = 200f;  // alert above 200 km/h
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.UpdateDisplay to rotate the needle and update text.
    /// </summary>
    protected override void UpdateDisplay(float value)
    {
        // Rotate needle
        if (needleTransform != null)
        {
            float t = GetNormalizedValue(value);
            float angle = Mathf.Lerp(needleMinAngle, needleMaxAngle, t);
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        // Arc fill
        if (arcFillImage != null)
            arcFillImage.fillAmount = GetNormalizedValue(value);

        // Text
        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.OnAlert — turns gauge red above speed limit.
    /// </summary>
    protected override void OnAlert(float value)
    {
        if (needleImage != null) needleImage.color = AlertColor;
        if (arcFillImage != null) arcFillImage.color = AlertColor;
        if (valueText != null) valueText.color = AlertColor;
    }

    protected override void OnAlertCleared()
    {
        if (needleImage != null) needleImage.color = NormalColor;
        if (arcFillImage != null) arcFillImage.color = NormalColor;
        if (valueText != null) valueText.color = Color.white;
    }
}
