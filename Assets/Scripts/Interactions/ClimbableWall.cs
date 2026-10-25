using UnityEngine;

public class ClimbableWall : MonoBehaviour, IInteractable {
    [SerializeField] private Transform climbPoint;

    [SerializeField] private Vector3 wallNormal = Vector3.forward;

    [SerializeField] private Collider climbCollider;

    [SerializeField] private BoxCollider topExitArea;

    public BoxCollider GetTopExitArea() {
        return topExitArea;
    }

    public Collider GetClimbCollider() {
        return climbCollider;
    }

    public Transform GetClimbPoint() {
        return climbPoint;
    }

    public Vector3 GetWallNormal() {
        return transform.TransformDirection(wallNormal).normalized;
    }

    private void OnDrawGizmos() {
        Vector3 normal = transform.TransformDirection(wallNormal).normalized;

        Gizmos.DrawRay(transform.position, normal * 2f);
    }

    public void Interact() {
    }
}