using System.Collections.Generic;
using Fusion;
using UnityEngine;

// Maneja las animaciones del personaje. Va en la RAÍZ del prefab del jugador
// (el que tiene el NetworkObject).
//
// Cómo funciona en red:
//  - Caminar, correr, saltar, caer y aterrizar NO se mandan por red: cada máquina
//    mira cómo se mueve el personaje (de dónde a dónde) y anima en consecuencia.
//  - Agacharse, arrastrarse, escalar y hacer tareas SÍ son datos de red, porque no
//    se pueden deducir del movimiento. Los escribe el Host.
public class PlayerAnimationController : NetworkBehaviour
{
    [Header("Referencias (se busca solo si falta)")]
    [SerializeField] private Animator animator;

    [Header("Suelo")]
    // Capas que cuentan como piso. Poné la capa de tu piso (por ejemplo "Ground").
    [SerializeField] private LayerMask groundLayers = Physics.DefaultRaycastLayers;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private float airborneDelay = 0.12f; // tiempo sin tocar el piso para considerarlo "en el aire"

    [Header("Saltos y caídas")]
    [SerializeField] private float jumpMinUpSpeed = 1.5f;       // velocidad hacia arriba para contar como salto
    [SerializeField] private float hardLandingFallSpeed = 12f;  // velocidad de caída desde la que el aterrizaje es "fuerte"

    [Header("Arrastrarse")]
    [SerializeField] private float crawlMaxSpeed = 2.5f; // a esta velocidad la animación va a su ritmo normal

    [Header("Idle con variantes (idle 2 e idle 3)")]
    [SerializeField] private float idleVariantAfter = 8f; // segundos quieto antes de hacer una variante
    [SerializeField] private int idleVariants = 2;        // cuántas variantes tenés (idle (2) y idle (3) = 2)

    [Header("Teclas (solo las usa el jugador local)")]
    [SerializeField] private KeyCode crouchKey = KeyCode.C;
    [SerializeField] private KeyCode proneKey = KeyCode.Z;

    // ---------------------------------------------------------
    //  ESTADOS DE RED (los escribe el Host)
    // ---------------------------------------------------------
    [Networked] private NetworkBool Crouching { get; set; }
    [Networked] private NetworkBool Prone { get; set; }
    [Networked] private NetworkBool ClimbingOn { get; set; }
    [Networked] private int ClimbDirection { get; set; }   // -1 bajando, 0 quieto, 1 subiendo
    [Networked] private NetworkBool DoingTask { get; set; }

    // Para que otros scripts (por ejemplo PlayerMovement) consulten el estado.
    public bool IsCrouching => IsReady && Crouching;
    public bool IsProne => IsReady && Prone;
    public bool IsClimbing => IsReady && ClimbingOn;
    public bool IsDoingTask => IsReady && DoingTask;

    // ---------------------------------------------------------
    //  NOMBRES DE LOS PARÁMETROS DEL ANIMATOR (tienen que coincidir exacto)
    // ---------------------------------------------------------
    private static readonly string[] ParameterNames =
    {
        "Speed", "Vertical", "CrawlSpeed", "Grounded", "VerticalVelocity",
        "Crouching", "Prone", "Climbing", "ClimbDir", "ClimbSpeed", "DoingTask",
        "IdleVariant", "Jump", "HardLanding", "Explo"
    };

    private static readonly int SpeedId = Animator.StringToHash("Speed");
    private static readonly int VerticalId = Animator.StringToHash("Vertical");
    private static readonly int CrawlSpeedId = Animator.StringToHash("CrawlSpeed");
    private static readonly int GroundedId = Animator.StringToHash("Grounded");
    private static readonly int VerticalVelocityId = Animator.StringToHash("VerticalVelocity");
    private static readonly int CrouchingId = Animator.StringToHash("Crouching");
    private static readonly int ProneId = Animator.StringToHash("Prone");
    private static readonly int ClimbingId = Animator.StringToHash("Climbing");
    private static readonly int ClimbDirId = Animator.StringToHash("ClimbDir");
    private static readonly int ClimbSpeedId = Animator.StringToHash("ClimbSpeed");
    private static readonly int DoingTaskId = Animator.StringToHash("DoingTask");
    private static readonly int IdleVariantId = Animator.StringToHash("IdleVariant");
    private static readonly int JumpId = Animator.StringToHash("Jump");
    private static readonly int HardLandingId = Animator.StringToHash("HardLanding");
    private static readonly int ExploId = Animator.StringToHash("Explo");

