using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class CategoryManager : MonoBehaviour
{
    [System.Serializable]
    public class Category
    {
        public string Name;
        public string Description;
        public List<string> quizFiles;
        public List<Category> subCategories; // Hierarchical child categories
    }

    [System.Serializable]
    public class CategoryListWrapper
    {
        public List<Category> categories;
    }

    public List<Category> categories = new List<Category>();

    private string jsonFilePath;

    void Awake()
    {
        // categories.json will live in persistent data folder
        jsonFilePath = Path.Combine(Application.persistentDataPath, "categories.json");
    }

    void Start()
    {
        LoadCategories();
        PrintCategories();
    }

    /// <summary>
    /// Loads categories.json from persistent data path. 
    /// If missing, creates a default file.
    /// </summary>
    public void LoadCategories()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogWarning($"categories.json not found in {Application.persistentDataPath}. Creating default file...");
            CreateDefaultCategoriesFile();
        }

        string json = File.ReadAllText(jsonFilePath);
        CategoryListWrapper wrapper = JsonUtility.FromJson<CategoryListWrapper>(json);

        if (wrapper != null && wrapper.categories != null)
        {
            categories = wrapper.categories;
            Debug.Log($" Loaded {categories.Count} categories from {jsonFilePath}");
        }
        else
        {
            Debug.LogError("Failed to parse categories.json or file was empty.");
        }
    }

    /// <summary>
    /// Saves the current categories list back to persistent data path.
    /// </summary>
    public void SaveCategories()
    {
        CategoryListWrapper wrapper = new CategoryListWrapper { categories = categories };
        string json = JsonUtility.ToJson(wrapper, true);
        File.WriteAllText(jsonFilePath, json);
        Debug.Log($"Saved categories to {jsonFilePath}");
    }

    /// <summary>
    /// Creates a sample categories.json file if none exists.
    /// </summary>
    private void CreateDefaultCategoriesFile()
    {
        Category sample = new Category
        {
            Name = "General Knowledge",
            Description = "Default category",
            quizFiles = new List<string> { "sample_quiz.json" },
            subCategories = new List<Category>()
        };

        categories = new List<Category> { sample };

        SaveCategories();
    }

    /// <summary>
    /// Recursively print all categories and quizzes in hierarchy.
    /// </summary>
    public void PrintCategories()
    {
        foreach (var category in categories)
        {
            PrintCategoryRecursive(category, 0);
        }
    }

    private void PrintCategoryRecursive(Category category, int depth)
    {
        string indent = new string(' ', depth * 2);
        Debug.Log($"{indent} {category.Name} — {category.Description}");

        if (category.quizFiles != null)
        {
            foreach (var quiz in category.quizFiles)
                Debug.Log($"{indent} Quiz: {quiz}");
        }

        if (category.subCategories != null)
        {
            foreach (var sub in category.subCategories)
                PrintCategoryRecursive(sub, depth + 1);
        }
    }
}
