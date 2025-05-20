using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
public class QuizMaker : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private GameObject multichoiceContainer;
    [SerializeField] private GameObject answerPrefab;

    [Header("Button Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Quiz Data")]
    [TextArea(15, 20)]
    [SerializeField] private string jsonString;
    private QuizQuestion currentQuestion;
    private HashSet<string> selectedAnswers = new HashSet<string>();
    private bool isAnswered = false;

    void Start()
    {
        ParseJsonData();
        DisplayQuestion();
    }

    void ParseJsonData()
    {
        currentQuestion = JsonConvert.DeserializeObject<QuizQuestion>(jsonString);
        selectedAnswers.Clear();
        isAnswered = false;
    }

    void DisplayQuestion()
    {
        if (currentQuestion != null)
        {
            questionText.text = currentQuestion.Question;
            instructionsText.text = currentQuestion.CorrectAnswer.Count > 1
                ? $"Select {currentQuestion.CorrectAnswer.Count} answers"
                : "Select one answer";

            foreach (Transform child in multichoiceContainer.transform)
            {
                Destroy(child.gameObject);
            }

            foreach (var option in currentQuestion.Options)
            {
                CreateAnswerButton(option.Key, option.Value);
            }
        }
    }

    void CreateAnswerButton(string optionKey, string optionValue)
    {
        GameObject answerButton = Instantiate(answerPrefab, multichoiceContainer.transform);
        answerButton.GetComponentInChildren<TextMeshProUGUI>().text = $"{optionKey}: {optionValue}";
        Button button = answerButton.GetComponent<Button>();
        button.onClick.AddListener(() => OnAnswerSelected(optionKey, button));

        Image buttonImage = button.GetComponent<Image>();
        if (buttonImage != null) buttonImage.color = defaultColor;
    }

    void OnAnswerSelected(string selectedOption, Button selectedButton)
    {
        if (isAnswered) return;

        if (currentQuestion.CorrectAnswer.Count == 1)
        {
            selectedAnswers.Clear();
            selectedAnswers.Add(selectedOption);
            ShowResults();
        }
        else
        {
            if (selectedAnswers.Contains(selectedOption))
            {
                selectedAnswers.Remove(selectedOption);
                selectedButton.GetComponent<Image>().color = defaultColor;
            }
            else
            {
                selectedAnswers.Add(selectedOption);
                if (selectedAnswers.Count >= currentQuestion.CorrectAnswer.Count)
                {
                    ShowResults();
                }
            }
        }
    }

    void ShowResults()
    {
        isAnswered = true;
        foreach (Transform child in multichoiceContainer.transform)
        {
            Button button = child.GetComponent<Button>();
            string optionKey = button.GetComponentInChildren<TextMeshProUGUI>().text.Split(':')[0].Trim();

            if (currentQuestion.CorrectAnswer.Contains(optionKey))
            {
                button.GetComponent<Image>().color = correctColor;
            }
            else if (selectedAnswers.Contains(optionKey))
            {
                button.GetComponent<Image>().color = incorrectColor;
            }
        }
    }

    public void SetJsonString(string newJsonString)
    {
        jsonString = newJsonString;
        ParseJsonData();
        DisplayQuestion();
    }

    public void SetJsonTextMesh(TextMeshProUGUI newTextMesh)
    {
        jsonString = newTextMesh.text;
        ParseJsonData();
        DisplayQuestion();
    }

    public void SetJsonText(Text newTextMesh)
    {
        jsonString = newTextMesh.text;
        ParseJsonData();
        DisplayQuestion();
    }

    public void RegenerateQuiz()
    {
        ParseJsonData();
        DisplayQuestion();
    }

    [System.Serializable]
    public class QuizQuestion
    {
        public string QuestionType;
        public string Question;
        public Dictionary<string, string> Options;
        public List<string> CorrectAnswer;
    }
}