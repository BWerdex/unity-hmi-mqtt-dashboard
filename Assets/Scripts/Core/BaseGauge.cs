using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Abstract base class for all HMI gauges.
/// Demonstrates: Encapsulation (private fields + properties), Abstraction (SetValue hides internals),
/// and acts as the base for Inheritance and Polymorphism.
/// </summary>
public abstract class BaseGauge : MonoBehaviour
{
    // ── Serialized config ────────────────────────────────────────────────────
    [Header("Gauge Config")]
    [SerializeField] protected float minValue = 0f;
    [SerializeField] protected float maxValue = 100f;
    [SerializeField] protected string gaugeLabel = "GAUGE";

    [Header("UI References")]
    [SerializeField] protected TextMeshProUGUI valueText;
    [SerializeField] protected TextMeshProUGUI labelText;
    [SerializeField] protected Image backgroundImage;

    // ── Private backing fields (Encapsulation) ───────────────────────────────
    private float _currentValue;
    private float _alertThresholdMin = float.MinValue;
    private float _alertThresholdMax = float.MaxValue;
    private bool _alertOnExceedMax = true;  // true = alert above max threshold, false = alert below min

    // ── Properties (Encapsulation: controlled access) ────────────────────────
    public float MinValue
    {
        get => minValue;
        set => minValue = value;
    }

    public float MaxValue
    {
        get => maxValue;
        set => maxValue = value;
    }

    public float CurrentValue
    {
        get => _currentValue;
        private set => _currentValue = Mathf.Clamp(value, minValue, maxValue);
    }

    public string Label
    {
        get => gaugeLabel;
        protected set => gaugeLabel = value;
    }

    public float AlertThresholdMin
    {
        get => _alertThresholdMin;
        protected set => _alertThresholdMin = value;
    }

    public float AlertThresholdMax
    {
        get => _alertThresholdMax;
        protected set => _alertThresholdMax = value;
    }

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    protected virtual void Awake()
    {
        if (labelText != null)
            labelText.text = gaugeLabel;
    }

    // ── Public API (Abstraction) ─────────────────────────────────────────────
    /// <summary>
    /// High-level method: update gauge value. Handles clamping, display refresh,
    /// and alert triggering automatically — callers don't need to know these details.
    /// </summary>
    public void SetValue(float value)
    {
        CurrentValue = value;                       // property setter clamps the value
        UpdateDisplay(CurrentValue);                // polymorphic display update
        if (IsInAlertState(CurrentValue))
            OnAlert(CurrentValue);
        else
            OnAlertCleared();
    }

    // ── Abstract / Virtual methods (Polymorphism hooks) ──────────────────────
    /// <summary>Override in each gauge to update the visual representation.</summary>
    protected abstract void UpdateDisplay(float value);

    /// <summary>Override to define gauge-specific alert behavior (e.g. flash red).</summary>
    protected virtual void OnAlert(float value) { }

    /// <summary>Override to reset visuals when alert condition clears.</summary>
    protected virtual void OnAlertCleared() { }

    // ── Private helpers ──────────────────────────────────────────────────────
    private bool IsInAlertState(float value)
    {
        bool aboveMax = _alertThresholdMax < float.MaxValue && value >= _alertThresholdMax;
        bool belowMin = _alertThresholdMin > float.MinValue && value <= _alertThresholdMin;
        return aboveMax || belowMin;
    }

    /// <summary>Normalize value to 0-1 range for use in display calculations.</summary>
    protected float GetNormalizedValue(float value)
    {
        return Mathf.InverseLerp(minValue, maxValue, value);
    }
}
