using UnityEngine;

public class ResolutionCanvasSelector : MonoBehaviour
{
    [SerializeField] private GameObject landscapeCanvas;
    [SerializeField] private GameObject portraitCanvas;

    void Update()
    {
        float aspect = (float)Screen.width / Screen.height;
        bool isLandscape = aspect >= 1f;

        if (landscapeCanvas.activeSelf != isLandscape)
            landscapeCanvas.SetActive(isLandscape);

        if (portraitCanvas.activeSelf == isLandscape)
            portraitCanvas.SetActive(!isLandscape);
    }
}
