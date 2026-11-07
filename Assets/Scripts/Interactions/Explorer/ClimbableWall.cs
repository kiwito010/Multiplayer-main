using UnityEngine;

public class ClimbableWall : MonoBehaviour, IInteractable {
    [Header("Superficie escalable")]
    [SerializeField] private MeshCollider climbCollider;

    [Header("Orientación")]
    [SerializeField] private Vector3 wallNormal = Vector3.forward;

    public MeshCollider GetClimbCollider() {
        return climbCollider;
    }

    public Vector3 GetWallNormal() {
        return transform.TransformDirection(wallNormal).normalized;
    }

    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;

        Gizmos.DrawRay(
            transform.position,
            GetWallNormal() * 2f
        );
    }

    public void Interact() {
    }
}
