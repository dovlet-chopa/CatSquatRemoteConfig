#if UNITY_EDITOR
    using UnityEditor;
    using UnityEngine;

    public class GoogleSheetRemoteConfigInfo : EditorWindow
    {
        private static readonly Vector2 windowSize = new Vector2(500, 300);
        private const string ApiKeyPrefKey = "GoogleSheetRemoteConfig_ApiKey";
        private const string SheetIdPrefKey = "GoogleSheetRemoteConfig_SheetId";
        
        private string apiKey;
        private string sheetId;
        
        private void OnEnable()
        {
            // Load the saved values from EditorPrefs
            apiKey = EditorPrefs.GetString(ApiKeyPrefKey, string.Empty);
            sheetId = EditorPrefs.GetString(SheetIdPrefKey, string.Empty);
        }

        [MenuItem("Remote Config/Open Config Window")]
        public static void ShowWindow()
        {
            GoogleSheetRemoteConfigInfo window = GetWindow<GoogleSheetRemoteConfigInfo>("Info");
            window.minSize = windowSize; 
            window.maxSize = windowSize;
            // window.position = new Rect(100, 100, windowSize.x, windowSize.y); 
        }

        private void OnGUI()
        {
            GUILayout.Space(10);
            GUILayout.Label("GoogleSheet Remote Config", EditorStyles.boldLabel);
            GUILayout.Space(5);
            
            apiKey = EditorGUILayout.TextField("API Key", apiKey);
            GUILayout.Space(5);
            
            sheetId = EditorGUILayout.TextField("Sheet ID", sheetId);
            GUILayout.Space(15);
            
            GUILayout.BeginHorizontal();
            
            // Enable the button only when both fields are filled
            EditorGUI.BeginDisabledGroup(string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(sheetId) || FindObjectOfType<GoogleSheetRemoteConfig>());
        
                if (GUILayout.Button("Initialize"))
                {
                    InitializeGoogleSheetRemoteConfigObject();
                }

            EditorGUI.EndDisabledGroup();
            
            // Save the values when they are changed
            GoogleSheetRemoteConfig existConfig = FindObjectOfType<GoogleSheetRemoteConfig>();
            string apiKeyCom = null;
            string sheetIdCom = null;
            
            if (existConfig != null)
            {
                apiKeyCom = existConfig.GetComponent<GoogleSheetRemoteConfig>().GetApiKey();
                sheetIdCom = existConfig.GetComponent<GoogleSheetRemoteConfig>().GetSheetId();
            }
            
            // Enable the button only when values are changed
            EditorGUI.BeginDisabledGroup((apiKey == apiKeyCom && sheetId == sheetIdCom) || !existConfig);
            
                EditorPrefs.SetString(ApiKeyPrefKey, apiKey);
                EditorPrefs.SetString(SheetIdPrefKey, sheetId);
                
                if (GUILayout.Button("Update"))
                {
                    GoogleSheetRemoteConfig remoteConfig = FindObjectOfType<GoogleSheetRemoteConfig>();
                    remoteConfig.SetApiKey(apiKey);
                    remoteConfig.SetSheetId(sheetId);
                }

            EditorGUI.EndDisabledGroup();
            
            GUILayout.EndHorizontal();
        }
        
        private void InitializeGoogleSheetRemoteConfigObject()
        {
            // Check if an object with the component already exists
            GoogleSheetRemoteConfig existingRemoteConfig = FindObjectOfType<GoogleSheetRemoteConfig>();

            if (existingRemoteConfig == null)
            {
                // Create a new GameObject and add the GoogleSheetRemoteConfig component
                GameObject configObject = new GameObject("GoogleSheetRemoteConfig");
                GoogleSheetRemoteConfig remoteConfig = configObject.AddComponent<GoogleSheetRemoteConfig>();
                
                remoteConfig.SetApiKey(apiKey);
                remoteConfig.SetSheetId(sheetId);

                Debug.Log("GoogleSheetRemoteConfig initialized with the provided API Key and Sheet ID");
            }
            else
            {
                Debug.LogWarning("GoogleSheetRemoteConfig is already initialized in the project.");
            }
        }
    }
#endif

[CustomEditor(typeof(GoogleSheetRemoteConfig))]
public class GoogleSheetRemoteConfigEditor : Editor
{
    private Texture2D customIcon;

    private void OnEnable()
    {
        // Load your custom icon from the specified path
        customIcon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoogleSheetRemoteConfig/Icon/sheet.png");
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        base.OnInspectorGUI();

        // Set the custom icon for the GameObject with the GoogleSheetRemoteConfig component
        if (target != null)
        {
            EditorGUIUtility.SetIconForObject(target, customIcon);
        }
    }
}

[InitializeOnLoad]
public class GoogleSheetRemoteConfigIcon
{
    static GoogleSheetRemoteConfigIcon()
    {
        // Subscribe to the hierarchy window's GUI event
        EditorApplication.hierarchyWindowItemOnGUI += OnHierarchyGUI;
    }

    private static void OnHierarchyGUI(int instanceID, Rect selectionRect)
    {
        // Get the GameObject associated with the instanceID
        GameObject obj = EditorUtility.InstanceIDToObject(instanceID) as GameObject;

        // Check if the GameObject has the GoogleSheetRemoteConfig component
        if (obj != null && obj.GetComponent<GoogleSheetRemoteConfig>() != null)
        {
            // Specify the icon you want to display
            Texture2D icon = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/GoogleSheetRemoteConfig/Icon/sheet.png");

            // Define where to draw the icon in the hierarchy
            Rect iconRect = new Rect(selectionRect.x - 15, selectionRect.y, 16, 16);

            // Draw the icon
            GUI.Label(iconRect, new GUIContent(icon));
        }
    }
}
