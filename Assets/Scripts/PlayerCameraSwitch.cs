using Fusion;
using UnityEngine;

// Deja prendida SOLO la camara (y el audio) del jugador local.
public class PlayerCameraSwitch : NetworkBehaviour
{
    [Header("Referencias (si faltan o estan mal, se corrigen solas)")]
    public Camera playerCamera;
    public AudioListener audioListener;

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

        // HasInputAuthority es true solo en la maquina del dueno de este personaje.
        bool isLocalPlayer = Object.HasInputAuthority;

        playerCamera.enabled = isLocalPlayer;

        if (audioListener != null)
        {
            audioListener.enabled = isLocalPlayer;
        }
    }
}