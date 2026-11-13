using Fusion;
using UnityEngine;

public class PlayerNetworkRepairActions : NetworkBehaviour {
    // VARIABLES:

    private NetworkInventoryState networkInventory;

    // MÉTODOS:

    public override void Spawned() {
        networkInventory = GetComponent<NetworkInventoryState>();
    }

    public void RequestInstallCable(RepairableComponent component) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestInstallCable(component.Object);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestInstallCable(NetworkObject target) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component =
            target.GetComponent<RepairableComponent>();

        if (component == null || networkInventory == null)
            return;

        if (component.repairingPlayer != Object.InputAuthority)
            return;

        // Solo permitimos modificar componentes cercanos.
        float distance = Vector3.Distance(
            transform.position,
            target.transform.position
        );

        if (distance > 3f)
            return;

        component.TryInstallCable(networkInventory);
    }

    public void RequestCableCut(RepairableComponent component, bool isLeft) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestCableCut(component.Object, isLeft);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCableCut(NetworkObject target, bool isLeft) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component = target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (component.repairingPlayer != Object.InputAuthority)
            return;

        float distance = Vector3.Distance(
            transform.position,
            target.transform.position
        );

        if (distance > 3f)
            return;

        component.RegisterCableCut(isLeft);
    }

    public void RequestRemoveBrokenCable(RepairableComponent component) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestRemoveBrokenCable(component.Object);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestRemoveBrokenCable(NetworkObject target) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component = target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (component.repairingPlayer != Object.InputAuthority)
            return;

        float distance = Vector3.Distance(transform.position, target.transform.position);

        if (distance > 3f)
            return;

        component.RegisterBrokenCableRemoved();
    }

    public void RequestBeginRepair(RepairableComponent component) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestBeginRepair(component.Object);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestBeginRepair(NetworkObject target) {

        if (target == null || !target.IsValid) {
            Debug.LogWarning("[REPAIR] Target inválido.");
            RPC_RepairRequestRejected(target);
            return;
        }

        RepairableComponent component =
            target.GetComponent<RepairableComponent>();

        if (component == null) {
            Debug.LogWarning("[REPAIR] No hay RepairableComponent.");
            RPC_RepairRequestRejected(target);
            return;
        }

        float distance = Vector3.Distance(
            transform.position,
            target.transform.position
        );

        Debug.Log(
            "[REPAIR] Solicitud de inicio | Jugador: " +
            Object.InputAuthority +
            " | Distancia: " + distance
        );

        if (distance > 3f) {
            Debug.LogWarning(
                "[REPAIR] Inicio rechazado por distancia: " + distance
            );

            RPC_RepairRequestRejected(target);
            return;
        }

        bool accepted =
            component.TryBeginRepair(Object.InputAuthority);

        if (!accepted) {
            Debug.LogWarning(
                "[REPAIR] Inicio rechazado por estado u ocupación."
            );

            RPC_RepairRequestRejected(target);
            return;
        }

        Debug.Log("[REPAIR] Inicio aceptado.");
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_RepairRequestRejected(NetworkObject target) {

        Debug.Log("[REPAIR CLIENT] RPC de rechazo recibido");

        PlayerRepairInteraction repairInteraction =
            GetComponent<PlayerRepairInteraction>();

        if (repairInteraction != null) {
            repairInteraction.OnRepairRequestRejected(target);
        }
    }

    public void RequestEndRepair(RepairableComponent component) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestEndRepair(component.Object);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestEndRepair(NetworkObject target) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component = target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        component.EndRepair(Object.InputAuthority);
    }

    public void RequestCableTwist(RepairableComponent component, bool isLeft) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestCableTwist(component.Object, isLeft);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCableTwist(NetworkObject target, bool isLeft) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component =
            target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (component.repairingPlayer != Object.InputAuthority)
            return;

        if (Vector3.Distance(transform.position, target.transform.position) > 3f)
            return;

        component.RegisterCableTwist(isLeft);
    }

    public void RequestCableWrapped(RepairableComponent component, bool isLeft) {
        if (!Object.HasInputAuthority || component == null)
            return;

        RPC_RequestCableWrapped(component.Object, isLeft);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestCableWrapped(NetworkObject target, bool isLeft) {
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component =
            target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (component.repairingPlayer != Object.InputAuthority)
            return;

        if (Vector3.Distance(transform.position, target.transform.position) > 3f)
            return;

        if (networkInventory == null)
            return;

        component.RegisterCableWrapped(isLeft, networkInventory);
    }
}
