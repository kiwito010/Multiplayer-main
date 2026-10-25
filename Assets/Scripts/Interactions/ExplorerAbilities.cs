using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplorerAbilities : MonoBehaviour {
    private PlayerInteraction playerInteraction;
    private GameInput gameInput;
    private PlayerMovementSingle playerMovementSingle;
    private Rigidbody rb;

    [SerializeField] private float climbSpeed = 3f;

    private bool isClimbing;
    private ClimbableWall currentWall;

    [SerializeField] private LayerMask climbableLayerMask;

    [SerializeField] private float topExitOffset = 0.2f;

    private void Awake() {
        playerInteraction = GetComponent<PlayerInteraction>();
        gameInput = GetComponent<GameInput>();
        playerMovementSingle = GetComponent<PlayerMovementSingle>();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable() {
        playerInteraction.OnInteract += PlayerInteraction_OnInteract;
    }

    private void OnDisable() {
        playerInteraction.OnInteract -= PlayerInteraction_OnInteract;
    }

    private void Update() {
        if (isClimbing)
            return;

        HandleProne();
    }

    private void FixedUpdate() {
        if (isClimbing) {
            HandleClimbing();
        }
    }

    private void PlayerInteraction_OnInteract(
        object sender,
        InteractEventArgs e) {
        if (e.interactable is ClimbInteractable climbInteractable) {
            ClimbableWall climbableWall = climbInteractable.GetClimbableWall();

            StartClimbing(climbableWall);
        }
    }

    private void HandleProne() {
        if (!gameInput.GetPronePressed())
            return;

        if (playerMovementSingle.IsProne()) {
            playerMovementSingle.ExitProne();
        } else {
            playerMovementSingle.EnterProne();
        }
    }

    private void StartClimbing(ClimbableWall climbableWall) {
        currentWall = climbableWall;
        isClimbing = true;

        playerMovementSingle.SetClimbing(true);

        rb.velocity = Vector3.zero;

        transform.position = climbableWall.GetClimbPoint().position;
    }

    private void HandleClimbing() {
        Vector2 input = gameInput.GetMovementVector();

        Vector3 wallNormal = currentWall.GetWallNormal();

        Vector3 wallRight =
            Vector3.Cross(
                wallNormal,
                Vector3.up
            ).normalized;

        Collider wallCollider = currentWall.GetClimbCollider();

        float wallTop = wallCollider.bounds.max.y;

        if (transform.position.y >= wallTop - topExitOffset && input.y > 0f) {
            ExitClimbFromTop();
            return;
        }

        float horizontalInput = input.x;

        Bounds bounds = wallCollider.bounds;

        Vector3 nextPosition =
            transform.position +
            wallRight *
            horizontalInput *
            climbSpeed *
            Time.fixedDeltaTime;

        Vector3 closestPoint =
            bounds.ClosestPoint(nextPosition);

        float horizontalDistance =
            Mathf.Abs(
                Vector3.Dot(
                    nextPosition - closestPoint,
                    wallRight
                )
            );

        if (horizontalDistance > 0.01f) {
            horizontalInput = 0f;
        }

        Vector3 climbDirection =
            Vector3.up * input.y +
            wallRight * horizontalInput;

        rb.velocity =
            climbDirection.normalized * climbSpeed;
    }

    private void ExitClimbFromTop() {
        BoxCollider exitArea =
            currentWall.GetTopExitArea();

        Vector3 exitPosition =
            exitArea.ClosestPoint(transform.position);

        exitPosition.y =
            exitArea.bounds.max.y;

        StopClimbing();

        transform.position = exitPosition;
    }

    public void StopClimbing() {
        isClimbing = false;

        playerMovementSingle.SetClimbing(false);

        rb.velocity = Vector3.zero;

        currentWall = null;
    }

    public bool IsClimbing() {
        return isClimbing;
    }
}