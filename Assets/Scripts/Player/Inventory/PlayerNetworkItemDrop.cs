
using Fusion;
using UnityEngine;

public class PlayerNetworkItemDrop : NetworkBehaviour {
    // VARIABLES:

    [SerializeField] private float dropDistance = 1.5f;

    private NetworkInventoryState networkInventory;

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

        // La posición se calcula en el Host usando el personaje.
        Vector3 dropPosition =
            transform.position +
            transform.forward * dropDistance +
            Vector3.up * 0.5f;

        // El objeto debe pertenecer al jugador.
        if (!pickableItem.TryDrop(Object.InputAuthority, dropPosition))
            return;

        // Quitamos el objeto del inventario sincronizado.
        networkInventory.TryRemoveItem(slotIndex, out NetworkId removedItemId);
    }
}
