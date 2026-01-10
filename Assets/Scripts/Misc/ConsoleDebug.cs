using TMPro;
using UnityEngine;
using System.Collections.Concurrent;
using UnityEngine.Events;

public class ConsoleDebug : MonoBehaviour
{
    public Transform container;

    public UnityEvent OnLLMServiceCreated;

    ConcurrentQueue<(string, LogType)> queue = new();

    const string triggerMessage = "LLM service created";

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
        while (queue.TryDequeue(out var item))
        {
            SpawnTMP(item.Item1, item.Item2);

            if (item.Item1 == triggerMessage)
                OnLLMServiceCreated?.Invoke();
        }
    }

    void HandleLogThreaded(string logString, string stackTrace, LogType type)
    {
        if (type == LogType.Warning) return;
        queue.Enqueue((logString, type));
    }

    void SpawnTMP(string logString, LogType type)
    {
        var go = new GameObject("LogMessage", typeof(TextMeshProUGUI));
        var tmp = go.GetComponent<TextMeshProUGUI>();

        tmp.fontSize = 24;
        tmp.richText = true;

        string color = type switch
        {
            LogType.Error or LogType.Exception => "#FF4C4C",
            LogType.Log => "#FFFFFF",
            _ => "#AAAAAA"
        };

        tmp.text = $"<color={color}>[{type}] {logString}</color>";
        go.transform.SetParent(container, false);
    }
}
