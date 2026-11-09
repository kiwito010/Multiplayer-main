using Fusion;
using UnityEngine;

public class NetworkCoolantDeposit : NetworkBehaviour {

    // VARIABLES:

    [SerializeField] private NetworkComputer networkComputer;
    [SerializeField] private NetworkCoolantSpawner coolantSpawner;

    [Header("Interacción")]
    [SerializeField] private float maxInteractionDistance = 3f;

    // MÉTODOS:

    public bool IsHackActive() {
        if (networkComputer == null ||
            networkComputer.Object == null ||
            !networkComputer.Object.IsValid)
            return false;

        return networkComputer.IsHackRunning &&
               !networkComputer.IsCompleted &&
               !networkComputer.CooldownTimer.IsRunning;
    }

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

        if (networkComputer == null || coolantSpawner == null)
            return;

        // Solo permitimos depositar durante un hackeo activo.
        if (!networkComputer.IsHackRunning ||
            networkComputer.IsCompleted ||
            networkComputer.CooldownTimer.IsRunning)
            return;

        // El hacker debe permanecer en la computadora.
        if (networkComputer.ActiveHacker == player)
            return;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return;

        float distance = Vector3.Distance(playerObject.transform.position, transform.position);

        if (distance > maxInteractionDistance)
            return;

        NetworkInventoryState inventory = playerObject.GetComponent<NetworkInventoryState>();

        if (inventory == null)
            return;

        int slot = inventory.EquippedSlot;

        if (slot < 0 || slot > 2)
            return;

        NetworkId itemId = inventory.GetItemIdInSlot(slot);

        if (itemId == default(NetworkId))
            return;

        if (!Runner.TryFindObject(itemId, out NetworkObject coolantObject))
            return;

        NetworkPickableItem item =
            coolantObject.GetComponent<NetworkPickableItem>();

        if (item == null || item.GetItemType() != ItemType.Coolant)
            return;

        if (!coolantSpawner.IsActiveCoolant(coolantObject))
            return;

        // Quitamos exactamente el objeto equipado.
        if (!inventory.TryRemoveItem(slot, out NetworkId removedId))
            return;

        inventory.SetEquippedSlot(-1);

        // Reducimos la temperatura.
        if (!networkComputer.TryApplyCoolant())
            return;

        // Avisamos al generador antes de eliminar el objeto.
        coolantSpawner.NotifyCoolantConsumed(coolantObject);

        Runner.Despawn(coolantObject);

        Debug.Log("Jugador " + player + " depositó refrigerante");
    }
}
