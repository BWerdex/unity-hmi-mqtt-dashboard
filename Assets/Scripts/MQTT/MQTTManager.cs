using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

/// <summary>
/// Singleton MQTT manager — wraps the M2MQTT library behind a clean, simple API.
/// Demonstrates: Abstraction (hides M2MQTT complexity), Singleton pattern.
///
/// THREAD SAFETY: M2MQTT fires message callbacks on a background thread.
/// Incoming messages are queued and dispatched on the Unity main thread in Update().
/// </summary>
public class MQTTManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static MQTTManager Instance { get; private set; }

    // ── Config ───────────────────────────────────────────────────────────────
    [Header("MQTT Config")]
    [SerializeField] private string brokerHost = "localhost";
    [SerializeField] private int brokerPort = 1883;
    [SerializeField] private string clientId = "UnityHMI";
    [SerializeField] private bool connectOnStart = true;

    // ── Events ───────────────────────────────────────────────────────────────
    /// <summary>Fired on the Unity main thread when a message arrives. Args: (topic, payload).</summary>
    public event Action<string, string> OnMessageReceived;

    // ── Private state ────────────────────────────────────────────────────────
    private MqttClient _client;
    private readonly Queue<(string topic, string payload)> _messageQueue = new();
    private readonly object _queueLock = new object();

    // ── Unity lifecycle ──────────────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (connectOnStart)
            Connect(brokerHost, brokerPort);
    }

    private void Update()
    {
        // Drain the thread-safe queue on the main thread before firing Unity events
        lock (_queueLock)
        {
            while (_messageQueue.Count > 0)
            {
                var (topic, payload) = _messageQueue.Dequeue();
                OnMessageReceived?.Invoke(topic, payload);
            }
        }
    }

    private void OnDestroy()
    {
        Disconnect();
    }

    // ── Public API (Abstraction) ─────────────────────────────────────────────

    /// <summary>Connect to an MQTT broker. All library setup is hidden here.</summary>
    public void Connect(string host, int port = 1883)
    {
        try
        {
            _client = new MqttClient(host, port, false, null, null, MqttSslProtocols.None);
            _client.MqttMsgPublishReceived += OnLibraryMessageReceived;

            string id = clientId + "_" + Guid.NewGuid().ToString("N").Substring(0, 6);
            _client.Connect(id);

            Debug.Log($"[MQTTManager] Connected to {host}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[MQTTManager] Connection failed: {e.Message}");
        }
    }

    /// <summary>Subscribe to an MQTT topic.</summary>
    public void Subscribe(string topic)
    {
        if (_client == null || !_client.IsConnected)
        {
            Debug.LogWarning("[MQTTManager] Cannot subscribe — not connected.");
            return;
        }

        _client.Subscribe(new[] { topic }, new[] { MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE });
        Debug.Log($"[MQTTManager] Subscribed to: {topic}");
    }

    /// <summary>Publish a string payload to a topic.</summary>
    public void Publish(string topic, string payload)
    {
        if (_client == null || !_client.IsConnected)
        {
            Debug.LogWarning("[MQTTManager] Cannot publish — not connected.");
            return;
        }

        _client.Publish(topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_LEAST_ONCE, false);
    }

    /// <summary>Disconnect from the broker.</summary>
    public void Disconnect()
    {
        if (_client != null && _client.IsConnected)
        {
            _client.Disconnect();
            Debug.Log("[MQTTManager] Disconnected.");
        }
    }

    public bool IsConnected => _client != null && _client.IsConnected;

    // ── Private: background thread callback ──────────────────────────────────
    private void OnLibraryMessageReceived(object sender, MqttMsgPublishEventArgs e)
    {
        string topic   = e.Topic;
        string payload = Encoding.UTF8.GetString(e.Message);

        // Queue for main-thread dispatch (Unity API is not thread-safe)
        lock (_queueLock)
        {
            _messageQueue.Enqueue((topic, payload));
        }
    }
}
