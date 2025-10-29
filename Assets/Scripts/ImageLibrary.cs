using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImageLibrary : MonoBehaviour
{
    [SerializeField] private ScrollRect imageScrollRect;
    [SerializeField] private GameObject imagePrefab;

    private void Start()
    {
        if (imageScrollRect == null) { return; }
        string imagesFolderPath = Path.Combine(Application.persistentDataPath, "Images");

        foreach (var images in GetImageFilePaths())
        {
            var _imagePrefab = Instantiate(imagePrefab, imageScrollRect.content);
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
}
