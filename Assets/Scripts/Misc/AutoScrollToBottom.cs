using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class AutoScrollToBottom : MonoBehaviour
{
    public ScrollRect scrollRect;
    bool pending;

    void OnRectTransformDimensionsChange()
    {
        if (!pending)
            StartCoroutine(ScrollNextFrame());
    }

    IEnumerator ScrollNextFrame()
    {
        pending = true;
        yield return null; // wait one frame

        scrollRect.verticalNormalizedPosition = 0f;
        pending = false;
    }
}
