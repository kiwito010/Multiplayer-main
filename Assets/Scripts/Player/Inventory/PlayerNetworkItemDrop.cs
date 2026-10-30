
using Fusion;
using UnityEngine;

public class PlayerNetworkItemDrop : NetworkBehaviour {
    // VARIABLES:
    //Referencias
    private NetworkInventoryState networkInventory;
    //

    [SerializeField] private float dropDistance = 1.5f;

    [SerializeField] private float throwForce = 3f;

    // MÉTODOS:

    public override void Spawned() {
        networkInventory = GetComponent<NetworkInventoryState>();
    }

    public void RequestDrop(int slotIndex) {
        if (!Object.HasInputAuthority)
            return;

        if (slotIndex < 0 || slotIndex > 2)
            return;

        RPC_RequestDrop(slotIndex);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]

    private void RPC_RequestDrop(int slotIndex) {
        if (networkInventory == null)
            return;

        if (slotIndex < 0 || slotIndex > 2)
            return;

        // Consultamos el contenido real del slot en el Host.
        NetworkId itemId = networkInventory.GetItemIdInSlot(slotIndex);

        if (itemId == default(NetworkId))
            return;

        if (!Runner.TryFindObject(itemId, out NetworkObject itemObject))
            return;

        NetworkPickableItem pickableItem = itemObject.GetComponent<NetworkPickableItem>();

        if (pickableItem == null)
            return;


        Vector3 throwDirection = transform.forward;

        Vector3 dropOrigin =
            transform.position + Vector3.up * 1.2f;

        Vector3 dropPosition =
            dropOrigin + throwDirection * dropDistance;

        // Evitamos colocar el cubo detrás de una pared cercana.
        if (Physics.Raycast(dropOrigin, throwDirection, out RaycastHit hit, dropDistance)) {
            dropPosition = hit.point - throwDirection * 0.3f;
        }

        if (!pickableItem.TryDrop(Object.InputAuthority, dropPosition, throwDirection, throwForce))
            return;

        // Quitamos el objeto del inventario de red.
        networkInventory.TryRemoveItem(slotIndex, out NetworkId removedItemId);

        // Ningún slot queda equipado después de lanzar.
        networkInventory.SetEquippedSlot(-1);

    }
}
