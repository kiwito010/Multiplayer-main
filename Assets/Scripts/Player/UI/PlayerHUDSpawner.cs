using Fusion;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUDSpawner : NetworkBehaviour {
    // VARIABLES:

    // Referencias:
    [SerializeField] private GameObject playerHUDPrefab;

    private GameObject hudInstance;

    // MÉTODOS:

    public override void Spawned() {
        if (!Object.HasInputAuthority)
            return;

        SpawnPlayerHUD();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        if (hudInstance != null) {
            Destroy(hudInstance);
        }
    }

    private void SpawnPlayerHUD() {
        if (playerHUDPrefab == null) {
            Debug.LogWarning("PlayerHUD Prefab no asignado");
            return;
        }

        hudInstance = Instantiate(playerHUDPrefab);

        // Configuramos el Canvas.
        Canvas canvas = hudInstance.GetComponent<Canvas>();

        if (canvas != null) {
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();

            if (scaler != null) {
                ConfigureScaler(scaler);
            }
        }

        // Buscamos los componentes del jugador.
        PlayerInventory playerInventory =
            GetComponent<PlayerInventory>();

        NetworkInventoryState networkInventory =
            GetComponent<NetworkInventoryState>();

        // Conectamos las interfaces.
        PlayerHUD playerHUD = hudInstance.GetComponent<PlayerHUD>();

        if (playerHUD != null) {
            playerHUD.Initialize(playerInventory, networkInventory);
        }
    }

    // Hace que la UI escale con el tamaño de la ventana, tomando como base
    // 1920x1080. Así se ve igual de bien en el Editor y en el build.
    private void ConfigureScaler(CanvasScaler scaler) {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
    }
}
