using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static QuizMakerNew;
using System.Linq;

public class QuizPlayer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI currentQuizNb;
    [SerializeField] private TextMeshProUGUI totalQuizNb;
    [SerializeField] private TMP_Text questionText;
    [SerializeField] private Transform answersParent;
    [SerializeField] private GameObject answerButtonPrefab;
    [SerializeField] private Button nextButton;

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
        ShowQuestion();
    }

    private void UpdateQuizCounterUI(int current, int total)
    {
        if (currentQuizNb != null)
            currentQuizNb.text = current.ToString();
        if (totalQuizNb != null)
            totalQuizNb.text = total.ToString();
    }


    private void ShowQuestion()
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
        nextButton.gameObject.SetActive(false);

        // Randomize the answer order
        List<TextAnswer> randomizedAnswers = currentQuiz.textAnswers
            .OrderBy(a => Random.value) // Shuffle
            .ToList();

        foreach (var ans in randomizedAnswers)
        {
            GameObject btnObj = Instantiate(answerButtonPrefab, answersParent);
            TMP_Text btnText = btnObj.GetComponentInChildren<TMP_Text>();
            btnText.text = ans.answer;

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
            Debug.Log("Wrong");

            foreach (Transform child in answersParent)
            {
                Button b = child.GetComponent<Button>();
                if (b == clickedButton) continue;

                TMP_Text t = b.GetComponentInChildren<TMP_Text>();
                if (currentQuiz.textAnswers.Any(a => a.answer == t.text && a.correctAnswer))
                {
                    SetButtonColor(b, Color.green);
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
