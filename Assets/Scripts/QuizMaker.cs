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
    [SerializeField] private TextMeshProUGUI currentQuestionText;
    [SerializeField] private TextMeshProUGUI totalQuestionsText;

    [Header("Button Colors")]
    [SerializeField] private Color correctColor = Color.green;
    [SerializeField] private Color incorrectColor = Color.red;
    [SerializeField] private Color defaultColor = Color.white;

    [Header("Quiz Data")]
    [TextArea(15, 20)]
    [SerializeField] private string jsonString;

    private List<QuizQuestion> allQuestions = new List<QuizQuestion>();
    private QuizQuestion currentQuestion;
    private HashSet<string> selectedAnswers = new HashSet<string>();
    private bool isAnswered = false;
    [SerializeField] private int questionIndex = 0;

    void Start()
    {
        ParseJsonData();
        DisplayQuestion();
        UpdateQuestionCounter();
    }

    void ParseJsonData()
    {
        allQuestions.Clear();
        selectedAnswers.Clear();
        isAnswered = false;
        questionIndex = 0;

        if (string.IsNullOrEmpty(jsonString))
        {
            Debug.LogError("JSON string is empty or null");
            return;
        }

        try
        {
            // Try to parse as array of questions first
            var questionArray = JsonConvert.DeserializeObject<QuizQuestion[]>(jsonString);
            if (questionArray != null && questionArray.Length > 0)
            {
                foreach (var q in questionArray)
                {
                    if (q != null && q.Options != null && q.CorrectAnswer != null)
                    {
                        allQuestions.Add(q);
                    }
                    else
                    {
                        Debug.LogWarning("Skipping invalid question in array");
                    }
                }
            }
        }
        catch
        {
            try
            {
                // Try to parse as single question
                var singleQuestion = JsonConvert.DeserializeObject<QuizQuestion>(jsonString);
                if (singleQuestion != null && singleQuestion.Options != null && singleQuestion.CorrectAnswer != null)
                {
                    allQuestions.Add(singleQuestion);
                }
                else
                {
                    Debug.LogError("Single question is invalid or missing required properties");
                }
            }
            catch
            {
                // Try to fix common JSON formatting issues and parse again
                string fixedJson = jsonString.Trim();
                if (!fixedJson.StartsWith("[") && fixedJson.Contains("},"))
                {
                    fixedJson = "[" + fixedJson + "]";
                    try
                    {
                        var fixedArray = JsonConvert.DeserializeObject<QuizQuestion[]>(fixedJson);
                        if (fixedArray != null)
                        {
                            foreach (var q in fixedArray)
                            {
                                if (q != null && q.Options != null && q.CorrectAnswer != null)
                                {
                                    allQuestions.Add(q);
                                }
                            }
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Failed to parse JSON data even after fixing: {e.Message}");
                    }
                }
                else
                {
                    Debug.LogError("Failed to parse JSON data - check format");
                }
            }
        }

        if (allQuestions.Count > 0)
        {
            currentQuestion = allQuestions[questionIndex];
        }
        else
        {
            Debug.LogError("No valid questions found in JSON data");
        }
    }

    void DisplayQuestion()
    {
        if (currentQuestion != null && currentQuestion.Options != null && currentQuestion.CorrectAnswer != null)
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
        else
        {
            Debug.LogError("Current question or its properties are null. Check JSON format.");
        }

        UpdateQuestionCounter();
    }

    void UpdateQuestionCounter()
    {
        if (allQuestions.Count > 0)
        {
            currentQuestionText.text = (questionIndex + 1).ToString();
            totalQuestionsText.text = allQuestions.Count.ToString();
        }
        else
        {
            currentQuestionText.text = "0";
            totalQuestionsText.text = "0";
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

    public void NextQuestion()
    {
        if (questionIndex < allQuestions.Count - 1)
        {
            questionIndex++;
            currentQuestion = allQuestions[questionIndex];
            selectedAnswers.Clear();
            isAnswered = false;
            DisplayQuestion();
        }
    }

    public void PreviousQuestion()
    {
        if (questionIndex > 0)
        {
            questionIndex--;
            currentQuestion = allQuestions[questionIndex];
            selectedAnswers.Clear();
            isAnswered = false;
            DisplayQuestion();
        }
    }

    public bool HasNextQuestion()
    {
        return questionIndex < allQuestions.Count - 1;
    }

    public bool HasPreviousQuestion()
    {
        return questionIndex > 0;
    }

    public int GetCurrentQuestionIndex()
    {
        return questionIndex;
    }

    public int GetTotalQuestions()
    {
        return allQuestions.Count;
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