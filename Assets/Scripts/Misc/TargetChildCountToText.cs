using TMPro;
using UnityEngine;

public class TargetChildCountToText : MonoBehaviour
{
    [SerializeField] private Transform targetTransform;
    private TextMeshProUGUI text;
    private int lastChildCount = -1;

    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
        UpdateText();
    }

    void Update()
    {
        if (targetTransform == null) return;

        int currentCount = targetTransform.childCount;
        if (currentCount != lastChildCount)
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        lastChildCount = targetTransform.childCount;
        text.text = lastChildCount.ToString();
    }
}
