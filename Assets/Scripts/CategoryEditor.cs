using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CategoryEditor : MonoBehaviour
{
    [Header("UI References")]
    [Header("Header")]
    [SerializeField] private TMP_InputField categoryName;
    [SerializeField] private TMP_Dropdown categorySortDropdown;
    [SerializeField] private Button addCategory;
    [SerializeField] private Button renameButton;
    [SerializeField] private Button deleteButton;
    [SerializeField] private Button setImageButton;
    [SerializeField] private Button addButton;
    [SerializeField] private Button openButton;
    [SerializeField] private Button selectButton;
    [SerializeField] private Button backButton;
    [SerializeField] private TMP_Text currentPathLabel;
    [Header("Footer")]
    [SerializeField] private TMP_Dropdown quizSortDropdown;
    [SerializeField] private Button startQuizz;
    [SerializeField] private SliderHandler startAmount;
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
    private List<string> sortedQuizJsons = new();


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
        // Quiz sort
        quizSortDropdown.ClearOptions();
        quizSortDropdown.AddOptions(new List<string> { "Name (A–Z)", "Name (Z–A)", "Random" });
        quizSortDropdown.onValueChanged.AddListener((int index) =>
        {
            LoadQuizzes();
        });
        // Category sort
        categorySortDropdown.ClearOptions();
        categorySortDropdown.AddOptions(new List<string> { "Name (A–Z)", "Name (Z–A)" });
        categorySortDropdown.onValueChanged.AddListener((int index) => RefreshUI());
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
        // 1. Clear UI
        foreach (Transform child in quizScrollRect.content)
            Destroy(child.gameObject);

        // 2. Clear our cache
        sortedQuizJsons.Clear();

        string quizFolderPath = Path.Combine(Application.persistentDataPath, "quizzes");
        if (!Directory.Exists(quizFolderPath))
            return;

        // 3. Determine paths to search
        List<string> selectedPaths = new List<string>();
        if (quizFilterCategories.Count == 0)
        {
            // Fallback: If nothing in filter, use current path (optional, based on your logic)
            // If you want it empty when no filter, keep it empty. 
            // But usually, you want to show the current folder's quizzes:
            /* string currentPath = string.Join("/", pathStack.Reverse().Skip(1));
               selectedPaths.Add(currentPath); 
            */
            // Based on your original code, if count == 0, we return.
            return;
        }
        selectedPaths.AddRange(quizFilterCategories);

        string[] quizFiles = Directory.GetFiles(quizFolderPath, "*.json");
        HashSet<string> addedFiles = new HashSet<string>();

        // We use a temporary list of tuples to handle sorting before saving
        List<(QuizMakerNew.Quiz quizObj, string jsonString)> tempQuizList = new();

        // 4. Load and Filter
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
                            tempQuizList.Add((quiz, json));
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

        // 5. Apply Sorting
        int sortMode = quizSortDropdown.value; // 0=A-Z, 1=Z-A, 2=Random

        if (sortMode == 0)
        {
            tempQuizList = tempQuizList.OrderBy(x => x.quizObj.quizName).ToList();
        }
        else if (sortMode == 1)
        {
            tempQuizList = tempQuizList.OrderByDescending(x => x.quizObj.quizName).ToList();
        }
        else if (sortMode == 2)
        {
            System.Random rng = new System.Random();
            tempQuizList = tempQuizList.OrderBy(x => rng.Next()).ToList();
        }

        // 6. Populate UI and Cache the JSONs
        foreach (var item in tempQuizList)
        {
            // Add to our cached list for StartQuizz to use
            sortedQuizJsons.Add(item.jsonString);

            // Build UI
            GameObject quizItem = new GameObject("QuizItem", typeof(RectTransform), typeof(TextMeshProUGUI), typeof(Button));
            quizItem.transform.SetParent(quizScrollRect.content, false);

            TextMeshProUGUI text = quizItem.GetComponent<TextMeshProUGUI>();
            text.text = item.quizObj.quizName;
            text.fontSize = 20;
            text.enableWordWrapping = true;

            var quizBtn = quizItem.GetComponent<Button>();
            var capturedQuiz = item.quizObj; // Capture for lambda
            quizBtn.onClick.AddListener(() =>
            {
                quizMaker.OpenQuiz(capturedQuiz);
                navigation.ShowPanel(navigation.link[1].panel);
            });
        }

        // Update start button interactivity
        startQuizz.interactable = sortedQuizJsons.Count > 0;
    }

    // Keep the original (misspelled) method name for compatibility, but update its implementation.
    public void RefreshCategriesEditor()
    {
        if (string.IsNullOrEmpty(categoriesJsonFilePath))
            return;

        // Capture current path names so we can attempt to restore the view after reloading.
        var savedPathNames = pathStack.Reverse().ToList();

        // Reload categories from file
        LoadCategories();

        // Clear existing navigation state and selections
        navigationStack.Clear();
        pathStack.Clear();
        selectedCategories.Clear();

        // Rebuild the navigation stack from the saved path names.
        // Start from root list
        List<CategoryManager.Category> currentList = rootData.categories;

        if (savedPathNames.Count == 0)
        {
            // Ensure at least root is displayed
            EnterCategory(rootData.categories, "Root");
        }
        else
        {
            for (int i = 0; i < savedPathNames.Count; i++)
            {
                string name = savedPathNames[i];

                if (i == 0)
                {
                    // First entry should be root (or whatever label was used)
                    EnterCategory(currentList, name);
                }
                else
                {
                    // Find the category with the saved name in the current list
                    var found = currentList.FirstOrDefault(c => string.Equals(c.Name, name, StringComparison.Ordinal));
                    if (found == null)
                    {
                        // Path no longer exists in the reloaded data; stop restoring deeper levels
                        break;
                    }

                    if (found.subCategories == null)
                        found.subCategories = new List<CategoryManager.Category>();

                    currentList = found.subCategories;
                    EnterCategory(currentList, found.Name);
                }
            }
        }

        // Final UI/toolbar/quiz refresh
        HandleToolbarButtons();
        LoadQuizzes();
    }

    // Optional: provide correctly spelled public method that forwards to the existing one.
    public void RefreshCategoriesEditor()
    {
        RefreshCategriesEditor();
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
        BuildCategoryList(categorySortDropdown.value);
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
        if (sortedQuizJsons.Count == 0)
        {
            Debug.LogWarning("No quizzes available to start.");
            return;
        }

        if (quizPlayer == null)
        {
            Debug.LogError("QuizPlayer reference missing.");
            return;
        }

        // 1. Determine how many to play
        bool unlimited = (int)startAmount.value == -1;
        int limit = unlimited ? sortedQuizJsons.Count : Mathf.Max(1, (int)startAmount.value);

        // 2. Slice the cached list
        // This respects the exact order (Random, A-Z) currently visible in the UI
        List<string> quizzesToPlay = sortedQuizJsons.Take(limit).ToList();

        // 3. Launch Player
        quizPlayer.gameObject.SetActive(true);
        quizPlayer.SetMultipleJsonStrings(quizzesToPlay);
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
