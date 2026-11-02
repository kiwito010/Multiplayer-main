using Fusion;
using UnityEngine;

public class FuseBox : NetworkBehaviour {
    // REFERENCIAS:

    [SerializeField] private RepairableComponent repairableComponent;

    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;
    [SerializeField] private GameObject installedFuseVisual;

    [SerializeField] private float interactionDistance = 3f;

    // ESTADO COMPARTIDO:

    [Networked] public NetworkBool isOpened { get; private set; }
    [Networked] public NetworkBool fuseInstalled { get; private set; }

    // MÉTODOS:

    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += OnUsedOnTarget;

        UpdateVisuals();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= OnUsedOnTarget;
    }

    private void OnUsedOnTarget(PlayerRef player, NetworkObject target) {
        if (!Object.HasStateAuthority)
            return;

        if (target != Object)
            return;

        NetworkObject playerObject = Runner.GetPlayerObject(player);

        if (playerObject == null)
            return;

        if (Vector3.Distance(
            playerObject.transform.position,
            transform.position
        ) > interactionDistance)
            return;

        if (!isOpened) {
            isOpened = true;
            Debug.Log("Caja del fusible abierta");
            return;
        }

        if (fuseInstalled)
            return;

        if (repairableComponent != null &&
            !repairableComponent.cableRepairCompleted)
            return;

        NetworkInventoryState inventory = playerObject.GetComponent<NetworkInventoryState>();

        if (inventory == null)
            return;

        if (!inventory.TryConsumeItem(
            ItemType.Fuse,
            out NetworkObject fuse
        ))
            return;

        fuseInstalled = true;

        if (fuse != null) {
            Runner.Despawn(fuse);
        }

        Debug.Log("Fusible instalado correctamente");
    }

    public override void Render() {
        UpdateVisuals();
    }

    private void UpdateVisuals() {
        if (closedVisual != null) {
            closedVisual.SetActive(!isOpened);
        }

        if (openedVisual != null) {
            openedVisual.SetActive(isOpened);
        }

        if (installedFuseVisual != null) {
            installedFuseVisual.SetActive(fuseInstalled);
        }
    }
}
