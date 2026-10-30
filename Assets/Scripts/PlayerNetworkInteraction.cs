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
                Debug.Log("BOTON DE INTERACCION DETECTADO");

                TryDetectAndNotify();
            }
        }
    }





    private void TryDetectAndNotify() {
        if (interactPoint == null) {
            Debug.Log("ERROR: InteractPoint es null");
            return;
        }

        Debug.Log("Intentando detectar objeto");

        if (Physics.Raycast(interactPoint.position, interactPoint.forward, out RaycastHit hit, range, interactableLayers)) {

            Debug.Log("Raycast detectó: " + hit.collider.name);

            NetworkObject targetNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();

            if (targetNetworkObject != null) {
                Debug.Log("NetworkObject encontrado: " + targetNetworkObject.name);

                RPC_NotifyUse(targetNetworkObject, Object.InputAuthority);
            } else {
                Debug.Log("ERROR: El objeto no tiene NetworkObject");
            }

        } else {
            Debug.Log("Raycast sin detección de objeto interactuable");
        }
    }

    // RpcSources.InputAuthority: solo el dueño de este jugador puede
    // disparar este RPC (nadie puede simular que otro jugador usó algo).
    // RpcTargets.All: el evento se replica en todas las máquinas, así
    // cualquier script de gameplay puede reaccionar en cualquier cliente.
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_NotifyUse(NetworkObject target, PlayerRef fromPlayer)
    {
        // Acá NO decidimos qué pasa. Solo avisamos "esto ocurrió" para que
        // tus propios scripts de gameplay decidan qué hacer.
        Debug.Log("Jugador " + fromPlayer + " interactuó con " + target.name);

        OnUsedOnTarget?.Invoke(fromPlayer, target);
    }
}