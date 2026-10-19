using Fusion;
using UnityEngine;

// Cada jugador spawneado trae su propia cámara dentro del prefab.
// Este script deja prendida SOLO la cámara (y el AudioListener) del jugador
// local, y apaga la de todos los demás.
public class PlayerCameraSwitch : NetworkBehaviour
{
    [Header("Referencias (arrastrar desde el prefab)")]
    public Camera playerCamera;
    public AudioListener audioListener;

    public override void Spawned()
    {
        // Si olvidaste arrastrar las referencias en el prefab, las buscamos
        // solas entre los hijos (incluso si están desactivadas).
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>(true);
        if (audioListener == null)
            audioListener = GetComponentInChildren<AudioListener>(true);

        bool isLocalPlayer = Object.HasInputAuthority;

        Debug.Log($"[PlayerCameraSwitch] '{name}' isLocal={isLocalPlayer} " +
                  $"camera={(playerCamera != null ? playerCamera.name : "NULL")} " +
                  $"listener={(audioListener != null ? audioListener.name : "NULL")}");

        // Apagamos TODAS las cámaras y listeners de este jugador, y recién
        // después prendemos los míos. Así no queda ninguno "suelto" encendido.
        foreach (var cam in GetComponentsInChildren<Camera>(true))
            cam.enabled = false;
        foreach (var listener in GetComponentsInChildren<AudioListener>(true))
            listener.enabled = false;

        if (isLocalPlayer)
        {
            if (playerCamera != null) playerCamera.enabled = true;
            if (audioListener != null) audioListener.enabled = true;
        }
    }
}