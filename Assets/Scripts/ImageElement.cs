using TMPro;
using System.IO;
using UnityEngine;
using UnityEngine.UI;


public class ImageElement : MonoBehaviour
{
    [Header("Parameters")]
    [SerializeField] public TextMeshProUGUI imageName;
    [SerializeField] public string imagePath;
    [SerializeField] public GameObject selectOverlay;
    public bool isFolder = false;
    [Header("Setup")]
    public Sprite folderIcon;
    public Image image;
    public Toggle toggle;
    public Button button;


    [HideInInspector] public bool isSelected = false;

    private void Start()
    {
        if (isFolder)
        {
            image.sprite = folderIcon;
        }
    }

    public void SelectImage(bool shouldSelect)
    {
        isSelected = shouldSelect;
        selectOverlay.SetActive(shouldSelect);
    }

    public void DeleteImage()
    {
        if (isSelected)
        {
            try
            {
                if (File.Exists(imagePath))
                {
                    File.Delete(imagePath);
                    Debug.Log($"Deleted image: {imagePath}");
                }
                else
                {
                    Debug.LogWarning($"Image not found at path: {imagePath}");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error deleting image: {e.Message}");
            }
        }
    }
}
