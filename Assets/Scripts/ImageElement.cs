using TMPro;
using System.IO;
using UnityEngine;


public class ImageElement : MonoBehaviour
{
    [SerializeField] public TextMeshProUGUI imageName;
    [SerializeField] public string imagePath;
    [SerializeField] public GameObject selectOverlay;

    [HideInInspector] public bool isSelected = false;

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
