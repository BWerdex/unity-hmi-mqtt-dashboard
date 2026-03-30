using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// RPM gauge — rotates needle and colors the arc red in the redline zone.
/// Demonstrates: Inheritance (extends BaseGauge), Polymorphism (overrides UpdateDisplay and OnAlert).
/// </summary>
public class RPMGauge : BaseGauge
{
    [Header("RPM Config")]
    [SerializeField] private Transform needleTransform;
    [SerializeField] private float needleMinAngle = 135f;
    [SerializeField] private float needleMaxAngle = -135f;
    [SerializeField] private Image needleImage;
    [SerializeField] private Image arcFillImage;
    [SerializeField] private Image redlineOverlayImage;  // static red zone overlay on the arc

    private static readonly Color NormalColor  = new Color(0f,    1f,    0.8f, 1f);  // #00FFCC
    private static readonly Color RedlineColor = new Color(1f,    0.27f, 0.27f, 1f); // #FF4444

    protected override void Awake()
    {
        base.Awake();
        Label = "RPM";
        MinValue = 0f;
        MaxValue = 8000f;
        AlertThresholdMax = 6500f;  // redline at 6500 RPM
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.UpdateDisplay — rotates needle and fills arc.
    /// </summary>
    protected override void UpdateDisplay(float value)
    {
        if (needleTransform != null)
        {
            float t = GetNormalizedValue(value);
            float angle = Mathf.Lerp(needleMinAngle, needleMaxAngle, t);
            needleTransform.localRotation = Quaternion.Euler(0f, 0f, angle);
        }

        if (arcFillImage != null)
            arcFillImage.fillAmount = GetNormalizedValue(value);

        if (valueText != null)
            valueText.text = Mathf.RoundToInt(value).ToString();
    }

    /// <summary>
    /// Polymorphism: overrides BaseGauge.OnAlert — turns gauge red at redline.
    /// </summary>
    protected override void OnAlert(float value)
    {
        if (needleImage != null) needleImage.color = RedlineColor;
        if (arcFillImage != null) arcFillImage.color = RedlineColor;
        if (valueText != null) valueText.color = RedlineColor;
    }

    protected override void OnAlertCleared()
    {
        if (needleImage != null) needleImage.color = NormalColor;
        if (arcFillImage != null) arcFillImage.color = NormalColor;
        if (valueText != null) valueText.color = Color.white;
    }
}
