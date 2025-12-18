using UnityEngine;

public class ResolutionCanvasSelector : MonoBehaviour
{
    [Header("Landscape")]
    [SerializeField] private GameObject landscapeCanvas;
    [SerializeField] private Navigation landscapeNavigation;
    [Header("Portrait")]
    [SerializeField] private GameObject portraitCanvas;
    [SerializeField] private Navigation portraitNavigation;

    private bool lastIsLandscape;

    void Start()
    {
        lastIsLandscape = IsLandscape();
        ApplyOrientation(lastIsLandscape);
    }

    void Update()
    {
        bool isLandscape = IsLandscape();
        if (isLandscape != lastIsLandscape)
        {
            ApplyOrientation(isLandscape);
            lastIsLandscape = isLandscape;
        }
    }

    private bool IsLandscape()
    {
        return Screen.width >= Screen.height;
    }

    private void ApplyOrientation(bool isLandscape)
    {
        landscapeCanvas.SetActive(isLandscape);
        portraitCanvas.SetActive(!isLandscape);

        if (isLandscape)
        {
            SyncNavigation(portraitNavigation, landscapeNavigation);
        }
        else
        {
            SyncNavigation(landscapeNavigation, portraitNavigation);
        }
    }

    private void SyncNavigation(Navigation from, Navigation to)
    {
        int index = from.GetCurrentIndex();
        if (index >= 0 && index < to.link.Count)
        {
            to.ShowPanel(to.link[index]);
        }
    }
}
