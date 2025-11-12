using System;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizMakerNew : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_InputField quizName;
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
    }

    public void AddAnswer()
    {
        Instantiate(answerPrefab, answersList.transform);
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
