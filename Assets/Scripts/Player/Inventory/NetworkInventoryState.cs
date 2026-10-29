
using Fusion;
using UnityEngine;

public class NetworkInventoryState : NetworkBehaviour {
    // VARIABLES:

    // Cada slot guarda el identificador de un objeto de red.
    // Un NetworkId vacío significa que el slot está libre.

    [Networked] private NetworkId Slot0 { get; set; }
    [Networked] private NetworkId Slot1 { get; set; }
    [Networked] private NetworkId Slot2 { get; set; }

    // MÉTODOS:

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
