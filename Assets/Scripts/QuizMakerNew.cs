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
    [SerializeField] private Button deleteButton;
    [Header("Setup")]
    [SerializeField] private TMP_Dropdown questionType;
    [SerializeField] private TMP_InputField questionInput;
    [SerializeField] private Image questionImage;
    [SerializeField] private TMP_Dropdown answerType;

    [SerializeField] private GameObject answerPrefab; 
    [SerializeField] private GameObject answersList;
    [SerializeField] private TMP_Dropdown categoryTMP;
    [Header("Managers")]
    [SerializeField] private CategoryManager categoryManager;
    [SerializeField] private ImageLibrary imageLibrary;

    private string _questionImagePath;
    private bool _correctAnswerSelected = false;

    [System.Serializable]
    public class Answer
    {
        public string textAnswer;
        public string imageAnswerFile;
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
        public Answer[] answers;
    }

    private void Awake()
    {
        categoryManager.LoadCategories();
        LoadCategories(); // <-- Add this so it updates dropdown after loading
        questionType.onValueChanged.AddListener((int value) => HandleQuestionType(value));
        answerType.onValueChanged.AddListener((int value) => HandleAnswerType(value));
    }
    private void HandleQuestionType(int value)
    {

        switch (value)
        {
            case 0:
                questionImage.gameObject.SetActive(false);
                break;
            case 1:
                questionImage.gameObject.SetActive(true);
                break;
        }
    }

    private void HandleAnswerType(int value)
    {
        var answers = answersList.GetComponentsInChildren<AnswerEditPrefab>();

        switch (value)
        {
            case 0:
                foreach (var answer in answers)
                {
                    answer.HandleType(AnswerEditPrefab.AnswerType.Text, imageLibrary);
                }
                break;
            case 1:

                break;
            case 2:
                foreach (var answer in answers)
                {
                    answer.HandleType(AnswerEditPrefab.AnswerType.Image, imageLibrary);
                }
                break;
        }
    }



    private void Update()
    {

        // Creation requirements check
        if (String.IsNullOrEmpty(questionInput.text) || categoryTMP.value == -1 || String.IsNullOrEmpty(quizName.text) || _correctAnswerSelected == false)
        {
            createButton.interactable = false;
        }
        else
        {
            createButton.interactable = true;
        }
        if (String.IsNullOrEmpty(quizName.text))
        {
            deleteButton.interactable = false;
        }
        else
        {
            deleteButton.interactable = true;
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


        int currentCount = answersList.transform.childCount;

        if (currentCount < maxAnswers)
        {
            // Instantiate new answer prefab
            GameObject newAnswer = Instantiate(answerPrefab, answersList.transform);

            // Optionally reset the answer prefab’s input fields/toggles
            AnswerEditPrefab answerComponent = newAnswer.GetComponent<AnswerEditPrefab>();
            if (answerComponent != null)
            {               
                answerComponent.answerText.text = "";
                answerComponent.correctAnswer.isOn = false;
                answerComponent.aiAnswer.isOn = false;
            }

            // Disable the button if we reached the max allowed answers
            addAnswer.interactable = (answersList.transform.childCount < maxAnswers);
            HandleAnswerType(answerType.value);
            AnswersValidityCheck();
            
        }
    }

    private void AnswersValidityCheck()
    {
        var answers = answersList.GetComponentsInChildren<AnswerEditPrefab>();

        // Find the active (toggled-on) answer
        AnswerEditPrefab activeAnswer = null;
        foreach (var answer in answers)
        {
            if (answer.correctAnswer.isOn)
            {
                activeAnswer = answer;
                break;
            }
        }

        // Update the correct answer selected flag
        _correctAnswerSelected = activeAnswer != null;

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

        ImageSetup(_quiz.questionImage);

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
        if (_quiz.answers != null && _quiz.answers.Length > 0)
        {
            foreach (var answerData in _quiz.answers)
            {
                GameObject newAnswer = Instantiate(answerPrefab, answersList.transform);
                AnswerEditPrefab answerComponent = newAnswer.GetComponent<AnswerEditPrefab>();
                if (answerComponent != null)
                {
                    answerComponent.answerText.text = answerData.textAnswer;
                    answerComponent.correctAnswer.isOn = answerData.correctAnswer;
                    answerComponent.aiAnswer.isOn = answerData.AIGen;
                }
            }
        }

        // Re-enable add answer button if below limit
        addAnswer.interactable = answersList.transform.childCount < 4;
        AnswersValidityCheck();
    }

    public void DeleteQuiz()
    {
        // Ensure quiz name is provided
        if (string.IsNullOrEmpty(quizName.text))
        {
            Debug.LogWarning("Cannot delete quiz: Quiz name is empty.");
            return;
        }

        // Build file path
        string folderPath = Path.Combine(Application.persistentDataPath, "quizzes");
        string fileName = "quiz_" + quizName.text + ".json";
        string filePath = Path.Combine(folderPath, fileName);

        // Check if file exists
        if (File.Exists(filePath))
        {
            try
            {
                File.Delete(filePath);
                Debug.Log($"Quiz deleted: {filePath}");

                // Reset UI after deletion
                ResetQuiz();
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to delete quiz: {e.Message}");
            }
        }
        else
        {
            Debug.LogWarning($"Quiz file not found: {filePath}");
        }
    }

    public void SetImage()
    {


        // Safety check
        if (imageLibrary == null)
        {
            Debug.LogError("ImageLibrary reference not assigned in the inspector!");
            return;
        }

        imageLibrary.mode = ImageLibrary.Mode.Select;

        // Enable the image library UI
        imageLibrary.gameObject.SetActive(true);

        // Assign callback to handle when the user selects an image
        imageLibrary.onSelectCallback = (string fullPath) =>
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogWarning("No image path returned from ImageLibrary.");
                imageLibrary.gameObject.SetActive(false);
                return;
            }

            //  Convert full path to relative path within persistent data
            string persistentPath = Application.persistentDataPath;
            string relativePath = fullPath.Replace(persistentPath + Path.DirectorySeparatorChar, "");

            //  Store only relative path
            _questionImagePath = relativePath;
            Debug.Log($"Stored relative image path: {relativePath}");

            //  Close the image library UI
            imageLibrary.gameObject.SetActive(false);
            ImageSetup(relativePath);
        };
    }

    private void ImageSetup(string _imagePath)
    {
        string _fullPath = Path.Combine(Application.persistentDataPath, _imagePath);
        if (!File.Exists(_fullPath)) { return; }
        byte[] imageBytes = File.ReadAllBytes(_fullPath);
        Texture2D texture = new Texture2D(2, 2);
        if (!texture.LoadImage(imageBytes))
        {
            Debug.LogError("Failed to load image from bytes!");
        }

        Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
        questionImage.sprite = sprite;
    }

    public void CreateQuiz()
    {
        // 1️ Create a new Quiz object
        Quiz quiz = new Quiz();
        quiz.quizName = quizName.text;
        quiz.questionType = questionType.options[questionType.value].text;
        quiz.question = questionInput.text;
        quiz.answerType = answerType.options[answerType.value].text;
        quiz.questionImage = _questionImagePath; // Set this if you plan to include images later
        quiz.category = categoryTMP.options[categoryTMP.value].text;

        // 2️ Collect answers from instantiated prefabs
        Transform contentTransform = answersList.transform;
        quiz.answers = new Answer[contentTransform.childCount];

        for (int i = 0; i < contentTransform.childCount; i++)
        {
            GameObject answerGO = contentTransform.GetChild(i).gameObject;

            TMP_InputField inputField = answerGO.GetComponentInChildren<TMP_InputField>();
            Toggle toggle = answerGO.GetComponentInChildren<Toggle>();
            AnswerEditPrefab answerScript = answerGO.GetComponent<AnswerEditPrefab>();

            Answer answer = new Answer
            {
                imageAnswerFile = answerScript.imagePath,
                textAnswer = answerScript.answerText.text,
                correctAnswer = answerScript.correctAnswer.isOn,
                AIGen = answerScript.aiAnswer.isOn // You can later add a toggle or flag for this
            };

            quiz.answers[i] = answer;
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
