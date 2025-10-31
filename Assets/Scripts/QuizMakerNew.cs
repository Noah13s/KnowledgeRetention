using TMPro;
using UnityEngine;

public class QuizMakerNew : MonoBehaviour
{
    [SerializeField] private TMP_Dropdown questionType;
    [SerializeField] private TMP_Dropdown answerType;

    [SerializeField] private GameObject answerPrefab; 
    [SerializeField] private GameObject answersList;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void ClearAnswers()
    {
        Transform contentTransform = answersList.transform;

        // Destroy all existing children under the ScrollRect content
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(contentTransform.GetChild(i).gameObject);
        }
    }

    public void AddAnswer()
    {
        Instantiate(answerPrefab, answersList.transform);
    }
}
