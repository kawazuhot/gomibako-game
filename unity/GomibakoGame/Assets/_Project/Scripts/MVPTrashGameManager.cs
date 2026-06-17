using UnityEngine;
using UnityEngine.SceneManagement;

public class MVPTrashGameManager : MonoBehaviour
{
    private const string RootName = "MVP_Root";

    [Header("Stage References")]
    [SerializeField] private Transform throwOrigin;
    [SerializeField] private TrashProjectile trash;
    [SerializeField] private Camera sceneCamera;
    [SerializeField] private TextMesh successText;
    [SerializeField] private Transform canTransform;
    [SerializeField] private Transform visualHoleTransform;
    [SerializeField] private Transform visualHoleRimTransform;
    [SerializeField] private Transform holeCenterTransform;
    [SerializeField] private CenterPassSuccessDetector centerPassDetector;
    [SerializeField] private LineRenderer pullLine;

    [Header("Slingshot Throw Settings")]
    [SerializeField] private float maxPullDistance = 250f;
    [SerializeField] private float forwardForceMultiplier = 0.05f;
    [SerializeField] private float upwardForceMultiplier = 0.035f;
    [SerializeField] private float sideForceMultiplier = 0.035f;
    [SerializeField] private float minPullDistance = 20f;

    [Header("Can Hole Tuning")]
    [SerializeField] private float canDiameter = 1f;
    [SerializeField] private float visualHoleDiameterMultiplier = 1.15f;
    [SerializeField] private float successCenterRadiusMultiplier = 0.25f;
    [SerializeField] private float rimThicknessMultiplier = 0.08f;
    [SerializeField] private float successDepthMultiplier = 0.4f;

    private bool isAiming;
    private Vector2 dragStartPos;
    private float nextAimLogTime;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void BootstrapOnPlay()
    {
        var existingManager = Object.FindFirstObjectByType<MVPTrashGameManager>();
        var hasReadableMvp = GameObject.Find("MVP_CanBin_HoleRim") != null
            && GameObject.Find("MVP_CanBin_Hole") != null
            && GameObject.Find("MVP_HoleCenter") != null
            && GameObject.Find("MVP_Can") != null
            && GameObject.Find("MVP_Instructions") != null
            && GameObject.Find("MVP_SuccessText") != null;
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
            trash = CreateTrash(GetSpawnPosition(), transform, canDiameter);
        }

        if (canTransform == null && trash != null)
        {
            canTransform = trash.transform;
        }

        FindTuningTargetsIfNeeded();
        EnsurePullLine();

        if (successText == null)
        {
            var successObject = GameObject.Find("MVP_SuccessText");
            if (successObject != null)
            {
                successText = successObject.GetComponent<TextMesh>();
            }
        }

