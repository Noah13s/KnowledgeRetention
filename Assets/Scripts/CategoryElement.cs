using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryElement : MonoBehaviour
{
    public TMP_Text categoryNameText;
    public Toggle toggle;

    [HideInInspector] public CategoryManager.Category CategoryData;

    public void Setup(CategoryManager.Category category, int depth)
    {
        CategoryData = category;
        categoryNameText.text = category.Name;
    }
}
