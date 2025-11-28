using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerEditPrefab : MonoBehaviour
{
    [System.Serializable]
    public enum AnswerType
    {
        Text,
        Image
    }
    public AnswerType type;
    public TMP_InputField answerText;
    public Toggle correctAnswer;
    public Toggle aiAnswer;
    public Image imageAnswer;

    [HideInInspector] public ImageLibrary imageLibrary;
    [HideInInspector] public string imagePath;

    private void Start()
    {
        correctAnswer.onValueChanged.AddListener((bool value) => {
            aiAnswer.interactable = !value;
        });
        aiAnswer.onValueChanged.AddListener((bool value) => {
            correctAnswer.interactable = !value;
        });        
    }

    public void RemoveAnswer()
    {
        Destroy(this.gameObject);
    }

    public void HandleType(AnswerType _type, ImageLibrary _imageLibrary)
    {
        type = _type;
        imageLibrary = _imageLibrary;
        switch (type)
        {
            case AnswerType.Text:
                answerText.gameObject.SetActive(true);
                aiAnswer.gameObject.SetActive(true);
                imageAnswer.transform.parent.gameObject.SetActive(false);
                break;
            case AnswerType.Image:
                answerText.gameObject.SetActive(false);
                aiAnswer.gameObject.SetActive(false);
                imageAnswer.transform.parent.gameObject.SetActive(true);
                ImageSetup(imagePath);
                break;
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
            imagePath = relativePath;
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
        imageAnswer.sprite = sprite;
    }
}