        trash.MarkResettable(this);
        ApplyCanHoleTuning();
        Retry();
    }

    private void OnValidate()
    {
        ApplyCanHoleTuning();
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
        spawn.transform.position = new Vector3(0f, 0.65f, -4.5f);

        BuildFloor(root.transform);
        BuildThrowPad(root.transform);
        BuildCanBin(root.transform, manager);
        BuildVendingMachine(root.transform);
        BuildInstructionText(root.transform, cameraObj);
        manager.pullLine = BuildPullLine(root.transform);
        var success = BuildSuccessText(root.transform, cameraObj);

        var trashObject = CreateTrash(spawn.transform.position, root.transform, manager.canDiameter);
        manager.canTransform = trashObject.transform;
        manager.Configure(spawn.transform, trashObject, cameraObj, success);
        manager.ApplyCanHoleTuning();
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
            trash.MarkResettable(this);
        }

        ConfigureCenterPassDetector();
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
        if (centerPassDetector != null)
        {
            centerPassDetector.ResetTracking();
        }
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
        floor.name = "MVP_StreetFloor";
        floor.transform.SetParent(parent);
        floor.transform.position = new Vector3(0f, -0.05f, 3f);
        floor.transform.localScale = new Vector3(14f, 0.1f, 20f);
        SetColor(floor, new Color(0.42f, 0.43f, 0.42f));

        var sidewalk = GameObject.CreatePrimitive(PrimitiveType.Cube);
        sidewalk.name = "MVP_Sidewalk";
        sidewalk.transform.SetParent(parent);
        sidewalk.transform.position = new Vector3(-4.6f, 0.02f, 3f);
        sidewalk.transform.localScale = new Vector3(3.2f, 0.12f, 20f);
        SetColor(sidewalk, new Color(0.66f, 0.67f, 0.64f));
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

    private static void BuildCanBin(Transform parent, MVPTrashGameManager settings)
    {
        var safeCanDiameter = Mathf.Max(0.01f, settings.canDiameter);
        var visualHoleDiameter = safeCanDiameter * Mathf.Max(0.01f, settings.visualHoleDiameterMultiplier);
        var rimThickness = safeCanDiameter * Mathf.Max(0f, settings.rimThicknessMultiplier);
        var binCenter = new Vector3(0f, 0.85f, 7.8f);
        var frontZ = 7.36f;
        var holeCenter = new Vector3(0f, 1.35f, frontZ);

        var bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        bin.name = "MVP_CanBin_Body";
        bin.transform.SetParent(parent);
        bin.transform.position = binCenter;
        bin.transform.localScale = new Vector3(1.8f, 1.7f, 0.8f);
        SetColor(bin, new Color(0.12f, 0.44f, 0.58f));
        DestroyComponent(bin.GetComponent<Collider>());

        var top = GameObject.CreatePrimitive(PrimitiveType.Cube);
        top.name = "MVP_CanBin_Top";
        top.transform.SetParent(parent);
        top.transform.position = new Vector3(0f, 1.74f, 7.8f);
        top.transform.localScale = new Vector3(1.95f, 0.12f, 0.9f);
        SetColor(top, new Color(0.08f, 0.28f, 0.36f));
        DestroyComponent(top.GetComponent<Collider>());

        var rim = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        rim.name = "MVP_CanBin_HoleRim";
        rim.transform.SetParent(parent);
        rim.transform.position = holeCenter + new Vector3(0f, 0f, -0.012f);
        rim.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        rim.transform.localScale = new Vector3(
            visualHoleDiameter + rimThickness * 2f,
            rimThickness,
            visualHoleDiameter + rimThickness * 2f);
        SetColor(rim, new Color(0.78f, 0.86f, 0.9f));
        DestroyComponent(rim.GetComponent<Collider>());
        settings.visualHoleRimTransform = rim.transform;

        var hole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        hole.name = "MVP_CanBin_Hole";
        hole.transform.SetParent(parent);
        hole.transform.position = holeCenter;
        hole.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        hole.transform.localScale = new Vector3(visualHoleDiameter, rimThickness, visualHoleDiameter);
        SetColor(hole, Color.black);
        DestroyComponent(hole.GetComponent<Collider>());
        settings.visualHoleTransform = hole.transform;

        var detectorObject = new GameObject("MVP_HoleCenter");
        detectorObject.transform.SetParent(parent);
        detectorObject.transform.position = holeCenter;
        detectorObject.transform.rotation = Quaternion.identity;
        settings.holeCenterTransform = detectorObject.transform;
        settings.centerPassDetector = detectorObject.AddComponent<CenterPassSuccessDetector>();
    }

    private static void BuildVendingMachine(Transform parent)
    {
        var vending = GameObject.CreatePrimitive(PrimitiveType.Cube);
        vending.name = "MVP_VendingMachine";
        vending.transform.SetParent(parent);
        vending.transform.position = new Vector3(2.2f, 1.25f, 7.9f);
        vending.transform.localScale = new Vector3(1.2f, 2.5f, 0.65f);
        SetColor(vending, new Color(0.78f, 0.08f, 0.08f));
        DestroyComponent(vending.GetComponent<Collider>());

        var display = GameObject.CreatePrimitive(PrimitiveType.Cube);
        display.name = "MVP_VendingMachine_Display";
        display.transform.SetParent(parent);
        display.transform.position = new Vector3(2.2f, 1.65f, 7.52f);
        display.transform.localScale = new Vector3(0.9f, 0.85f, 0.04f);
        SetColor(display, new Color(0.85f, 0.95f, 1f));
        DestroyComponent(display.GetComponent<Collider>());

        var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
        slot.name = "MVP_VendingMachine_Slot";
        slot.transform.SetParent(parent);
        slot.transform.position = new Vector3(2.2f, 0.55f, 7.5f);
        slot.transform.localScale = new Vector3(0.7f, 0.18f, 0.04f);
        SetColor(slot, new Color(0.08f, 0.08f, 0.08f));
        DestroyComponent(slot.GetComponent<Collider>());
    }

    private static void BuildInstructionText(Transform parent, Camera cameraObj)
    {
        var textObject = new GameObject("MVP_Instructions");
        textObject.transform.SetParent(parent);
        textObject.transform.position = new Vector3(0f, 2.9f, -2.4f);

        var text = textObject.AddComponent<TextMesh>();
        text.text = "Can Shoot Stage 1 / Drag and release to throw / R to reset";
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
        textObject.transform.position = new Vector3(0f, 2.1f, 3f);

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

    private static TrashProjectile CreateTrash(Vector3 position, Transform parent, float diameter)
    {
        var safeDiameter = Mathf.Max(0.01f, diameter);

        var trashObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trashObject.name = "MVP_Can";
        trashObject.transform.SetParent(parent);
        trashObject.transform.position = position;
        trashObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        trashObject.transform.localScale = new Vector3(safeDiameter, safeDiameter * 0.8f, safeDiameter);
        SetColor(trashObject, new Color(0.86f, 0.92f, 0.96f));

        var label = GameObject.CreatePrimitive(PrimitiveType.Cube);
        label.name = "MVP_Can_Label";
        label.transform.SetParent(trashObject.transform);
        label.transform.localPosition = new Vector3(0f, 0.02f, -0.52f);
        label.transform.localRotation = Quaternion.identity;
        label.transform.localScale = new Vector3(0.7f, 0.32f, 0.04f);
        SetColor(label, new Color(0.1f, 0.45f, 0.95f));
        DestroyComponent(label.GetComponent<Collider>());

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

    private void SetSuccessVisible(bool visible)
    {
        if (successText != null)
        {
            successText.gameObject.SetActive(visible);
        }
    }

    private void FindTuningTargetsIfNeeded()
    {
        if (visualHoleTransform == null)
        {
            var found = GameObject.Find("MVP_CanBin_Hole");
            visualHoleTransform = found != null ? found.transform : null;
        }

        if (visualHoleRimTransform == null)
        {
            var found = GameObject.Find("MVP_CanBin_HoleRim");
            visualHoleRimTransform = found != null ? found.transform : null;
        }

        if (holeCenterTransform == null)
        {
            var found = GameObject.Find("MVP_HoleCenter");
            holeCenterTransform = found != null ? found.transform : null;
        }

        if (centerPassDetector == null && holeCenterTransform != null)
        {
            centerPassDetector = holeCenterTransform.GetComponent<CenterPassSuccessDetector>();
        }

        if (pullLine == null)
        {
            var found = GameObject.Find("MVP_PullDirectionLine");
            pullLine = found != null ? found.GetComponent<LineRenderer>() : null;
        }
    }

    private void ApplyCanHoleTuning()
    {
        var safeCanDiameter = Mathf.Max(0.01f, canDiameter);
        var visualHoleDiameter = safeCanDiameter * Mathf.Max(0.01f, visualHoleDiameterMultiplier);
        var rimThickness = safeCanDiameter * Mathf.Max(0f, rimThicknessMultiplier);

        if (canTransform != null)
        {
            canTransform.localScale = new Vector3(safeCanDiameter, safeCanDiameter * 0.8f, safeCanDiameter);
        }

        if (visualHoleRimTransform != null)
        {
            visualHoleRimTransform.localScale = new Vector3(
                visualHoleDiameter + rimThickness * 2f,
                rimThickness,
                visualHoleDiameter + rimThickness * 2f);
        }

        if (visualHoleTransform != null)
        {
            visualHoleTransform.localScale = new Vector3(visualHoleDiameter, rimThickness, visualHoleDiameter);
        }

        if (holeCenterTransform != null && visualHoleTransform != null)
        {
            holeCenterTransform.position = visualHoleTransform.position;
            holeCenterTransform.rotation = Quaternion.identity;
        }

        ConfigureCenterPassDetector();
    }

    private void ConfigureCenterPassDetector()
    {
        if (centerPassDetector == null)
        {
            return;
        }

        centerPassDetector.Configure(trash, canDiameter, successCenterRadiusMultiplier, successDepthMultiplier);
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
        if (pullLine == null || canTransform == null)
        {
            return;
        }

        var start = canTransform.position + Vector3.up * 0.35f;
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
