using LLMUnity;
using LLMUnitySamples;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using static QuizMakerNew;

public class QuizPlayer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentQuizNb;
    [SerializeField] private TextMeshProUGUI totalQuizNb;
    [Header("Question")]
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Image questionImage;
    [Header("Answer")]
    [SerializeField] private Transform answersParent;
    [SerializeField] private TMP_InputField answerInputField;
    [SerializeField] private GameObject answerButtonPrefab;
    [SerializeField] private Button nextButton;
    [SerializeField] private Button inputConfirmButton;
    [Header("Setup")]
    [SerializeField] private MobileDemo mobileDemo;
    [SerializeField] private TextMeshProUGUI aiResponse;

    private List<Quiz> loadedQuizzes = new();
    private int currentQuizIndex = 0;
    private Quiz currentQuiz;
    private bool answered = false;
    private bool quizCompleted = false;

    // Store buttons for easier color control later
    private List<(Button button, bool isCorrect)> currentButtons = new();

    // =========================================
    // Public ENTRY POINT
    // =========================================
    public void SetMultipleJsonStrings(List<string> jsonList)
    {
        ResetQuizPlayer(); // Reset first before loading new data

        foreach (var json in jsonList)
        {
            Quiz quiz = JsonUtility.FromJson<Quiz>(json);
            if (quiz != null)
                loadedQuizzes.Add(quiz);
        }

        if (loadedQuizzes.Count == 0)
        {
            Debug.LogWarning("No valid quizzes loaded.");
            return;
        }

        currentQuizIndex = 0;
        StartNextQuiz();
    }

    // =========================================
    // MAIN QUIZ FLOW
    // =========================================
    private void StartNextQuiz()
    {
        if (currentQuizIndex >= loadedQuizzes.Count)
        {
            quizCompleted = true;
            OnAllQuizzesCompleted();
            return;
        }

        currentQuiz = loadedQuizzes[currentQuizIndex];
        UpdateQuizCounterUI(currentQuizIndex + 1, loadedQuizzes.Count);
        currentQuizIndex++;

        answered = false;
        if (currentQuiz.answerType == "Input")
        {
            ShowInputQuestion();
        }
        else
        {
            ShowQuestion();
        }
    }

    private void UpdateQuizCounterUI(int current, int total)
    {
        if (currentQuizNb != null)
            currentQuizNb.text = current.ToString();
        if (totalQuizNb != null)
            totalQuizNb.text = total.ToString();
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

    private void ShowInputQuestion()
    {
        if (currentQuiz == null)
        {
            Debug.LogError("No quiz loaded!");
            return;
        }
        // Clear old answers
        foreach (Transform child in answersParent)
            Destroy(child.gameObject);

        currentButtons.Clear();
        questionText.text = currentQuiz.question;
        answerInputField.gameObject.SetActive(true);
        answerInputField.text = "";
        answerInputField.GetComponent<Image>().color = Color.white;
        inputConfirmButton.gameObject.SetActive(true);
        Debug.Log(currentQuiz.questionType);

        if (currentQuiz.questionType == "Question + Image")
        {
            ImageSetup(currentQuiz.questionImage);
            questionImage.transform.parent.gameObject.SetActive(true);
        }
        else if (currentQuiz.questionType == "Question only")
        {
            questionImage.transform.parent.gameObject.SetActive(false);
        }
        nextButton.gameObject.SetActive(false);
    }

    public void CheckInput()
    {
        mobileDemo.onInputFieldSubmit($"Check if the answered response corresponds to the awaited response.Be permissive the answer doesn't need to be exactly the one awaited but if its missing context or words return partial. Answer by true or false or partial. If the anwser is missing some context or words return partial or false. The awaited response is {currentQuiz.inputAnswer}.The answered response is {answerInputField.text}. Respond either true, false or partial. Prioritize true and false");
        inputConfirmButton.interactable = false;

        // Remove previous listeners to avoid duplicates
        mobileDemo.onAIResponseComplete.RemoveAllListeners();

        mobileDemo.onAIResponseComplete.AddListener(() =>
        {
            string result = aiResponse.text.Trim().ToLower();

            Image fieldImage = answerInputField.GetComponent<Image>();

            if (result == "true")
            {
                fieldImage.color = Color.green;
            }
            else if (result == "false")
            {
                fieldImage.color = Color.red;
                mobileDemo.onInputFieldSubmit($"Briefly explain why the answered response is false and what was the awaited response. The awaited response is {currentQuiz.inputAnswer}.The answered response is {answerInputField.text}");
                mobileDemo.onAIResponseComplete.RemoveAllListeners();
                mobileDemo.onAIResponseComplete.AddListener(() =>
                {
                    answerInputField.text = aiResponse.text;
                });
            }
            else if (result=="partial")
            {
                fieldImage.color = Color.yellow;
                mobileDemo.onInputFieldSubmit($"Briefly explain why the answered response is partially true and what was the awaited response.The awaited response is {currentQuiz.inputAnswer}.The answered response is {answerInputField.text}");
                mobileDemo.onAIResponseComplete.RemoveAllListeners();
                mobileDemo.onAIResponseComplete.AddListener(() =>
                {
                    answerInputField.text = aiResponse.text;
                });
            }
            else
            {
                fieldImage.color = Color.magenta;
                Debug.LogWarning("Unexpected AI response: " + aiResponse.text);
                answerInputField.text = aiResponse.text;
            }
            nextButton.gameObject.SetActive(true);
            inputConfirmButton.interactable = true;

        });
    }

    private void ShowQuestion()
    {
        if (currentQuiz == null)
        {
            Debug.LogError("No quiz loaded!");
            return;
        }
        inputConfirmButton.gameObject.SetActive(false);
        answerInputField.gameObject.SetActive(false);
        // Clear old answers
        foreach (Transform child in answersParent)
            Destroy(child.gameObject);

        currentButtons.Clear();
        questionText.text = currentQuiz.question;
        Debug.Log(currentQuiz.questionType);
        if (currentQuiz.questionType == "Question + Image") { 
            ImageSetup(currentQuiz.questionImage); 
            questionImage.transform.parent.gameObject.SetActive(true); 
        } else if (currentQuiz.questionType == "Question only")
        {
            questionImage.transform.parent.gameObject.SetActive(false);
        }    
        nextButton.gameObject.SetActive(false);

        // Randomize the answer order
        List<Answer> randomizedAnswers = currentQuiz.answers
            .OrderBy(a => Random.value) // Shuffle
            .ToList();

        foreach (var ans in randomizedAnswers)
        {
            GameObject btnObj = Instantiate(answerButtonPrefab, answersParent);
            AnswerResponsePrefab answerPrefabScript  = btnObj.GetComponent<AnswerResponsePrefab>();
            TMP_Text btnText = answerPrefabScript.answerText;
            Image btnImage = answerPrefabScript.answerImage;
            if (currentQuiz.answerType == "Text select")
            {
                btnText.gameObject.SetActive(true);
                btnImage.gameObject.SetActive(false);
                btnText.text = ans.textAnswer;
            }
            else if (currentQuiz.answerType == "Image select")
            {
                btnText.gameObject.SetActive(false);
                btnImage.gameObject.SetActive(true);
                answerPrefabScript.ImageSetup(ans.imageAnswerFile);
            }


            Button btn = btnObj.GetComponent<Button>();
            bool isCorrect = ans.correctAnswer;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnAnswerSelected(btn, isCorrect));

            currentButtons.Add((btn, isCorrect));
        }
    }

    private void OnAnswerSelected(Button clickedButton, bool isCorrect)
    {
        if (answered) return;
        answered = true;

        foreach (Transform child in answersParent)
        {
            Button b = child.GetComponent<Button>();
            b.interactable = false;
        }

        if (isCorrect)
        {
            SetButtonColor(clickedButton, Color.green);
            Debug.Log("Correct");
        }
        else
        {
            SetButtonColor(clickedButton, Color.red);

            // highlight the real correct button
            foreach (var pair in currentButtons)
            {
                if (pair.isCorrect)
                {
                    SetButtonColor(pair.button, Color.green);
                    break;
                }
            }
        }


        nextButton.gameObject.SetActive(true);
    }

    private void SetButtonColor(Button button, Color color)
    {
        Image img = button.GetComponent<Image>();
        if (img != null) img.color = color;

        AnswerResponsePrefab prefabScript = button.GetComponent<AnswerResponsePrefab>();
        prefabScript.answerImage.color = color;

        TMP_Text txt = button.GetComponentInChildren<TMP_Text>();
        if (txt != null) txt.color = Color.white;
    }


    // =========================================
    // Public "Next" BUTTON
    // =========================================
    public void NextQuestion()
    {
        if (quizCompleted)
        {
            Debug.Log("All quizzes already finished.");
            return;
        }

        answered = false;
        StartNextQuiz();
    }

    // =========================================
    // RESET & CLEANUP
    // =========================================
    private void OnAllQuizzesCompleted()
    {
        Debug.Log("All category quizzes finished!");
        nextButton.gameObject.SetActive(false);

        ResetQuizPlayer();
        gameObject.SetActive(false);
    }

    private void ResetQuizPlayer()
    {
        loadedQuizzes.Clear();
        currentQuiz = null;
        currentQuizIndex = 0;
        answered = false;
        quizCompleted = false;
        currentButtons.Clear();

        if (questionText != null) questionText.text = "";
        if (answersParent != null)
        {
            foreach (Transform child in answersParent)
                Destroy(child.gameObject);
        }
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        if (currentQuizNb != null) currentQuizNb.text = "0";
        if (totalQuizNb != null) totalQuizNb.text = "0";

        Debug.Log("QuizPlayer reset and ready for new quizzes.");
    }

}
