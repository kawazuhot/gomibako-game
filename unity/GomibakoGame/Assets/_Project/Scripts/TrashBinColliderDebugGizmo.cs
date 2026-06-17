using UnityEngine;

public class TrashBinColliderDebugGizmo : MonoBehaviour
{
    [SerializeField] private Color solidColliderColor = new Color(0.2f, 0.6f, 1f, 0.35f);
    [SerializeField] private Color triggerColliderColor = new Color(0.1f, 1f, 0.25f, 0.6f);

    private void OnDrawGizmos()
    {
        var colliders = GetComponentsInChildren<BoxCollider>();
        foreach (var box in colliders)
        {
            if (box == null)
            {
                continue;
            }

            Gizmos.color = box.isTrigger ? triggerColliderColor : solidColliderColor;
            Gizmos.matrix = box.transform.localToWorldMatrix;
            Gizmos.DrawWireCube(box.center, box.size);
        }

        Gizmos.matrix = Matrix4x4.identity;
    }
}
