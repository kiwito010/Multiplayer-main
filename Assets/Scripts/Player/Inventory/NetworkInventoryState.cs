
using Fusion;
using UnityEngine;

public class NetworkInventoryState : NetworkBehaviour {
    // VARIABLES:

    // Cada slot guarda el identificador de un objeto de red.
    // Un NetworkId vacío significa que el slot está libre.

    [Networked] private NetworkId Slot0 { get; set; }
    [Networked] private NetworkId Slot1 { get; set; }
    [Networked] private NetworkId Slot2 { get; set; }

    [Networked] public int EquippedSlot { get; private set; }

    // MÉTODOS:

    public override void Spawned() {
        if (Object.HasStateAuthority) {
            EquippedSlot = -1;
        }
    }

    public bool HasFreeSlot() {
        return Slot0 == default(NetworkId) ||
               Slot1 == default(NetworkId) ||
               Slot2 == default(NetworkId);
    }

    public bool TryAddItem(NetworkObject item) {
        if (!Object.HasStateAuthority)
            return false;

        if (item == null)
            return false;

        NetworkId itemId = item.Id;

        if (Slot0 == itemId || Slot1 == itemId || Slot2 == itemId)
            return false;

        if (Slot0 == default(NetworkId)) {
            Slot0 = itemId;
            return true;
        }

        if (Slot1 == default(NetworkId)) {
            Slot1 = itemId;
            return true;
        }

        if (Slot2 == default(NetworkId)) {
            Slot2 = itemId;
            return true;
        }

        return false;
    }

    public bool TryRemoveItem(int slotIndex, out NetworkId itemId) {
        itemId = default(NetworkId);

        if (!Object.HasStateAuthority)
            return false;

        switch (slotIndex) {
            case 0:
                itemId = Slot0;
                if (itemId == default(NetworkId))
                    return false;

                Slot0 = default(NetworkId);
                return true;

            case 1:
                itemId = Slot1;
                if (itemId == default(NetworkId))
                    return false;

                Slot1 = default(NetworkId);
                return true;

            case 2:
                itemId = Slot2;
                if (itemId == default(NetworkId))
                    return false;

                Slot2 = default(NetworkId);
                return true;

            default:
                return false;
        }
    }

    public NetworkId GetItemIdInSlot(int index) {
        switch (index) {
            case 0:
                return Slot0;

            case 1:
                return Slot1;

            case 2:
                return Slot2;

            default:
                return default(NetworkId);
        }
    }

    public void SetEquippedSlot(int slotIndex) {
        if (!Object.HasStateAuthority)
            return;

        if (slotIndex < -1 || slotIndex > 2)
            return;

        if (slotIndex >= 0 && GetItemIdInSlot(slotIndex) == default(NetworkId))
            return;

        EquippedSlot = slotIndex;

        Debug.Log("Fusion - Slot equipado: " + EquippedSlot);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RPC_SetEquippedSlot(int slotIndex) {
        SetEquippedSlot(slotIndex);
    }

    public string GetItemNameInSlot(int index) {
        NetworkId itemId = GetItemIdInSlot(index);

        if (itemId == default(NetworkId))
            return "";

        if (Runner.TryFindObject(itemId, out NetworkObject itemObject)) {
            return itemObject.name;
        }

        return "";
    }
}
