using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ImageLibrary : MonoBehaviour
{
    [Header("Tools")]
    [SerializeField] private Button rename;
    [SerializeField] private Button delete;
    [Header("Setup")]
    [SerializeField] private ScrollRect imageScrollRect;
    [SerializeField] private GameObject imagePrefab;
    [SerializeField] private TMP_Dropdown sortDropdown;

    [Header("Hidden")]
    private List<ImageElement> SelectedElements = new();

    private void Start()
    {
        // Example options: 0 = Name (A-Z), 1 = Name (Z-A), 2 = Date (Newest), 3 = Date (Oldest)
        sortDropdown.ClearOptions();
        sortDropdown.AddOptions(new List<string> { "Name (A–Z)", "Name (Z–A)", "Date (Newest)", "Date (Oldest)" });
        sortDropdown.onValueChanged.AddListener(OnSortOptionChanged);
        RefreshImageList();
    }

    private void OnSortOptionChanged(int optionIndex)
    {
        BuildImageLibrary(optionIndex);
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
            _imagePrefab.GetComponent<Toggle>().onValueChanged.AddListener((bool value) => { 
                NumberOfSelection(); 
            });
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
            _imagePrefab.GetComponent<Image>().sprite = sprite;
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

        // Populate UI
        foreach (var imagePath in imagePaths)
        {
            var prefabInstance = Instantiate(imagePrefab, imageScrollRect.content);
            var imageElemScript = prefabInstance.GetComponent<ImageElement>();

            imageElemScript.imageName.text = Path.GetFileName(imagePath);
            imageElemScript.imagePath = imagePath;
            prefabInstance.GetComponent<Toggle>().onValueChanged.AddListener((bool value) => { NumberOfSelection(); });

            byte[] imageBytes = File.ReadAllBytes(imagePath);
            Texture2D texture = new Texture2D(2, 2);
            if (!texture.LoadImage(imageBytes))
            {
                Debug.LogError("Failed to load image from bytes!");
                continue;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            prefabInstance.GetComponent<Image>().sprite = sprite;
        }
    }


    /// <summary>
    /// Retrieves a list of image file paths in the "Images" folder under Application.persistentDataPath.
    /// </summary>
    public static List<string> GetImageFilePaths()
    {
        string imagesFolderPath = Path.Combine(Application.persistentDataPath, "Images");

        List<string> imagePaths = new List<string>();

        // Create the folder if it doesn't exist
        if (!Directory.Exists(imagesFolderPath))
        {
            Debug.LogWarning($"Images folder not found. Creating new folder at: {imagesFolderPath}");
            Directory.CreateDirectory(imagesFolderPath);
            return imagePaths; // Return empty list since no files yet
        }

        // Get all image files with supported extensions
        string[] supportedExtensions = { "*.png", "*.jpg", "*.jpeg" };
        foreach (string ext in supportedExtensions)
        {
            string[] files = Directory.GetFiles(imagesFolderPath, ext, SearchOption.TopDirectoryOnly);
            imagePaths.AddRange(files);
        }

        return imagePaths;
    }

    public void RefreshImageList()
    {
        ClearImageList();
        BuildImageLibrary(sortDropdown.value);
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
            if (child.GetComponent<ImageElement>().isSelected)
                SelectedElements.Add(child.gameObject.GetComponent<ImageElement>());
        }
        HandleTools();
        return SelectedElements.Count;
    }

    private void HandleTools()
    {
        if (SelectedElements.Count == 1)
        {
            rename.interactable = true;
        }
        else
        {
            rename.interactable = false;
        }

        if (SelectedElements.Count > 0)
        {
            delete.interactable = true;
        }
        else
        {
            delete.interactable = false;
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

    public void ImportImage()
    {
        // Define allowed image file types
        string[] imageFileTypes = new string[]
        {
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
                string destPath = Path.Combine(imagesDir, fileName);

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
}
