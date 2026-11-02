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

        RepairableComponent component =
            target.GetComponent<RepairableComponent>();

        if (component == null)
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
        if (target == null || !target.IsValid)
            return;

        RepairableComponent component = target.GetComponent<RepairableComponent>();

        if (component == null)
            return;

        if (Vector3.Distance(transform.position, target.transform.position) > 3f)
            return;

        component.TryBeginRepair(Object.InputAuthority);
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
}
