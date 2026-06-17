using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrashBinTrigger : MonoBehaviour
{
    [SerializeField] private bool requireNotRising = true;
    [SerializeField] private float maxUpwardVelocity = 0.75f;
    [SerializeField] private bool requireInsideDepth = true;
    [SerializeField] private float minLocalZ = 0.18f;
    [SerializeField] private bool requireProjectileCenterInside = true;

    public void SetEntryGate(bool notRising, float upwardVelocityLimit, bool insideDepth, float minimumLocalZ)
    {
        requireNotRising = notRising;
        maxUpwardVelocity = upwardVelocityLimit;
        requireInsideDepth = insideDepth;
        minLocalZ = minimumLocalZ;
    }

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnValidate()
    {
        var col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        if (col is MeshCollider meshCol)
        {
            meshCol.convex = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        TryMarkSuccess(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TryMarkSuccess(other);
    }

    private void TryMarkSuccess(Collider other)
    {
        var projectile = other.GetComponentInParent<TrashProjectile>();
        if (projectile == null || projectile.IsSuccess)
        {
            return;
        }

        if (requireProjectileCenterInside && !IsProjectileCenterInside(projectile.transform.position))
        {
            return;
        }

        if (requireNotRising && projectile.Velocity.y > maxUpwardVelocity)
        {
            return;
        }

        if (requireInsideDepth)
        {
            var localPoint = transform.InverseTransformPoint(projectile.transform.position);
            if (localPoint.z < minLocalZ)
            {
                return;
            }
        }

        projectile.MarkSuccess();
    }

    private bool IsProjectileCenterInside(Vector3 worldPosition)
    {
        var box = GetComponent<BoxCollider>();
        if (box == null)
        {
            return true;
        }

        var localPosition = transform.InverseTransformPoint(worldPosition);
        var halfSize = box.size * 0.5f;
        var delta = localPosition - box.center;

        return Mathf.Abs(delta.x) <= halfSize.x
            && Mathf.Abs(delta.y) <= halfSize.y
            && Mathf.Abs(delta.z) <= halfSize.z;
    }

    private void OnDrawGizmos()
    {
        var col = GetComponent<BoxCollider>();
        if (col == null)
        {
            return;
        }

        Gizmos.color = new Color(0f, 1f, 0.35f, 0.35f);
        var oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(col.center, col.size);
        Gizmos.color = new Color(0f, 0.85f, 0.2f, 1f);
        Gizmos.DrawWireCube(col.center, col.size);
        Gizmos.matrix = oldMatrix;
    }
}
