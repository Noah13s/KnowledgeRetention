using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;
    // Start is called before the first frame update
    void Start()
    {
        slider.onValueChanged.AddListener((float _val) =>  { UpdateText(_val); });
    }

    private void UpdateText(float _val)
    {
        switch (slider.value)
        {
            case 0:
                text.text = "5";
                break;
            case 1:
                text.text = "10";
                break;
            case 2:
                text.text = "20";
                break;
            case 3:
                text.text = "30";
                break;
            case 4:
                text.text = "ALL";
                break;
            default:
                break;
        }
    }
}
