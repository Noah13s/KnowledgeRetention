using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    [System.Serializable]
    public class PanelButtonLink
    {
        public GameObject panel;
        public Button button;
        public UnityEvent onOpen;
        public UnityEvent onClose;

        [HideInInspector] public Color defaultColor;
    }

    [SerializeField] Color selectedColor = Color.blue;
    [SerializeField] bool selectFirstOnStart = true;

    [Header("List of panels and their corresponding buttons")]
    public List<PanelButtonLink> link = new List<PanelButtonLink>();

    public PanelButtonLink currentLink;

    void Awake()
    {
        // Cache default button colors
        foreach (var item in link)
        {
            if (item.button != null)
            {
                var img = item.button.GetComponent<Image>();
                if (img != null)
                {
                    item.defaultColor = img.color;
                }
            }
        }
    }

    void Start()
    {
        // Assign button listeners
        foreach (var item in link)
        {
            if (item.button == null)
                continue;

            var targetLink = item;
            item.button.onClick.AddListener(() => ShowPanel(targetLink));
        }

        if (selectFirstOnStart && link.Count > 0 && link[0].panel != null)
        {
            ShowPanel(link[0]);
        }
        else
        {
            // Make sure only one is active if selectFirstOnStart = false
            HideAll();
        }
    }

    public void ShowPanel(PanelButtonLink _link)
    {
        currentLink = _link;
        var panelToShow = _link.panel;
        foreach (var item in link)
        {
            if (item.panel == null)
                continue;

            bool isTarget = item.panel == panelToShow;
            bool wasActive = item.panel.activeSelf;
            item.panel.SetActive(isTarget);

            // Invoke events
            if (isTarget && !wasActive)
            {
                item.onOpen?.Invoke();
            }
            else if (!isTarget && wasActive)
            {
                item.onClose?.Invoke();
            }

            // Update button color
            if (item.button != null)
            {
                var img = item.button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = isTarget ? selectedColor : item.defaultColor;
                }
            }
        }
    }
    public int GetCurrentIndex()
    {
        if (currentLink == null)
            return -1;

        return link.IndexOf(currentLink);
    }

    void HideAll()
    {
        foreach (var item in link)
        {
            if (item.panel != null)
                item.panel.SetActive(false);

            if (item.button != null)
            {
                var img = item.button.GetComponent<Image>();
                if (img != null)
                {
                    img.color = item.defaultColor;
                }
            }
        }
    }
}
