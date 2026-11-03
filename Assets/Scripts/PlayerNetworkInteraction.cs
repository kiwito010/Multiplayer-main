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

    // Evento de C# (no de Fusion) al que se puede suscribir cualquier otro
    // script tuyo, en cualquier parte del proyecto, sin que este script
    // necesite saber nada de ellos. Parámetros: quién usó, y sobre qué
    // NetworkObject lo usó.
    public static event System.Action<PlayerRef, NetworkObject> OnUsedOnTarget;
    public event System.Action<NetworkObject> OnLocalUsedOnTarget;

    private GameInput gameInput;

    public override void Spawned()
    {
        gameInput = GetComponent<GameInput>();

        // Si interactPoint esta vacio o apunta a algo que no es hijo de ESTE
        // jugador (ej: el prefab del Project, compartido), usamos la camara propia.
        if (interactPoint == null || !interactPoint.IsChildOf(transform))
        {
            var ownCamera = GetComponentInChildren<Camera>(true);
            if (ownCamera != null) interactPoint = ownCamera.transform;
        }
    }

    private void Update() {
        if (!Object.HasInputAuthority || gameInput == null)
            return;

        if (gameInput.GetInteractPressed()) {
            TryDetectAndNotify();
        }
    }

    private void TryDetectAndNotify() 
    {
        if (interactPoint == null) {
            Debug.Log("ERROR: InteractPoint es null");
            return;
        }

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

                OnLocalUsedOnTarget?.Invoke(targetNetworkObject);

                RPC_NotifyUse(targetNetworkObject, Object.InputAuthority);
            } else {
                Debug.Log("ERROR: El objeto no tiene NetworkObject");
            }

        } else {
            Debug.Log("Raycast sin detección de objeto interactuable");
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_NotifyUse(NetworkObject target, PlayerRef fromPlayer) {
        Debug.Log("Jugador " + fromPlayer + " interactuó con " + target.name);
        OnUsedOnTarget?.Invoke(fromPlayer, target);
    }
}