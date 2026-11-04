using Fusion;
using UnityEngine;

// Este script hace UNA sola cosa: cuando el jugador local aprieta "usar",
// detecta qué NetworkObject tiene enfrente (con un raycast) y avisa a toda
// la red "el jugador X usó algo sobre el objeto Y".
//
// Lo que significa ese "uso" (cortar, construir, disparar, reparar...) NO
// está acá. Este script solo transporta el evento por la red de forma
// sincronizada. Vos te enganchás desde tus propios scripts de gameplay
// usando el evento estático OnUsedOnTarget, más abajo.
public class PlayerNetworkInteraction : NetworkBehaviour
{
    [Header("Configuración de detección")]
    public float range = 3f;                 // Alcance del raycast
    public Transform interactPoint;           // Desde dónde sale el rayo (normalmente la cámara)
    public LayerMask interactableLayers;      // Qué capas puede detectar

    [Header("Cooldown de red (evita spamear el botón)")]
    public float useRate = 0.2f;
    [Networked] private TickTimer UseCooldown { get; set; }

    // Evento de C# (no de Fusion) al que se puede suscribir cualquier otro
    // script tuyo, en cualquier parte del proyecto, sin que este script
    // necesite saber nada de ellos. Parámetros: quién usó, y sobre qué
    // NetworkObject lo usó.
    public static event System.Action<PlayerRef, NetworkObject> OnUsedOnTarget;
    public event System.Action<NetworkObject> OnLocalUsedOnTarget;

    [Header("Depuración")]
    [SerializeField] private bool debugLogs = false; // activalo solo para investigar un problema

    // Frame de Unity en el que se mandó el último aviso (ver FixedUpdateNetwork).
    private int _lastNotifyFrame = -1;

    private void Log(string message)
    {
        if (debugLogs) Debug.Log("[PlayerNetworkInteraction] " + message);
    }


    public override void Spawned()
    {
        // Si interactPoint esta vacio o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project, compartido), usamos la camara propia.
        if (interactPoint == null || !interactPoint.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) interactPoint = ownCamera.transform;
        }
    }




    public override void FixedUpdateNetwork() {
        if (!Object.HasInputAuthority)
            return;

        if (GetInput(out NetworkInputData input)) {
            if (input.useButtonPressed) {
                // Cuando Fusion corrige la predicción vuelve a simular ticks que ya
                // pasaron, dentro del mismo frame. Sin esta guarda, un solo apretado
                // mandaría el aviso varias veces.
                if (Time.frameCount == _lastNotifyFrame) return;
                _lastNotifyFrame = Time.frameCount;

                TryDetectAndNotify();
            }
        }
    }





    private void TryDetectAndNotify()
    {
        if (!UseCooldown.ExpiredOrNotRunning(Runner))
            return;

        if (interactPoint == null) {
            Debug.LogError("[PlayerNetworkInteraction] 'Interact Point' es null: no tengo desde dónde tirar el rayo.");
            return;
        }

        UseCooldown = TickTimer.CreateFromSeconds(Runner, useRate);

        if (Physics.Raycast(interactPoint.position, interactPoint.forward, out RaycastHit hit, range, interactableLayers)) {

            NetworkObject targetNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();

            if (targetNetworkObject != null) {
                Log("Interactuó con " + targetNetworkObject.name);

                OnLocalUsedOnTarget?.Invoke(targetNetworkObject);

                RPC_NotifyUse(targetNetworkObject, Object.InputAuthority);
            } else {
                Log("Lo que miraste (" + hit.collider.name + ") no tiene NetworkObject.");
            }

        } else {
            Log("No hay nada interactuable adelante.");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_NotifyUse(NetworkObject target, PlayerRef fromPlayer)
    {
        Log("Jugador " + fromPlayer + " interactuó con " + target.name);
        OnUsedOnTarget?.Invoke(fromPlayer, target);
    }
}