using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageLibrary : MonoBehaviour
{
    [System.Serializable]
    public enum Mode
    {
        Edit,
        Select
    }

    [Header("Tools")]
    [SerializeField] private Button import;
    [SerializeField] private TMP_InputField input;
    [SerializeField] private Button open;
    [SerializeField] private Button cancel;
    [SerializeField] private Button rename;
    [SerializeField] private Button delete;
    [SerializeField] private Button goBack;
    [Header("Setup")]
    [SerializeField] public Mode mode;
    [SerializeField] private ScrollRect imageScrollRect;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private TMP_Dropdown sortDropdown;

    [Header("Hidden")]
    public List<ImageElement> SelectedElements = new();
    public Action<string> onSelectCallback;


    // New: ToggleGroup to enforce single selection in Select mode
    private ToggleGroup selectionGroup;
    private string rootFilePath;
    private string currentFilePath;

    private void Start()
    {
        rootFilePath = Path.Combine(Application.persistentDataPath, "Images");
        currentFilePath = rootFilePath;

        // Example options: 0 = Name (A-Z), 1 = Name (Z-A), 2 = Date (Newest), 3 = Date (Oldest)
        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string> { "Name (A–Z)", "Name (Z–A)", "Date (Newest)", "Date (Oldest)" });
        sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        RefreshImageList();
    }

    private void OnEnable()
    {
        RefreshImageList();
    }

    private void OnSortOptionChanged(int optionIndex)
    {
        BuildImageLibrary(optionIndex);
    }

    // Ensure a ToggleGroup exists on the content transform (for Select mode)
    private void EnsureToggleGroup()
    {
        if (imageScrollRect == null || imageScrollRect.content == null) return;

        if (selectionGroup == null)
        {
            selectionGroup = imageScrollRect.content.GetComponent<ToggleGroup>();
            if (selectionGroup == null)
                selectionGroup = imageScrollRect.content.gameObject.AddComponent<ToggleGroup>();

            // allowSwitchOff true -> user can unselect the single selected toggle
            selectionGroup.allowSwitchOff = true;
        }
    }

    // Remove ToggleGroup (when switching back to Edit mode)
    private void RemoveToggleGroup()
    {
        if (selectionGroup != null)
        {
            Destroy(selectionGroup);
            selectionGroup = null;
        }
    }

    private void BuildImageLibrary()
    {
        if (imageScrollRect == null) { return; }
        string imagesFolderPath = Path.Combine(Application.persistentDataPath, "Images");

        foreach (var images in GetImageFilePaths())
        {
            var _imagePrefab = Instantiate(imagePrefab, imageScrollRect.content);
            var imageElemScript = _imagePrefab.GetComponent<ImageElement>();

            imageElemScript.imageName.text = Path.GetFileName(images);
            imageElemScript.imagePath = images;

            Toggle t = imageElemScript.toggle;
            if (t != null)
            {
                // assign to group if in Select mode
                if (mode == Mode.Select)
                {
                    EnsureToggleGroup();
                    t.group = selectionGroup;
                }
                else
                {
                    t.group = null;
                }

                // Ensure ImageElement selection reflects toggle and update selection count on change
                t.isOn = imageElemScript.isSelected;
                t.onValueChanged.AddListener((bool value) =>
                {
                    if (imageElemScript != null)
                        imageElemScript.isSelected = value;
                    NumberOfSelection();
                });
            }

            // Read image bytes
            byte[] imageBytes = File.ReadAllBytes(images);

            // Create a Texture2D and load the image data
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError("Failed to load image from bytes!");
                return;
            }

            // Create a new Sprite from the texture
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f)
            );

            // Assign the sprite to the UI Image
            imageElemScript.image.sprite = sprite;
        }
    }

    private void BuildImageLibrary(int sortMode = 0)
    {
        if (imageScrollRect == null) { return; }

        string imagesFolderPath = Path.Combine(Application.persistentDataPath, "Images");
        var imagePaths = GetImageFilePaths();

        // Apply sorting before displaying
        switch (sortMode)
        {
            case 0: // Name (A-Z)
                imagePaths = imagePaths.OrderBy(f => Path.GetFileName(f)).ToList();
                break;
            case 1: // Name (Z-A)
                imagePaths = imagePaths.OrderByDescending(f => Path.GetFileName(f)).ToList();
                break;
            case 2: // Date (Newest)
                imagePaths = imagePaths.OrderByDescending(f => File.GetCreationTime(f)).ToList();
                break;
            case 3: // Date (Oldest)
                imagePaths = imagePaths.OrderBy(f => File.GetCreationTime(f)).ToList();
                break;
        }

        // Clear existing UI elements
        foreach (Transform child in imageScrollRect.content)
        {
            Destroy(child.gameObject);
        }

        // If we're in Select mode, ensure toggle group exists
        if (mode == Mode.Select)
            EnsureToggleGroup();
        else
            RemoveToggleGroup();

        // Populate UI
        foreach (var imagePath in imagePaths)
        {
            var prefabInstance = Instantiate(imagePrefab, imageScrollRect.content);
            var imageElemScript = prefabInstance.GetComponent<ImageElement>();

            imageElemScript.imageName.text = Path.GetFileName(imagePath);
            imageElemScript.imagePath = imagePath;

            // Set folder flag
            imageElemScript.isFolder = Directory.Exists(imagePath);

            // If it is a folder, skip image loading
            if (imageElemScript.isFolder)
            {
                imageElemScript.toggle.gameObject.SetActive(false);
                Button b = imageElemScript.button;
                b.onClick.AddListener(() =>
                {
                    currentFilePath = Path.Combine(currentFilePath, imageElemScript.imageName.text);
                    RefreshImageList();
                });
            }
            else
            {
                imageElemScript.button.gameObject.SetActive(false);
                Toggle t = imageElemScript.toggle;
                if (t != null)
                {
                    if (mode == Mode.Select)
                    {
                        EnsureToggleGroup();
                        t.group = selectionGroup;
                    }
                    else
                    {
                        t.group = null;
                    }

                    // initialize isSelected to toggle state and keep them in sync
                    t.isOn = imageElemScript.isSelected;
                    t.onValueChanged.AddListener((bool value) =>
                    {
                        if (imageElemScript != null)
                            imageElemScript.isSelected = value;
                        NumberOfSelection();
                    });
                }

                byte[] imageBytes = File.ReadAllBytes(imagePath);
                Texture2D texture = new Texture2D(2, 2);
                if (!texture.LoadImage(imageBytes))
                {
                    Debug.LogError("Failed to load image from bytes!");
                    continue;
                }

                Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                imageElemScript.image.sprite = sprite;
            }

        }
    }


    /// <summary>
    /// Retrieves a list of image file paths in the "Images" folder under Application.persistentDataPath.
    /// </summary>
    public List<string> GetImageFilePaths()
    {
        string imagesFolderPath = currentFilePath;

        List<string> results = new List<string>();

        if (!Directory.Exists(imagesFolderPath))
        {
            Directory.CreateDirectory(imagesFolderPath);
            return results;
        }

        // Add subfolders
        string[] folders = Directory.GetDirectories(imagesFolderPath, "*", SearchOption.TopDirectoryOnly);
        results.AddRange(folders);

        // Add image files
        string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg" };
        foreach (string ext in supportedExtensions)
        {
            string[] files = Directory.GetFiles(imagesFolderPath, ext, SearchOption.TopDirectoryOnly);
            results.AddRange(files);
        }

        return results;
    }


    public void RefreshImageList()
    {
        ClearImageList();
        SetupMode();
        BuildImageLibrary(sortDropdown.value);
        HandleTools();
    } 

    private void SetupMode()
    {
        switch (mode)
        {
            case Mode.Edit:
                // Remove toggle group in edit mode so multiple selections are possible
                RemoveToggleGroup();

                rename.gameObject.SetActive(true);
                delete.gameObject.SetActive(true);
                import.gameObject.SetActive(true);
                input.gameObject.SetActive(true);
                open.gameObject.SetActive(false);
                cancel.gameObject.SetActive(false);
                break;
            case Mode.Select:
                // Ensure toggle group exists for single selection
                EnsureToggleGroup();

                rename.gameObject.SetActive(false);
                delete.gameObject.SetActive(false);
                import.gameObject.SetActive(false);
                input.gameObject.SetActive(false);
                open.gameObject.SetActive(true);
                cancel.gameObject.SetActive(true);
                break;
        }
        // clear any previous selection state
        SelectedElements.Clear();
    }

    private void ClearImageList()
    {
        if (imageScrollRect == null || imageScrollRect.content == null)
            return;

        Transform contentTransform = imageScrollRect.content;

        // Destroy all existing children under the ScrollRect content
        for (int i = contentTransform.childCount - 1; i >= 0; i--)
        {
            Destroy(contentTransform.GetChild(i).gameObject);
        }
    }

    private int NumberOfSelection()
    {
        SelectedElements.Clear();
        foreach (Transform child in imageScrollRect.content)
        {
            var elem = child.GetComponent<ImageElement>();
            if (elem != null && elem.isSelected)
            {
                SelectedElements.Add(elem); 
                input.text = Path.GetFileNameWithoutExtension(elem.imageName.text);
            }
        }
        HandleTools();
        return SelectedElements.Count;
    }

    private void HandleTools()
    {
        if (SelectedElements.Count == 1)
        {
            rename.interactable = true;
            input.interactable = true;
            open.interactable = true;
        }
        else
        {
            rename.interactable = false;
            input.interactable = false;
            open.interactable = false;
        }

        if (SelectedElements.Count > 0)
        {
            delete.interactable = true;
        }
        else
        {
            delete.interactable = false;
        }

        if (Path.GetFullPath(currentFilePath) == Path.GetFullPath(rootFilePath))
        {
            goBack.interactable = false;
        }
        else
        {
            goBack.interactable = true;
        }
    }

    public void DeleteImage()
    {
        if (SelectedElements.Count < 1) { return; }
        var _SelectedElements = SelectedElements;
        foreach (ImageElement image in _SelectedElements)
        {
            if (image.isSelected)
            {
                image.DeleteImage();
                Destroy(image.gameObject);
            }
        }
        Invoke(nameof(RefreshImageList), 0.05f);
        Invoke(nameof(NumberOfSelection), 0.05f);
    }

    public void OpenImage()
    {
        // Allow renaming only when exactly one image is selected
        if (SelectedElements.Count != 1)
        {
            Debug.LogWarning("Please select exactly one image to rename.");
            return;
        }

        var imageElement = SelectedElements[0];
        var _path = imageElement.imagePath.Replace("\\", "/");
        var _correctedPath = _path.Replace(Application.persistentDataPath, "").TrimStart('/');
        onSelectCallback?.Invoke(_correctedPath);
        mode = Mode.Edit;
    }

    public void CancelSelection()
    {
        mode = Mode.Edit;
        onSelectCallback?.Invoke(null);
    }

    public void RenameImage(TMP_InputField newName)
    {
        // Allow renaming only when exactly one image is selected
        if (SelectedElements.Count != 1)
        {
            Debug.LogWarning("Please select exactly one image to rename.");
            return;
        }

        // Get selected image
        var imageElement = SelectedElements[0];

        // Validate the new name
        string newFileName = newName.text.Trim();
        if (string.IsNullOrEmpty(newFileName))
        {
            Debug.LogWarning("New image name cannot be empty.");
            return;
        }

        // Preserve the original file extension
        string oldPath = imageElement.imagePath;
        string extension = Path.GetExtension(oldPath);
        string folder = Path.GetDirectoryName(oldPath);
        string newPath = Path.Combine(folder, newFileName + extension);

        try
        {
            // If a file with that name already exists, create a unique one
            int counter = 1;
            while (File.Exists(newPath))
            {
                newPath = Path.Combine(folder, $"{newFileName}_{counter}{extension}");
                counter++;
            }

            // Rename (move) the file
            File.Move(oldPath, newPath);

            // Update the ImageElement data
            imageElement.imagePath = newPath;
            imageElement.imageName.text = Path.GetFileName(newPath);

            Debug.Log($"Image renamed to: {Path.GetFileName(newPath)}");

            // Optional: refresh the gallery
            RefreshImageList();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error while renaming image: {e.Message}");
        }
    }

    public Sprite GetOpenedImage()
    {
        if (SelectedElements.Count == 1)
        {
            var element = SelectedElements[0];
            var image = element.image;
            if (image != null)
                return image.sprite;
        }

        Debug.LogWarning("No image selected or multiple selections.");
        return null;
    }

    public void GoUpOneFolder()
    {
        if (string.IsNullOrEmpty(currentFilePath) || string.IsNullOrEmpty(rootFilePath))
            return;

        string parent = Directory.GetParent(currentFilePath)?.FullName;
        if (parent == null)
            return;

        // Prevent going above the root
        string normalizedParent = Path.GetFullPath(parent);
        string normalizedRoot = Path.GetFullPath(rootFilePath);

        if (normalizedParent.StartsWith(normalizedRoot))
        {
            currentFilePath = normalizedParent;
            RefreshImageList();
        }
    }

    public void ImportImage()
    {
        // Define allowed image file types
        string[] imageFileTypes = new string[] {
            NativeFilePicker.ConvertExtensionToFileType("png"),
            NativeFilePicker.ConvertExtensionToFileType("jpg"),
            NativeFilePicker.ConvertExtensionToFileType("jpeg")
        };

        // Pick an image file
        NativeFilePicker.PickFile((path) =>
        {
            if (path == null)
            {
                Debug.Log("Import cancelled by user.");
                return;
            }

            Debug.Log("Picked image: " + path);

            try
            {
                // Create target directory if not exists
                string imagesDir = Path.Combine(Application.persistentDataPath, "images");
                if (!Directory.Exists(imagesDir))
                    Directory.CreateDirectory(imagesDir);

                // Get file name
                string fileName = Path.GetFileName(path);
                string destPath = Path.Combine(currentFilePath, fileName);

                // If file with same name exists, create unique name
                int counter = 1;
                while (File.Exists(destPath))
                {
                    string nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
                    string ext = Path.GetExtension(fileName);
                    destPath = Path.Combine(imagesDir, $"{nameWithoutExt}_{counter}{ext}");
                    counter++;
                }

                // Copy image to persistent data folder
                File.Copy(path, destPath);

                Debug.Log($"Image successfully imported to: {destPath}");

                // Optional: refresh UI or trigger event
                // profileListToRefresh.RefreshProfileList(); 
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error while importing image: {e.Message}");
            }

        }, imageFileTypes);

        RefreshImageList();
    }

    public void ExportAllPersistentData()
    {
        try
        {
            string sourceDir = Application.persistentDataPath;
            string tempDir = Path.Combine(sourceDir, "ExportPersistentTemp");
            string archivePath = Path.Combine(sourceDir, "PersistentDataBackup.zip");

            if (Directory.Exists(tempDir))
                Directory.Delete(tempDir, true);
            Directory.CreateDirectory(tempDir);

            CopyDirectoryRecursive(sourceDir, tempDir);

            if (File.Exists(archivePath))
                File.Delete(archivePath);

            ZipFile.CreateFromDirectory(tempDir, archivePath);

            Directory.Delete(tempDir, true);

            NativeFilePicker.ExportFile(archivePath, (success) =>
            {
                try
                {
                    if (File.Exists(archivePath))
                        File.Delete(archivePath);
                }
                catch (System.Exception cleanupErr)
                {
                    Debug.LogError("Failed to delete temp archive: " + cleanupErr.Message);
                }
            });
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error exporting persistent data: " + e.Message);
        }
    }

    private void CopyDirectoryRecursive(string source, string destination)
    {
        if (!Directory.Exists(destination))
            Directory.CreateDirectory(destination);

        foreach (string dir in Directory.GetDirectories(source))
        {
            if (dir.EndsWith("ExportPersistentTemp"))
                continue;

            string targetSubDir = Path.Combine(destination, Path.GetFileName(dir));
            CopyDirectoryRecursive(dir, targetSubDir);
        }

        foreach (string file in Directory.GetFiles(source))
        {
            string targetFile = Path.Combine(destination, Path.GetFileName(file));
            File.Copy(file, targetFile, true);
        }
    }

    public void ImportPersistentDataZip()
    {
        string[] fileTypes = {
        NativeFilePicker.ConvertExtensionToFileType("zip")
    };

        NativeFilePicker.PickFile((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("Import cancelled.");
                return;
            }

            try
            {
                if (!File.Exists(path))
                {
                    Debug.LogError("Selected file not found.");
                    return;
                }

                string persistent = Application.persistentDataPath;

                // Clean current persistent data
                foreach (string dir in Directory.GetDirectories(persistent))
                {
                    Directory.Delete(dir, true);
                }
                foreach (string file in Directory.GetFiles(persistent))
                {
                    File.Delete(file);
                }

                // Extract the zip
                ZipFile.ExtractToDirectory(path, persistent);

                Debug.Log("Persistent data imported successfully.");
            }
            catch (System.Exception e)
            {
                Debug.LogError("Error importing persistent data: " + e.Message);
            }

        }, fileTypes);
    }
}
