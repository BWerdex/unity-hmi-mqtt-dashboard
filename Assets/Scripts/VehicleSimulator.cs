using System.Collections;
using UnityEngine;

/// <summary>
/// Simulates realistic vehicle telemetry and publishes it to the MQTT broker.
/// Cycles through IDLE → ACCELERATING → CRUISING → BRAKING states automatically.
/// Attach to a GameObject in the scene and enable/disable via the Inspector toggle.
/// </summary>
public class VehicleSimulator : MonoBehaviour
{
    [Header("Simulation Config")]
    [SerializeField] private float publishIntervalSeconds = 0.1f;
    [SerializeField] private bool autoStart = true;

    // ── Private simulation state ─────────────────────────────────────────────
    private enum DriveState { Idle, Accelerating, Cruising, Braking }

    private DriveState _state = DriveState.Idle;
    private float _speed    = 0f;
    private float _rpm      = 800f;
    private float _battery  = 85f;
    private float _temp     = 60f;

    // State timers
    private float _stateTimer = 0f;
    private float _stateDuration = 4f;

    // Per-state target ranges
    private float _targetSpeed;
    private float _targetRPM;

    private void Start()
    {
        if (autoStart)
            StartCoroutine(SimulationLoop());
    }

    public void StartSimulation()  => StartCoroutine(SimulationLoop());
    public void StopSimulation()   => StopAllCoroutines();

    // ── Main loop ────────────────────────────────────────────────────────────
    private IEnumerator SimulationLoop()
    {
        TransitionToState(DriveState.Idle);

        while (true)
        {
            _stateTimer += publishIntervalSeconds;

            UpdatePhysics();
            PublishData();

            if (_stateTimer >= _stateDuration)
                TransitionToNextState();

            yield return new WaitForSeconds(publishIntervalSeconds);
        }
    }

    // ── State machine ────────────────────────────────────────────────────────
    private void TransitionToNextState()
    {
        DriveState next = _state switch
        {
            DriveState.Idle         => DriveState.Accelerating,
            DriveState.Accelerating => DriveState.Cruising,
            DriveState.Cruising     => Random.value > 0.3f ? DriveState.Braking : DriveState.Accelerating,
            DriveState.Braking      => _speed < 20f ? DriveState.Idle : DriveState.Cruising,
            _                       => DriveState.Idle
        };
        TransitionToState(next);
    }

    private void TransitionToState(DriveState next)
    {
        _state = next;
        _stateTimer = 0f;

        switch (_state)
        {
            case DriveState.Idle:
                _targetSpeed    = Random.Range(0f, 5f);
                _targetRPM      = Random.Range(750f, 950f);
                _stateDuration  = Random.Range(2f, 4f);
                break;

            case DriveState.Accelerating:
                _targetSpeed    = Random.Range(80f, 200f);
                _targetRPM      = Random.Range(3500f, 7000f);
                _stateDuration  = Random.Range(4f, 8f);
                break;

            case DriveState.Cruising:
                _targetSpeed    = Random.Range(100f, 160f);
                _targetRPM      = Random.Range(2500f, 4000f);
                _stateDuration  = Random.Range(5f, 10f);
                break;

            case DriveState.Braking:
                _targetSpeed    = Random.Range(0f, 40f);
                _targetRPM      = Random.Range(800f, 2000f);
                _stateDuration  = Random.Range(2f, 5f);
                break;
        }

        Debug.Log($"[VehicleSimulator] State → {_state}  target speed={_targetSpeed:F0}  rpm={_targetRPM:F0}");
    }

    // ── Physics update ───────────────────────────────────────────────────────
    private void UpdatePhysics()
    {
        float t = publishIntervalSeconds;

        // Speed and RPM converge toward targets at different rates per state
        float speedRate = _state == DriveState.Braking ? 18f : 8f;
        float rpmRate   = 10f;

        _speed = Mathf.MoveTowards(_speed, _targetSpeed, speedRate * t);
        _rpm   = Mathf.MoveTowards(_rpm,   _targetRPM,   rpmRate * 100f * t);

        // Battery drains faster at high speed/RPM, charges a tiny bit at idle
        float drainRate = _state == DriveState.Idle ? -0.002f
                        : Mathf.Lerp(0.005f, 0.04f, _speed / 260f);
        _battery = Mathf.Clamp(_battery - drainRate, 0f, 100f);

        // Temperature rises with RPM, cools slowly toward ambient (60°C)
        float heatTarget = Mathf.Lerp(60f, 115f, _rpm / 8000f);
        _temp = Mathf.MoveTowards(_temp, heatTarget, 0.5f * t);

        // Add small noise for realism
        _speed   += Random.Range(-0.5f, 0.5f);
        _rpm     += Random.Range(-50f, 50f);
        _temp    += Random.Range(-0.2f, 0.2f);

        _speed   = Mathf.Clamp(_speed,   0f, 260f);
        _rpm     = Mathf.Clamp(_rpm,     0f, 8000f);
        _temp    = Mathf.Clamp(_temp,   40f, 120f);
    }

    // ── MQTT publish ─────────────────────────────────────────────────────────
    private void PublishData()
    {
        if (MQTTManager.Instance == null || !MQTTManager.Instance.IsConnected)
            return;

        string json = $"{{\"speed\":{_speed:F1},\"rpm\":{_rpm:F0},\"battery\":{_battery:F1},\"temp\":{_temp:F1}}}";
        MQTTManager.Instance.Publish("hmi/vehicle", json);
    }

    // ── Inspector context menu helpers ───────────────────────────────────────
    [ContextMenu("Simulate: Emergency Brake")]
    private void SimulateEmergencyBrake() => TransitionToState(DriveState.Braking);

    [ContextMenu("Simulate: Full Acceleration")]
    private void SimulateFullAcceleration() => TransitionToState(DriveState.Accelerating);

    [ContextMenu("Simulate: Idle")]
    private void SimulateIdle() => TransitionToState(DriveState.Idle);
}
