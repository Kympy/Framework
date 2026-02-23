using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections.Generic;

public class PrefabIconGenerator : EditorWindow
{
    private const string WORK_SCENE_PATH = "Assets/Scenes/IconCapture.unity";
    private const string REFERENCE_DUMMY_NAME = "_ReferenceDummy";

    // EditorPrefs keys
    private const string PREF_TARGET_FOLDER = "PrefabIconGenerator_TargetFolder";
    private const string PREF_PREFIX = "PrefabIconGenerator_Prefix";
    private const string PREF_SUFFIX = "PrefabIconGenerator_Suffix";
    private const string PREF_IMAGE_FORMAT = "PrefabIconGenerator_ImageFormat";
    private const string PREF_IMAGE_WIDTH = "PrefabIconGenerator_ImageWidth";
    private const string PREF_IMAGE_HEIGHT = "PrefabIconGenerator_ImageHeight";
    private const string PREF_AUTO_FACE_CAMERA = "PrefabIconGenerator_AutoFaceCamera";
    private const string PREF_AUTO_FRAME_OBJECT = "PrefabIconGenerator_AutoFrameObject";
    private const string PREF_FRAME_PADDING = "PrefabIconGenerator_FramePadding";
    private const string PREF_OUTPUT_PATH = "PrefabIconGenerator_OutputPath";
    private const string PREF_CAMERA_NAME = "PrefabIconGenerator_CameraName";
    private const string PREF_IMPORT_AS_SPRITE = "PrefabIconGenerator_ImportAsSprite";

