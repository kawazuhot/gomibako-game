using UnityEngine;

public class TrashProjectile : MonoBehaviour
{
    [SerializeField] private bool useGravity = true;
    [SerializeField] private float outOfBoundsY = -5f;

    private Rigidbody body;
    private MVPTrashGameManager manager;
    private Quaternion resetRotation;
    public bool IsReadyToThrow { get; private set; } = true;
    public bool IsSuccess { get; set; }
    public Vector3 Velocity => body != null ? body.linearVelocity : Vector3.zero;

    private void Awake()
    {
        resetRotation = transform.rotation;
        body = GetComponent<Rigidbody>();
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
        }
        body.isKinematic = true;
        body.useGravity = useGravity;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        body.interpolation = RigidbodyInterpolation.Interpolate;
    }

    public void MarkResettable(MVPTrashGameManager gameManager)
    {
        manager = gameManager;
    }

    public void Launch(Vector3 force)
    {
        if (manager == null || !IsReadyToThrow)
        {
            return;
        }

        IsReadyToThrow = false;
        IsSuccess = false;
        body.isKinematic = false;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        body.AddForce(force, ForceMode.Impulse);
    }

    public void ResetState(Vector3 position)
    {
        IsReadyToThrow = true;
        IsSuccess = false;
        body.isKinematic = true;
        body.linearVelocity = Vector3.zero;
        body.angularVelocity = Vector3.zero;
        transform.position = position;
        transform.rotation = resetRotation;
    }

    private void Update()
    {
        if (!IsReadyToThrow && transform.position.y < outOfBoundsY)
        {
            manager?.Retry();
        }
    }

    public void MarkSuccess()
    {
        if (IsSuccess)
        {
            return;
        }

        IsSuccess = true;
        if (manager != null)
        {
            manager.OnTrashSuccess();
        }
    }
}
