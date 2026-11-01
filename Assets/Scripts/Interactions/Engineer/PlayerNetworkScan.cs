
using Fusion;
using UnityEngine;

public class PlayerNetworkScan : NetworkBehaviour {
    // VARIABLES:

    private NetworkObject scanTarget;
    private NetworkObject lastRequestedTarget;

    private NetworkInventoryState networkInventory;

    // MÉTODOS:

    public override void Spawned() {
        networkInventory = GetComponent<NetworkInventoryState>();
    }

    public void RequestScan(NetworkObject target) {
        if (!Object.HasInputAuthority)
            return;

        // Evitamos mandar un RPC cada frame.
        if (lastRequestedTarget == target)
            return;

        lastRequestedTarget = target;
        RPC_SetScanTarget(target);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetScanTarget(NetworkObject target) {
        if (scanTarget != null && scanTarget.IsValid) {
            DiagnosableBase previous =
                scanTarget.GetComponent<DiagnosableBase>();

            if (previous != null)
                previous.SetBeingScanned(false);
        }

        scanTarget = target;
    }

    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority)
            return;

        if (scanTarget == null || !scanTarget.IsValid)
            return;

        DiagnosableBase diagnosable =
            scanTarget.GetComponent<DiagnosableBase>();

        if (diagnosable == null)
            return;

        if (!HasMagnifyingGlassEquipped() ||
            !IsTargetInRange() ||
            diagnosable.IsDiagnosed()) {

            diagnosable.SetBeingScanned(false);
            return;
        }

        diagnosable.SetBeingScanned(true);
        diagnosable.AddScanProgress(Runner.DeltaTime);
    }

    private bool HasMagnifyingGlassEquipped() {
        if (networkInventory == null)
            return false;

        int slotIndex = networkInventory.EquippedSlot;

        if (slotIndex < 0 || slotIndex > 2)
            return false;

        NetworkId itemId = networkInventory.GetItemIdInSlot(slotIndex);

        if (itemId == default(NetworkId))
            return false;

        if (!Runner.TryFindObject(itemId, out NetworkObject itemObject))
            return false;

        NetworkPickableItem item = itemObject.GetComponent<NetworkPickableItem>();

        return item != null && item.GetItemType() == ItemType.MagnifyingGlass;
    }

    private bool IsTargetInRange() {
        float distance = Vector3.Distance(transform.position, scanTarget.transform.position);

        return distance <= 3f;
    }
}
