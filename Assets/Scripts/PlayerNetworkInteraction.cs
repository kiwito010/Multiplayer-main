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

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData input))
        {
            bool canUse = UseCooldown.ExpiredOrNotRunning(Runner);

            if (input.useButtonPressed && canUse)
            {
                UseCooldown = TickTimer.CreateFromSeconds(Runner, useRate);
                TryDetectAndNotify();
            }
        }
    }

    private void TryDetectAndNotify()
    {
        if (interactPoint == null) return;

        if (Physics.Raycast(interactPoint.position, interactPoint.forward, out RaycastHit hit, range, interactableLayers))
        {
            // Buscamos si lo que golpeamos (o algún padre suyo) es un
            // NetworkObject. Si no lo es, no hay nada que sincronizar.
            var targetNetworkObject = hit.collider.GetComponentInParent<NetworkObject>();
            if (targetNetworkObject != null)
            {
                // Le pedimos a quien tiene autoridad sobre ESTE jugador que
                // confirme el evento hacia todos (así todos los clientes se
                // enteran del mismo "uso", en el mismo momento).
                RPC_NotifyUse(targetNetworkObject, Object.InputAuthority);
            }
        }
    }

    // RpcSources.InputAuthority: solo el dueño de este jugador puede
    // disparar este RPC (nadie puede simular que otro jugador usó algo).
    // RpcTargets.All: el evento se replica en todas las máquinas, así
    // cualquier script de gameplay puede reaccionar en cualquier cliente.
    [Rpc(RpcSources.InputAuthority, RpcTargets.All)]
    private void RPC_NotifyUse(NetworkObject target, PlayerRef fromPlayer)
    {
        // Acá NO decidimos qué pasa. Solo avisamos "esto ocurrió" para que
        // tus propios scripts de gameplay decidan qué hacer.
        OnUsedOnTarget?.Invoke(fromPlayer, target);
    }
}
