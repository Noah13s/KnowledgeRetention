using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Answer : MonoBehaviour
{
    public TMP_InputField answerText;
    public Toggle correctAnswer;
    public Toggle aiAnswer;

    private void Start()
    {
        correctAnswer.onValueChanged.AddListener((bool value) => {
            aiAnswer.interactable = !value;
        });
        aiAnswer.onValueChanged.AddListener((bool value) => {
            correctAnswer.interactable = !value;
        });
    }

    public void RemoveAnswer()
    {
        Destroy(this.gameObject);
    }
}
