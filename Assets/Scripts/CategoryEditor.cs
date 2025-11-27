using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryLibrary : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField categoryName;
    [SerializeField] private TMP_Dropdown sortDropdown;
    [SerializeField] private Button addCategory;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button setImageButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text currentPathLabel;
    [SerializeField] private Button startQuizz;
    [Header("Setup")]
    [SerializeField] private GameObject categoryPrefab;
    [SerializeField] private ScrollRect quizScrollRect;
    [SerializeField] private ScrollRect categoryScrollRect;
    [SerializeField] private ScrollRect categorySelectionScrollRect;
    [SerializeField] private ImageLibrary imageLibrary;
    [SerializeField] private QuizPlayer quizPlayer;
    [SerializeField] private QuizMakerNew quizMaker;
    [SerializeField] private Navigation navigation;
    [Header("DeleteConfirmation")]
    [SerializeField] private GameObject deleteConfirmationPanel;
    [SerializeField] private TextMeshProUGUI instructions;

    private List<CategoryElement> selectedCategories = new();
    private List<string> quizFilterCategories = new();


    private string categoriesJsonFilePath;
    private string quizzJsonFilePath;
    private CategoryManager.CategoryListWrapper rootData = new();
    private List<CategoryManager.Category> currentCategoryList; // what we’re displaying now
    private Stack<List<CategoryManager.Category>> navigationStack = new(); // for back navigation
    private Stack<string> pathStack = new(); // for displaying the path

    private void Awake()
    {
        categoriesJsonFilePath = Path.Combine(Application.persistentDataPath, "categories.json");
        quizzJsonFilePath = Path.Combine(Application.persistentDataPath, "");
    }

    private void Start()
    {
        SetupSortDropdown();
        LoadCategories();
        EnterCategory(rootData.categories, "Root");
        categoryName.onValueChanged.AddListener(HandleToolbarButtons);
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
        if (!File.Exists(categoriesJsonFilePath))
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
            string json = File.ReadAllText(categoriesJsonFilePath);
            rootData = JsonUtility.FromJson<CategoryManager.CategoryListWrapper>(json);
        }
    }


    private void LoadQuizzes()
    {
        foreach (Transform child in quizScrollRect.content)
            Destroy(child.gameObject);

        string quizFolderPath = Path.Combine(Application.persistentDataPath, "quizzes");
        if (!Directory.Exists(quizFolderPath))
            return;

        // Use full paths stored in quizFilterCategories, or fallback to current path
        List<string> selectedPaths = new List<string>();
        if (quizFilterCategories.Count > 0)
        {
            selectedPaths.AddRange(quizFilterCategories);
        }
        else
        {
            string currentCategoryPath = string.Join("/", pathStack.Reverse().Skip(1));
            selectedPaths.Add(currentCategoryPath);
        }

        string[] quizFiles = Directory.GetFiles(quizFolderPath, "*.json");
        HashSet<string> addedFiles = new HashSet<string>();
        List<QuizMakerNew.Quiz> quizzesToDisplay = new List<QuizMakerNew.Quiz>();

        foreach (string file in quizFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                QuizMakerNew.Quiz quiz = JsonUtility.FromJson<QuizMakerNew.Quiz>(json);
                if (quiz == null || string.IsNullOrEmpty(quiz.category))
                    continue;

                foreach (string catPath in selectedPaths)
                {
                    if (string.Equals(quiz.category, catPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!addedFiles.Contains(file))
                        {
                            quizzesToDisplay.Add(quiz);
                            addedFiles.Add(file);
                        }
                        break;
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"Failed to load quiz file {file}: {e.Message}");
            }
        }

        foreach (var quiz in quizzesToDisplay)
        {
            GameObject quizItem = new GameObject("QuizItem", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Button));
            quizItem.transform.SetParent(quizScrollRect.content, false);

            TextMeshProUGUI text = quizItem.GetComponent<TextMeshProUGUI>();
            text.text = quiz.quizName;
            text.fontSize = 20;
            text.enableWordWrapping = true;

            var quizBtn = quizItem.GetComponent<Button>();
            var capturedQuiz = quiz;
            quizBtn.onClick.AddListener(() =>
            {
                quizMaker.OpenQuiz(capturedQuiz);
                navigation.ShowPanel(navigation.link[1].panel);
            });
        }
    }



    private void SaveCategories()
    {
        string json = JsonUtility.ToJson(rootData, true);
        File.WriteAllText(categoriesJsonFilePath, json);
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

        // Clear UI selections to avoid invalid toggle references
        selectedCategories.Clear();
        RefreshUI();
        UpdatePathLabel();
        HandleToolbarButtons();
        LoadQuizzes();
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


    private void HandleToolbarButtons(string value)
    {
        HandleToolbarButtons();
    }

    private void HandleToolbarButtons()
    {
        bool hasSelection = selectedCategories.Count > 0;
        bool singleSelection = selectedCategories.Count == 1;

        renameButton.interactable = singleSelection && !String.IsNullOrEmpty(categoryName.text);
        deleteButton.interactable = hasSelection;
        openButton.interactable = singleSelection; // Always openable when one is selected
        setImageButton.interactable = singleSelection;

        startQuizz.interactable = quizScrollRect.content.transform.childCount > 0;
        addCategory.interactable = !String.IsNullOrEmpty(categoryName.text);

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

    public void StartQuizz()
    {
        string quizFolderPath = Path.Combine(Application.persistentDataPath, "quizzes");
        if (!Directory.Exists(quizFolderPath))
        {
            Debug.LogWarning("No quizzes folder found.");
            return;
        }

        List<string> selectedPaths = new List<string>();

        // If filters exist, use them
        if (quizFilterCategories.Count > 0)
        {
            selectedPaths.AddRange(quizFilterCategories);
        }
        else
        {
            // Fallback: current navigation path (exclude "Root")
            string currentCategoryPath = string.Join("/", pathStack.Reverse().Skip(1));
            selectedPaths.Add(currentCategoryPath);
        }

        string[] quizFiles = Directory.GetFiles(quizFolderPath, "*.json");
        List<(QuizMakerNew.Quiz quiz, string filePath)> matchedQuizzes = new();
        HashSet<string> addedFiles = new HashSet<string>();

        foreach (string file in quizFiles)
        {
            try
            {
                string json = File.ReadAllText(file);
                QuizMakerNew.Quiz quiz = JsonUtility.FromJson<QuizMakerNew.Quiz>(json);

                if (quiz == null || string.IsNullOrEmpty(quiz.category))
                    continue;

                foreach (string catPath in selectedPaths)
                {
                    if (string.Equals(quiz.category, catPath, StringComparison.OrdinalIgnoreCase))
                    {
                        if (!addedFiles.Contains(file))
                        {
                            matchedQuizzes.Add((quiz, file));
                            addedFiles.Add(file);
                        }
                        break;
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Failed to load quiz file {file}: {e.Message}");
            }
        }

        if (matchedQuizzes.Count == 0)
        {
            Debug.LogWarning("No quizzes found for the selected categories.");
            return;
        }

        if (quizPlayer == null)
        {
            Debug.LogError("QuizPlayer reference missing.");
            return;
        }

        quizPlayer.gameObject.SetActive(true);

        // Pass all quiz JSON files to the player
        List<string> quizJsonList = new List<string>();

        foreach (var (_, filePath) in matchedQuizzes)
        {
            quizJsonList.Add(File.ReadAllText(filePath));
        }

        quizPlayer.SetMultipleJsonStrings(quizJsonList);

        Debug.Log($"Started {matchedQuizzes.Count} quizzes.");
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

            //  Convert full path to relative path within persistent data
            string persistentPath = Application.persistentDataPath;
            string relativePath = fullPath.Replace(persistentPath + Path.DirectorySeparatorChar, "");

            //  Store only relative path
            categoryElement.CategoryData.ImageFile = relativePath;

            Debug.Log($"Stored relative image path: {relativePath}");

            //  Close the image library UI
            imageLibrary.gameObject.SetActive(false);
            setImageButton.interactable = true;

            //  Save and refresh
            SaveCategories();
            RefreshUI();
            selectedCategories.Clear();
            HandleToolbarButtons();
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
        selectedCategories.Clear();
        SaveCategories();
        RefreshUI();
    }

    public void RequestDelete()
    {
        deleteConfirmationPanel?.SetActive(true);

        string categoriesText = "None";

        if (selectedCategories != null && selectedCategories.Count > 0)
        {
            categoriesText = string.Join(", ",
                selectedCategories
                    .Where(c => c != null && c.categoryNameText != null)
                    .Select(c => c.categoryNameText.text));
        }

        instructions.text = "Are you sure you want to delete: " + categoriesText;
    }
    public void DeleteCategory()
    {
        foreach (var elem in selectedCategories)
            currentCategoryList.Remove(elem.CategoryData);

        deleteConfirmationPanel?.SetActive(false);
        selectedCategories.Clear();
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
    public void SelectCategories()
    {
        // base path parts for the current view (exclude "Root")
        List<string> basePathParts = pathStack.Reverse().Skip(1).ToList();

        foreach (var elem in selectedCategories)
        {
            string name = elem.CategoryData.Name;

            // Build the full path of the selected category
            var pathParts = new List<string>(basePathParts) { name };
            string fullPath = string.Join("/", pathParts);

            // Collect this path + all subcategories
            List<string> allPaths = new List<string>();
            CollectCategoryPaths(elem.CategoryData, fullPath, allPaths);

            foreach (string p in allPaths)
            {
                if (!quizFilterCategories.Contains(p))
                {
                    quizFilterCategories.Add(p);

                    GameObject item = new GameObject("SelectedCategory", typeof(RectTransform), typeof(TextMeshProUGUI));
                    item.transform.SetParent(categorySelectionScrollRect.content, false);

                    TextMeshProUGUI text = item.GetComponent<TextMeshProUGUI>();
                    text.text = p;
                    text.fontSize = 22;
                    text.enableWordWrapping = true;
                }
            }
        }

        LoadQuizzes();
        HandleToolbarButtons();
    }

    private void CollectCategoryPaths(
    CategoryManager.Category category,
    string currentPath,
    List<string> output)
    {
        output.Add(currentPath);

        if (category.subCategories == null)
            return;

        foreach (var sub in category.subCategories)
        {
            string subPath = currentPath + "/" + sub.Name;
            CollectCategoryPaths(sub, subPath, output);
        }
    }


    public void ClearSelectedCategories()
    {
        foreach (Transform child in categorySelectionScrollRect.content)
            Destroy(child.gameObject);

        quizFilterCategories.Clear();

        LoadQuizzes();
    }



}
