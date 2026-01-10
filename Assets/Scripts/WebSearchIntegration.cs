using Arikan;
using Arikan.Models;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class WebSearchIntegration : MonoBehaviour
{
    [Header("UI / Prefabs")]
    [SerializeField] private GameObject baseImage;                    // existing UI GameObject (e.g. a RawImage inside a GameObject)
    [SerializeField] private Transform webSearchesContainer;          // parent for instantiated results
    [SerializeField] private RawImage resultImagePrefab;              // prefab used for subsequent results (must contain RawImage)

    [Header("Search")]
    public string searchQuery = "";
    [SerializeField] private string location = "us-en";
    [SerializeField] private SafeSearch safeSearch = SafeSearch.Off;
    [SerializeField] private int resultsPerPage = 15;                 // how many results the DuckDuckGo wrapper returns per page (keeps parity with ApiExample)

    private int index = 0;                    // 0 -> baseImage, 1 -> first search result, etc.
    private int currentPage = 1;              // page number to request next from DuckDuckGo
    private bool isSearching = false;         // prevent duplicate simultaneous page requests

    // thumbnails collected from DuckDuckGo pages (flattened)
    private List<string> thumbnailUrls = new List<string>();

    // instantiated UI items (index 0 = baseImage gameobject)
    private List<GameObject> instances = new List<GameObject>();

    private void Start()
    {
        // ensure base image is the first instance
        instances.Clear();
        if (baseImage == null)
        {
            Debug.LogError("baseImage is null. Assign a GameObject with a RawImage component.");
            return;
        }

        // make sure baseImage is child of the container so ShowIndex works consistently.
        if (baseImage.transform.parent != webSearchesContainer)
            baseImage.transform.SetParent(webSearchesContainer, false);

        instances.Add(baseImage);
        ShowIndex(0);
    }

    // Called by UI Next button
    public void Next()
    {
        index++;

        // if instance already created for this index, just show it
        if (index < instances.Count)
        {
            ShowIndex(index);
            return;
        }

        // index > 0 corresponds to thumbnail index (index-1)
        int neededThumbIndex = index - 1;

        // if thumbnail already downloaded/known, instantiate it now
        if (neededThumbIndex < thumbnailUrls.Count)
        {
            InstantiateResultAt(neededThumbIndex);
            ShowIndex(index);
            return;
        }

        // need more results from the API
        if (!isSearching)
        {
            isSearching = true;
            DuckDuckGo.Search(searchQuery, safeSearch, currentPage, location, OnSearchCallback);
        }

        // nothing to show yet — will show when callback completes.
    }

    // Called by UI Previous button
    public void Previous()
    {
        if (index <= 0) return;
        index--;
        ShowIndex(index);
    }

    // DuckDuckGo callback (mirrors ApiExample pattern)
    void OnSearchCallback(ImageSearchResult result)
    {
        isSearching = false;

        if (result == null || result.results == null || result.results.Count == 0)
        {
            Debug.LogWarning("No more results from DuckDuckGo or result was null.");
            return;
        }

        // append thumbnails from this page to flattened list
        foreach (var item in result.results)
        {
            // item.thumbnail (string) exists in the API used in your reference script
            thumbnailUrls.Add(item.thumbnail);
        }

        // prepare for next page fetch if needed later
        currentPage++;

        // If current index needs a thumbnail that is now available, instantiate it
        int neededThumbIndex = index - 1;
        if (neededThumbIndex >= 0 && neededThumbIndex < thumbnailUrls.Count && index >= instances.Count)
        {
            InstantiateResultAt(neededThumbIndex);
            ShowIndex(index);
        }
    }

    // instantiate a result GameObject for a given thumbnail index (only once)
    private void InstantiateResultAt(int thumbIndex)
    {
        // create a new UI element from the prefab (keeps layout consistent)
        var inst = Instantiate(resultImagePrefab, webSearchesContainer);
        inst.gameObject.name = $"WebResult_{thumbIndex + 1}";
        instances.Add(inst.gameObject);

        // set a placeholder (optional) and begin async download
        string thumbUrl = thumbnailUrls[thumbIndex];
        var uwrOp = UnityWebRequestTexture.GetTexture(thumbUrl).SendWebRequest();
        uwrOp.completed += (asyncOp) =>
        {
            // check success (UnityWebRequest.Result for modern Unity)
#if UNITY_2020_1_OR_NEWER
            if (uwrOp.webRequest.result == UnityWebRequest.Result.Success)
#else
            if (!uwrOp.webRequest.isNetworkError && !uwrOp.webRequest.isHttpError)
#endif
            {
                var tex = DownloadHandlerTexture.GetContent(uwrOp.webRequest);
                if (inst != null)
                {
                    inst.texture = tex;
                    inst.SetNativeSize();
                }
            }
            else
            {
                Debug.LogWarning($"Failed to download thumbnail: {uwrOp.webRequest.error}");
            }
        };
    }

    // make only the requested index active
    private void ShowIndex(int showIdx)
    {
        for (int i = 0; i < instances.Count; i++)
            instances[i].SetActive(i == showIdx);
    }
    // --- RESET FUNCTION ---
    public void ResetSearch()
    {
        // destroy all instances except baseImage
        for (int i = instances.Count - 1; i > 0; i--)
        {
            if (instances[i] != null)
                Destroy(instances[i]);
        }

        // keep only baseImage in the list
        instances.Clear();
        instances.Add(baseImage);

        // reset counters
        index = 0;
        currentPage = 1;
        thumbnailUrls.Clear();
        isSearching = false;

        ShowIndex(0);
    }
}
