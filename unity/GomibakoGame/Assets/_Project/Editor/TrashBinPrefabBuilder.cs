using System.IO;
using UnityEditor;
using UnityEngine;

public static class TrashBinPrefabBuilder
{
    public const string SourcePrefabPath = "Assets/AK Studio Art/Waste Can/Prefabs/Waste Can (5).prefab";
    public const string OutputPrefabPath = "Assets/_Project/Prefabs/TrashBinPrefab.prefab";
    private const string MaterialPath = "Assets/_Project/Materials/TrashBin_Outdoor_URP.mat";
    private const float BinVisualScale = 2f;

    [MenuItem("Gomibako/Rebuild Trash Bin Prefab")]
    public static GameObject RebuildTrashBinPrefab()
    {
        return EnsureTrashBinPrefab();
    }

    public static GameObject EnsureTrashBinPrefab()
    {
        EnsureDirectory("Assets/_Project/Materials");
        EnsureDirectory("Assets/_Project/Prefabs");

        var material = CreateOrUpdateMaterial();
        var root = new GameObject("TrashBinPrefab");

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform);

        var sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab != null)
        {
            var visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(sourcePrefab);
            visualInstance.name = "Waste Can Visual";
            visualInstance.transform.SetParent(visual.transform);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            visualInstance.transform.localScale = Vector3.one * BinVisualScale;
            AssignMaterialToRenderers(visualInstance, material);
            RemoveMeshColliders(visualInstance);
        }
        else
        {
            BuildFallbackVisual(visual.transform, material);
        }

        var colliders = new GameObject("Colliders");
        colliders.transform.SetParent(root.transform);
        var successDetector = new GameObject("SuccessDetector");
        successDetector.transform.SetParent(root.transform);

        CreateBoxCollider(colliders.transform, "Wall_Left", new Vector3(-1.14f, 1.18f, 0f), new Vector3(0.18f, 2.20f, 1.78f), false);
        CreateBoxCollider(colliders.transform, "Wall_Right", new Vector3(1.14f, 1.18f, 0f), new Vector3(0.18f, 2.20f, 1.78f), false);
        CreateBoxCollider(colliders.transform, "Wall_Back", new Vector3(0f, 1.18f, 0.96f), new Vector3(2.28f, 2.20f, 0.18f), false);
        CreateBoxCollider(colliders.transform, "Wall_Front", new Vector3(0f, 0.55f, -0.96f), new Vector3(2.28f, 1.10f, 0.18f), false);
        CreateBoxCollider(colliders.transform, "Bottom", new Vector3(0f, 0.08f, 0f), new Vector3(2.10f, 0.16f, 1.70f), false);

        CreateBoxCollider(colliders.transform, "Rim_Left", new Vector3(-1.22f, 2.36f, 0f), new Vector3(0.24f, 0.24f, 1.96f), false);
        CreateBoxCollider(colliders.transform, "Rim_Right", new Vector3(1.22f, 2.36f, 0f), new Vector3(0.24f, 0.24f, 1.96f), false);
        CreateBoxCollider(colliders.transform, "Rim_Back", new Vector3(0f, 2.36f, 0.98f), new Vector3(2.46f, 0.24f, 0.24f), false);
        CreateBoxCollider(colliders.transform, "Rim_Front", new Vector3(0f, 2.36f, -0.98f), new Vector3(2.46f, 0.24f, 0.24f), false);

        var trigger = CreateBoxCollider(successDetector.transform, "InnerSuccessTrigger", new Vector3(0f, 1.72f, 0.10f), new Vector3(1.20f, 0.56f, 1.02f), true);
        var triggerDetector = trigger.AddComponent<TrashBinTrigger>();
        triggerDetector.SetEntryGate(true, 0.75f, false, 0f);
        AddTriggerDebugVisual(trigger.transform);
        root.AddComponent<TrashBinColliderDebugGizmo>();

        var savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, OutputPrefabPath);
        Object.DestroyImmediate(root);
        AssetDatabase.ImportAsset(OutputPrefabPath);
        AssetDatabase.SaveAssets();
        return savedPrefab;
    }

    private static Material CreateOrUpdateMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(FindLitShader());
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = FindLitShader();
        }

        material.name = "TrashBin_Outdoor_URP";
        material.color = new Color(0.18f, 0.34f, 0.30f);
        EditorUtility.SetDirty(material);
        return material;
    }

    private static Shader FindLitShader()
    {
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        return shader;
    }

    private static void AssignMaterialToRenderers(GameObject root, Material material)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        foreach (var renderer in renderers)
        {
            var materials = renderer.sharedMaterials;
            for (var i = 0; i < materials.Length; i++)
            {
                materials[i] = material;
            }
            renderer.sharedMaterials = materials;
        }
    }

    private static void RemoveMeshColliders(GameObject root)
    {
        var meshColliders = root.GetComponentsInChildren<MeshCollider>(true);
        foreach (var meshCollider in meshColliders)
        {
            Object.DestroyImmediate(meshCollider);
        }
    }

    private static GameObject CreateBoxCollider(Transform parent, string name, Vector3 localPosition, Vector3 size, bool isTrigger)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.localPosition = localPosition;
        go.transform.localRotation = Quaternion.identity;
        go.transform.localScale = Vector3.one;

        var box = go.AddComponent<BoxCollider>();
        box.size = size;
        box.isTrigger = isTrigger;
        return go;
    }

    private static void AddTriggerDebugVisual(Transform parent)
    {
        var debug = GameObject.CreatePrimitive(PrimitiveType.Cube);
        debug.name = "InnerSuccessTrigger_DebugVisual";
        debug.transform.SetParent(parent);
        debug.transform.localPosition = Vector3.zero;
        debug.transform.localRotation = Quaternion.identity;
        debug.transform.localScale = Vector3.one;
        var collider = debug.GetComponent<Collider>();
        if (collider != null)
        {
            Object.DestroyImmediate(collider);
        }

        var renderer = debug.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            renderer.enabled = false;
        }
    }

    private static void BuildFallbackVisual(Transform parent, Material material)
    {
        var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Fallback Trash Bin Visual";
        body.transform.SetParent(parent);
        body.transform.localPosition = new Vector3(0f, 1.3f, 0f);
        body.transform.localScale = new Vector3(2.5f, 2.6f, 2.0f);
        body.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(body.GetComponent<Collider>());
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.ImportAsset(path);
        }
    }
}