    // ---------------------------------------------------------
    //  ESTADO LOCAL
    // ---------------------------------------------------------
    private const float MeasureWindow = 0.05f; // cada cuánto se mide el movimiento
    private const float TeleportSpeed = 40f;   // más rápido que esto es un teletransporte, no movimiento

    private CharacterController _cc;
    private readonly HashSet<int> _animatorParams = new HashSet<int>();

    // Medición del movimiento
    private Vector3 _windowStart;
    private float _windowTime;
    private float _targetSpeed;
    private float _targetForward;
    private float _verticalSpeed;
    private float _speed;
    private float _forward;

    // Suelo y saltos
    private bool _grounded = true;
    private float _ungroundedTime;
    private float _airTime;
    private float _maxFallSpeed;
    private bool _jumpTriggered;

    // Idle
    private float _idleTime;
    private float _variantResetTimer;

    private bool IsReady => Object != null && Object.IsValid;

    // ---------------------------------------------------------
    //  ARRANQUE
    // ---------------------------------------------------------
    public override void Spawned()
    {
        _cc = GetComponent<CharacterController>();

        // Si no está asignado (o apunta a algo que no es de ESTE jugador), lo buscamos entre los hijos.
        if (animator == null || !animator.transform.IsChildOf(transform))
        {
            animator = GetComponentInChildren<Animator>(true);
        }

        if (animator == null)
        {
            Debug.LogError("[PlayerAnimationController] No encontré ningún Animator en este jugador.");
            return;
        }

        // El movimiento lo maneja el CharacterController, no la animación.
        animator.applyRootMotion = false;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            _animatorParams.Add(parameter.nameHash);
        }

        ReportMissingParameters();

