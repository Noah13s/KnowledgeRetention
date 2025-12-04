using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderHandler : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TextMeshProUGUI text;
    public int value = -1;
    // Start is called before the first frame update
    void Start()
    {
        slider.onValueChanged.AddListener((float _val) =>  { UpdateText(_val); });
        value = (int)slider.value;
    }

    private void UpdateText(float _val)
    {
        switch (slider.value)
        {
            case 0:
                text.text = "1";
                value = 1;
                break;
            case 1:
                text.text = "5";
                value = 5;
                break;
            case 2:
                text.text = "10";
                value = 10;
                break;
            case 3:
                text.text = "25";
                value = 25;
                break;
            case 4:
                text.text = "ALL";
                value = -1;
                break;
            default:
                break;
        }
    }
}
