using UnityEngine;
using UnityEngine.UI;

public class Answer : MonoBehaviour
{
    [SerializeField] Toggle correctAnswer;
    [SerializeField] Toggle aiAnswer;

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
