using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerMovementSingle : MonoBehaviour {
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpForce = 5f;
 
    [SerializeField] private float jumpStaminaCost = 1f;

    [SerializeField] private float sprintSpeed = 8.5f;

    [SerializeField] private float maxStamina = 5;
    [SerializeField] private float staminaRechargeSpeed = 1;

    [SerializeField] private float currentStamina;
    private bool isSprinting;
    private bool emptyStamina;

    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;

    [SerializeField] private CapsuleCollider capsuleCollider;
    [SerializeField] private float crouchingHeight = 1f;

    private float standingHeight;
    private Vector3 standingCenter;
    private Vector3 crouchingCenter;

    [SerializeField] private Transform playerCamera;
    [SerializeField] private float crouchCameraOffset = 0.5f;

    [SerializeField] private float crouchSpeed = 8f;

    private bool isCrouching;

    [SerializeField] BoxCollider proneCollider;

    [SerializeField] private float proneHeight = 0.5f;
    [SerializeField] private float proneCameraOffset = 0.95f;

    private Vector3 proneCenter;
    private bool isProne;

    [SerializeField] private LayerMask obstacleLayer;

    private Vector3 standingCameraPosition;

    private GameInput gameInput;
    private Rigidbody rb;

    private bool jumpRequested;


    private void Awake() {
        rb = GetComponent<Rigidbody>();
        gameInput = GetComponent<GameInput>();

        standingCenter = capsuleCollider.center;
        standingHeight = capsuleCollider.height;

        crouchingCenter = standingCenter;
        crouchingCenter.y = standingCenter.y - (standingHeight - crouchingHeight) / 2f;

        proneCenter = standingCenter;
        proneCenter.y = standingCenter.y - (standingHeight - proneHeight) / 2f;

        standingCameraPosition = playerCamera.localPosition;

        currentStamina = maxStamina;
    }

    private void Update() {
        if (gameInput.GetJumpPressed() && IsGrounded()) {
            jumpRequested = true;

                currentStamina -= jumpStaminaCost;

                if (currentStamina < 0f) { 
                    currentStamina = 0f;
                }
        }

        HandleStance();
        HandleSprint();
    }

    private void FixedUpdate() {
        HandleMovement();

        if (jumpRequested) {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
            jumpRequested = false;
        }
    }

    private bool IsGrounded() {
        return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
    }

    private void HandleStance() {
        Vector3 targetCameraPosition;

        if (gameInput.GetCrouchPressed()) {
            if (isCrouching && CanStandUp()) {
                isCrouching = false;
            } else if (!isCrouching) {
                isCrouching = true;
            }
        }
        if (isProne) {
            capsuleCollider.height = proneHeight;
            capsuleCollider.center = proneCenter;

            targetCameraPosition = standingCameraPosition + Vector3.down * proneCameraOffset;
        } else if (isCrouching) {
            capsuleCollider.height = crouchingHeight;
            capsuleCollider.center = crouchingCenter;

            targetCameraPosition = standingCameraPosition + Vector3.down * crouchCameraOffset;
        } else {
            capsuleCollider.height = standingHeight;
            capsuleCollider.center = standingCenter;

            targetCameraPosition = standingCameraPosition;
        }

        playerCamera.localPosition = Vector3.Lerp(playerCamera.localPosition, targetCameraPosition, crouchSpeed * Time.deltaTime);

    }

    private bool CanStandUp() {

        float radius = capsuleCollider.radius;

        Vector3 bottom = capsuleCollider.transform.position + Vector3.up * radius;
        Vector3 top = capsuleCollider.transform.position + Vector3.up * (standingHeight - radius);

        return !Physics.CheckCapsule(bottom, top, radius, obstacleLayer);
    }

    public void EnterProne() {
        isProne = true;

        capsuleCollider.enabled = false;
        proneCollider.enabled = true;
    }

    public void ExitProne() {
        isProne = false;

        proneCollider.enabled = false;
        capsuleCollider.enabled = true;
    }

    public bool IsProne() {
        return isProne;
    }

    private void HandleMovement() {
        Vector2 inputVector = gameInput.GetMovementVector();

        Vector3 moveDirection =
        transform.right * inputVector.x +
        transform.forward * inputVector.y;

        float currentSpeed = moveSpeed;

        if (isSprinting) {
            currentSpeed = sprintSpeed;
        }
        Vector3 velocity = rb.velocity;

        velocity.x = moveDirection.x * currentSpeed;
        velocity.z = moveDirection.z * currentSpeed;

        rb.velocity = velocity;
    }

    private void HandleSprint() {
        Vector2 inputVector = gameInput.GetMovementVector();
        bool isMovingForward = inputVector.y > 0;

        if (emptyStamina) {
            isSprinting = false;
            RechargeStamina();
            return;
        }

        if (gameInput.GetSprintPressed() && isMovingForward && currentStamina > 0 && !isCrouching && !isProne) {
            isSprinting = true;

            if (IsGrounded()) {
                currentStamina -= Time.deltaTime;
            }
            if (currentStamina <= 0f) {
                currentStamina = 0f;
                isSprinting = false;
                emptyStamina = true;
            }

        } else {
            isSprinting = false;
            RechargeStamina();
        }
    }

    private void RechargeStamina() {
        currentStamina += staminaRechargeSpeed * Time.deltaTime;

        if (currentStamina >= maxStamina) {
            currentStamina = maxStamina;
            emptyStamina = false;
        }
    }

    public float GetStaminaNormalized() {
        return currentStamina / maxStamina;
    }

    public bool ShouldShowStaminaUI() { 
        return isSprinting || currentStamina < maxStamina;
    }
}
