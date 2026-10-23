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

    // Velocidad vertical acumulada (qué tan rápido estamos cayendo).
    // Es [Networked] a propósito: cuando el cliente corrige su predicción,
    // Fusion "rebobina" y vuelve a simular varios ticks. Las variables
    // [Networked] se rebobinan con él; una variable común no, y quedaría
    // con un valor de un tick que ya no corresponde.
    [Networked] private float VerticalVelocity { get; set; }

    // Si estábamos tocando el piso al terminar el tick anterior. Lo guardamos
    // nosotros porque, como más abajo reiniciamos el CharacterController en
    // cada tick, su propio isGrounded se resetea y no sirve para esto.
    [Networked] private NetworkBool IsGrounded { get; set; }

    // Spawned() es una función de Fusion que se llama UNA sola vez, apenas
    // este objeto aparece en la red (equivalente al Start() de Unity, pero
    // garantizado a ejecutarse después de que la red terminó de configurar
    // el objeto).
    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();

        // Si cameraPivot esta vacio o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project, compartido), usamos la camara propia.
        if (cameraPivot == null || !cameraPivot.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) cameraPivot = ownCamera.transform;
        }

        // El CharacterController guarda internamente su propia posición, y
        // si el objeto fue colocado en otro lugar (como hace Runner.Spawn con
        // el SpawnPoint), puede "volver" a la posición vieja del prefab apenas
        // se llama a Move(). Apagarlo y prenderlo lo obliga a releer la
        // posición REAL actual del transform. Physics.SyncTransforms() hace
        // que la física también se entere del cambio de inmediato.
        _characterController.enabled = false;
        _characterController.enabled = true;
        Physics.SyncTransforms();

        // Leemos la sensibilidad que el jugador configuró en el panel de
        // Opciones (Paso "OptionsMenu.cs"). Si nunca la tocó, usamos el
        // valor por defecto de este mismo script como respaldo.
        lookSensitivity = PlayerPrefs.GetFloat("OptionSensitivity", lookSensitivity);

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
            if (IsGrounded)
            {
                // Si estábamos tocando el piso, mantenemos una caída chica
                // constante (no cero), para que el personaje quede "pegado"
                // al piso entre tick y tick.
                VerticalVelocity = -2f;
            }
            else
            {
                // Si NO estábamos tocando el piso (cayendo), la velocidad de
                // caída se acelera con el tiempo, como la gravedad real.
                VerticalVelocity += Physics.gravity.y * Runner.DeltaTime;
            }

            // --- LA CLAVE PARA QUE NO SE DESFASE EN EL CLIENTE ---
            // Cuando el cliente corrige su predicción, Fusion mueve el
            // transform a la posición real del servidor y re-simula. Pero el
            // CharacterController guarda su propia posición interna y NO se
            // entera de ese cambio: seguiría desde su posición vieja y
            // adelantada, sumando el movimiento dos veces (el jugador "corre
            // de más" en su pantalla y el Host lo ve en otro lugar).
            // Apagarlo y prenderlo lo obliga a releer la posición del
            // transform, y SyncTransforms avisa a la física del cambio.
            _characterController.enabled = false;
            _characterController.enabled = true;
            Physics.SyncTransforms();

            // Combinamos movimiento horizontal + caída vertical en un solo Move().
            Vector3 finalMove = (moveDirection * moveSpeed) + (Vector3.up * VerticalVelocity);
            _characterController.Move(finalMove * Runner.DeltaTime);

            // Guardamos si quedamos en el piso para el próximo tick. Hay que
            // leerlo ACÁ, justo después del Move(), que es cuando es válido.
            IsGrounded = _characterController.isGrounded;
        }
    }
}