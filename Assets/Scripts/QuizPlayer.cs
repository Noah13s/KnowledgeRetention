using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using static QuizMakerNew;
using System.Linq;

public class QuizPlayer : MonoBehaviour
{

    [Header("UI References")]
    public TMP_Text questionText;
    public Transform answersParent;
    public GameObject answerButtonPrefab;
    public Button nextButton;

    private List<Quiz> loadedQuizzes = new();
    private int currentQuizIndex = 0;
    private Quiz currentQuiz;
    private bool answered = false;
    private bool quizCompleted = false;

    // =========================================
    // PUBLIC ENTRY POINT
    // =========================================
    public void SetMultipleJsonStrings(List<string> jsonList)
    {
        ResetQuizPlayer(); //  Reset first before loading new data

        foreach (var json in jsonList)
        {
            Quiz quiz = JsonUtility.FromJson<Quiz>(json);
            if (quiz != null)
                loadedQuizzes.Add(quiz);
        }

        if (loadedQuizzes.Count == 0)
        {
            Debug.LogWarning(" No valid quizzes loaded.");
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
        currentQuizIndex++;

        answered = false;
        ShowQuestion();
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

        questionText.text = currentQuiz.question;
        nextButton.gameObject.SetActive(false);

        // Randomize the answer order
        List<TextAnswer> randomizedAnswers = currentQuiz.textAnswers
            .OrderBy(a => Random.value) // Shuffle using Unity’s Random
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
        }
    }

    private void OnAnswerSelected(Button clickedButton, bool isCorrect)
    {
        if (answered) return;
        answered = true;

        // Disable all answer buttons
        foreach (Transform child in answersParent)
        {
            Button b = child.GetComponent<Button>();
            b.interactable = false;
        }

        // Feedback
        TMP_Text buttonText = clickedButton.GetComponentInChildren<TMP_Text>();
        if (isCorrect)
        {
            buttonText.text += " ";
            Debug.Log(" Correct!");
        }
        else
        {
            buttonText.text += " ";
            Debug.Log(" Wrong!");
        }

        // Wait for "Next"
        nextButton.gameObject.SetActive(true);
    }

    // =========================================
    // PUBLIC "Next" BUTTON
    // =========================================
    public void NextQuestion()
    {
        if (quizCompleted)
        {
            Debug.Log(" All quizzes already finished.");
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
        Debug.Log(" All category quizzes finished!");
        nextButton.gameObject.SetActive(false);

        //  Reset the player so it's ready for the next start
        ResetQuizPlayer();

        // Optionally hide the player UI
        gameObject.SetActive(false);
    }

    private void ResetQuizPlayer()
    {
        // Clear quiz data
        loadedQuizzes.Clear();
        currentQuiz = null;
        currentQuizIndex = 0;
        answered = false;
        quizCompleted = false;

        // Clear UI
        if (questionText != null) questionText.text = "";
        if (answersParent != null)
        {
            foreach (Transform child in answersParent)
                Destroy(child.gameObject);
        }
        if (nextButton != null)
            nextButton.gameObject.SetActive(false);

        Debug.Log(" QuizPlayer reset and ready for new quizzes.");
    }
}
