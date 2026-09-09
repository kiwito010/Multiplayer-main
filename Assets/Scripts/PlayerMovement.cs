using Fusion;
using UnityEngine;

// NetworkBehaviour es como MonoBehaviour, pero con superpoderes de red:
// nos da acceso a FixedUpdateNetwork(), a la propiedad "Object" (el
// NetworkObject del que hablamos en la Fase 3), y al atributo [Networked].
public class PlayerMovement : NetworkBehaviour
{
    [Header("Configuración de movimiento")]
    public float moveSpeed = 5f;      // Velocidad de caminata (unidades por segundo)
    public float lookSensitivity = 2f; // Qué tan rápido gira la cámara con el mouse

    [Header("Referencias (arrastrar desde el prefab)")]
    public Transform cameraPivot; // El objeto PlayerCamera que creamos en la Fase 3

    // Referencia al Character Controller que le agregamos al jugador en la Fase 3.
    private CharacterController _characterController;

    // [Networked] es la palabra clave más importante de Fusion: le dice al
    // motor "esta variable tiene que estar sincronizada entre todas las
    // máquinas". Fusion se encarga de mandarla por la red automáticamente.
    // La usamos para guardar el ángulo vertical de la cámara (mirar arriba/
    // abajo), así los demás jugadores también pueden verla si la necesitan
    // (por ejemplo, más adelante, para animaciones de apuntado).
    [Networked] private float NetworkedPitch { get; set; }

    // Guardamos la velocidad vertical acumulada (qué tan rápido estamos
    // cayendo), en vez de aplicar un empujón fijo cada frame. Esto es más
    // robusto que depender solo de "isGrounded" (que a veces falla un
    // frame y deja que el jugador se hunda un poco en el piso).
    private float _verticalVelocity;

    // Spawned() es una función de Fusion que se llama UNA sola vez, apenas
    // este objeto aparece en la red (equivalente al Start() de Unity, pero
    // garantizado a ejecutarse después de que la red terminó de configurar
    // el objeto).
    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();

        // Línea de prueba temporal, para diagnosticar — la borramos después.
        Debug.Log($"[PlayerMovement] Spawned() ejecutado en '{gameObject.name}'. HasInputAuthority = {Object.HasInputAuthority}");

        // Object.HasInputAuthority es true SOLO en la máquina del jugador
        // dueño de este personaje (es decir, "¿este soy YO?").
        // Bloqueamos y ocultamos el cursor del mouse solo si el personaje
        // que acaba de aparecer es el mío, para no tocarle el mouse a los
        // demás jugadores.
        if (Object.HasInputAuthority)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // FixedUpdateNetwork() es el corazón de la simulación en Fusion.
    // A diferencia de Update() (que corre distinto en cada PC según los FPS),
    // esta función corre a un ritmo FIJO y sincronizado (los "ticks" de red),
    // así todas las máquinas calculan exactamente lo mismo con el mismo input.
    public override void FixedUpdateNetwork()
    {
        // GetInput<T> intenta recuperar el paquete de input que armamos en
        // NetworkManagerFusion.OnInput(). Puede fallar (devolver false) si,
        // por ejemplo, este objeto pertenece a OTRO jugador (nosotros no
        // tenemos su input, solo el nuestro) — por eso el "if".
        if (GetInput(out NetworkInputData input))
        {
            // --- ROTACIÓN HORIZONTAL (girar el cuerpo) ---
            // Tomamos la rotación actual y le sumamos lo que giró el mouse
            // en X, multiplicado por la sensibilidad.
            float yaw = input.lookYaw * lookSensitivity;
            transform.Rotate(0, yaw, 0);
            // Rotamos el TRANSFORM del jugador (no solo la cámara) para que
            // los demás jugadores vean hacia dónde estás mirando/caminando.
            // Esto se sincroniza automáticamente porque el NetworkTransform
            // (Fase 3, Paso 6) vigila la rotación de este objeto.

            // --- ROTACIÓN VERTICAL (mirar arriba/abajo, solo la cámara) ---
            NetworkedPitch -= input.lookPitch * lookSensitivity;
            NetworkedPitch = Mathf.Clamp(NetworkedPitch, -80f, 80f); // Evita que gires la cámara 360°
            if (cameraPivot != null)
            {
                cameraPivot.localRotation = Quaternion.Euler(NetworkedPitch, 0, 0);
            }

            // --- MOVIMIENTO HORIZONTAL ---
            // Convertimos el input (Horizontal/Vertical) en una dirección
            // relativa a hacia dónde está mirando el jugador ahora mismo.
            Vector3 moveDirection = transform.right * input.moveDirection.x
                                   + transform.forward * input.moveDirection.y;

            // --- GRAVEDAD ---
            if (_characterController.isGrounded)
            {
                // Si estamos tocando el piso, mantenemos una caída chica
                // constante (no cero). Esto "pega" al personaje contra el
                // piso entre frame y frame, evitando que pierda el contacto
                // por un instante y empiece a acelerar de nuevo desde cero
                // (que es lo que suele causar que se hunda o atraviese pisos).
                _verticalVelocity = -2f;
            }
            else
            {
                // Si NO estamos tocando el piso (saltando o cayendo),
                // la velocidad de caída se acelera con el tiempo, como
                // la gravedad real.
                _verticalVelocity += Physics.gravity.y * Runner.DeltaTime;
            }

            // Combinamos movimiento horizontal + caída vertical en un solo
            // Move(), en vez de llamar a Move() dos veces por separado
            // (llamarlo una sola vez es más preciso para la detección de
            // colisiones del Character Controller).
            Vector3 finalMove = (moveDirection * moveSpeed) + (Vector3.up * _verticalVelocity);
            _characterController.Move(finalMove * Runner.DeltaTime);
        }
    }
}