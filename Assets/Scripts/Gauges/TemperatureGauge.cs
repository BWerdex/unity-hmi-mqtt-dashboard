using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Temperature gauge — vertical or arc fill with overheat warning.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay and OnAlert).
/// </summary>
public class TemperatureGauge : BaseGauge
{
    [Header("Temperature Config")]
    [SerializeField] private Image fillBarImage;
    [SerializeField] private Image thermometerIconImage;

    private static readonly Color CoolColor    = new Color(0.27f, 0.67f, 1f,    1f); // #45ABFF blue
    private static readonly Color NormalColor  = new Color(0f,    1f,    0.8f,  1f); // #00FFCC cyan
    private static readonly Color WarmColor    = new Color(1f,    0.67f, 0f,    1f); // #FFAA00 amber
    private static readonly Color OverheatColor= new Color(1f,    0.27f, 0.27f, 1f); // #FF4444 red

    protected override void Awake()
    {
        base.Awake();
        Label = "TEMPERATURE";
        MinValue = 40f;
        MaxValue = 120f;
        AlertThresholdMax = 100f;  // overheat above 100°C
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.UpdateDisplay — fills bar and picks color by temperature zone.
    /// </summary>
    protected override void UpdateDisplay(float value)
    {
        float normalized = GetNormalizedValue(value);

        if (fillBarImage != null)
        {
            fillBarImage.fillAmount = normalized;
            fillBarImage.color = GetColorForTemp(value);
        }

        if (valueText != null)
            valueText.text = $"{Mathf.RoundToInt(value)}°C";
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.OnAlert — turns red on overheat.
    /// </summary>
    protected override void OnAlert(float value)
    {
        if (thermometerIconImage != null) thermometerIconImage.color = OverheatColor;
        if (valueText != null) valueText.color = OverheatColor;
    }

    protected override void OnAlertCleared()
    {
        if (thermometerIconImage != null) thermometerIconImage.color = Color.white;
        if (valueText != null) valueText.color = Color.white;
    }

    private Color GetColorForTemp(float value)
    {
        if (value < 60f)  return CoolColor;
        if (value < 80f)  return NormalColor;
        if (value < 100f) return WarmColor;
        return OverheatColor;
    }
}