        _windowStart = transform.position;
    }

    // Avisa una sola vez qué parámetros le faltan al Animator, así sabés qué crear.
    private void ReportMissingParameters()
    {
        var missing = new List<string>();

        foreach (string name in ParameterNames)
        {
            if (!_animatorParams.Contains(Animator.StringToHash(name))) missing.Add(name);
        }

        if (missing.Count > 0)
        {
            Debug.LogWarning("[PlayerAnimationController] Al Animator le faltan estos parámetros: " + string.Join(", ", missing) +
                             ". Las animaciones que dependen de ellos no van a andar hasta que los crees.");
        }
    }

    // ---------------------------------------------------------
    //  TECLAS DEL JUGADOR LOCAL
    // ---------------------------------------------------------
    private void Update()
    {
        if (!IsReady || !Object.HasInputAuthority) return;

        if (Input.GetKeyDown(crouchKey)) SetCrouching(!Crouching);
        if (Input.GetKeyDown(proneKey)) SetProne(!Prone);
    }

    // ---------------------------------------------------------
    //  ANIMAR (cada frame, en TODAS las máquinas)
    // ---------------------------------------------------------
    private void LateUpdate()
    {
        if (animator == null || !IsReady) return;

        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        MeasureMotion(dt);
        UpdateGround(dt);
        UpdateIdleVariant(dt);
        PushToAnimator(dt);
    }

    // Mide cuánto se movió el personaje. Se hace en ventanas cortas (no frame a frame)
    // para que no tiemble cuando los frames y los ticks de red no coinciden.
    private void MeasureMotion(float dt)
    {
        _windowTime += dt;
        if (_windowTime < MeasureWindow) return;

        Vector3 velocity = (transform.position - _windowStart) / _windowTime;

        _windowStart = transform.position;
        _windowTime = 0f;

        // Un teletransporte (aparecer, reaparecer) no es movimiento real.
        if (velocity.magnitude > TeleportSpeed) velocity = Vector3.zero;

        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);

        _targetSpeed = horizontal.magnitude;
        _targetForward = Vector3.Dot(horizontal, transform.forward); // + adelante, - atrás
        _verticalSpeed = velocity.y;
    }

    private void UpdateGround(float dt)
    {
        bool touchingGround = CheckGround();
        bool wasGrounded = _grounded;

        if (touchingGround) _ungroundedTime = 0f;
        else _ungroundedTime += dt;

        // Pasa a "en el aire" recién después de un instante sin tocar el piso (evita parpadeos en escalones).
        _grounded = _ungroundedTime < airborneDelay;

        // Acaba de despegar.
        if (wasGrounded && !_grounded)
        {
            _airTime = 0f;
            _maxFallSpeed = 0f;
            _jumpTriggered = false;
        }

        if (!_grounded)
        {
            _airTime += dt;
            _maxFallSpeed = Mathf.Max(_maxFallSpeed, -_verticalSpeed);

            // Salto: subiendo con fuerza justo después de despegar.
            if (!_jumpTriggered && _airTime < 0.4f && _verticalSpeed > jumpMinUpSpeed)
            {
                _jumpTriggered = true;
                SetTrigger(JumpId);
            }
        }

        // Acaba de aterrizar: si venía cayendo muy rápido, aterrizaje fuerte.
        if (!wasGrounded && _grounded && _maxFallSpeed >= hardLandingFallSpeed)
        {
            SetTrigger(HardLandingId);
        }
    }

    private bool CheckGround()
    {
        Vector3 feet = transform.position;

        // Los pies se toman del collider, así no importa dónde esté el pivote del modelo.
        if (_cc != null)
        {
            Bounds bounds = _cc.bounds;
            feet = new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        }

        // El rayo arranca un poco adentro del jugador (ahí no choca con su propio collider).
        return Physics.Raycast(feet + Vector3.up * 0.1f, Vector3.down, 0.1f + groundCheckDistance,
            groundLayers, QueryTriggerInteraction.Ignore);
    }

    // Si el personaje está un rato quieto, hace una de las variantes de idle.
    private void UpdateIdleVariant(float dt)
    {
        bool standingStill = _grounded && _speed < 0.1f && !Crouching && !Prone && !ClimbingOn && !DoingTask;

        if (standingStill)
        {
            _idleTime += dt;

            if (_idleTime >= idleVariantAfter)
            {
                _idleTime = 0f;
                SetInt(IdleVariantId, Random.Range(1, idleVariants + 1));
                _variantResetTimer = 0.3f;
            }
        }
        else
        {
            _idleTime = 0f;
        }

        // La variante se "suelta" enseguida: el Animator vuelve solo al idle cuando termina.
        if (_variantResetTimer > 0f)
        {
            _variantResetTimer -= dt;
            if (_variantResetTimer <= 0f) SetInt(IdleVariantId, 0);
        }
    }

    private void PushToAnimator(float dt)
    {
        // Suaviza los cambios bruscos de velocidad.
        float smoothing = 1f - Mathf.Exp(-12f * dt);
        _speed = Mathf.Lerp(_speed, _targetSpeed, smoothing);
        _forward = Mathf.Lerp(_forward, _targetForward, smoothing);

        // Hacia dónde mira el movimiento: 1 = adelante, -1 = atrás, 0 = de costado o quieto.
        float direction = _speed > 0.1f ? Mathf.Clamp(_forward / _speed, -1f, 1f) : 0f;

        bool climbing = ClimbingOn;
        int climbDirection = ClimbDirection;

        SetFloat(SpeedId, _speed);
        SetFloat(VerticalId, direction);
        SetFloat(CrawlSpeedId, Mathf.Clamp01(_speed / Mathf.Max(0.01f, crawlMaxSpeed)));
        SetFloat(VerticalVelocityId, _verticalSpeed);
        SetBool(GroundedId, _grounded);

        SetBool(CrouchingId, Crouching);
        SetBool(ProneId, Prone);
        SetBool(DoingTaskId, DoingTask);

        SetBool(ClimbingId, climbing);
        SetFloat(ClimbDirId, climbing ? climbDirection : 0f);
        SetFloat(ClimbSpeedId, climbing && climbDirection != 0 ? 1f : 0f);
    }

    // ---------------------------------------------------------
    //  PARA USAR DESDE OTROS SCRIPTS
    //  Se pueden llamar desde la máquina del dueño del jugador o desde el Host.
    // ---------------------------------------------------------
    public void SetCrouching(bool value)
    {
        if (!IsReady) return;

        if (Object.HasStateAuthority) ApplyCrouching(value);
        else if (Object.HasInputAuthority) RPC_SetCrouching(value);
    }

    public void SetProne(bool value)
    {
        if (!IsReady) return;

        if (Object.HasStateAuthority) ApplyProne(value);
        else if (Object.HasInputAuthority) RPC_SetProne(value);
    }

    // direction: 1 = subiendo, -1 = bajando, 0 = agarrado a la escalera sin moverse.
    // Llamalo solo cuando CAMBIE algo (no en cada frame).
    public void SetClimbing(bool isClimbing, int direction = 0)
    {
        if (!IsReady) return;

        direction = Mathf.Clamp(direction, -1, 1);

        if (Object.HasStateAuthority) ApplyClimbing(isClimbing, direction);
        else if (Object.HasInputAuthority) RPC_SetClimbing(isClimbing, direction);
    }

    public void SetDoingTask(bool value)
    {
        if (!IsReady) return;

        if (Object.HasStateAuthority) DoingTask = value;
        else if (Object.HasInputAuthority) RPC_SetDoingTask(value);
    }

    // Dispara la animación "Explo" para todos los jugadores (solo desde la máquina del dueño).
    public void PlayExplo()
    {
        if (!IsReady || !Object.HasInputAuthority) return;

        RPC_PlayExplo();
    }

    // ---------------------------------------------------------
    //  CAMBIOS DE ESTADO (los hace el Host)
    // ---------------------------------------------------------
    private void ApplyCrouching(bool value)
    {
        Crouching = value;
        if (value) Prone = false; // agachado y boca abajo a la vez no existe
    }

    private void ApplyProne(bool value)
    {
        Prone = value;
        if (value) Crouching = false;
    }

    private void ApplyClimbing(bool isClimbing, int direction)
    {
        ClimbingOn = isClimbing;
        ClimbDirection = isClimbing ? direction : 0;

        if (isClimbing)
        {
            Crouching = false;
            Prone = false;
        }
    }

    // ---------------------------------------------------------
    //  PEDIDOS DE UN CLIENTE AL HOST
    // ---------------------------------------------------------
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetCrouching(bool value) { ApplyCrouching(value); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetProne(bool value) { ApplyProne(value); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetClimbing(bool isClimbing, int direction) { ApplyClimbing(isClimbing, direction); }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetDoingTask(bool value) { DoingTask = value; }

    // Lo ven todos, incluido el que lo disparó.
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_PlayExplo() { SetTrigger(ExploId); }

    // ---------------------------------------------------------
    //  ESCRIBIR EN EL ANIMATOR (solo si el parámetro existe)
    // ---------------------------------------------------------
    private void SetFloat(int id, float value)
    {
        if (_animatorParams.Contains(id)) animator.SetFloat(id, value);
    }

    private void SetBool(int id, bool value)
    {
        if (_animatorParams.Contains(id)) animator.SetBool(id, value);
    }

    private void SetInt(int id, int value)
    {
        if (_animatorParams.Contains(id)) animator.SetInteger(id, value);
    }

    private void SetTrigger(int id)
    {
        if (animator != null && _animatorParams.Contains(id)) animator.SetTrigger(id);
    }
}