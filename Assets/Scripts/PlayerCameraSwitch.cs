using Fusion;
using UnityEngine;

// Hace dos cosas:
//  1) Deja prendida SOLO la camara (y el audio) del jugador local.
//  2) Mirar con el mouse (valores de PlayerLook), pasando por la red:
//     el giro del cuerpo (yaw) y la inclinacion de la camara (pitch) se
//     calculan desde el input de red, igual que el movimiento.
public class PlayerCameraSwitch : NetworkBehaviour
{
    [Header("Referencias (si faltan o estan mal, se corrigen solas)")]
    public Camera playerCamera;
    public AudioListener audioListener;

    [Header("Mirar (valores de PlayerLook)")]
    [SerializeField] private Transform cameraTransform; // Opcional: si esta vacio usa la transform de playerCamera
    [SerializeField] private float lookSensitivity = 1f;

    // Inclinacion vertical de la camara, sincronizada para que tambien se vea
    // en los demas jugadores.
    [Networked] private float CameraPitch { get; set; }

    public override void Spawned()
    {
        // Si la referencia esta vacia, o apunta a algo que NO es hijo de ESTE
        // jugador (por ejemplo, la camara del prefab en la carpeta Project,
        // que es UNA sola compartida por todos los jugadores), buscamos la
        // camara propia de este jugador entre sus hijos.
        if (playerCamera == null || !playerCamera.transform.IsChildOf(transform))
        {
            Debug.LogWarning("[PlayerCameraSwitch] 'Player Camera' no apuntaba a la camara de este jugador " +
                             "(seguramente se arrastro desde Project). Se corrigio sola, pero conviene arreglarla en el prefab.");
            playerCamera = GetComponentInChildren<Camera>(true);
        }

        if (audioListener == null || !audioListener.transform.IsChildOf(transform))
        {
            audioListener = GetComponentInChildren<AudioListener>(true);
        }

        // Lo mismo para el objeto que recibe el pitch.
        if (cameraTransform == null || !cameraTransform.IsChildOf(transform))
        {
            cameraTransform = playerCamera != null ? playerCamera.transform : null;
        }

        // HasInputAuthority es true solo en la maquina del dueno de este personaje.
        bool isLocalPlayer = Object.HasInputAuthority;

        if (playerCamera != null)
        {
            playerCamera.enabled = isLocalPlayer;
        }

        if (audioListener != null)
        {
            audioListener.enabled = isLocalPlayer;
        }

        // Sensibilidad del menu de opciones (si el jugador la toco); si no, la de este script.
        lookSensitivity = PlayerPrefs.GetFloat("OptionSensitivity", lookSensitivity);

        if (isLocalPlayer)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    // Corre por tick de red: gira el cuerpo y calcula la inclinacion de la camara.
    public override void FixedUpdateNetwork()
    {
        if (!GetInput(out NetworkInputData input)) return;

        // Giro horizontal del cuerpo (yaw).
        transform.Rotate(Vector3.up, input.lookYaw * lookSensitivity);

        // Giro vertical de la camara (pitch), limitado a +-90 como en PlayerLook.
        CameraPitch = Mathf.Clamp(CameraPitch - input.lookPitch * lookSensitivity, -90f, 90f);
    }

    // Corre cada frame: aplica la inclinacion a la camara de todos los jugadores.
    public override void Render()
    {
        if (cameraTransform != null)
        {
            cameraTransform.localRotation = Quaternion.Euler(CameraPitch, 0f, 0f);
        }
    }
}