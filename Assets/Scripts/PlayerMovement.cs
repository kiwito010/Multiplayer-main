using Fusion;
using UnityEngine;

// PlayerMovement multijugador (Fusion). Combina:
//  - PlayerMovementSingle: caminar, sprint, stamina, salto, agacharse, boca abajo
//  - PlayerLook: mirar con el mouse (yaw en el cuerpo, pitch en la cámara)
//  - PlayerMovement (Fusion): simulación por ticks con CharacterController
//
// IMPORTANTE: en el prefab del jugador NO deben estar PlayerMovementSingle,
// PlayerLook ni el Rigidbody. Este script los reemplaza.
public class PlayerMovement : NetworkBehaviour
{
    // ---------------------------------------------------------
    //  ACCESO AL JUGADOR LOCAL (para UI y para recolectar input)
    // ---------------------------------------------------------
    public static PlayerMovement Local { get; private set; }
    public static GameInput LocalGameInput { get; private set; }

    // ---------------------------------------------------------
    //  VALORES (tomados de PlayerMovementSingle y PlayerLook)
    // ---------------------------------------------------------
    [Header("Movimiento")]
    public float moveSpeed = 5f;
    [SerializeField] private float sprintSpeed = 8.5f;
    [SerializeField] private float jumpForce = 5f;
    [SerializeField] private float jumpStaminaCost = 1f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;
    [SerializeField] private float staminaRechargeSpeed = 1f;

    [Header("Mirar")]
    public float lookSensitivity = 1f;

    [Header("Referencias (arrastrar desde el prefab)")]
    public Transform cameraPivot; // La cámara: recibe el pitch y el desplazamiento al agacharse
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask obstacleLayer;

    [Header("Postura")]
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float proneHeight = 0.5f;
    [SerializeField] private float crouchCameraOffset = 0.5f;
    [SerializeField] private float proneCameraOffset = 0.95f;
    [SerializeField] private float crouchSpeed = 8f; // velocidad con que se mueve la cámara al cambiar de postura

    // ---------------------------------------------------------
    //  ESTADO SINCRONIZADO ([Networked])
    //  Todo lo que afecta la simulación va acá para que el host y el
    //  cliente calculen lo mismo y Fusion pueda rebobinar sin errores.
    // ---------------------------------------------------------
    [Networked] private float NetworkedPitch { get; set; }
    [Networked] private float VerticalVelocity { get; set; }
    [Networked] private NetworkBool GroundedAfterMove { get; set; }
    [Networked] private float CurrentStamina { get; set; }
    [Networked] private NetworkBool EmptyStamina { get; set; }
    [Networked] private NetworkBool Sprinting { get; set; }
    [Networked] private NetworkBool Crouching { get; set; }
    [Networked] private NetworkBool Prone { get; set; }
    [Networked] private NetworkButtons ButtonsPrevious { get; set; }

    // ---------------------------------------------------------
    //  ESTADO LOCAL (no necesita sincronizarse)
    // ---------------------------------------------------------
    private CharacterController _cc;
    private float _standingHeight;
    private Vector3 _standingCenter;
    private Vector3 _standingCameraLocalPos;

    private bool IsReady => Object != null && Object.IsValid;

