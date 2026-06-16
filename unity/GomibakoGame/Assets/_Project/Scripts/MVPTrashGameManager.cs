using UnityEngine;
using UnityEngine.SceneManagement;

public class MVPTrashGameManager : MonoBehaviour
{
    private const string RootName = "MVP_Root";

    [Header("Stage References")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private TrashProjectile trash;
    [SerializeField] private Camera sceneCamera;

    [Header("Throw Settings")]
    [SerializeField] private float minImpulse = 5.5f;
    [SerializeField] private float maxImpulse = 13.5f;
    [SerializeField] private float maxDragPixels = 280f;

    private bool isAiming;
    private Vector2 dragStartPos;
    private float nextAimLogTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnPlay()
    {
        var existingManager = Object.FindFirstObjectByType<MVPTrashGameManager>();
        var hasReadableMvp = GameObject.Find("MVP_TrashBin_Rim") != null && GameObject.Find("MVP_Instructions") != null;
        if (existingManager != null && hasReadableMvp)
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
        var cameraObj = BuildCamera();
        BuildLight();

        var spawn = new GameObject("MVP_ThrowOrigin");
        spawn.transform.SetParent(root.transform);
        spawn.transform.position = new Vector3(0f, 0.65f, -4.5f);

        BuildFloor(root.transform);
        BuildThrowPad(root.transform);
        BuildTrashBin(root.transform);
        BuildInstructionText(root.transform, cameraObj);

        var trashObject = CreateTrash(spawn.transform.position, root.transform);
        var manager = root.AddComponent<MVPTrashGameManager>();
        manager.Configure(spawn.transform, trashObject, cameraObj);
        return manager;
    }

    public void Configure(Transform origin, TrashProjectile projectile, Camera cameraRef)
    {
        throwOrigin = origin;
        trash = projectile;
        sceneCamera = cameraRef;

        if (trash != null)
        {
            trash.MarkResettable(this);
        }
    }

    public Vector3 GetSpawnPosition()
    {
        return throwOrigin != null ? throwOrigin.position : new Vector3(0f, 0.65f, -4.5f);
    }

    public void Retry()
    {
        if (trash == null)
        {
            return;
        }

        trash.ResetState(GetSpawnPosition());
        Physics.SyncTransforms();
    }

    public void OnTrashSuccess()
    {
        if (trash != null && trash.IsSuccess)
        {
            Debug.Log("SUCCESS");
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
        }

        if (!isAiming)
        {
            return;
        }

        var dragDelta = (Vector2)Input.mousePosition - dragStartPos;
        var impulse = CalculateImpulse(dragDelta);
        var direction = CalculateDirection(dragDelta);

        if (Input.GetMouseButton(0) && Time.time >= nextAimLogTime)
        {
            Debug.Log($"AIM power={impulse:0.00} direction={direction.ToString("F2")}");
            nextAimLogTime = Time.time + 0.25f;
        }

        if (Input.GetMouseButtonUp(0))
        {
            Debug.Log($"THROW power={impulse:0.00} direction={direction.ToString("F2")}");
            trash.Launch(direction * impulse);
            isAiming = false;
        }
    }

    private float CalculateImpulse(Vector2 dragDelta)
    {
        var dragRate = Mathf.Clamp01(dragDelta.magnitude / maxDragPixels);
        return Mathf.Lerp(minImpulse, maxImpulse, dragRate);
    }

    private Vector3 CalculateDirection(Vector2 dragDelta)
    {
        var horizontal = Mathf.Clamp(dragDelta.x / maxDragPixels, -0.55f, 0.55f);
        var upward = Mathf.Clamp(0.45f + dragDelta.y / maxDragPixels * 0.35f, 0.25f, 0.75f);
        return new Vector3(horizontal, upward, 1f).normalized;
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
        cameraObj.transform.position = new Vector3(0f, 5.2f, -10.5f);
        cameraObj.transform.LookAt(new Vector3(0f, 0.9f, 4.2f));
        cameraObj.fieldOfView = 50f;
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
        light.intensity = 1.2f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void BuildFloor(Transform parent)
    {
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "MVP_Floor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(0f, -0.05f, 3f);
        floor.transform.localScale = new Vector3(14f, 0.1f, 20f);
        SetColor(floor, new Color(0.72f, 0.74f, 0.72f));
    }

    private static void BuildThrowPad(Transform parent)
    {
        var pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "MVP_ThrowPad";
        pad.transform.SetParent(parent);
        pad.transform.position = new Vector3(0f, 0.02f, -4.5f);
        pad.transform.localScale = new Vector3(1.4f, 0.04f, 1.4f);
        SetColor(pad, new Color(0.25f, 0.45f, 0.75f));
    }

    private static void BuildTrashBin(Transform parent)
    {
        var bin = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bin.name = "MVP_TrashBin_Body";
        bin.transform.SetParent(parent);
        bin.transform.position = new Vector3(0f, 0.55f, 8f);
        bin.transform.localScale = new Vector3(1.35f, 0.55f, 1.35f);
        SetColor(bin, new Color(0.18f, 0.28f, 0.34f));
        DestroyComponent(bin.GetComponent<Collider>());

        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "MVP_TrashBin_Rim";
        rim.transform.SetParent(parent);
        rim.transform.position = new Vector3(0f, 1.15f, 8f);
        rim.transform.localScale = new Vector3(1.55f, 0.08f, 1.55f);
        SetColor(rim, new Color(0.04f, 0.08f, 0.1f));
        DestroyComponent(rim.GetComponent<Collider>());

        var trigger = GameObject.CreatePrimitive(PrimitiveType.Cube);
        trigger.name = "MVP_BinTrigger_SUCCESS";
        trigger.transform.SetParent(parent);
        trigger.transform.position = new Vector3(0f, 1.2f, 8f);
        trigger.transform.localScale = new Vector3(1.25f, 2.2f, 1.25f);
        DestroyComponent(trigger.GetComponent<MeshRenderer>());
        var triggerCollider = trigger.GetComponent<BoxCollider>();
        triggerCollider.isTrigger = true;
        trigger.AddComponent<TrashBinTrigger>();
    }

    private static void BuildInstructionText(Transform parent, Camera cameraObj)
    {
        var textObject = new GameObject("MVP_Instructions");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(0f, 2.9f, -2.4f);

        var text = textObject.AddComponent<TextMesh>();
        text.text = "Drag and release to throw / R to reset";
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

    private static TrashProjectile CreateTrash(Vector3 position, Transform parent)
    {
        var trashObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        trashObject.name = "MVP_Trash";
        trashObject.transform.SetParent(parent);
        trashObject.transform.position = position;
        trashObject.transform.localScale = Vector3.one * 0.48f;
        SetColor(trashObject, Color.white);

        var body = trashObject.AddComponent<Rigidbody>();
        body.mass = 0.45f;
        body.linearDamping = 0.05f;
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
}
