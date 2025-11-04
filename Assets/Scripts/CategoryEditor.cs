using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryLibrary : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private ScrollRect categoryScrollRect;
    [SerializeField] private GameObject categoryPrefab;
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button setImageButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text currentPathLabel;
    [Header("Setup")]
    [SerializeField] ImageLibrary imageLibrary;

    private List<CategoryElement> selectedCategories = new();

    private string jsonFilePath;
    private CategoryManager.CategoryListWrapper rootData = new();
    private List<CategoryManager.Category> currentCategoryList; // what we’re displaying now
    private Stack<List<CategoryManager.Category>> navigationStack = new(); // for back navigation
    private Stack<string> pathStack = new(); // for displaying the path

    private void Awake()
    {
        jsonFilePath = Path.Combine(Application.persistentDataPath, "categories.json");
    }

    private void Start()
    {
        SetupSortDropdown();
        LoadCategories();
        EnterCategory(rootData.categories, "Root");
    }


    private void SetupSortDropdown()
    {
        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string> { "Name (A–Z)", "Name (Z–A)" });
        sortDropdown.onValueChanged.AddListener((int index) => RefreshUI());
    }

    // ---------------- LOAD / SAVE ----------------

    private void LoadCategories()
    {
        if (!File.Exists(jsonFilePath))
        {
            var defaultCategory = new CategoryManager.Category
            {
                Name = "General",
                Description = "Default category",
                quizFiles = new List<string>(),
                subCategories = new List<CategoryManager.Category>()
            };

            rootData.categories = new List<CategoryManager.Category> { defaultCategory };
            SaveCategories();
        }
        else
        {
            string json = File.ReadAllText(jsonFilePath);
            rootData = JsonUtility.FromJson<CategoryManager.CategoryListWrapper>(json);
        }
    }

    private void SaveCategories()
    {
        string json = JsonUtility.ToJson(rootData, true);
        File.WriteAllText(jsonFilePath, json);
    }

    // ---------------- NAVIGATION ----------------

    public void EnterCategory(List<CategoryManager.Category> listToShow, string pathName)
    {
        currentCategoryList = listToShow;
        navigationStack.Push(listToShow);
        pathStack.Push(pathName);

        RefreshUI();
        UpdatePathLabel();
    }

    public void GoBack()
    {
        if (navigationStack.Count <= 1)
            return;

        navigationStack.Pop();
        pathStack.Pop();

        currentCategoryList = navigationStack.Peek();
        RefreshUI();
        UpdatePathLabel();
        HandleToolbarButtons();
    }

    private void UpdatePathLabel()
    {
        currentPathLabel.text = string.Join(" / ", pathStack.Reverse());
        backButton.interactable = navigationStack.Count > 1;
    }

    // ---------------- BUILD UI ----------------

    private void RefreshUI()
    {
        BuildCategoryList(sortDropdown.value);
    }

    private void BuildCategoryList(int sortMode)
    {
        foreach (Transform child in categoryScrollRect.content)
            Destroy(child.gameObject);

        var sorted = sortMode == 1
            ? currentCategoryList.OrderByDescending(c => c.Name).ToList()
            : currentCategoryList.OrderBy(c => c.Name).ToList();

        foreach (var cat in sorted)
        {
            var item = Instantiate(categoryPrefab, categoryScrollRect.content);
            var elem = item.GetComponent<CategoryElement>();
            elem.Setup(cat, 0);
            elem.toggle.onValueChanged.AddListener((bool isOn) => OnCategorySelected(elem, isOn));
        }

        HandleToolbarButtons();
    }

    private void OnCategorySelected(CategoryElement elem, bool selected)
    {
        if (selected)
            selectedCategories.Add(elem);
        else
            selectedCategories.Remove(elem);

        HandleToolbarButtons();
    }


    private void HandleToolbarButtons()
    {
        bool hasSelection = selectedCategories.Count > 0;
        bool singleSelection = selectedCategories.Count == 1;

        renameButton.interactable = singleSelection;
        deleteButton.interactable = hasSelection;
        openButton.interactable = singleSelection; // Always openable when one is selected
        setImageButton.interactable = singleSelection;
    }


    // ---------------- CATEGORY ACTIONS ----------------

    public void OpenSelectedCategory()
    {
        if (selectedCategories.Count != 1)
            return;

        var cat = selectedCategories[0].CategoryData;

        // Ensure subcategory list exists
        if (cat.subCategories == null)
            cat.subCategories = new List<CategoryManager.Category>();

        // Clear current selections before rebuilding UI
        selectedCategories.Clear();

        // Enter the subcategory view
        EnterCategory(cat.subCategories, cat.Name);

        // Refresh toolbar buttons (disable everything)
        HandleToolbarButtons();
    }

    public void SetImage()
    {
        if (selectedCategories.Count != 1)
        {
            Debug.LogWarning("Please select exactly one category to set an image.");
            return;
        }

        var categoryElement = selectedCategories[0];

        // Safety check
        if (imageLibrary == null)
        {
            Debug.LogError("ImageLibrary reference not assigned in the inspector!");
            return;
        }

        imageLibrary.mode = ImageLibrary.Mode.Select;

        // Enable the image library UI
        imageLibrary.gameObject.SetActive(true);

        // Temporarily disable the button to prevent re-entry
        setImageButton.interactable = false;

        // Assign callback to handle when the user selects an image
        imageLibrary.onSelectCallback = (string fullPath) =>
        {
            if (string.IsNullOrEmpty(fullPath))
            {
                Debug.LogWarning("No image path returned from ImageLibrary.");
                imageLibrary.gameObject.SetActive(false);
                setImageButton.interactable = true;
                return;
            }

            // ✅ Convert full path to relative path within persistent data
            string persistentPath = Application.persistentDataPath;
            string relativePath = fullPath.Replace(persistentPath + Path.DirectorySeparatorChar, "");

            // ✅ Store only relative path
            categoryElement.CategoryData.ImageFile = relativePath;

            Debug.Log($"Stored relative image path: {relativePath}");

            // ✅ Close the image library UI
            imageLibrary.gameObject.SetActive(false);
            setImageButton.interactable = true;

            // ✅ Save and refresh
            SaveCategories();
            RefreshUI();
        };
    }





    public void AddCategory(TMP_InputField nameField)
    {
        var newCat = new CategoryManager.Category
        {
            Name = nameField.text,
            Description = "",
            quizFiles = new List<string>(),
            subCategories = new List<CategoryManager.Category>()
        };

        currentCategoryList.Add(newCat);
        SaveCategories();
        RefreshUI();
    }

    public void DeleteCategory()
    {
        foreach (var elem in selectedCategories)
            currentCategoryList.Remove(elem.CategoryData);

        SaveCategories();
        RefreshUI();
    }

    public void RenameCategory(TMP_InputField nameField)
    {
        if (selectedCategories.Count != 1)
            return;

        string newName = nameField.text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Debug.LogWarning("Category name cannot be empty.");
            return;
        }

        var cat = selectedCategories[0].CategoryData;
        cat.Name = newName;

        SaveCategories();

        // Clear selection and refresh UI
        selectedCategories.Clear();
        RefreshUI();
        HandleToolbarButtons();
    }


}
