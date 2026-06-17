using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class MVPTrashGameManager : MonoBehaviour
{
    private const string RootName = "MVP_Root";

    [Header("Stage References")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private TrashProjectile trash;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private TextMesh successText;
    [SerializeField] private Transform trashTransform;
    [SerializeField] private LineRenderer pullLine;

    [Header("Slingshot Throw Settings")]
    [SerializeField] private float maxPullDistance = 250f;
    [SerializeField] private float forwardForceMultiplier = 0.05f;
    [SerializeField] private float upwardForceMultiplier = 0.035f;
    [SerializeField] private float sideForceMultiplier = 0.035f;
    [SerializeField] private float minPullDistance = 20f;

    private bool isAiming;
    private Vector2 dragStartPos;
    private float nextAimLogTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnPlay()
    {
        var existingManager = Object.FindFirstObjectByType<MVPTrashGameManager>();
        var hasOutdoorMvp = GameObject.Find("TrashBinPrefab") != null
            && GameObject.Find("InnerSuccessTrigger") != null
            && GameObject.Find("MVP_Trash") != null
            && GameObject.Find("MVP_Instructions") != null
            && GameObject.Find("MVP_SuccessText") != null;

        if (existingManager != null && hasOutdoorMvp)
        {
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (scene.name == "Main" || string.IsNullOrEmpty(scene.name))
        {
            RebuildPlayableScene();
        }
    }

    private void Start()
    {
        if (sceneCamera == null)
        {
            sceneCamera = Camera.main;
        }

        if (trash == null)
        {
            trash = Object.FindFirstObjectByType<TrashProjectile>();
        }

        if (trash == null)
        {
            trash = CreateTrash(GetSpawnPosition(), transform);
        }

        if (trashTransform == null && trash != null)
        {
            trashTransform = trash.transform;
        }

        if (successText == null)
        {
            var successObject = GameObject.Find("MVP_SuccessText");
            if (successObject != null)
            {
                successText = successObject.GetComponent<TextMesh>();
            }
        }

        FindPullLineIfNeeded();
        EnsurePullLine();
        trash.MarkResettable(this);
        Retry();
    }

    private void Update()
    {
        HandleMouseAim();
        HandleRetry();
    }

    public static MVPTrashGameManager RebuildPlayableScene()
    {
        DestroyIfExists(RootName);

        var root = new GameObject(RootName);
        var manager = root.AddComponent<MVPTrashGameManager>();
        var cameraObj = BuildCamera();
        BuildLight();

        var spawn = new GameObject("MVP_ThrowOrigin");
        spawn.transform.SetParent(root.transform);
        spawn.transform.position = new Vector3(0f, 0.65f, -5.2f);

        BuildStage(root.transform);
        BuildThrowPad(root.transform);
        BuildOutdoorTrashBin(root.transform);
        BuildBackground(root.transform);
        BuildInstructionText(root.transform, cameraObj);
        manager.pullLine = BuildPullLine(root.transform);
        var success = BuildSuccessText(root.transform, cameraObj);

        var trashObject = CreateTrash(spawn.transform.position, root.transform);
        manager.trashTransform = trashObject.transform;
        manager.Configure(spawn.transform, trashObject, cameraObj, success);
        return manager;
    }

    public void Configure(Transform origin, TrashProjectile projectile, Camera cameraRef, TextMesh successMessage)
    {
        throwOrigin = origin;
        trash = projectile;
        sceneCamera = cameraRef;
        successText = successMessage;

        if (trash != null)
        {
            trashTransform = trash.transform;
            trash.MarkResettable(this);
        }
    }

    public Vector3 GetSpawnPosition()
    {
        return throwOrigin != null ? throwOrigin.position : new Vector3(0f, 0.65f, -5.2f);
    }

    public void Retry()
    {
        if (trash == null)
        {
            return;
        }

        trash.ResetState(GetSpawnPosition());
        SetSuccessVisible(false);
        Physics.SyncTransforms();
    }

    public void OnTrashSuccess()
    {
        if (trash != null && trash.IsSuccess)
        {
            Debug.Log("SUCCESS");
            SetSuccessVisible(true);
        }
    }

    private void HandleRetry()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            Retry();
        }
    }

    private void HandleMouseAim()
    {
        if (trash == null || !trash.IsReadyToThrow)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0))
        {
            isAiming = true;
            dragStartPos = Input.mousePosition;
            nextAimLogTime = 0f;
            Debug.Log($"MouseDown position={dragStartPos}");
            UpdatePullLine(Vector3.zero);
        }

        if (!isAiming)
        {
            return;
        }

        var pullVector = (Vector2)Input.mousePosition - dragStartPos;
        var throwForce = CalculateSlingshotForce(pullVector);
        var pullDistance = Mathf.Min(pullVector.magnitude, maxPullDistance);
        UpdatePullLine(throwForce);

        if (Input.GetMouseButton(0) && Time.time >= nextAimLogTime)
        {
            Debug.Log($"Pull distance={pullDistance:0.0} force={throwForce.ToString("F2")}");
            nextAimLogTime = Time.time + 0.25f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            HidePullLine();
            if (pullDistance >= minPullDistance)
            {
                Debug.Log($"Release force={throwForce.ToString("F2")}");
                trash.Launch(throwForce);
            }
            else
            {
                Debug.Log($"Release canceled pullDistance={pullDistance:0.0}");
            }
            isAiming = false;
        }
    }

    private Vector3 CalculateSlingshotForce(Vector2 pullVector)
    {
        var clampedPull = Vector2.ClampMagnitude(pullVector, maxPullDistance);
        var pullDown = Mathf.Max(0f, -clampedPull.y);
        var pullSide = clampedPull.x;
        var forwardForce = pullDown * forwardForceMultiplier;
        var upwardForce = pullDown * upwardForceMultiplier;
        var sideForce = -pullSide * sideForceMultiplier;

        return Vector3.forward * forwardForce + Vector3.up * upwardForce + Vector3.right * sideForce;
    }

    private static Camera BuildCamera()
    {
        var existing = Camera.main;
        var cameraObj = existing != null ? existing : Object.FindFirstObjectByType<Camera>();

        if (cameraObj == null)
        {
            var go = new GameObject("Main Camera");
            cameraObj = go.AddComponent<Camera>();
        }

        cameraObj.tag = "MainCamera";
        cameraObj.transform.position = new Vector3(0f, 5.7f, -12.2f);
        cameraObj.transform.LookAt(new Vector3(0f, 0.95f, 4.8f));
        cameraObj.fieldOfView = 48f;
        cameraObj.clearFlags = CameraClearFlags.Skybox;
        return cameraObj;
    }

    private static void BuildLight()
    {
        var light = Object.FindFirstObjectByType<Light>();
        if (light == null)
        {
            var lightObject = new GameObject("Directional Light");
            light = lightObject.AddComponent<Light>();
        }

        light.type = LightType.Directional;
        light.intensity = 1.25f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void BuildStage(Transform parent)
    {
        var nearSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        nearSidewalk.name = "MVP_NearSidewalk";
        nearSidewalk.transform.SetParent(parent);
        nearSidewalk.transform.position = new Vector3(0f, -0.06f, -4.8f);
        nearSidewalk.transform.localScale = new Vector3(14f, 0.12f, 3.4f);
        SetColor(nearSidewalk, new Color(0.64f, 0.65f, 0.62f));

        var road = GameObject.CreatePrimitive(PrimitiveType.Cube);
        road.name = "MVP_Road";
        road.transform.SetParent(parent);
        road.transform.position = new Vector3(0f, -0.08f, 1.8f);
        road.transform.localScale = new Vector3(14f, 0.08f, 9.8f);
        SetColor(road, new Color(0.30f, 0.31f, 0.31f));

        var farSidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        farSidewalk.name = "MVP_FarSidewalk";
        farSidewalk.transform.SetParent(parent);
        farSidewalk.transform.position = new Vector3(0f, -0.04f, 8.8f);
        farSidewalk.transform.localScale = new Vector3(14f, 0.14f, 4.2f);
        SetColor(farSidewalk, new Color(0.67f, 0.68f, 0.65f));

        for (var i = -2; i <= 2; i++)
        {
            var line = GameObject.CreatePrimitive(PrimitiveType.Cube);
            line.name = $"MVP_RoadCenterLine_{i}";
            line.transform.SetParent(parent);
            line.transform.position = new Vector3(0f, 0.01f, i * 1.8f + 1.2f);
            line.transform.localScale = new Vector3(0.16f, 0.02f, 0.85f);
            SetColor(line, new Color(0.95f, 0.86f, 0.34f));
            DestroyComponent(line.GetComponent<Collider>());
        }
    }

    private static void BuildThrowPad(Transform parent)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "MVP_ThrowPad";
        pad.transform.SetParent(parent);
        pad.transform.position = new Vector3(0f, 0.02f, -5.2f);
        pad.transform.localScale = new Vector3(1.3f, 0.04f, 1.3f);
        SetColor(pad, new Color(0.24f, 0.45f, 0.76f));
    }

    private static void BuildOutdoorTrashBin(Transform parent)
    {
#if UNITY_EDITOR
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Prefabs/TrashBinPrefab.prefab");
        if (prefab != null)
        {
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.name = "TrashBinPrefab";
            instance.transform.SetParent(parent);
            instance.transform.position = new Vector3(0f, 0f, 9.1f);
            instance.transform.rotation = Quaternion.identity;
            instance.transform.localScale = Vector3.one;
            return;
        }
#endif

        var binRoot = new GameObject("TrashBinPrefab");
        binRoot.transform.SetParent(parent);
        binRoot.transform.position = new Vector3(0f, 0f, 9.1f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(binRoot.transform);
        var colliders = new GameObject("Colliders");
        colliders.transform.SetParent(binRoot.transform);
        var detector = new GameObject("SuccessDetector");
        detector.transform.SetParent(binRoot.transform);

        CreateBinVisualPanel("Visual_Back", visual.transform, new Vector3(0f, 1.30f, 0.96f), new Vector3(2.50f, 2.60f, 0.16f), new Color(0.16f, 0.36f, 0.42f));
        CreateBinVisualPanel("Visual_Left", visual.transform, new Vector3(-1.24f, 1.30f, 0f), new Vector3(0.16f, 2.60f, 2.00f), new Color(0.13f, 0.31f, 0.36f));
        CreateBinVisualPanel("Visual_Right", visual.transform, new Vector3(1.24f, 1.30f, 0f), new Vector3(0.16f, 2.60f, 2.00f), new Color(0.13f, 0.31f, 0.36f));
        CreateBinVisualPanel("Visual_FrontLow", visual.transform, new Vector3(0f, 0.66f, -1.00f), new Vector3(2.50f, 1.32f, 0.16f), new Color(0.16f, 0.36f, 0.42f));
        CreateBinVisualPanel("Visual_InnerDark", visual.transform, new Vector3(0f, 2.10f, 0f), new Vector3(1.84f, 0.16f, 1.44f), new Color(0.03f, 0.04f, 0.04f));

        CreateBinVisualPanel("Visual_Rim_Back", visual.transform, new Vector3(0f, 2.64f, 1.04f), new Vector3(2.72f, 0.26f, 0.28f), new Color(0.08f, 0.12f, 0.13f));
        CreateBinVisualPanel("Visual_Rim_Front", visual.transform, new Vector3(0f, 2.64f, -1.04f), new Vector3(2.72f, 0.26f, 0.28f), new Color(0.08f, 0.12f, 0.13f));
        CreateBinVisualPanel("Visual_Rim_Left", visual.transform, new Vector3(-1.32f, 2.64f, 0f), new Vector3(0.28f, 0.26f, 2.12f), new Color(0.08f, 0.12f, 0.13f));
        CreateBinVisualPanel("Visual_Rim_Right", visual.transform, new Vector3(1.32f, 2.64f, 0f), new Vector3(0.28f, 0.26f, 2.12f), new Color(0.08f, 0.12f, 0.13f));

        CreateInvisibleCollider("Wall_Left", colliders.transform, new Vector3(-1.14f, 1.18f, 0f), new Vector3(0.18f, 2.20f, 1.78f), false);
        CreateInvisibleCollider("Wall_Right", colliders.transform, new Vector3(1.14f, 1.18f, 0f), new Vector3(0.18f, 2.20f, 1.78f), false);
        CreateInvisibleCollider("Wall_Back", colliders.transform, new Vector3(0f, 1.18f, 0.96f), new Vector3(2.28f, 2.20f, 0.18f), false);
        CreateInvisibleCollider("Wall_Front", colliders.transform, new Vector3(0f, 0.55f, -0.96f), new Vector3(2.28f, 1.10f, 0.18f), false);
        CreateInvisibleCollider("Bottom", colliders.transform, new Vector3(0f, 0.08f, 0f), new Vector3(2.10f, 0.16f, 1.70f), false);

        CreateInvisibleCollider("Rim_Left", colliders.transform, new Vector3(-1.22f, 2.36f, 0f), new Vector3(0.24f, 0.24f, 1.96f), false);
        CreateInvisibleCollider("Rim_Right", colliders.transform, new Vector3(1.22f, 2.36f, 0f), new Vector3(0.24f, 0.24f, 1.96f), false);
        CreateInvisibleCollider("Rim_Back", colliders.transform, new Vector3(0f, 2.36f, 0.98f), new Vector3(2.46f, 0.24f, 0.24f), false);
        CreateInvisibleCollider("Rim_Front", colliders.transform, new Vector3(0f, 2.36f, -0.98f), new Vector3(2.46f, 0.24f, 0.24f), false);

        var successTrigger = CreateInvisibleCollider("InnerSuccessTrigger", detector.transform, new Vector3(0f, 1.72f, 0.10f), new Vector3(1.20f, 0.56f, 1.02f), true);
        var triggerDetector = successTrigger.AddComponent<TrashBinTrigger>();
        triggerDetector.SetEntryGate(true, 0.75f, false, 0f);
        binRoot.AddComponent<TrashBinColliderDebugGizmo>();
    }

    private static void BuildBackground(Transform parent)
    {
        for (var i = -1; i <= 1; i += 2)
        {
            var building = GameObject.CreatePrimitive(PrimitiveType.Cube);
            building.name = i < 0 ? "MVP_LeftBuilding" : "MVP_RightBuilding";
            building.transform.SetParent(parent);
            building.transform.position = new Vector3(i * 5.2f, 1.8f, 10.8f);
            building.transform.localScale = new Vector3(2.2f, 3.6f, 1.0f);
            SetColor(building, new Color(0.55f, 0.56f, 0.58f));
            DestroyComponent(building.GetComponent<Collider>());
        }
    }

    private static void CreateBinVisualPanel(string name, Transform parent, Vector3 localPosition, Vector3 localScale, Color color)
    {
        var panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent);
        panel.transform.localPosition = localPosition;
        panel.transform.localScale = localScale;
        SetColor(panel, color);
        DestroyComponent(panel.GetComponent<Collider>());
    }

    private static GameObject CreateInvisibleCollider(string name, Transform parent, Vector3 localPosition, Vector3 localScale, bool isTrigger)
    {
        var colliderObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        colliderObject.name = name;
        colliderObject.transform.SetParent(parent);
        colliderObject.transform.localPosition = localPosition;
        colliderObject.transform.localScale = localScale;
        DestroyComponent(colliderObject.GetComponent<MeshRenderer>());
        var box = colliderObject.GetComponent<BoxCollider>();
        box.isTrigger = isTrigger;
        return colliderObject;
    }

    private static void BuildInstructionText(Transform parent, Camera cameraObj)
    {
        var textObject = new GameObject("MVP_Instructions");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(0f, 2.9f, -3.0f);

        var text = textObject.AddComponent<TextMesh>();
        text.text = "Trash Shoot Stage 1 / Pull down and release / R to reset";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 56;
        text.characterSize = 0.08f;
        text.color = Color.black;

        if (cameraObj != null)
        {
            textObject.transform.rotation = Quaternion.LookRotation(textObject.transform.position - cameraObj.transform.position);
        }
    }

    private static TextMesh BuildSuccessText(Transform parent, Camera cameraObj)
    {
        var textObject = new GameObject("MVP_SuccessText");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(0f, 2.2f, 3.6f);

        var text = textObject.AddComponent<TextMesh>();
        text.text = "SUCCESS!";
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.fontSize = 80;
        text.characterSize = 0.12f;
        text.color = new Color(0.05f, 0.55f, 0.1f);

        if (cameraObj != null)
        {
            textObject.transform.rotation = Quaternion.LookRotation(textObject.transform.position - cameraObj.transform.position);
        }

        textObject.SetActive(false);
        return text;
    }

    private static LineRenderer BuildPullLine(Transform parent)
    {
        var lineObject = new GameObject("MVP_PullDirectionLine");
        lineObject.transform.SetParent(parent);
        var line = lineObject.AddComponent<LineRenderer>();
        line.positionCount = 2;
        line.startWidth = 0.08f;
        line.endWidth = 0.02f;
        line.useWorldSpace = true;
        line.enabled = false;

        var shader = Shader.Find("Sprites/Default");
        if (shader != null)
        {
            line.sharedMaterial = new Material(shader);
            line.startColor = new Color(1f, 0.75f, 0.05f);
            line.endColor = new Color(1f, 0.15f, 0.05f);
        }

        return line;
    }

    private static TrashProjectile CreateTrash(Vector3 position, Transform parent)
    {
        var trashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        trashObject.name = "MVP_Trash";
        trashObject.transform.SetParent(parent);
        trashObject.transform.position = position;
        trashObject.transform.localScale = Vector3.one * 0.48f;
        SetColor(trashObject, new Color(0.88f, 0.88f, 0.82f));

        var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
        label.name = "MVP_Trash_CrumpleMark";
        label.transform.SetParent(trashObject.transform);
        label.transform.localPosition = new Vector3(0.08f, 0.02f, -0.44f);
        label.transform.localRotation = Quaternion.Euler(0f, 20f, 0f);
        label.transform.localScale = new Vector3(0.32f, 0.08f, 0.04f);
        SetColor(label, new Color(0.62f, 0.62f, 0.58f));
        DestroyComponent(label.GetComponent<Collider>());

        var body = trashObject.AddComponent<Rigidbody>();
        body.mass = 0.40f;
        body.linearDamping = 0.04f;
        body.angularDamping = 0.05f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        return trashObject.AddComponent<TrashProjectile>();
    }

    private static void SetColor(GameObject target, Color color)
    {
        var renderer = target.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Material material = null;
        if (renderer.sharedMaterial != null)
        {
            material = new Material(renderer.sharedMaterial);
        }

        if (material == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            if (shader != null)
            {
                material = new Material(shader);
            }
        }

        if (material != null)
        {
            material.color = color;
            renderer.sharedMaterial = material;
        }
    }

    private static void DestroyIfExists(string objectName)
    {
        var found = GameObject.Find(objectName);
        if (found != null)
        {
            DestroyObject(found);
        }
    }

    private static void DestroyComponent(Component component)
    {
        if (component != null)
        {
            DestroyObject(component);
        }
    }

    private static void DestroyObject(Object target)
    {
        if (Application.isPlaying)
        {
            Object.Destroy(target);
        }
        else
        {
            Object.DestroyImmediate(target);
        }
    }

    private void SetSuccessVisible(bool visible)
    {
        if (successText != null)
        {
            successText.gameObject.SetActive(visible);
        }
    }

    private void FindPullLineIfNeeded()
    {
        if (pullLine == null)
        {
            var found = GameObject.Find("MVP_PullDirectionLine");
            pullLine = found != null ? found.GetComponent<LineRenderer>() : null;
        }
    }

    private void EnsurePullLine()
    {
        if (pullLine == null)
        {
            pullLine = BuildPullLine(transform);
        }

        HidePullLine();
    }

    private void UpdatePullLine(Vector3 throwForce)
    {
        if (pullLine == null || trashTransform == null)
        {
            return;
        }

        var start = trashTransform.position + Vector3.up * 0.35f;
        var forceMagnitude = throwForce.magnitude;
        var end = forceMagnitude > 0.001f
            ? start + throwForce.normalized * Mathf.Clamp(forceMagnitude * 0.22f, 0.2f, 3.0f)
            : start;

        pullLine.SetPosition(0, start);
        pullLine.SetPosition(1, end);
        pullLine.enabled = true;
    }

    private void HidePullLine()
    {
        if (pullLine != null)
        {
            pullLine.enabled = false;
        }
    }
}
