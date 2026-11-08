using Fusion;
using UnityEngine;

// NetworkBehaviour es como MonoBehaviour, pero con superpoderes de red:
// nos da acceso a FixedUpdateNetwork(), a la propiedad "Object" (el
// NetworkObject) y al atributo [Networked].
//
// Este script combina:
//  - El movimiento multijugador que ya funcionaba (CharacterController + Fusion).
//  - El sprint y la stamina de PlayerMovementSingle, adaptados a la red.
public class PlayerMovement : NetworkBehaviour
{
    private PlayerClimb playerClimb;

    // Acceso rápido al jugador LOCAL (el tuyo). Sirve para que la UI de
    // stamina encuentre al jugador, que aparece en runtime y no se puede
    // arrastrar en el Inspector. Es null hasta que tu jugador aparece.
    public static PlayerMovement Local { get; private set; }

    [Header("Configuración de movimiento")]
    public float moveSpeed = 5f;                       // CONFIRMAR: velocidad de caminata de PlayerMovementSingle
    [SerializeField] private float sprintSpeed = 8.5f; // CONFIRMAR: velocidad de sprint de PlayerMovementSingle
    public float lookSensitivity = 1f;                 // Qué tan rápido gira la cámara con el mouse
    [SerializeField] private float mouseInputScale = 0.05f;

    [Header("Salto")]
    [SerializeField] private float jumpForce = 5f;          // CONFIRMAR: fuerza de salto
    [SerializeField] private float jumpStaminaCost = 1f;    // CONFIRMAR: stamina que cuesta cada salto

    [Header("Crouch")]
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    private float standingHeight;
    private float standingRadius;
    private Vector3 standingCenter;
    private Vector3 standingCameraPosition;


    [Networked] private NetworkBool isCrouching { get; set; }
    [Networked] private NetworkBool crouchWasPressed { get; set; }

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;              // CONFIRMAR: stamina máxima de PlayerMovementSingle
    [SerializeField] private float staminaRechargeSpeed = 1f;    // CONFIRMAR: velocidad de recarga de PlayerMovementSingle

    [Header("Referencias (arrastrar desde el prefab)")]
    public Transform cameraPivot; // El objeto PlayerCamera

    [Header("UI")]

    // Referencia al Character Controller del jugador.
    private CharacterController _characterController;

    // [Networked] le dice al motor "esta variable tiene que estar sincronizada
    // entre todas las máquinas". Todo lo que afecta la simulación va acá,
    // porque cuando el cliente corrige su predicción Fusion "rebobina" y
    // vuelve a simular: las variables [Networked] se rebobinan con él, las
    // variables comunes no.
    [Networked] private float NetworkedPitch { get; set; }
    [Networked] private float VerticalVelocity { get; set; }

    // Si estábamos tocando el piso al terminar el tick anterior.
    [Networked] private NetworkBool IsGrounded { get; set; }

    // --- Estado del sprint y la stamina (antes eran variables comunes) ---
    [Networked] private float CurrentStamina { get; set; }
    [Networked] private NetworkBool EmptyStamina { get; set; }
    [Networked] private NetworkBool IsSprinting { get; set; }

    // Si la tecla de salto estaba apretada en el tick anterior. Sirve para
    // saltar UNA vez por pulsación y no seguir saltando mientras la mantenés.
    [Networked] private NetworkBool JumpWasPressed { get; set; }

    [Header("Prone")]
    [SerializeField] private float proneHeight = 0.5f;
    [SerializeField] private float proneSpeed = 1.5f;
    [SerializeField] private float proneRadius = 0.2f;

    [Header("Colisión horizontal de Prone")]
    [SerializeField] private float proneBodyLength = 1.6f;
    [SerializeField] private float proneBodyRadius = 0.22f;
    [SerializeField] private float proneGroundClearance = 0.05f;
    [SerializeField] private LayerMask proneCollisionLayers = ~0;

    [Networked] private NetworkBool isProne { get; set; }
    [Networked] private NetworkBool proneWasPressed { get; set; }

    // Spawned() se llama UNA sola vez, apenas este objeto aparece en la red.
    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();

        playerClimb = GetComponent<PlayerClimb>();

