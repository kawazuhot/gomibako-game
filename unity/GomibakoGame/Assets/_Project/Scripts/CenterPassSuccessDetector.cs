using UnityEngine;

public class CenterPassSuccessDetector : MonoBehaviour
{
    [SerializeField] private Transform canTransform;
    [SerializeField] private TrashProjectile projectile;
    [SerializeField] private float canDiameter = 1f;
    [SerializeField] private float successCenterRadiusMultiplier = 0.25f;
    [SerializeField] private float successDepthMultiplier = 0.4f;

    private bool hasPreviousPosition;
    private Vector3 previousCanPosition;

    public float SuccessCenterRadius => Mathf.Max(0.01f, canDiameter) * Mathf.Max(0.01f, successCenterRadiusMultiplier);
    public float SuccessDepth => Mathf.Max(0.01f, canDiameter) * Mathf.Max(0.01f, successDepthMultiplier);

    private void Update()
    {
        if (projectile == null || canTransform == null)
        {
            return;
        }

        if (projectile.IsSuccess || projectile.IsReadyToThrow)
        {
            ResetTracking();
            return;
        }

        var currentPosition = canTransform.position;
        if (!hasPreviousPosition)
        {
            previousCanPosition = currentPosition;
            hasPreviousPosition = true;
            return;
        }

        CheckCenterPass(previousCanPosition, currentPosition);
        previousCanPosition = currentPosition;
    }

    public void Configure(TrashProjectile targetProjectile, float diameter, float centerRadiusMultiplier, float depthMultiplier)
    {
        projectile = targetProjectile;
        canTransform = targetProjectile != null ? targetProjectile.transform : null;
        canDiameter = diameter;
        successCenterRadiusMultiplier = centerRadiusMultiplier;
        successDepthMultiplier = depthMultiplier;
        ResetTracking();
    }

    public void ResetTracking()
    {
        hasPreviousPosition = false;
        if (canTransform != null)
        {
            previousCanPosition = canTransform.position;
        }
    }

    private void CheckCenterPass(Vector3 previousPosition, Vector3 currentPosition)
    {
        var forward = transform.forward.normalized;
        var previousDepth = Vector3.Dot(previousPosition - transform.position, forward);
        var currentDepth = Vector3.Dot(currentPosition - transform.position, forward);
        var targetDepth = SuccessDepth;

        if (previousDepth > targetDepth || currentDepth < targetDepth)
        {
            return;
        }

        var depthDelta = currentDepth - previousDepth;
        if (Mathf.Abs(depthDelta) < 0.0001f)
        {
            return;
        }

        var t = Mathf.Clamp01((targetDepth - previousDepth) / depthDelta);
        var passPosition = Vector3.Lerp(previousPosition, currentPosition, t);
        var planeCenter = transform.position + forward * targetDepth;
        var lateralOffset = Vector3.ProjectOnPlane(passPosition - planeCenter, forward);

        if (lateralOffset.magnitude <= SuccessCenterRadius)
        {
            projectile.MarkSuccess();
        }
    }

    private void OnDrawGizmos()
    {
        var forward = transform.forward.normalized;
        var up = transform.up.normalized;
        var right = transform.right.normalized;
        var center = transform.position + forward * SuccessDepth;
        var radius = SuccessCenterRadius;

        Gizmos.color = Color.green;
        const int segments = 48;
        var previous = center + right * radius;
        for (var i = 1; i <= segments; i++)
        {
            var angle = Mathf.PI * 2f * i / segments;
            var next = center + (right * Mathf.Cos(angle) + up * Mathf.Sin(angle)) * radius;
            Gizmos.DrawLine(previous, next);
            previous = next;
        }

        Gizmos.DrawLine(transform.position, center);
    }
}
