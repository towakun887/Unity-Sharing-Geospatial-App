using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.ARFoundation;
using Google.XR.ARCoreExtensions;
using System.IO;

public class SetupGeospatialApp : EditorWindow
{
    [MenuItem("Tools/Setup Geospatial Calibration App")]
    public static void SetupScene()
    {
        // 1. Setup AR Session
        var arSessionObj = new GameObject("AR Session");
        var arSession = arSessionObj.AddComponent<ARSession>();
        var arInputManager = arSessionObj.AddComponent<ARInputManager>();

        // 2. Setup XR Origin
        var xrOriginObj = new GameObject("XR Origin (AR Rig)");
        var xrOrigin = xrOriginObj.AddComponent<ARSessionOrigin>(); // Or XROrigin in newer versions
        
        var cameraOffset = new GameObject("Camera Offset");
        cameraOffset.transform.SetParent(xrOriginObj.transform);
        
        var mainCameraObj = new GameObject("Main Camera");
        mainCameraObj.tag = "MainCamera";
        mainCameraObj.transform.SetParent(cameraOffset.transform);
        var camera = mainCameraObj.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.Color;
        camera.backgroundColor = Color.black;
        var cameraManager = mainCameraObj.AddComponent<ARCameraManager>();
        mainCameraObj.AddComponent<ARCameraBackground>();
        
        // Add TrackedPoseDriver (using legacy or input system if available)
        mainCameraObj.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>();
        
        xrOrigin.camera = camera;

        // 3. Setup ARCore Extensions & Earth Manager
        var arcoreExtensions = xrOriginObj.AddComponent<ARCoreExtensions>();
        var earthManager = xrOriginObj.AddComponent<AREarthManager>();

        // Create and Configure ARCoreExtensionsConfig
        string settingsDir = "Assets/Settings";
        if (!Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }
        string configPath = "Assets/Settings/ARCoreExtensionsConfig.asset";
        ARCoreExtensionsConfig config = AssetDatabase.LoadAssetAtPath<ARCoreExtensionsConfig>(configPath);
        if (config == null)
        {
            config = ScriptableObject.CreateInstance<ARCoreExtensionsConfig>();
            config.GeospatialMode = GeospatialMode.Enabled;
            AssetDatabase.CreateAsset(config, configPath);
        }
        else
        {
            config.GeospatialMode = GeospatialMode.Enabled;
            EditorUtility.SetDirty(config);
        }
        AssetDatabase.SaveAssets();

        // Assign ARCoreExtensions fields
        arcoreExtensions.ARCoreExtensionsConfig = config;
        arcoreExtensions.Session = arSession;
        arcoreExtensions.SessionOrigin = xrOrigin;
        arcoreExtensions.CameraManager = cameraManager;

        // 4. Setup UI Manager
        var appManagerObj = new GameObject("AppManager");
        var appManager = appManagerObj.AddComponent<AppManager>();
        var tcpServer = appManagerObj.AddComponent<TcpServerController>();
        var geoController = appManagerObj.AddComponent<GeospatialController>();
        var qrGenerator = appManagerObj.AddComponent<QRCodeGenerator>();

        appManager.tcpServerController = tcpServer;
        appManager.geospatialController = geoController;
        appManager.qrCodeGenerator = qrGenerator;
        
        tcpServer.geospatialController = geoController;
        geoController.earthManager = earthManager;

        // 5. Setup Canvases
        var canvasObj = new GameObject("Canvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObj.AddComponent<GraphicRaycaster>();

        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
        eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Start Screen
        var startScreen = new GameObject("StartScreen");
        startScreen.transform.SetParent(canvasObj.transform, false);
        var startScreenRect = startScreen.AddComponent<RectTransform>();
        startScreenRect.anchorMin = Vector2.zero;
        startScreenRect.anchorMax = Vector2.one;
        startScreenRect.sizeDelta = Vector2.zero;

        var startButtonObj = new GameObject("StartButton");
        startButtonObj.transform.SetParent(startScreen.transform, false);
        var startButtonImg = startButtonObj.AddComponent<Image>();
        var startButton = startButtonObj.AddComponent<Button>();
        var btnRect = startButtonObj.GetComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(300, 80);

        var btnTextObj = new GameObject("Text");
        btnTextObj.transform.SetParent(startButtonObj.transform, false);
        var btnText = btnTextObj.AddComponent<TextMeshProUGUI>();
        btnText.text = "キャリブレーション開始";
        btnText.color = Color.black;
        btnText.alignment = TextAlignmentOptions.Center;

        var statusTextObj = new GameObject("StatusText");
        statusTextObj.transform.SetParent(startScreen.transform, false);
        var statusText = statusTextObj.AddComponent<TextMeshProUGUI>();
        statusText.text = "Initializing...";
        statusText.alignment = TextAlignmentOptions.Center;
        var statusRect = statusTextObj.GetComponent<RectTransform>();
        statusRect.anchoredPosition = new Vector2(0, 100);
        statusRect.sizeDelta = new Vector2(600, 100);

        // Calibration Screen
        var calibScreen = new GameObject("CalibrationScreen");
        calibScreen.transform.SetParent(canvasObj.transform, false);
        var calibScreenRect = calibScreen.AddComponent<RectTransform>();
        calibScreenRect.anchorMin = Vector2.zero;
        calibScreenRect.anchorMax = Vector2.one;
        calibScreenRect.sizeDelta = Vector2.zero;
        calibScreen.SetActive(false);

        var qrCodeObj = new GameObject("QRCodeImage");
        qrCodeObj.transform.SetParent(calibScreen.transform, false);
        var qrCodeImg = qrCodeObj.AddComponent<RawImage>();
        var qrRect = qrCodeObj.GetComponent<RectTransform>();
        qrRect.sizeDelta = new Vector2(800, 800); // Screen size QR

        var debugTextObj = new GameObject("DebugText");
        debugTextObj.transform.SetParent(calibScreen.transform, false);
        var debugText = debugTextObj.AddComponent<TextMeshProUGUI>();
        var debugRect = debugTextObj.GetComponent<RectTransform>();
        debugRect.anchorMin = new Vector2(0, 0); // Bottom Left
        debugRect.anchorMax = new Vector2(1, 0); // Bottom Right
        debugRect.pivot = new Vector2(0.5f, 0f); // Pivot at bottom
        debugRect.anchoredPosition = new Vector2(0, 50); // 50px from bottom
        debugRect.sizeDelta = new Vector2(-100, 0); // Left/Right 50px margins, Height 0 (auto-fit)
        debugText.color = Color.yellow; // Make it more visible against camera background
        debugText.alignment = TextAlignmentOptions.Bottom;

        var fitter = debugTextObj.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        // Add semi-transparent background Image child
        var bgObj = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        bgObj.transform.SetParent(debugTextObj.transform, false);
        bgObj.transform.SetAsFirstSibling();
        var bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = new Vector2(-15, -10);
        bgRect.offsetMax = new Vector2(15, 10);
        var bgImg = bgObj.GetComponent<Image>();
        bgImg.color = new Color(0f, 0f, 0f, 0.65f);

        // Assign to AppManager
        appManager.startScreenCanvas = startScreen;
        appManager.calibrationScreenCanvas = calibScreen;
        appManager.startButton = startButton;
        appManager.statusText = statusText;
        
        qrGenerator.qrCodeImage = qrCodeImg;
        qrGenerator.debugText = debugText;

        Debug.Log("Scene Setup Complete! Please save the scene.");
    }
}
