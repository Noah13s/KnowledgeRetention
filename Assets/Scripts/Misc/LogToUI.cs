using TMPro;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.Events;
using System.Collections.Generic;

public class LogToUI : MonoBehaviour
{
    [Header("UI Settings")]
    [SerializeField] private Transform container;
    [SerializeField] private TextMeshProUGUI logPrefab; // Using a prefab is better for styling
    [SerializeField] private int maxMessages = 50;

    [Header("Filters")]
    [SerializeField] private bool showLogs = true;
    [SerializeField] private bool showWarnings = false;
    [SerializeField] private bool showErrors = true;

    [Header("Log Triggers")]
    [SerializeField] private List<LogTrigger> customTriggers;

    [System.Serializable]
    public class LogTrigger
    {
        public string messageToMatch;
        public UnityEvent onTriggered;
    }

    private readonly ConcurrentQueue<(string message, LogType type)> _queue = new();
    private readonly List<GameObject> _activeMessages = new();

    void OnEnable()
    {
        Application.logMessageReceivedThreaded += HandleLogThreaded;
    }

    void OnDisable()
    {
        Application.logMessageReceivedThreaded -= HandleLogThreaded;
    }

    void Update()
    {
        while (_queue.TryDequeue(out var item))
        {
            ProcessLog(item.message, item.type);
        }
    }

    private void HandleLogThreaded(string logString, string stackTrace, LogType type)
    {
        // Filter logic
        if (type == LogType.Log && !showLogs) return;
        if (type == LogType.Warning && !showWarnings) return;
        if ((type == LogType.Error || type == LogType.Exception) && !showErrors) return;

        _queue.Enqueue((logString, type));
    }

    private void ProcessLog(string logString, LogType type)
    {
        SpawnLogUI(logString, type);

        // Check against custom triggers
        foreach (var trigger in customTriggers)
        {
            if (logString.Contains(trigger.messageToMatch))
            {
                trigger.onTriggered?.Invoke();
            }
        }
    }

    private void SpawnLogUI(string logString, LogType type)
    {
        TextMeshProUGUI tmp;

        if (logPrefab != null)
        {
            tmp = Instantiate(logPrefab, container, false);
        }
        else
        {
            // Fallback if no prefab is assigned
            var go = new GameObject("LogEntry", typeof(TextMeshProUGUI));
            go.transform.SetParent(container, false);
            tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = 18;
        }

        tmp.text = FormatLog(logString, type);
        _activeMessages.Add(tmp.gameObject);

        // Maintain max message count to prevent memory/UI bloat
        if (_activeMessages.Count > maxMessages)
        {
            Destroy(_activeMessages[0]);
            _activeMessages.RemoveAt(0);
        }
    }

    private string FormatLog(string msg, LogType type)
    {
        string color = type switch
        {
            LogType.Error or LogType.Exception => "#FF4C4C",
            LogType.Warning => "#FFCC00",
            LogType.Log => "#FFFFFF",
            _ => "#AAAAAA"
        };

        return $"<color={color}>[{type}] {msg}</color>";
    }
}