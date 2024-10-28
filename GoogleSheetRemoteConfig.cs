using System;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;

public class GoogleSheetRemoteConfig : MonoBehaviour
{
    public static GoogleSheetRemoteConfig Instance { get; private set; }
    
    [HideInInspector] public string apiKey;
    [HideInInspector] public string sheetId;
    private string baseUrl = "https://sheets.googleapis.com/v4/spreadsheets/";

    public void SetApiKey(string apiKey) => this.apiKey = apiKey;
    public void SetSheetId(string sheetId) => this.sheetId = sheetId;
    public string GetApiKey() => apiKey;
    public string GetSheetId() => sheetId;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    private IEnumerator FetchData(string range, Action<string> OnSuccess)
    {
        // Construct the URL for fetching the data
        string url = $"{baseUrl}{sheetId}/values/{range}?key={apiKey}";

        UnityWebRequest request = UnityWebRequest.Get(url);
        yield return request.SendWebRequest();

        // Check if the request was successful
        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonData = request.downloadHandler.text;
            string sheetData = JObject.Parse(jsonData)["values"].ToString();
            
            Debug.Log($"Data from {range.Substring(0, range.IndexOf("!"))} Sheet: " + sheetData);
            OnSuccess?.Invoke(sheetData);
        }
        else
        {
            // Log full error details
            Debug.LogError("Failed to fetch data: " + request.error);
            Debug.LogError("Response: " + request.downloadHandler.text);
        }
    }

    public void LoadSheetData(string range, Action<List<List<object>>> OnSuccess)
    {
        StartCoroutine(FetchData(range, result =>
        {
            try
            {
                if (string.IsNullOrEmpty(result))
                {
                    throw new Exception("Data is null or empty");
                }

                // Parse the result into a List<List<object>>
                var sheetData = JsonConvert.DeserializeObject<List<List<object>>>(result);

                // Invoke the success callback with the parsed data
                OnSuccess?.Invoke(EqualizeData(sheetData));
            }
            catch (Exception ex)
            { 
                Debug.LogError(ex);
            }
        }));
    }
    
    List<List<object>> EqualizeData(List<List<object>> listOfLists)
    {
        // Find the maximum row count
        int maxCount = 0;
        foreach (var row in listOfLists)
        {
            if (row.Count > maxCount)
            {
                maxCount = row.Count;
            }
        }

        // Add empty values to each row until it reaches the max count
        foreach (var row in listOfLists)
        {
            int countDifference = maxCount - row.Count;
            for (int i = 0; i < countDifference; i++)
            {
                row.Add(null); // or use "" for an empty string if you prefer
            }
        }

        return listOfLists;
    }
}