    [MenuItem("Tools/Prefab Icon Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<PrefabIconGenerator>("Prefab Icon Generator");
        window.minSize = new Vector2(400, 600);

        // Load or create work scene
        LoadWorkScene();
    }

    private static void LoadWorkScene()
    {
        // Check if work scene exists
        if (File.Exists(WORK_SCENE_PATH))
        {
            EditorSceneManager.OpenScene(WORK_SCENE_PATH);
            UnityEngine.Debug.Log($"Loaded work scene: {WORK_SCENE_PATH}");
        }
        else
        {
            // Create new scene
            Scene newScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // Add a camera for capturing
            GameObject cameraObj = new GameObject("Capture Camera");
            Camera cam = cameraObj.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 1, -3);
            cam.transform.LookAt(Vector3.zero);

            // Add a light
            GameObject lightObj = new GameObject("Directional Light");
            Light light = lightObj.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50, -30, 0);

            // Add reference dummy cube for camera positioning
            GameObject dummyObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            dummyObj.name = REFERENCE_DUMMY_NAME;
            dummyObj.transform.position = Vector3.zero;
            dummyObj.transform.localScale = Vector3.one;

            // Add a material to make it more visible
            Material dummyMat = new Material(Shader.Find("Standard"));
            dummyMat.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
            dummyObj.GetComponent<Renderer>().material = dummyMat;

            // Ensure directory exists
            string sceneDir = Path.GetDirectoryName(WORK_SCENE_PATH);
            if (!Directory.Exists(sceneDir))
            {
                Directory.CreateDirectory(sceneDir);
            }

            // Save scene
            EditorSceneManager.SaveScene(newScene, WORK_SCENE_PATH);
            UnityEngine.Debug.Log($"Created new work scene: {WORK_SCENE_PATH}");
        }
    }

    // Folder Selection
    private DefaultAsset targetFolder;

    // Naming Convention
    private string prefix = "";
    private string suffix = "_Icon";

    // Image Settings
    private enum ImageFormat { PNG, JPG }
    private ImageFormat imageFormat = ImageFormat.PNG;
    private int imageWidth = 256;
    private int imageHeight = 256;

    // Camera Settings
    private Camera captureCamera;
    private bool autoFaceCamera = true;
    private bool autoFrameObject = true;
    private float framePadding = 1.3f;

    // Output Settings
    private string outputPath = "Assets/GeneratedIcons";
    private bool importAsSprite = true;

    // Preview
    private List<GameObject> foundPrefabs = new List<GameObject>();
    private Vector2 scrollPosition;
    private GameObject selectedPrefab;
    private GameObject previewInstance;

    private void OnEnable()
    {
        LoadSettings();
    }

    private void OnDisable()
    {
        SaveSettings();
    }

    private void LoadSettings()
    {
        // Load target folder
        string folderPath = EditorPrefs.GetString(PREF_TARGET_FOLDER, "");
        if (!string.IsNullOrEmpty(folderPath))
        {
            targetFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(folderPath);
            if (targetFolder != null)
            {
                ScanFolder();
            }
        }

        // Load naming convention
        prefix = EditorPrefs.GetString(PREF_PREFIX, "");
        suffix = EditorPrefs.GetString(PREF_SUFFIX, "_Icon");

        // Load image settings
        imageFormat = (ImageFormat)EditorPrefs.GetInt(PREF_IMAGE_FORMAT, (int)ImageFormat.PNG);
        imageWidth = EditorPrefs.GetInt(PREF_IMAGE_WIDTH, 256);
        imageHeight = EditorPrefs.GetInt(PREF_IMAGE_HEIGHT, 256);

        // Load camera settings
        autoFaceCamera = EditorPrefs.GetBool(PREF_AUTO_FACE_CAMERA, true);
        autoFrameObject = EditorPrefs.GetBool(PREF_AUTO_FRAME_OBJECT, true);
        framePadding = EditorPrefs.GetFloat(PREF_FRAME_PADDING, 1.3f);

        string cameraName = EditorPrefs.GetString(PREF_CAMERA_NAME, "");
        if (!string.IsNullOrEmpty(cameraName))
        {
            GameObject cameraObj = GameObject.Find(cameraName);
            if (cameraObj != null)
            {
                captureCamera = cameraObj.GetComponent<Camera>();
            }
        }

        // Auto-find camera if not assigned
        if (captureCamera == null)
        {
            captureCamera = FindCameraInScene();
        }

        // Load output path
        outputPath = EditorPrefs.GetString(PREF_OUTPUT_PATH, "Assets/GeneratedIcons");
        importAsSprite = EditorPrefs.GetBool(PREF_IMPORT_AS_SPRITE, true);
    }

    private Camera FindCameraInScene()
    {
        // Try to find "Capture Camera" first
        GameObject cameraObj = GameObject.Find("Capture Camera");
        if (cameraObj != null)
        {
            Camera cam = cameraObj.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        // Find any camera in the scene
        Camera[] cameras = GameObject.FindObjectsByType<Camera>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (cameras.Length > 0)
        {
            return cameras[0];
        }

        return null;
    }

    private void SaveSettings()
    {
        // Save target folder
        if (targetFolder != null)
        {
            EditorPrefs.SetString(PREF_TARGET_FOLDER, AssetDatabase.GetAssetPath(targetFolder));
        }

        // Save naming convention
        EditorPrefs.SetString(PREF_PREFIX, prefix);
        EditorPrefs.SetString(PREF_SUFFIX, suffix);

        // Save image settings
        EditorPrefs.SetInt(PREF_IMAGE_FORMAT, (int)imageFormat);
        EditorPrefs.SetInt(PREF_IMAGE_WIDTH, imageWidth);
        EditorPrefs.SetInt(PREF_IMAGE_HEIGHT, imageHeight);

        // Save camera settings
        EditorPrefs.SetBool(PREF_AUTO_FACE_CAMERA, autoFaceCamera);
        EditorPrefs.SetBool(PREF_AUTO_FRAME_OBJECT, autoFrameObject);
        EditorPrefs.SetFloat(PREF_FRAME_PADDING, framePadding);
        if (captureCamera != null)
        {
            EditorPrefs.SetString(PREF_CAMERA_NAME, captureCamera.gameObject.name);
        }

        // Save output path
        EditorPrefs.SetString(PREF_OUTPUT_PATH, outputPath);
        EditorPrefs.SetBool(PREF_IMPORT_AS_SPRITE, importAsSprite);
    }

    private void OnGUI()
    {
        GUILayout.Label("Prefab Icon Generator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // Folder Selection
        EditorGUILayout.LabelField("Target Folder", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        targetFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefab Folder", targetFolder, typeof(DefaultAsset), false);
        if (EditorGUI.EndChangeCheck())
        {
            ScanFolder();
            SaveSettings();
        }

        EditorGUILayout.Space();

        // Naming Convention
        EditorGUILayout.LabelField("Naming Convention", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        prefix = EditorGUILayout.TextField("Prefix", prefix);
        suffix = EditorGUILayout.TextField("Suffix", suffix);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        EditorGUILayout.HelpBox($"Example: {prefix}PrefabName{suffix}.{imageFormat.ToString().ToLower()}", MessageType.Info);
        EditorGUILayout.Space();

        // Image Settings
        EditorGUILayout.LabelField("Image Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        imageFormat = (ImageFormat)EditorGUILayout.EnumPopup("Format", imageFormat);
        imageWidth = EditorGUILayout.IntField("Width", imageWidth);
        imageHeight = EditorGUILayout.IntField("Height", imageHeight);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }
        EditorGUILayout.Space();

        // Camera Settings
        EditorGUILayout.LabelField("Camera Settings", EditorStyles.boldLabel);
        EditorGUI.BeginChangeCheck();
        captureCamera = (Camera)EditorGUILayout.ObjectField("Capture Camera", captureCamera, typeof(Camera), true);

        if (captureCamera == null)
        {
            EditorGUILayout.HelpBox("No camera assigned. Will auto-find camera in scene.", MessageType.Warning);
        }

        autoFaceCamera = EditorGUILayout.Toggle("Auto Face Camera", autoFaceCamera);
        autoFrameObject = EditorGUILayout.Toggle("Auto Frame Object", autoFrameObject);

        // Frame padding slider (only show if auto frame is enabled)
        if (autoFrameObject)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Frame Padding");
            framePadding = EditorGUILayout.Slider(framePadding, 1.0f, 2.0f);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.HelpBox($"Padding: {framePadding:F2}x (1.0 = tight fit, 2.0 = double space)", MessageType.None);
        }

        EditorGUILayout.HelpBox(
            "Auto Face Camera: Model rotates to face camera\n" +
            "Auto Frame Object: Model moves to fit in camera view",
            MessageType.Info);

        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }
        EditorGUILayout.Space();

        // Output Settings
        EditorGUILayout.LabelField("Output Settings", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        var newOutputPath = EditorGUILayout.TextField("Output Path", outputPath);
        if (newOutputPath != outputPath)
        {
            outputPath = newOutputPath;
            SaveSettings();
        }
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string path = EditorUtility.OpenFolderPanel("Select Output Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                {
                    outputPath = "Assets" + path.Substring(Application.dataPath.Length);
                    SaveSettings();
                    Repaint();
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid Path", "Please select a folder inside the Assets directory.", "OK");
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        // Quick access buttons
        EditorGUILayout.BeginHorizontal();
        GUI.enabled = AssetDatabase.IsValidFolder(outputPath);
        if (GUILayout.Button("Select in Project"))
        {
            UnityEngine.Object folderAsset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(outputPath);
            if (folderAsset != null)
            {
                Selection.activeObject = folderAsset;
                EditorGUIUtility.PingObject(folderAsset);
            }
        }
        if (GUILayout.Button("Show in Explorer"))
        {
            EditorUtility.RevealInFinder(outputPath);
        }
        GUI.enabled = true;
        EditorGUILayout.EndHorizontal();

        // Import as Sprite option
        EditorGUI.BeginChangeCheck();
        importAsSprite = EditorGUILayout.Toggle("Import as Sprite (2D and UI)", importAsSprite);
        if (EditorGUI.EndChangeCheck())
        {
            SaveSettings();
        }

        EditorGUILayout.Space();

        // Prefab List
        EditorGUILayout.LabelField($"Found Prefabs ({foundPrefabs.Count})", EditorStyles.boldLabel);
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(150));
        foreach (var prefab in foundPrefabs)
        {
            EditorGUILayout.BeginHorizontal();
            bool isSelected = selectedPrefab == prefab;
            if (GUILayout.Toggle(isSelected, "", GUILayout.Width(20)))
            {
                selectedPrefab = prefab;
            }
            else if (isSelected)
            {
                selectedPrefab = null;
            }
            EditorGUILayout.ObjectField(prefab, typeof(GameObject), false);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.Space();

        // Preview Button
        EditorGUILayout.LabelField("Preview", EditorStyles.boldLabel);
        GUI.enabled = selectedPrefab != null;
        string previewButtonText = previewInstance != null ? "Clear Preview" : "Preview Selected";
        if (GUILayout.Button(previewButtonText))
        {
            TogglePreview();
        }
        GUI.enabled = true;

        if (previewInstance != null)
        {
            EditorGUILayout.HelpBox("Preview is active in scene. Adjust camera and lights as desired.", MessageType.Info);
        }

        EditorGUILayout.Space();

        // Capture Buttons
        EditorGUILayout.LabelField("Capture", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        GUI.enabled = selectedPrefab != null;
        if (GUILayout.Button("Capture Selected"))
        {
            CaptureSingleIcon(selectedPrefab);
        }
        GUI.enabled = true;

        GUI.enabled = foundPrefabs.Count > 0;
        if (GUILayout.Button("Capture All"))
        {
            CaptureAllIcons();
        }
        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void ScanFolder()
    {
        foundPrefabs.Clear();

        if (targetFolder == null)
        {
            EditorUtility.DisplayDialog("Error", "Please select a target folder.", "OK");
            return;
        }

        string folderPath = AssetDatabase.GetAssetPath(targetFolder);
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            EditorUtility.DisplayDialog("Error", "Please select a valid folder.", "OK");
            return;
        }

        // Find all prefabs in the folder
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null && Is3DPrefab(prefab))
            {
                foundPrefabs.Add(prefab);
            }
        }

        UnityEngine.Debug.Log($"Found {foundPrefabs.Count} 3D prefabs in {folderPath}");
    }

    private bool Is3DPrefab(GameObject prefab)
    {
        // Check if it's not a UI prefab (doesn't have RectTransform)
        if (prefab.GetComponent<RectTransform>() != null)
        {
            return false;
        }

        // Check if it has a MeshFilter, MeshRenderer, or SkinnedMeshRenderer
        return prefab.GetComponentInChildren<MeshFilter>() != null ||
               prefab.GetComponentInChildren<MeshRenderer>() != null ||
               prefab.GetComponentInChildren<SkinnedMeshRenderer>() != null;
    }

    private void CaptureSingleIcon(GameObject prefab)
    {
        if (prefab == null)
            return;

        // Clear preview before capture
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        // Hide reference dummy
        HideReferenceDummy(true);

        try
        {
            if (CaptureIcon(prefab))
            {
                EditorUtility.DisplayDialog("Success", $"Icon captured successfully!\nSaved to: {outputPath}", "OK");
                AssetDatabase.Refresh();
            }
        }
        finally
        {
            // Restore reference dummy
            HideReferenceDummy(false);
        }
    }

    private void CaptureAllIcons()
    {
        if (!ValidateSettings())
            return;

        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }

        // Clear preview before batch capture
        if (previewInstance != null)
        {
            DestroyImmediate(previewInstance);
            previewInstance = null;
        }

        // Hide reference dummy for all captures
        HideReferenceDummy(true);

        int successCount = 0;
        int totalCount = foundPrefabs.Count;

        try
        {
            for (int i = 0; i < totalCount; i++)
            {
                EditorUtility.DisplayProgressBar("Capturing Icons", $"Processing {foundPrefabs[i].name} ({i + 1}/{totalCount})", (float)i / totalCount);

                if (CaptureIcon(foundPrefabs[i]))
                {
                    successCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
            AssetDatabase.Refresh();

            // Restore reference dummy
            HideReferenceDummy(false);
        }

        EditorUtility.DisplayDialog("Complete", $"Successfully captured {successCount}/{totalCount} icons.\nSaved to: {outputPath}", "OK");
    }

    private bool CaptureIcon(GameObject prefab)
    {
        if (!ValidateSettings())
            return false;

        // Ensure output directory exists
        if (!AssetDatabase.IsValidFolder(outputPath))
        {
            Directory.CreateDirectory(outputPath);
            AssetDatabase.Refresh();
        }

        // Instantiate the prefab
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
        if (instance == null)
        {
            UnityEngine.Debug.LogError($"Failed to instantiate prefab: {prefab.name}");
            return false;
        }

        try
        {
            // Auto-find camera if not assigned
            if (captureCamera == null)
            {
                captureCamera = FindCameraInScene();
                if (captureCamera == null)
                {
                    UnityEngine.Debug.LogError("No camera found in scene!");
                    return false;
                }
            }

            // Store original camera settings
            float originalOrthographicSize = captureCamera.orthographicSize;

            // Position and orient the model
            PositionModelForCapture(instance, captureCamera);

            // Setup render texture

            // Setup render texture
            RenderTexture renderTexture = new RenderTexture(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 4;

            // Store original camera settings
            RenderTexture originalTarget = captureCamera.targetTexture;
            CameraClearFlags originalClearFlags = captureCamera.clearFlags;
            Color originalBackgroundColor = captureCamera.backgroundColor;

            // Setup camera for transparent background
            captureCamera.targetTexture = renderTexture;
            captureCamera.clearFlags = CameraClearFlags.SolidColor;
            captureCamera.backgroundColor = new Color(0, 0, 0, 0);

            // Render
            captureCamera.Render();

            // Read pixels from render texture
            RenderTexture.active = renderTexture;
            Texture2D screenshot = new Texture2D(imageWidth, imageHeight, TextureFormat.ARGB32, false);
            screenshot.ReadPixels(new Rect(0, 0, imageWidth, imageHeight), 0, 0);
            screenshot.Apply();
            RenderTexture.active = null;

            // Restore camera settings
            captureCamera.targetTexture = originalTarget;
            captureCamera.clearFlags = originalClearFlags;
            captureCamera.backgroundColor = originalBackgroundColor;
            captureCamera.orthographicSize = originalOrthographicSize;

            // Save to file
            byte[] bytes;
            string extension;

            if (imageFormat == ImageFormat.PNG)
            {
                bytes = screenshot.EncodeToPNG();
                extension = ".png";
            }
            else
            {
                bytes = screenshot.EncodeToJPG(90);
                extension = ".jpg";
            }

            string fileName = prefix + prefab.name + suffix + extension;
            string filePath = Path.Combine(outputPath, fileName);
            File.WriteAllBytes(filePath, bytes);

            // Import and configure texture settings
            AssetDatabase.Refresh();
            if (importAsSprite)
            {
                ConfigureTextureAsSprite(filePath);
            }

            // Cleanup
            DestroyImmediate(screenshot);
            renderTexture.Release();
            DestroyImmediate(renderTexture);

            UnityEngine.Debug.Log($"Captured icon: {filePath}");
            return true;
        }
        finally
        {
            // Always cleanup the instance
            DestroyImmediate(instance);
        }
    }

    private bool ValidateSettings()
    {
        // Camera will be auto-found in CaptureIcon if not assigned

        if (imageWidth <= 0 || imageHeight <= 0)
        {
            EditorUtility.DisplayDialog("Error", "Image dimensions must be greater than 0.", "OK");
            return false;
        }

        if (string.IsNullOrEmpty(outputPath))
        {
            EditorUtility.DisplayDialog("Error", "Please specify an output path.", "OK");
            return false;
        }

        return true;
    }

    private void TogglePreview()
    {
        if (previewInstance != null)
        {
            // Clear preview
            DestroyImmediate(previewInstance);
            previewInstance = null;
            UnityEngine.Debug.Log("Preview cleared");
        }
        else if (selectedPrefab != null)
        {
            // Create preview
            previewInstance = PrefabUtility.InstantiatePrefab(selectedPrefab) as GameObject;
            if (previewInstance != null)
            {
                previewInstance.name = "[PREVIEW] " + selectedPrefab.name;

                // Find camera if not assigned
                if (captureCamera == null)
                {
                    captureCamera = FindCameraInScene();
                }

                // Use the same positioning logic as capture
                if (captureCamera != null)
                {
                    PositionModelForCapture(previewInstance, captureCamera);
                }
                else
                {
                    // Fallback: just center at origin
                    previewInstance.transform.position = Vector3.zero;
                    Bounds bounds = CalculateBounds(previewInstance);
                    previewInstance.transform.position = -bounds.center;
                }

                // Focus scene view on preview
                Selection.activeGameObject = previewInstance;
                SceneView.FrameLastActiveSceneView();

                UnityEngine.Debug.Log($"Preview created: {selectedPrefab.name}");
            }
        }
    }

    private void HideReferenceDummy(bool hide)
    {
        GameObject dummy = GameObject.Find(REFERENCE_DUMMY_NAME);
        if (dummy != null)
        {
            dummy.SetActive(!hide);
        }
    }

    private void ConfigureTextureAsSprite(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }
    }

    private Bounds CalculateBounds(GameObject obj)
    {
        Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(obj.transform.position, Vector3.one);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    private void PositionModelForCapture(GameObject instance, Camera camera)
    {
        // Position at origin first
        instance.transform.position = Vector3.zero;

        // Calculate initial bounds
        Bounds bounds = CalculateBounds(instance);

        // Move model so its mesh center (not pivot) is at origin
        Vector3 meshCenterOffset = bounds.center;
        instance.transform.position = -meshCenterOffset;

        // Recalculate bounds after centering
        bounds = CalculateBounds(instance);

        // Auto face camera if enabled (rotate model to face camera)
        if (autoFaceCamera)
        {
            // Direction from model to camera (on horizontal plane)
            Vector3 directionToCamera = camera.transform.position - bounds.center;
            directionToCamera.y = 0; // Remove Y component to keep model upright

            if (directionToCamera.sqrMagnitude > 0.001f) // Check if not zero
            {
                // Make model's forward face the camera
                instance.transform.rotation = Quaternion.LookRotation(directionToCamera);
                // Recalculate bounds after rotation
                bounds = CalculateBounds(instance);
            }
        }

        // Auto frame object: move model to appropriate distance from camera
        if (autoFrameObject)
        {
            // Camera stays in place, we move the model
            Vector3 cameraForward = camera.transform.forward;
            float aspectRatio = (float)imageWidth / imageHeight;

            float distance;

            if (camera.orthographic)
            {
                // For orthographic camera, adjust orthographic size
                float verticalSize = bounds.size.y / 2f * framePadding;
                float horizontalSize = bounds.size.x / 2f * framePadding / aspectRatio;
                camera.orthographicSize = Mathf.Max(verticalSize, horizontalSize);

                // Position model in front of camera
                distance = Mathf.Max(bounds.size.z * 2f, 10f);
            }
            else
            {
                // For perspective camera, calculate distance based on FOV
                float fov = camera.fieldOfView * Mathf.Deg2Rad;

                // Calculate distance for vertical field of view
                float verticalDistance = (bounds.size.y / 2f * framePadding) / Mathf.Tan(fov / 2f);

                // Calculate horizontal FOV and distance
                float horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(fov / 2f) * aspectRatio);
                float horizontalDistance = (bounds.size.x / 2f * framePadding) / Mathf.Tan(horizontalFOV / 2f);

                // Use the larger distance to ensure object fits
                distance = Mathf.Max(verticalDistance, horizontalDistance);

                // Add depth safety margin
                distance += bounds.size.z / 2f;
            }

            // Position model at calculated distance in front of camera
            // Model's bounds.center should align with camera's forward direction
            Vector3 targetPosition = camera.transform.position + cameraForward * distance;

            // Adjust for current bounds center
            Vector3 currentBoundsCenter = bounds.center;
            instance.transform.position += (targetPosition - currentBoundsCenter);
        }
    }
}