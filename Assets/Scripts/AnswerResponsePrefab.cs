using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AnswerResponsePrefab : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI answerText;
    [SerializeField] public Image answerImage;

    [HideInInspector] public ImageLibrary imageLibrary;


    public void ImageSetup(string _imagePath)
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
        answerImage.sprite = sprite;
    }
}
