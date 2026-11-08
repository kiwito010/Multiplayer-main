using UnityEngine;

public class ClimbableWall : MonoBehaviour, IInteractable {

    [Header("Superficie escalable")]
    [SerializeField] private BoxCollider climbCollider;

    [Header("Colliders adicionales")]
    [SerializeField] private BoxCollider[] climbZones;

    [Header("Orientación")]
    [SerializeField] private Vector3 wallNormal = Vector3.forward;

    public BoxCollider GetClimbCollider() {
        return climbCollider;
    }

    public Vector3 GetWallNormal() {
        return transform.TransformDirection(wallNormal).normalized;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;

        Gizmos.DrawRay(transform.position, GetWallNormal() * 2f);
    }

    public void Interact() {
    }


    public BoxCollider[] GetClimbZones() {
        return climbZones;
    }

    public Collider GetClosestClimbCollider(Vector3 position) {
        Collider closest = GetClimbCollider();
        float bestDistance = float.MaxValue;

        if (closest != null) {
            bestDistance = (position - closest.bounds.ClosestPoint(position)).sqrMagnitude;
        }

        if (climbZones == null)
            return closest;

        foreach (BoxCollider zone in climbZones) {
            if (zone == null || !zone.enabled ||
                !zone.gameObject.activeInHierarchy)
                continue;

            float distance = (position - zone.ClosestPoint(position)).sqrMagnitude;

            if (distance < bestDistance) {
                bestDistance = distance;
                closest = zone;
            }
        }

        return closest;
    }

}
