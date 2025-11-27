using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.IO;


public class CategoryElement : MonoBehaviour
{
    public TMP_Text categoryNameText;
    public Toggle toggle;
    public Image image; 

    [HideInInspector] public CategoryManager.Category CategoryData;

    public void Setup(CategoryManager.Category category, int depth)
    {
        CategoryData = category;
        categoryNameText.text = category.Name;

        if (!string.IsNullOrEmpty(CategoryData.ImageFile)){
            string _fullPath = Path.Combine(Application.persistentDataPath, CategoryData.ImageFile);
            if (!File.Exists(_fullPath)) { return; }
            byte[] imageBytes = File.ReadAllBytes(_fullPath);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError("Failed to load image from bytes!");
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            image.sprite = sprite;
        }
    }
}
