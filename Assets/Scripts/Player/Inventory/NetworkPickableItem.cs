
using Fusion;
using UnityEngine;

public class NetworkPickableItem : NetworkBehaviour {
    // VARIABLES:

    [SerializeField] private GameObject itemVisual;

    [SerializeField] private Collider itemCollider;

    [Networked] private NetworkBool isPickedUp { get; set; }
    [Networked] private PlayerRef pickedBy { get; set; }

    // EVENTOS:

    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += PlayerNetworkInteraction_OnUsedOnTarget;

        UpdateVisual();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= PlayerNetworkInteraction_OnUsedOnTarget;
    }


    private void PlayerNetworkInteraction_OnUsedOnTarget(PlayerRef player, NetworkObject target) {
        // Solo la autoridad puede modificar el objeto.
        if (!Object.HasStateAuthority)
            return;

        // Comprobamos que se interactuó con este objeto.
        if (target != Object)
            return;

        // Evitamos recoger el mismo objeto dos veces.
        if (isPickedUp)
            return;

        // Buscamos el personaje que realizó la interacción.
        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return;

        // Buscamos su inventario de red.
        NetworkInventoryState inventory =
            playerObject.GetComponent<NetworkInventoryState>();

        if (inventory == null) {
            Debug.LogWarning("El jugador no tiene NetworkInventoryState");
            return;
        }

        // Intentamos guardar el objeto.
        if (!inventory.TryAddItem(Object)) {
            Debug.Log("El inventario está lleno");
            return;
        }

        // Confirmamos la recogida.
        pickedBy = player;
        isPickedUp = true;

        Debug.Log("Objeto guardado en el inventario de " + player);
    }


    public override void Render() {
        UpdateVisual();
    }

    private void UpdateVisual() {
        if (itemVisual != null) {
            itemVisual.SetActive(!isPickedUp);
        }

        if (itemCollider != null) {
            itemCollider.enabled = !isPickedUp;
        }
    }
}
