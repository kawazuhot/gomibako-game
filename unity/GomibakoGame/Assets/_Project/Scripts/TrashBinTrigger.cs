using UnityEngine;

[RequireComponent(typeof(Collider))]
public class TrashBinTrigger : MonoBehaviour
{
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
}

