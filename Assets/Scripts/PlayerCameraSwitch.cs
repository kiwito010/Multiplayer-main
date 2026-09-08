using Fusion;
using UnityEngine;

// Este script resuelve el problema que mencioné en la Fase 3:
// cada jugador spawneado trae su propia cámara adentro del prefab,
// así que sin este script, cuando hay 2+ jugadores conectados,
// vas a tener 2+ cámaras activas al mismo tiempo compitiendo por
// la pantalla. Acá "apagamos" todas menos la nuestra.
public class PlayerCameraSwitch : NetworkBehaviour
{
    [Header("Referencias (arrastrar desde el prefab)")]
    public Camera playerCamera;       // El componente Camera dentro de PlayerCamera
    public AudioListener audioListener; // Cada Camera trae uno; solo puede haber UNO activo en toda la escena

    public override void Spawned()
    {
        // Como explicamos antes: HasInputAuthority es true únicamente en
        // la máquina del dueño de este personaje.
        bool isLocalPlayer = Object.HasInputAuthority;

        // Si ES mi personaje: dejo la cámara y el audio prendidos.
        // Si NO es mi personaje (es un rival que veo en mi pantalla):
        // apago su cámara y su audio listener, para no interferir con los míos.
        playerCamera.enabled = isLocalPlayer;

        if (audioListener != null)
        {
            audioListener.enabled = isLocalPlayer;
        }
    }
}