        // Si cameraPivot esta vacio o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project), usamos la camara propia.
        if (cameraPivot == null || !cameraPivot.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) cameraPivot = ownCamera.transform;
        }

        standingHeight = _characterController.height;
        standingRadius = _characterController.radius;
        standingCenter = _characterController.center;

        if (cameraPivot != null) {
            standingCameraPosition = cameraPivot.localPosition;
        }
        // El CharacterController guarda internamente su propia posición; apagarlo
        // y prenderlo lo obliga a releer la posición REAL actual del transform
        // (la del SpawnPoint).
        _characterController.enabled = false;

        ApplyCrouch();

        _characterController.enabled = true;
        Physics.SyncTransforms();

        // Sensibilidad configurada en el panel de Opciones (si existe).
        lookSensitivity = PlayerPrefs.GetFloat("OptionSensitivity", lookSensitivity);

        // La stamina arranca llena. Solo quien tiene autoridad sobre el
        // objeto (el Host) puede escribir el valor inicial.
        if (Object.HasStateAuthority)
        {
            CurrentStamina = maxStamina;
        }

        // HasInputAuthority es true SOLO en la máquina del dueño de este personaje.
        if (Object.HasInputAuthority) {
            Local = this;

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Se llama cuando el jugador desaparece de la red.
    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (Local == this) {
            Local = null;
        }
    }

    // FixedUpdateNetwork() es el corazón de la simulación en Fusion: corre a
    // un ritmo FIJO (los "ticks" de red), así todas las máquinas calculan lo
    // mismo con el mismo input.
    public override void FixedUpdateNetwork(){
        if (GetInput(out NetworkInputData input)){

            bool crouchDown = input.crouchPressed && !crouchWasPressed;
            crouchWasPressed = input.crouchPressed;

            bool proneDown = input.pronePressed && !proneWasPressed;
            proneWasPressed = input.pronePressed;
            
            if (playerClimb == null || !playerClimb.IsClimbing) {
            
                if (proneDown) {
                    if (!isProne) {
                        if (CanFitProneAt(transform.position)) {
                            isProne = true;
                            isCrouching = false;
                        }
                    } else if (CanStandUp()) {
                        isProne = false;
                    }
                }

                if (crouchDown) {
                    if (isProne) {
                        if (CanUseHeight(crouchHeight)) {
                            isProne = false;
                            isCrouching = true;
                        }
                    } else if (isCrouching) {
                        if (CanStandUp())
                            isCrouching = false;
                    } else {
                        isCrouching = true;
                    }
                }
            }

            // --- ROTACIÓN HORIZONTAL (girar el cuerpo) ---
            float yaw = input.lookYaw * lookSensitivity * mouseInputScale;
            transform.Rotate(0f, yaw, 0f);


            // --- ROTACIÓN VERTICAL (mirar arriba/abajo, solo la cámara) ---
            NetworkedPitch -= input.lookPitch * lookSensitivity * mouseInputScale;
            NetworkedPitch = Mathf.Clamp(NetworkedPitch, -80f, 80f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(NetworkedPitch, 0, 0);
            }

            if (playerClimb != null && playerClimb.IsClimbing) {
                HandleClimbing(input);
                return;
            }

            // --- MOVIMIENTO HORIZONTAL ---
            Vector3 moveDirection = transform.right * input.moveDirection.x
                                   + transform.forward * input.moveDirection.y;

            // --- SPRINT Y STAMINA (de PlayerMovementSingle) ---
            HandleSprint(input, Runner.DeltaTime);

            // Igual que en el script viejo: si está en sprint usa sprintSpeed.
            float currentSpeed = isProne
                ? proneSpeed
                : isCrouching
                    ? crouchSpeed
                    : IsSprinting ? sprintSpeed : moveSpeed;
            // --- GRAVEDAD ---
            if (IsGrounded)
            {
                VerticalVelocity = -2f; // caída chica constante: queda "pegado" al piso
            }
            else
            {
                VerticalVelocity += Physics.gravity.y * Runner.DeltaTime;
            }

            // --- SALTO ---
            // "jumpDown" es true solo en el tick en que se aprieta la tecla
            // (no mientras se mantiene). Solo se puede saltar desde el piso
            // y con stamina disponible; cada salto cuesta stamina.
            bool jumpDown = input.jumpPressed && !JumpWasPressed;
            JumpWasPressed = input.jumpPressed;

            if (jumpDown && IsGrounded && !EmptyStamina &&
                !isProne && !isCrouching) {
                CurrentStamina = Mathf.Max(0f, CurrentStamina - jumpStaminaCost);
                VerticalVelocity = jumpForce;
            }

            // --- LA CLAVE PARA QUE NO SE DESFASE EN EL CLIENTE ---
            // Apagar y prender el CharacterController lo obliga a releer la
            // posición del transform después de una corrección de Fusion.
            _characterController.enabled = false;

            ApplyCrouch();

            _characterController.enabled = true;
            Physics.SyncTransforms();

            // Movimiento horizontal + caída vertical en un solo Move().
            Vector3 horizontalMove = moveDirection * currentSpeed * Runner.DeltaTime;

            if (isProne && horizontalMove.sqrMagnitude > 0.000001f) {
                // Comprobar el volumen del cuerpo en el destino.
                // Dividir el avance evita saltarse obstáculos delgados.
                float distance = horizontalMove.magnitude;
                int steps = Mathf.CeilToInt(distance / 0.05f);

                Vector3 step = horizontalMove / steps;
                Vector3 allowedMove = Vector3.zero;

                for (int i = 0; i < steps; i++) {
                    Vector3 candidate = transform.position + allowedMove + step;

                    if (!CanFitProneAt(candidate))
                        break;

                    allowedMove += step;
                }

                horizontalMove = allowedMove;
            }

            Vector3 verticalMove =
                Vector3.up * VerticalVelocity * Runner.DeltaTime;

            _characterController.Move(horizontalMove + verticalMove);

            // Guardamos si quedamos en el piso para el próximo tick.
            IsGrounded = _characterController.isGrounded;
        }
    }

    // ---------------------------------------------------------
    //  SPRINT Y STAMINA (adaptado de PlayerMovementSingle)
    //  Cambios respecto al original:
    //   - Time.deltaTime -> dt (Runner.DeltaTime), porque corre por tick de red.
    //   - gameInput.GetMovementVector() -> input.moveDirection
    //   - gameInput.GetSprintPressed()  -> input.sprintPressed
    //   - IsGrounded() -> IsGrounded (dato de red, sale del CharacterController)
    // ---------------------------------------------------------
    private void HandleSprint(NetworkInputData input, float dt)
    {
        bool isMovingForward = input.moveDirection.y > 0f;

        if (EmptyStamina)
        {
            IsSprinting = false;
            RechargeStamina(dt);
            return;
        }

        // NOTA: el original también pedía "&& !isCrouching && !isProne".
        // Esas variables están en la parte del script que no me pasaste;
        // cuando agreguemos agacharse/boca abajo se suman acá.
        if (input.sprintPressed && !isCrouching && !isProne && isMovingForward && CurrentStamina > 0f) { 
            IsSprinting = true;

            if (IsGrounded) {
                CurrentStamina -= dt;
            }

            if (CurrentStamina <= 0f) {
                CurrentStamina = 0f;
                IsSprinting = false;
                EmptyStamina = true;
            }
        }
        else
        {
            IsSprinting = false;
            RechargeStamina(dt);
        }
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

    // ---------------------------------------------------------
    //  API PÚBLICA (mismos nombres que en PlayerMovementSingle)
    //  Leer una variable [Networked] antes de que el objeto esté listo da
    //  error, por eso el chequeo de Object.IsValid.
    // ---------------------------------------------------------
    public float GetStaminaNormalized()
    {
        if (Object == null || !Object.IsValid) return 1f;
        return CurrentStamina / maxStamina;
    }

    public bool ShouldShowStaminaUI()
    {
        if (Object == null || !Object.IsValid) return false;
        return IsSprinting || CurrentStamina < maxStamina;
    }

    private void ApplyCrouch() {
        if (_characterController == null)
            return;

        float targetHeight = standingHeight;

        if (isProne) {
            targetHeight = proneHeight;
        } else if (isCrouching) {
            targetHeight = crouchHeight;
        }

        float targetRadius = isProne ? proneRadius : standingRadius;

        _characterController.radius = targetRadius;

        targetHeight = Mathf.Max(targetHeight, targetRadius * 2f);

        float heightDifference = standingHeight - targetHeight;

        _characterController.height = targetHeight;

        _characterController.center =
            standingCenter + Vector3.down * (heightDifference / 2f);
    }

    public override void Render() {
        if (Object.HasInputAuthority) {
            UpdateCrouchCamera();
        }

        if (!Object.HasInputAuthority && !Object.HasStateAuthority) {
            ApplyCrouch();
        }
    }

    private void UpdateCrouchCamera() {
        if (cameraPivot == null || _characterController == null)
            return;

        float targetHeight = standingHeight;

        if (isProne) {
            targetHeight = proneHeight;
        } else if (isCrouching) {
            targetHeight = crouchHeight;
        }

        targetHeight = Mathf.Max(
            targetHeight,
            _characterController.radius * 2f
        );

        Vector3 targetPosition = standingCameraPosition;

        if (isProne) {
            targetPosition += Vector3.down * 0.95f;
        } else if (isCrouching) {
            targetPosition += Vector3.down * 0.5f;
        }

        float smoothFactor = 1f -
            Mathf.Exp(-crouchTransitionSpeed * Time.deltaTime);

        cameraPivot.localPosition = Vector3.Lerp(
            cameraPivot.localPosition,
            targetPosition,
            smoothFactor
        );
    }

    private bool CanStandUp() {
        return CanUseHeight(standingHeight);
    }

    private bool CanUseHeight(float desiredHeight) {
        if (_characterController == null)
            return false;

        float radius = Mathf.Max(0.01f, standingRadius - 0.02f);

        float height = Mathf.Max(desiredHeight, standingRadius * 2f);

        float heightDifference = standingHeight - height;

        Vector3 targetCenter =
            standingCenter + Vector3.down * (heightDifference / 2f);

        Vector3 center = transform.TransformPoint(targetCenter);
        Vector3 up = transform.up;

        float halfSegment = Mathf.Max(0f, height / 2f - radius);

        Vector3 bottom = center - up * halfSegment;
        Vector3 top = center + up * halfSegment;

        Collider[] hits = Physics.OverlapCapsule(
            bottom,
            top,
            radius,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits) {
            if (hit.transform.IsChildOf(transform))
                continue;

            if (hit.bounds.max.y <=
                _characterController.bounds.min.y + 0.05f)
                continue;

            return false;
        }

        return true;
    }

    private bool CanFitProneAt(Vector3 rootPosition) {
        float radius = proneBodyRadius;

        float halfSegment =
            Mathf.Max(0f, proneBodyLength * 0.5f - radius);

        Vector3 feetPosition = rootPosition +
            transform.rotation * (
                standingCenter - Vector3.up * (standingHeight * 0.5f)
            );

        Vector3 center = feetPosition +
            Vector3.up * (radius + proneGroundClearance);

        Vector3 forward = transform.forward;

        Vector3 front = center + forward * halfSegment;
        Vector3 back = center - forward * halfSegment;

        Collider[] hits = Physics.OverlapCapsule(
            front,
            back,
            radius,
            proneCollisionLayers,
            QueryTriggerInteraction.Ignore
        );

        foreach (Collider hit in hits) {
            if (hit.transform.IsChildOf(transform))
                continue;

            // Ignorar el suelo debajo del cuerpo.
            if (hit.bounds.max.y <=
                center.y - radius + 0.02f)
                continue;

            return false;
        }

        return true;
    }

    public bool CanStartClimb() {
        return !isProne && !isCrouching;
    }

    public void SnapToClimbPoint(Vector3 position) {
        if (!Object.HasStateAuthority)
            return;

        _characterController.enabled = false;
        transform.position = position;
        VerticalVelocity = 0f;
        IsGrounded = false;
        _characterController.enabled = true;

        Physics.SyncTransforms();
    }



    private void HandleClimbing(NetworkInputData input) {
        ClimbableWall wall = playerClimb.GetCurrentWall();

        if (wall == null) {
            playerClimb.StopClimbing();
            return;
        }

        // Space: soltar la pared.
        bool jumpDown = input.jumpPressed && !JumpWasPressed;
        JumpWasPressed = input.jumpPressed;

        if (jumpDown) {
            playerClimb.StopClimbing();
            VerticalVelocity = 0f;
            return;
        }

        Collider wallCollider = wall.GetClosestClimbCollider(transform.position);

        if (wallCollider == null) {
            playerClimb.StopClimbing();
            return;
        }

        Vector3 wallNormal = wall.GetWallNormal();

        // Determinar de qué lado de la pared estamos.
        if (Vector3.Dot(
            transform.position - wallCollider.bounds.center,
            wallNormal) < 0f) {
            wallNormal = -wallNormal;
        }

        Vector3 wallRight =
            Vector3.Cross(wallNormal, Vector3.up).normalized;

        Vector2 movement = input.moveDirection;

        Vector3 climbDirection = Vector3.up * movement.y + wallRight * movement.x;

        climbDirection = Vector3.ClampMagnitude(climbDirection, 1f);

        Vector3 desiredMove =
            climbDirection *
            playerClimb.ClimbSpeed *
            Runner.DeltaTime;

        Vector3 currentPosition = transform.position;

        // Movimiento lateral.
        Vector3 lateralMove = wallRight * Vector3.Dot(desiredMove, wallRight);

        Vector3 candidate = currentPosition + lateralMove;

        if (lateralMove.sqrMagnitude > 0.000001f &&
            TryFindClimbSurface(
                wall,
                candidate,
                wallNormal,
                out _)) {
            currentPosition = candidate;
        }

        // Movimiento vertical.
        Vector3 verticalMove = Vector3.up * desiredMove.y;

        candidate = currentPosition + verticalMove;

        if (verticalMove.sqrMagnitude > 0.000001f &&
            TryFindClimbSurface(
                wall,
                candidate,
                wallNormal,
                out _)) {
            currentPosition = candidate;
        }

        // Mantener al jugador cerca de la superficie.
        if (TryFindClimbSurface(
            wall,
            currentPosition,
            wallNormal,
            out RaycastHit hit)) {
            float desiredDistance =
                _characterController.radius + 0.08f;

            float currentDistance = Vector3.Dot( currentPosition - hit.point, wallNormal);

            float correction = Mathf.Clamp(
                desiredDistance - currentDistance,
                -0.1f,
                0.1f
            );

            currentPosition += wallNormal * correction;
        }

        VerticalVelocity = 0f;
        IsGrounded = false;
        IsSprinting = false;

        Vector3 displacement = currentPosition - transform.position;

        _characterController.Move(displacement);
    }

    private bool TryFindClimbSurface(
        ClimbableWall wall,
        Vector3 candidatePosition,
        Vector3 wallNormal,
        out RaycastHit surfaceHit) {

        const float probeDistance = 2f;

        Vector3 rayOrigin = candidatePosition + wallNormal * probeDistance;

        Ray ray = new Ray(rayOrigin, -wallNormal);

        surfaceHit = default;
        bool found = false;
        float bestDistance = float.MaxValue;

        BoxCollider[] zones = wall.GetClimbZones();

        if (zones != null) {
            foreach (BoxCollider zone in zones) {
                if (zone == null || !zone.enabled ||
                    !zone.gameObject.activeInHierarchy)
                    continue;

                if (!zone.Raycast(
                    ray, out RaycastHit hit, probeDistance * 2f))
                    continue;

                if (Vector3.Dot(hit.normal, wallNormal) <= 0.25f)
                    continue;

                float distance = (candidatePosition - hit.point).sqrMagnitude;

                if (distance < bestDistance) {
                    bestDistance = distance;
                    surfaceHit = hit;
                    found = true;
                }
            }
        }

        // Compatibilidad con la pared original.
        if (!found && (zones == null || zones.Length == 0)) {
            Collider originalCollider = wall.GetClimbCollider();

            if (originalCollider != null &&
                originalCollider.Raycast(ray, out RaycastHit hit, probeDistance * 2f) && Vector3.Dot(hit.normal, wallNormal) > 0.25f) {

                surfaceHit = hit;
                found = true;
            }
        }

        return found;
    }


}