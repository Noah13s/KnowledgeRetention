using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static QuizMakerNew;

public class QuizMakerNew : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_InputField quizName;
    [SerializeField] private Button addAnswer;
    [SerializeField] private Button createButton;
    [Header("Setup")]
    [SerializeField] private TMP_Dropdown questionType;
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private TMP_Dropdown answerType;

    [SerializeField] private GameObject answerPrefab; 
    [SerializeField] private GameObject answersList;
    [SerializeField] private TMP_Dropdown categoryTMP;

    [SerializeField] private CategoryManager categoryManager;
    
    [System.Serializable]
    public class TextAnswer
    {
        public string answer;
        public bool correctAnswer;
        public bool AIGen;
    }
    [System.Serializable]
    public class Quiz
    {
        public string quizName;
        public string questionType;// text // text + image //
        public string question;
        public string questionImage;
        public string answerType;// text // image // input //
        public string category;
        public TextAnswer[] textAnswers;
    }

    private void Awake()
    {
        categoryManager.LoadCategories();
        LoadCategories(); // <-- Add this so it updates dropdown after loading
    }

    private void Update()
    {
        // Creation requirements check
        if (String.IsNullOrEmpty(questionInput.text) || categoryTMP.value == -1 || String.IsNullOrEmpty(quizName.text))
        {
            createButton.interactable = false;
        }
        else
        {
            createButton.interactable = true;
        }
    }

    public void LoadCategories()
    {
        categoryTMP.options.Clear();

        foreach (var category in categoryManager.categories)
        {
            AddCategoryAndSubcategories(category, "");
        }

        categoryTMP.RefreshShownValue();
    }

    /// <summary>
    /// Recursively adds categories and subcategories to the dropdown.
    /// </summary>
    private void AddCategoryAndSubcategories(CategoryManager.Category category, string parentPath)
    {
        string fullName = string.IsNullOrEmpty(parentPath)
            ? category.Name
            : $"{parentPath}/{category.Name}";

        var option = new TMP_Dropdown.OptionData(fullName);
        categoryTMP.options.Add(option);

        if (category.subCategories != null && category.subCategories.Count > 0)
        {
            foreach (var subCat in category.subCategories)
            {
                AddCategoryAndSubcategories(subCat, fullName);
            }
        }
    }


    public void ClearAnswers()
    {
        Transform contentTransform = answersList.transform;

        // Destroy all existing children under the ScrollRect content
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(contentTransform.GetChild(i).gameObject);
        }
        addAnswer.interactable = true;
    }

    public void AddAnswer()
    {
        // Limit only applies to text-based answers (can be changed)
        int maxAnswers = 4;

        // Allow adding answers only if the selected answer type is "Text"
        if (answerType.options[answerType.value].text.Equals("Text select", StringComparison.OrdinalIgnoreCase))
        {
            int currentCount = answersList.transform.childCount;

            if (currentCount < maxAnswers)
            {
                // Instantiate new answer prefab
                GameObject newAnswer = Instantiate(answerPrefab, answersList.transform);

                // Optionally reset the answer prefab’s input fields/toggles
                Answer answerComponent = newAnswer.GetComponent<Answer>();
                if (answerComponent != null)
                {
                    answerComponent.answerText.text = "";
                    answerComponent.correctAnswer.isOn = false;
                    answerComponent.aiAnswer.isOn = false;
                }

                // Disable the button if we reached the max allowed answers
                addAnswer.interactable = (answersList.transform.childCount < maxAnswers);
                AnswersValidityCheck();
            }
        }
    }

    private void AnswersValidityCheck()
    {
        var answers = answersList.GetComponentsInChildren<Answer>();

        // Find the active (toggled-on) answer
        Answer activeAnswer = null;
        foreach (var answer in answers)
        {
            if (answer.correctAnswer.isOn)
            {
                activeAnswer = answer;
                break;
            }
        }

        // Update interactivity for all answers
        foreach (var answer in answers)
        {
            answer.correctAnswer.onValueChanged.RemoveListener(AnswersValidityCheck);
            answer.correctAnswer.interactable = (activeAnswer == null || answer == activeAnswer);
            answer.correctAnswer.onValueChanged.AddListener(AnswersValidityCheck);
        }
    }

    private void AnswersValidityCheck(bool value)
    {
        AnswersValidityCheck();
    }

    public void ResetQuiz()
    {
        // Set quiz name
        quizName.text = "";

        // Set question type dropdown
        questionType.value = 0;

        // Set question text
        questionInput.text = "";

        // Set answer type dropdown
        answerType.value = 0;

        // Set category dropdown
        categoryTMP.value = 1;

        // Clear any existing answers
        ClearAnswers();
    }

    public void OpenQuiz(Quiz _quiz)
    {
        // Set quiz name
        quizName.text = _quiz.quizName;

        // Set question type dropdown
        int questionTypeIndex = questionType.options.FindIndex(opt => opt.text == _quiz.questionType);
        questionType.value = questionTypeIndex >= 0 ? questionTypeIndex : 0;

        // Set question text
        questionInput.text = _quiz.question;

        // Set answer type dropdown
        int answerTypeIndex = answerType.options.FindIndex(opt => opt.text == _quiz.answerType);
        answerType.value = answerTypeIndex >= 0 ? answerTypeIndex : 0;

        // Set category dropdown
        int categoryIndex = categoryTMP.options.FindIndex(opt => opt.text == _quiz.category);
        categoryTMP.value = categoryIndex >= 0 ? categoryIndex : 0;

        // Clear any existing answers
        ClearAnswers();

        // Recreate answers from quiz data
        if (_quiz.textAnswers != null && _quiz.textAnswers.Length > 0)
        {
            foreach (var answerData in _quiz.textAnswers)
            {
                GameObject newAnswer = Instantiate(answerPrefab, answersList.transform);
                Answer answerComponent = newAnswer.GetComponent<Answer>();
                if (answerComponent != null)
                {
                    answerComponent.answerText.text = answerData.answer;
                    answerComponent.correctAnswer.isOn = answerData.correctAnswer;
                    answerComponent.aiAnswer.isOn = answerData.AIGen;
                }
            }
        }

        // Re-enable add answer button if below limit
        addAnswer.interactable = answersList.transform.childCount < 4;
        AnswersValidityCheck();
    }


    public void CreateQuiz()
    {
        // 1️ Create a new Quiz object
        Quiz quiz = new Quiz();
        quiz.quizName = quizName.text;
        quiz.questionType = questionType.options[questionType.value].text;
        quiz.question = questionInput.text;
        quiz.answerType = answerType.options[answerType.value].text;
        quiz.questionImage = ""; // Set this if you plan to include images later
        quiz.category = categoryTMP.options[categoryTMP.value].text;

        // 2️ Collect answers from instantiated prefabs
        Transform contentTransform = answersList.transform;
        quiz.textAnswers = new TextAnswer[contentTransform.childCount];

        for (int i = 0; i < contentTransform.childCount; i++)
        {
            GameObject answerGO = contentTransform.GetChild(i).gameObject;

            TMP_InputField inputField = answerGO.GetComponentInChildren<TMP_InputField>();
            Toggle toggle = answerGO.GetComponentInChildren<Toggle>();
            Answer answerScript = answerGO.GetComponent<Answer>();

            TextAnswer answer = new TextAnswer
            {
                answer = answerScript.answerText.text,
                correctAnswer = answerScript.correctAnswer.isOn,
                AIGen = answerScript.aiAnswer.isOn // You can later add a toggle or flag for this
            };

            quiz.textAnswers[i] = answer;
        }

        // 3️ Convert to JSON
        string json = JsonUtility.ToJson(quiz, true);

        // 4️ Define save path
        string folderPath = Path.Combine(Application.persistentDataPath, "quizzes");
        if (!Directory.Exists(folderPath))
            Directory.CreateDirectory(folderPath);

        string fileName = "quiz_" + quiz.quizName + ".json";
        string filePath = Path.Combine(folderPath, fileName);

        // 5️ Write to file
        File.WriteAllText(filePath, json);

        // 6️ Optional: confirmation log
        Debug.Log($"Quiz saved to: {filePath}");
    }
}
