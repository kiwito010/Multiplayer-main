using Fusion;
using UnityEngine;

public class NetworkComputer : NetworkBehaviour {

    // ESTADOS DE RED:

    [Networked] public NetworkBool IsCompleted { get; private set; }

    [Networked] public PlayerRef ActiveHacker { get; private set; }

    // MÉTODOS:

    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += HandleInteraction;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= HandleInteraction;
    }

    private void HandleInteraction(PlayerRef player, NetworkObject target) {
        if (!Object.HasStateAuthority)
            return;

        if (target != Object)
            return;

        if (IsCompleted)
            return;

        if (ActiveHacker != PlayerRef.None)
            return;

        ActiveHacker = player;

        Debug.Log("Jugador " + player + " comenzó a usar la computadora");

        RPC_OpenComputer(player);
    }

    public bool IsBeingUsed() {
        return ActiveHacker != PlayerRef.None;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenComputer(PlayerRef hacker) {
        if (Runner.LocalPlayer != hacker)
            return;

        NetworkObject playerObject = Runner.GetPlayerObject(hacker);

        if (playerObject == null) {
            Debug.LogWarning("No se encontró el jugador local.");
            return;
        }

        PlayerHUDSpawner hudSpawner =
            playerObject.GetComponent<PlayerHUDSpawner>();

        if (hudSpawner == null) {
            Debug.LogWarning("PlayerHUDSpawner no encontrado.");
            return;
        }

        PlayerHUD hud = hudSpawner.GetHUD();

        if (hud == null) {
            Debug.LogWarning("PlayerHUD no encontrado.");
            return;
        }

        ComputerUI computerUI = hud.GetComputerUI();

        if (computerUI == null) {
            Debug.LogWarning("ComputerUI no asignado en PlayerHUD.");
            return;
        }

        computerUI.ShowNetwork(this);

        Debug.Log("ComputerUI abierta para el jugador local.");
    }

    public void RequestExit() {
        if (Object.HasStateAuthority) {
            if (ActiveHacker == Runner.LocalPlayer) {
                ActiveHacker = PlayerRef.None;
                Debug.Log("Computadora liberada por el Host");
            }

            return;
        }

        RPC_RequestExit();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestExit(RpcInfo info = default) {
        if (!Object.HasStateAuthority)
            return;

        Debug.Log(
            "Solicitud de salida. ActiveHacker: " + ActiveHacker +
            " | Solicitante: " + info.Source
        );

        if (ActiveHacker != info.Source) {
            Debug.LogWarning("Salida rechazada: jugador no autorizado");
            return;
        }

        ActiveHacker = PlayerRef.None;

        Debug.Log("Computadora liberada por el Client");
    }
}
