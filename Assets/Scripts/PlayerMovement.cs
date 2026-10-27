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
    // Acceso rápido al jugador LOCAL (el tuyo). Sirve para que la UI de
    // stamina encuentre al jugador, que aparece en runtime y no se puede
    // arrastrar en el Inspector. Es null hasta que tu jugador aparece.
    public static PlayerMovement Local { get; private set; }

    [Header("Configuración de movimiento")]
    public float moveSpeed = 5f;                       // CONFIRMAR: velocidad de caminata de PlayerMovementSingle
    [SerializeField] private float sprintSpeed = 8.5f; // CONFIRMAR: velocidad de sprint de PlayerMovementSingle
    public float lookSensitivity = 2f;                 // Qué tan rápido gira la cámara con el mouse

    [Header("Salto")]
    [SerializeField] private float jumpForce = 5f;          // CONFIRMAR: fuerza de salto
    [SerializeField] private float jumpStaminaCost = 1f;    // CONFIRMAR: stamina que cuesta cada salto

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 5f;              // CONFIRMAR: stamina máxima de PlayerMovementSingle
    [SerializeField] private float staminaRechargeSpeed = 1f;    // CONFIRMAR: velocidad de recarga de PlayerMovementSingle

    [Header("Referencias (arrastrar desde el prefab)")]
    public Transform cameraPivot; // El objeto PlayerCamera

    [Header("UI")]
    // Arrastrá acá tu prefab de la barra de stamina (el que tiene StaminaUI).
    // Se crea SOLO para el jugador local, así cada uno ve únicamente su barra.
    [SerializeField] private GameObject staminaUIPrefab;

    // Lo que se creó para la barra, para poder borrarlo cuando el jugador se va.
    private GameObject _hudRoot;

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

    // Spawned() se llama UNA sola vez, apenas este objeto aparece en la red.
    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();

        // Si cameraPivot esta vacio o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project), usamos la camara propia.
        if (cameraPivot == null || !cameraPivot.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) cameraPivot = ownCamera.transform;
        }

        // El CharacterController guarda internamente su propia posición; apagarlo
        // y prenderlo lo obliga a releer la posición REAL actual del transform
        // (la del SpawnPoint).
        _characterController.enabled = false;
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
        if (Object.HasInputAuthority)
        {
            Local = this;
            SpawnStaminaUI();
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Se llama cuando el jugador desaparece de la red.
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this)
        {
            Local = null;
        }

        if (_hudRoot != null)
        {
            Destroy(_hudRoot);
        }
    }

    // FixedUpdateNetwork() es el corazón de la simulación en Fusion: corre a
    // un ritmo FIJO (los "ticks" de red), así todas las máquinas calculan lo
    // mismo con el mismo input.
    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            // --- ROTACIÓN HORIZONTAL (girar el cuerpo) ---
            float yaw = input.lookYaw * lookSensitivity;
            transform.Rotate(0, yaw, 0);

            // --- ROTACIÓN VERTICAL (mirar arriba/abajo, solo la cámara) ---
            NetworkedPitch -= input.lookPitch * lookSensitivity;
            NetworkedPitch = Mathf.Clamp(NetworkedPitch, -80f, 80f);

            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(NetworkedPitch, 0, 0);
            }

            // --- MOVIMIENTO HORIZONTAL ---
            Vector3 moveDirection = transform.right * input.moveDirection.x
                                   + transform.forward * input.moveDirection.y;

            // --- SPRINT Y STAMINA (de PlayerMovementSingle) ---
            HandleSprint(input, Runner.DeltaTime);

            // Igual que en el script viejo: si está en sprint usa sprintSpeed.
            float currentSpeed = IsSprinting ? sprintSpeed : moveSpeed;

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

            if (jumpDown && IsGrounded && !EmptyStamina)
            {
                CurrentStamina = Mathf.Max(0f, CurrentStamina - jumpStaminaCost);
                VerticalVelocity = jumpForce;
            }

            // --- LA CLAVE PARA QUE NO SE DESFASE EN EL CLIENTE ---
            // Apagar y prender el CharacterController lo obliga a releer la
            // posición del transform después de una corrección de Fusion.
            _characterController.enabled = false;
            _characterController.enabled = true;
            Physics.SyncTransforms();

            // Movimiento horizontal + caída vertical en un solo Move().
            Vector3 finalMove = (moveDirection * currentSpeed) + (Vector3.up * VerticalVelocity);
            _characterController.Move(finalMove * Runner.DeltaTime);

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
        if (input.sprintPressed && isMovingForward && CurrentStamina > 0f)
        {
            IsSprinting = true;

            if (IsGrounded)
            {
                CurrentStamina -= dt;
            }
            if (CurrentStamina <= 0f)
            {
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

    // Crea la barra de stamina para el jugador local.
    private void SpawnStaminaUI()
    {
        if (staminaUIPrefab == null) return;

        GameObject instance = Instantiate(staminaUIPrefab);

        // Si el prefab trae su propio Canvas, lo forzamos a "Screen Space -
        // Overlay": se dibuja siempre sobre la pantalla del jugador, sin
        // depender de ninguna cámara. Si estuviera en World Space o en
        // Screen Space - Camera (apuntando a otra cámara), la barra quedaría
        // en el mundo o en una cámara que no es la tuya.
        Canvas ownCanvas = instance.GetComponentInChildren<Canvas>(true);
        if (ownCanvas != null)
        {
            ownCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _hudRoot = instance;
            return;
        }

        // Si es solo la barra (sin Canvas), le creamos un Canvas propio.
        // No usamos uno de la escena a propósito: podría ser el de una
        // computadora u otra UI en el mundo, y la barra quedaría escondida.
        var canvasObject = new GameObject("StaminaHUD Canvas",
            typeof(Canvas), typeof(UnityEngine.UI.CanvasScaler), typeof(UnityEngine.UI.GraphicRaycaster));
        canvasObject.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        instance.transform.SetParent(canvasObject.transform, false);
        _hudRoot = canvasObject;
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
}