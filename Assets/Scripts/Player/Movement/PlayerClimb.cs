
using Fusion;
using UnityEngine;

public class PlayerClimb : NetworkBehaviour {

    private PlayerMovement playerMovement;

    [Header("Climb")]
    [SerializeField] private float climbSpeed = 3f;
    [SerializeField] private float topExitOffset = 0.2f;

    [Networked] public NetworkBool IsClimbing { get; private set; }
    [Networked] private NetworkObject currentWallObject { get; set; }

    public float ClimbSpeed => climbSpeed;

    public override void Spawned() {
        playerMovement = GetComponent<PlayerMovement>();

        PlayerNetworkInteraction.OnUsedOnTarget += HandleClimbInteraction;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= HandleClimbInteraction;
    }

    private void HandleClimbInteraction(
    PlayerRef player,
    NetworkObject target) {
        Debug.Log("[Climb DEBUG] Evento recibido");

        if (!Object.HasStateAuthority)
            return;

        if (player != Object.InputAuthority)
            return;

        Debug.Log("[Climb DEBUG] Jugador validado");

        if (target == null || !target.IsValid)
            return;

        Debug.Log("[Climb DEBUG] Objeto detectado: " + target.name);

        ClimbInteractable climbInteractable =
            target.GetComponentInChildren<ClimbInteractable>(true);

        if (climbInteractable == null) {
            Debug.LogWarning("[Climb DEBUG] No encontré ClimbInteractable");
            return;
        }

        Debug.Log("[Climb DEBUG] ClimbInteractable encontrado");

        ClimbableWall wall = climbInteractable.GetClimbableWall();

        if (wall == null) {
            Debug.LogWarning("[Climb DEBUG] ClimbableWall es null");
            return;
        }

        if (wall.GetClimbPoint() == null) {
            Debug.LogWarning("[Climb DEBUG] ClimbPoint es null");
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            wall.GetClimbPoint().position
        );

        Debug.Log("[Climb DEBUG] Distancia: " + distance);

        if (distance > 3.5f)
            return;

        Debug.Log("[Climb] Jugador " + player + " quiere escalar " + wall.name);

        if (IsClimbing)
            return;

        if (playerMovement == null || !playerMovement.CanStartClimb())
            return;

        currentWallObject = target;
        IsClimbing = true;

        Vector3 startPosition = GetClimbStartPosition(wall);

        playerMovement.SnapToClimbPoint(startPosition);

        Debug.Log("[Climb] Escalada iniciada: " + wall.name);
    }

    public ClimbableWall GetCurrentWall() {
        if (currentWallObject == null)
            return null;

        return currentWallObject.GetComponentInChildren<ClimbableWall>(true);
    }

    private Vector3 GetClimbStartPosition(ClimbableWall wall) {
        Collider wallCollider = wall.GetClimbCollider();

        Vector3 wallNormal = wall.GetWallNormal();

        // Elegimos el lado de la pared donde está el jugador.
        Vector3 towardPlayer = transform.position - wallCollider.bounds.center;

        if (Vector3.Dot(towardPlayer, wallNormal) < 0f) {
            wallNormal = -wallNormal;
        }

        // Punto de la pared más cercano al jugador.
        Vector3 closestPoint =
            wallCollider.ClosestPoint(transform.position);

        CharacterController controller =
            GetComponent<CharacterController>();

        float distance = controller.radius + 0.08f;

        Vector3 targetPosition =
            closestPoint + wallNormal * distance;

        // Conservamos la altura del jugador.
        targetPosition.y = transform.position.y;

        return targetPosition;
    }

    public void StopClimbing() {
        if (!Object.HasStateAuthority)
            return;

        IsClimbing = false;
        currentWallObject = null;
    }
}
