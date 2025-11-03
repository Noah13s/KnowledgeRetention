using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuizMakerNew : MonoBehaviour
{
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
        public string questionType;// text // text + image //
        public string question;
        public string questionImage;
        public string answerType;// text // image // input //
        public TextAnswer[] textAnswers;
    }

    private void Awake()
    {
        categoryManager.LoadCategories();
    }

    // Start is called before the first frame update
    void Start()
    {
        foreach (var category in categoryManager.categories)
        {
            var _optionData = new TMP_Dropdown.OptionData();
            _optionData.text = category.Name;
            categoryTMP.options.Add(_optionData);
        }
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

    public void CreateQuiz()
    {
        // 1️ Create a new Quiz object
        Quiz quiz = new Quiz();
        quiz.questionType = questionType.options[questionType.value].text;
        quiz.question = questionInput.text;
        quiz.answerType = answerType.options[answerType.value].text;
        quiz.questionImage = ""; // Set this if you plan to include images later

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

        string fileName = "quiz_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
        string filePath = Path.Combine(folderPath, fileName);

        // 5️ Write to file
        File.WriteAllText(filePath, json);

        // 6️ Optional: confirmation log
        Debug.Log($"Quiz saved to: {filePath}");
    }
}
