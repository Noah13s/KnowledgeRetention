using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

public class QuizMaker : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private TextMeshProUGUI instructionsText;
    [SerializeField] private GameObject multichoiceContainer;
    [SerializeField] private GameObject answerPrefab;
    [SerializeField] private TextMeshProUGUI currentQuestionText;
    [SerializeField] private TextMeshProUGUI totalQuestionsText;

    [Header("Button Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Quiz Data")]
    [TextArea(15, 20)]
    [SerializeField] private string jsonString;

    private QuizMakerNew.Quiz currentQuiz;
    private List<QuizMakerNew.TextAnswer> answers = new();
    private HashSet<int> selectedAnswers = new();
    private bool isAnswered = false;

    void Start()
    {
        if (!string.IsNullOrEmpty(jsonString))
        {
            LoadQuizFromJson(jsonString);
        }
    }

    public void SetJsonString(string newJsonString)
    {
        jsonString = newJsonString;
        LoadQuizFromJson(jsonString);
    }

    private void LoadQuizFromJson(string json)
    {
        try
        {
            currentQuiz = JsonUtility.FromJson<QuizMakerNew.Quiz>(json);
            if (currentQuiz == null)
            {
                Debug.LogError("Failed to parse quiz JSON: invalid structure.");
                return;
            }

            answers = new List<QuizMakerNew.TextAnswer>(currentQuiz.textAnswers);
            DisplayQuestion();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error parsing quiz JSON: {e.Message}");
        }
    }

    private void DisplayQuestion()
    {
        if (currentQuiz == null)
        {
            Debug.LogError("No quiz loaded.");
            return;
        }

        isAnswered = false;
        selectedAnswers.Clear();

        questionText.text = currentQuiz.question;
        instructionsText.text = "Select one answer"; // single question quiz

        foreach (Transform child in multichoiceContainer.transform)
            Destroy(child.gameObject);

        for (int i = 0; i < answers.Count; i++)
        {
            CreateAnswerButton(i, answers[i]);
        }

        // Since each quiz is just one question, set counters to 1/1
        currentQuestionText.text = "1";
        totalQuestionsText.text = "1";
    }

    private void CreateAnswerButton(int index, QuizMakerNew.TextAnswer answer)
    {
        GameObject answerButton = Instantiate(answerPrefab, multichoiceContainer.transform);
        TextMeshProUGUI textComp = answerButton.GetComponentInChildren<TextMeshProUGUI>();
        textComp.text = answer.answer;

        Button button = answerButton.GetComponent<Button>();
        Image image = answerButton.GetComponent<Image>();
        image.color = defaultColor;

        button.onClick.AddListener(() => OnAnswerSelected(index, button));
    }

    private void OnAnswerSelected(int index, Button button)
    {
        if (isAnswered) return;

        selectedAnswers.Clear();
        selectedAnswers.Add(index);

        ShowResults();
    }

    private void ShowResults()
    {
        isAnswered = true;

        for (int i = 0; i < multichoiceContainer.transform.childCount; i++)
        {
            Transform child = multichoiceContainer.transform.GetChild(i);
            Button button = child.GetComponent<Button>();
            Image image = button.GetComponent<Image>();
            var answer = answers[i];

            if (answer.correctAnswer)
                image.color = correctColor;
            else if (selectedAnswers.Contains(i))
                image.color = incorrectColor;
            else
                image.color = defaultColor;
        }
    }

    // (These methods are placeholders now since new quizzes are single-question)
    public void NextQuestion() { }
    public void PreviousQuestion() { }
    public bool HasNextQuestion() => false;
    public bool HasPreviousQuestion() => false;
    public int GetCurrentQuestionIndex() => 0;
    public int GetTotalQuestions() => 1;
}
