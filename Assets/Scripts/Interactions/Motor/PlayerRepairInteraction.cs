using Fusion;
using UnityEngine;

public class PlayerRepairInteraction : NetworkBehaviour {
    // VARIABLES:

    private PlayerNetworkInteraction networkInteraction;
    private PlayerHUDSpawner hudSpawner;
    private PlayerInventory playerInventory;
    private RepairableComponent pendingRepair;
    private PlayerNetworkRepairActions repairActions;

    // MÉTODOS:

    public override void Spawned() {
        if (!Object.HasInputAuthority)
            return;

        networkInteraction = GetComponent<PlayerNetworkInteraction>();
        hudSpawner = GetComponent<PlayerHUDSpawner>();
        playerInventory = GetComponent<PlayerInventory>();
        repairActions = GetComponent<PlayerNetworkRepairActions>();

        if (networkInteraction != null) {
            networkInteraction.OnLocalUsedOnTarget += OnLocalUsedOnTarget;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (networkInteraction != null) {
            networkInteraction.OnLocalUsedOnTarget -= OnLocalUsedOnTarget;
        }
    }

    private void Update() {
        if (!Object.HasInputAuthority || pendingRepair == null)
            return;

        if (pendingRepair.repairingPlayer != Object.InputAuthority)
            return;

        PlayerHUD hud = hudSpawner.GetHUD();

        if (hud != null) {
            hud.OpenCableRepair(pendingRepair, playerInventory);
        }

        pendingRepair = null;
    }

    private void OnLocalUsedOnTarget(NetworkObject target) {
        if (target == null)
            return;

        RepairableComponent component = target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (!component.IsDiagnosed()) {
            Debug.Log("Primero tenés que diagnosticar este componente");
            return;
        }

        if (component.IsRepaired())
            return;

        if (hudSpawner == null || playerInventory == null)
            return;

        if (pendingRepair != null || repairActions == null)
            return;

        if (component.repairingPlayer != PlayerRef.None)
            return;

        pendingRepair = component;
        repairActions.RequestBeginRepair(component);
    }
}
