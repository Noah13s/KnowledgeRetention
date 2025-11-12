using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Navigation : MonoBehaviour
{
    [System.Serializable]
    public class PanelButtonLink
    {
        public GameObject panel;
        public Button button;
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
    /// Shows the selected panel and hides all others.
    /// </summary>
    /// <param name="panelToShow">The panel to activate.</param>
    public void ShowPanel(GameObject panelToShow)
    {
        foreach (var item in link)
        {
            if (item.panel != null)
            {
                item.panel.SetActive(item.panel == panelToShow);
            }
        }
    }
}