    // ---------------------------------------------------------
    //  SPAWN
    // ---------------------------------------------------------
    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        // Si cameraPivot está vacío o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project), usamos la cámara propia.
        if (cameraPivot == null || !cameraPivot.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) cameraPivot = ownCamera.transform;
        }

        // Medidas de pie, tomadas del CharacterController del prefab.
        _standingHeight = _cc.height;
        _standingCenter = _cc.center;
        if (cameraPivot != null) _standingCameraLocalPos = cameraPivot.localPosition;

        // Obliga al CharacterController a releer la posición real del spawn.
        _cc.enabled = false;
        _cc.enabled = true;
        Physics.SyncTransforms();

        // Sensibilidad configurada en el menú de opciones (si existe).
        lookSensitivity = PlayerPrefs.GetFloat("OptionSensitivity", lookSensitivity);

        // La stamina arranca llena (solo quien tiene autoridad puede escribirla).
        if (Object.HasStateAuthority)
        {
            CurrentStamina = maxStamina;
        }

        if (Object.HasInputAuthority)
        {
            Local = this;

            LocalGameInput = GetComponent<GameInput>();
            if (LocalGameInput == null)
            {
                LocalGameInput = gameObject.AddComponent<GameInput>();
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            // Los jugadores remotos no deben reaccionar a MI teclado.
            var interaction = GetComponent<PlayerInteraction>();
            if (interaction != null) interaction.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this)
        {
            Local = null;
            LocalGameInput = null;
        }
    }

    // ---------------------------------------------------------
    //  SIMULACIÓN (corre por tick de red)
    // ---------------------------------------------------------
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        float dt = Runner.DeltaTime;

        // Botones que se apretaron en ESTE tick (no los que siguen apretados).
        NetworkButtons pressed = input.buttons.GetPressed(ButtonsPrevious);
        ButtonsPrevious = input.buttons;

        // --- MIRAR (de PlayerLook) ---
        transform.Rotate(0f, input.lookYaw * lookSensitivity, 0f);
        NetworkedPitch = Mathf.Clamp(NetworkedPitch - input.lookPitch * lookSensitivity, -90f, 90f);

        bool grounded = CheckGrounded();

        // --- SALTO: pedido y costo de stamina (de PlayerMovementSingle) ---
        bool jumpRequested = false;
        if (pressed.IsSet((int)PlayerButtons.Jump) && grounded && !EmptyStamina)
        {
            jumpRequested = true;
            CurrentStamina = Mathf.Max(0f, CurrentStamina - jumpStaminaCost);
        }

        // --- POSTURA ---
        if (pressed.IsSet((int)PlayerButtons.Crouch))
        {
            if (Crouching && CanStandUp())
            {
                Crouching = false;
            }
            else if (!Crouching)
            {
                Crouching = true;
            }
        }
        ApplyStanceToController();

        // --- SPRINT Y STAMINA ---
        bool sprintHeld = input.buttons.IsSet((int)PlayerButtons.Sprint);
        bool isMovingForward = input.moveDirection.y > 0f;

        if (EmptyStamina)
        {
            Sprinting = false;
            RechargeStamina(dt);
        }
        else if (sprintHeld && isMovingForward && CurrentStamina > 0f && !Crouching && !Prone)
        {
            Sprinting = true;

            if (grounded)
            {
                CurrentStamina -= dt;
            }
            if (CurrentStamina <= 0f)
            {
                CurrentStamina = 0f;
                Sprinting = false;
                EmptyStamina = true;
            }
        }
        else
        {
            Sprinting = false;
            RechargeStamina(dt);
        }

        // --- GRAVEDAD Y SALTO ---
        if (GroundedAfterMove)
        {
            VerticalVelocity = -2f; // pegado al piso
        }
        else
        {
            VerticalVelocity += Physics.gravity.y * dt;
        }

        if (jumpRequested)
        {
            VerticalVelocity = jumpForce;
        }

        // --- MOVIMIENTO ---
        Vector3 moveDirection = transform.right * input.moveDirection.x
                              + transform.forward * input.moveDirection.y;

        float currentSpeed = Sprinting ? sprintSpeed : moveSpeed;

        // El CharacterController guarda su propia posición interna; si Fusion
        // corrigió el transform (predicción del cliente), hay que obligarlo a
        // releerla antes de mover, o el jugador se desfasa.
        _cc.enabled = false;
        _cc.enabled = true;
        Physics.SyncTransforms();

        Vector3 finalMove = (moveDirection * currentSpeed) + (Vector3.up * VerticalVelocity);
        _cc.Move(finalMove * dt);

        GroundedAfterMove = _cc.isGrounded;
    }

    // ---------------------------------------------------------
    //  VISUAL (corre cada frame, en todas las copias del jugador)
    // ---------------------------------------------------------
    public override void Render()
    {
        if (cameraPivot == null) return;

        // Pitch: lo aplicamos acá para que también se vea en los jugadores remotos.
        cameraPivot.localRotation = Quaternion.Euler(NetworkedPitch, 0f, 0f);

        // Altura de la cámara según la postura (suavizada).
        Vector3 target = _standingCameraLocalPos;
        if (Prone)
        {
            target += Vector3.down * proneCameraOffset;
        }
        else if (Crouching)
        {
            target += Vector3.down * crouchCameraOffset;
        }

        cameraPivot.localPosition = Vector3.Lerp(cameraPivot.localPosition, target, crouchSpeed * Time.deltaTime);
    }

    // ---------------------------------------------------------
    //  AUXILIARES (de PlayerMovementSingle)
    // ---------------------------------------------------------
    private bool CheckGrounded()
    {
        if (groundCheck != null)
        {
            return Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundLayer);
        }
        return GroundedAfterMove;
    }

    private void RechargeStamina(float dt)
    {
        CurrentStamina += staminaRechargeSpeed * dt;

        if (CurrentStamina >= maxStamina)
        {
            CurrentStamina = maxStamina;
            EmptyStamina = false;
        }
    }

    private bool CanStandUp()
    {
        float radius = _cc.radius;

        Vector3 bottom = transform.position + Vector3.up * radius;
        Vector3 top = transform.position + Vector3.up * (_standingHeight - radius);

        return !Physics.CheckCapsule(bottom, top, radius, obstacleLayer);
    }

    // El CharacterController no puede ser más bajo que el doble de su radio;
    // si crouchingHeight o proneHeight son menores, Unity los limita.
    private void ApplyStanceToController()
    {
        float targetHeight = Prone ? proneHeight : (Crouching ? crouchingHeight : _standingHeight);
        targetHeight = Mathf.Max(targetHeight, 2f * _cc.radius);

        // Mantiene los pies a la misma altura al cambiar de postura.
        Vector3 center = _standingCenter;
        center.y = _standingCenter.y - (_standingHeight - targetHeight) / 2f;

        _cc.height = targetHeight;
        _cc.center = center;
    }

    // ---------------------------------------------------------
    //  API PÚBLICA (igual que en PlayerMovementSingle)
    // ---------------------------------------------------------
    public void EnterProne() { SetProne(true); }
    public void ExitProne() { SetProne(false); }
    public bool IsProne() { return Prone; }

    // La postura "boca abajo" es estado sincronizado: lo escribe el host.
    // Si lo llama el dueño del jugador en un cliente, se lo pide al host.
    private void SetProne(bool value)
    {
        if (!IsReady) return;

        if (Object.HasStateAuthority)
        {
            Prone = value;
        }
        else if (Object.HasInputAuthority)
        {
            RPC_SetProne(value);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetProne(bool value)
    {
        Prone = value;
    }

    public float GetStaminaNormalized()
    {
        if (!IsReady) return 1f;
        return CurrentStamina / maxStamina;
    }

    public bool ShouldShowStaminaUI()
    {
        if (!IsReady) return false;
        return Sprinting || CurrentStamina < maxStamina;
    }
}