using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AppManager : MonoBehaviour
{
    public GameObject startScreenCanvas;
    public GameObject calibrationScreenCanvas;
    public Button startButton;
    public TextMeshProUGUI statusText;
    
    public TcpServerController tcpServerController;
    public GeospatialController geospatialController;
    public QRCodeGenerator qrCodeGenerator;

    [Header("Debug UI References")]
    public TextMeshProUGUI serverStatusText;
    public TextMeshProUGUI locationCoordinatesText;
    public TextMeshProUGUI headingText;
    public TextMeshProUGUI accuracyLocationLevelText;
    public TextMeshProUGUI accuracyLocationValueText;
    public TextMeshProUGUI accuracyHeadingLevelText;
    public TextMeshProUGUI accuracyHeadingValueText;

    private void Start()
    {
        ShowStartScreen();
        startButton.onClick.AddListener(OnStartCalibration);
    }

    private void ShowStartScreen()
    {
        startScreenCanvas.SetActive(true);
        calibrationScreenCanvas.SetActive(false);
        
        string ip = TcpServerController.GetLocalIPAddress();
        if (string.IsNullOrEmpty(ip))
        {
            statusText.text = "Error: Not connected to Wi-Fi.";
            startButton.interactable = false;
        }
        else
        {
            statusText.text = $"Ready. Local IP: {ip}\nWaiting to start calibration...";
            startButton.interactable = true;
        }
    }

    private void OnStartCalibration()
    {
        // TODO: Ensure camera and location permissions are granted before proceeding
        startScreenCanvas.SetActive(false);
        calibrationScreenCanvas.SetActive(true);
        
        // Pass Expected Device ID to validate incoming requests
        tcpServerController.ExpectedDeviceId = qrCodeGenerator.DeviceId;
        
        // Start TCP Server
        tcpServerController.StartServer();
        
        // Setup QR Code
        qrCodeGenerator.GenerateAndShowQRCode(tcpServerController.ServerPort);
    }

    private void Update()
    {
        // Continuously update the debug text with connection and live location data
        if (calibrationScreenCanvas != null && calibrationScreenCanvas.activeSelf && qrCodeGenerator != null && qrCodeGenerator.debugText != null)
        {
            string locUI = geospatialController.GetLocationStringUI();
            qrCodeGenerator.debugText.text = $"[ Connection Info ]\n{qrCodeGenerator.ConnectionString}\n{tcpServerController.LastConnectionInfo}\n\n[ Live Data ]\n{locUI}";
        }

        UpdateDebugUI();
    }

    private void UpdateDebugUI()
    {
        if (calibrationScreenCanvas == null || !calibrationScreenCanvas.activeSelf) return;

        if (serverStatusText != null && tcpServerController != null)
        {
            if (!tcpServerController.IsServerRunning)
            {
                serverStatusText.text = "停止中";
            }
            else if (tcpServerController.RequestCount == 0)
            {
                serverStatusText.text = "通信待機中...";
            }
            else
            {
                serverStatusText.text = $"待機中: リクエスト{tcpServerController.RequestCount}件";
            }
        }

        if (geospatialController != null && geospatialController.IsTracking)
        {
            var pose = geospatialController.CurrentPose;

            if (locationCoordinatesText != null)
            {
                locationCoordinatesText.text = $"緯度: {pose.Latitude:F6}  経度: {pose.Longitude:F6}";
            }

            if (headingText != null)
            {
                string compassDir = GetCompassDirection(pose.Heading);
                headingText.text = $"方位: {pose.Heading:F2}° ({compassDir})  高度: {pose.Altitude:F2}m";
            }

            if (accuracyLocationLevelText != null || accuracyLocationValueText != null)
            {
                string horizLevel = GetAccuracyLevel(pose.HorizontalAccuracy, 1.0, 3.0);
                string vertLevel = GetAccuracyLevel(pose.VerticalAccuracy, 1.0, 3.0);
                string overallLevel = GetLowerAccuracyLevel(horizLevel, vertLevel);
                
                if (accuracyLocationLevelText != null) accuracyLocationLevelText.text = overallLevel;
                if (accuracyLocationValueText != null) accuracyLocationValueText.text = $"水平誤差: {pose.HorizontalAccuracy:F2}m / 高度誤差: {pose.VerticalAccuracy:F2}m";
            }

            if (accuracyHeadingLevelText != null || accuracyHeadingValueText != null)
            {
                string headingLevel = GetAccuracyLevel(pose.OrientationYawAccuracy, 5.0, 15.0);
                if (accuracyHeadingLevelText != null) accuracyHeadingLevelText.text = headingLevel;
                if (accuracyHeadingValueText != null) accuracyHeadingValueText.text = $"方位誤差: {pose.OrientationYawAccuracy:F2}°";
            }
        }
        else
        {
            if (locationCoordinatesText != null) locationCoordinatesText.text = "位置情報: 取得中...";
            if (headingText != null) headingText.text = "方位: ---  高度: ---";
            if (accuracyLocationLevelText != null) accuracyLocationLevelText.text = "---";
            if (accuracyLocationValueText != null) accuracyLocationValueText.text = "水平誤差: --- / 高度誤差: ---";
            if (accuracyHeadingLevelText != null) accuracyHeadingLevelText.text = "---";
            if (accuracyHeadingValueText != null) accuracyHeadingValueText.text = "方位誤差: ---";
        }
    }

    private string GetAccuracyLevel(double value, double highThreshold, double midThreshold)
    {
        if (value <= highThreshold) return "HIGH";
        if (value <= midThreshold) return "MID";
        return "LOW";
    }

    private static readonly string[] CompassDirections = {
        "N", "NNE", "NE", "ENE", "E", "ESE", "SE", "SSE",
        "S", "SSW", "SW", "WSW", "W", "WNW", "NW", "NNW"
    };

    private string GetCompassDirection(double heading)
    {
        // 0-360度に正規化
        heading = (heading % 360.0 + 360.0) % 360.0;
        // 1方位は22.5度。北を中心とするため11.25度ずらす
        int index = (int)Math.Floor((heading + 11.25) / 22.5) % 16;
        return CompassDirections[index];
    }

    private string GetLowerAccuracyLevel(string level1, string level2)
    {
        if (level1 == "LOW" || level2 == "LOW") return "LOW";
        if (level1 == "MID" || level2 == "MID") return "MID";
        return "HIGH";
    }
}
