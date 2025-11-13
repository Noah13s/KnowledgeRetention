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
    }

    [Header("List of panels and their corresponding buttons")]
    public List<PanelButtonLink> link;

    private void Start()
    {
        // Attach listeners to each button
        foreach (var item in link)
        {
            if (item.button != null)
            {
                GameObject targetPanel = item.panel; // capture local reference
                item.button.onClick.AddListener(() => ShowPanel(targetPanel));
            }
        }

        // Optionally, show only the first panel at start
        if (link.Count > 0 && link[0].panel != null)
        {
            ShowPanel(link[0].panel);
        }
    }

    /// <summary>
    /// Shows the selected panel and hides all others, invoking open/close events.
    /// </summary>
    /// <param name="panelToShow">The panel to activate.</param>
    public void ShowPanel(GameObject panelToShow)
    {
        foreach (var item in link)
        {
            if (item.panel == null)
                continue;

            bool shouldShow = item.panel == panelToShow;
            bool wasActive = item.panel.activeSelf;

            item.panel.SetActive(shouldShow);

            if (shouldShow && !wasActive)
            {
                item.onOpen?.Invoke();
            }
            else if (!shouldShow && wasActive)
            {
                item.onClose?.Invoke();
            }
        }
    }
}
