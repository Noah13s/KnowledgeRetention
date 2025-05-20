using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

[Serializable]
public class NamedEvent
{
    public string eventName;
    public UnityEvent eventAction;
}

public class EventManager : MonoBehaviour
{
    [SerializeField] private List<NamedEvent> events = new List<NamedEvent>();
    private static EventManager instance;

    public static EventManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("EventManager");
                instance = go.AddComponent<EventManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    public void AddListener(string eventName, UnityAction listener)
    {
        NamedEvent namedEvent = events.Find(e => e.eventName == eventName);
        if (namedEvent != null)
        {
            namedEvent.eventAction.AddListener(listener);
        }
    }

    public void TriggerEvent(string eventName)
    {
        NamedEvent namedEvent = events.Find(e => e.eventName == eventName);
        if (namedEvent != null)
        {
            namedEvent.eventAction.Invoke();
        }
    }
}