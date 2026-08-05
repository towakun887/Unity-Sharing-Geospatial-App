using UnityEditor;
using UnityEditor.XR.Management;
using UnityEditor.XR.Management.Metadata;
using UnityEngine;
using UnityEngine.XR.Management;

[InitializeOnLoad]
public class EnableARCoreXR
{
    static EnableARCoreXR()
    {
        EditorApplication.delayCall += EnableARCore;
    }

    private static void EnableARCore()
    {
        // Get or Create XRGeneralSettings
        XRGeneralSettingsPerBuildTarget buildTargetSettings = null;
        EditorBuildSettings.TryGetConfigObject(XRGeneralSettings.k_SettingsKey, out buildTargetSettings);

        if (buildTargetSettings == null)
        {
            // If the XR Plug-in Management window was never opened, we can't easily create it via script without reflection
            // But we can try to load it
            var assetPath = "Assets/XR/XRGeneralSettings.asset"; // sometimes it is in Assets/XR
            // Newer Unity versions store it in ProjectSettings/XRGeneralSettings.asset
            Debug.LogWarning("Could not find XRGeneralSettings. Please open Edit > Project Settings > XR Plug-in Management to initialize.");
            return;
        }

        var settings = buildTargetSettings.SettingsForBuildTarget(BuildTargetGroup.Android);
        if (settings != null && settings.Manager != null)
        {
            var manager = settings.Manager;
            bool success = XRPackageMetadataStore.AssignLoader(manager, "Unity.XR.ARCore.ARCoreLoader", BuildTargetGroup.Android);
            if (success)
            {
                Debug.Log("Successfully enabled ARCore in XR Plug-in Management for Android!");
            }
        }
    }
}
