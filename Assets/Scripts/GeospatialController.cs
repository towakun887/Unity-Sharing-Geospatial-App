using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using Google.XR.ARCoreExtensions;

public class GeospatialController : MonoBehaviour
{
    public AREarthManager earthManager;

    public bool IsTracking => earthManager != null && earthManager.EarthTrackingState == TrackingState.Tracking;
    public GeospatialPose CurrentPose => earthManager != null ? earthManager.CameraGeospatialPose : default;

    private void Start()
    {
        // Ensure location services are started (usually handled by Unity if enabled in Player Settings)
        if (!Input.location.isEnabledByUser)
        {
            Debug.LogWarning("Location services not enabled by user.");
        }
        else
        {
            Input.location.Start();
        }
    }

    [System.Serializable]
    public class LocationData
    {
        public bool isTracking;
        public double latitude;
        public double longitude;
        public double altitude;
        public float eunRotationX;
        public float eunRotationY;
        public float eunRotationZ;
        public float eunRotationW;
        public double accuracy;
        public double altitudeAccuracy;
        public double headingAccuracy;
    }

    public string GetLocationJson()
    {
        LocationData data = new LocationData();

        if (earthManager == null)
        {
            data.isTracking = false;
            return JsonUtility.ToJson(data);
        }

        var trackingState = earthManager.EarthTrackingState;
        if (trackingState == TrackingState.Tracking)
        {
            var cameraGeospatialPose = earthManager.CameraGeospatialPose;
            data.isTracking = true;
            data.latitude = cameraGeospatialPose.Latitude;
            data.longitude = cameraGeospatialPose.Longitude;
            data.altitude = cameraGeospatialPose.Altitude;
            
            // Full 3D orientation (quaternion) for relative position calculation
            var eunRot = cameraGeospatialPose.EunRotation;
            data.eunRotationX = eunRot.x;
            data.eunRotationY = eunRot.y;
            data.eunRotationZ = eunRot.z;
            data.eunRotationW = eunRot.w;

            data.accuracy = cameraGeospatialPose.HorizontalAccuracy;
            data.altitudeAccuracy = cameraGeospatialPose.VerticalAccuracy;
            data.headingAccuracy = cameraGeospatialPose.OrientationYawAccuracy;
        }
        else
        {
            data.isTracking = false;
        }

        return JsonUtility.ToJson(data);
    }

    public string GetLocationStringUI()
    {
        if (earthManager == null)
        {
            return "• EarthManager: Missing";
        }

        var state = earthManager.EarthState;
        var trackingState = earthManager.EarthTrackingState;

        if (trackingState != TrackingState.Tracking)
        {
            return $"• State: {state}\n• Tracking: {trackingState}\n• Waiting for VPS/GPS...";
        }

        var pose = earthManager.CameraGeospatialPose;
        return $"• State: {state}\n" +
               $"• Tracking: {trackingState}\n" +
               $"• Lat: {pose.Latitude:F5}\n" +
               $"• Lon: {pose.Longitude:F5}\n" +
               $"• Alt: {pose.Altitude:F2}m\n" +
               $"• Rot(EUN): {pose.EunRotation.x:F2}, {pose.EunRotation.y:F2}, {pose.EunRotation.z:F2}, {pose.EunRotation.w:F2}\n" +
               $"• Acc (H/V/Y): {pose.HorizontalAccuracy:F2}m / {pose.VerticalAccuracy:F2}m / {pose.OrientationYawAccuracy:F2}°";
    }
}
