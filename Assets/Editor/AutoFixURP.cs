using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR.ARFoundation;

[InitializeOnLoad]
public class AutoFixURP
{
    static AutoFixURP()
    {
        EditorApplication.delayCall += FixURP;
    }

    [MenuItem("Tools/Fix AR Camera Black Screen")]
    public static void FixURP()
    {
        string[] guids = AssetDatabase.FindAssets("t:ScriptableRendererData");
        bool added = false;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableRendererData>(path);
            if (rendererData != null)
            {
                bool hasARBackground = false;
                foreach (var feature in rendererData.rendererFeatures)
                {
                    if (feature != null && feature.GetType().Name == "ARBackgroundRendererFeature")
                    {
                        hasARBackground = true;
                        break;
                    }
                }

                if (!hasARBackground)
                {
                    var arFeature = ScriptableObject.CreateInstance<ARBackgroundRendererFeature>();
                    if (arFeature != null)
                    {
                        arFeature.name = "AR Background Renderer Feature";
                        AssetDatabase.AddObjectToAsset(arFeature, rendererData);
                        rendererData.rendererFeatures.Add(arFeature);
                        
                        EditorUtility.SetDirty(rendererData);
                        EditorUtility.SetDirty(arFeature);
                        added = true;
                        Debug.Log($"Added ARBackgroundRendererFeature to {path}");
                    }
                }
            }
        }
        
        if (added)
        {
            AssetDatabase.SaveAssets();
            Debug.Log("Successfully fixed AR Camera black screen issue by adding AR Background Renderer Feature.");
        }
    }
}
