
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    // VARIABLES:

    // Referencias:
    private PlayerInventory playerInventory;
    private NetworkInventoryState networkInventory;

    [SerializeField] private TextMeshProUGUI[] slotTexts;
    [SerializeField] private Image[] slotImages;

    private Color normalColor;
    private Color selectedColor;

    [SerializeField] private RectTransform inventoryPanel;

    [SerializeField] private Vector2 openPosition;
    [SerializeField] private Vector2 closedPosition;

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float closeDelay = 1f;

    private Vector2 targetPosition;

    private string[] lastSlotNames;
    private bool isInitialized;


    [Header("Diseño del inventario")]
    [SerializeField] private Image inventoryBackground;

    [Header("Engineer")]
    [SerializeField] private Sprite engineerBackground;
    [SerializeField] private Color engineerNormalColor;
    [SerializeField] private Color engineerSelectedColor;

    [Header("Hacker")]
    [SerializeField] private Sprite hackerBackground;
    [SerializeField] private Color hackerNormalColor;
    [SerializeField] private Color hackerSelectedColor;

    [Header("Explorer")]
    [SerializeField] private Sprite explorerBackground;
    [SerializeField] private Color explorerNormalColor;
    [SerializeField] private Color explorerSelectedColor;


    // MÉTODOS:

    public void Initialize(PlayerInventory inventory, NetworkInventoryState networkState) {
        // Evitamos dejar eventos conectados al inventario anterior.
        UnsubscribeEvents();

        playerInventory = inventory;
        networkInventory = networkState;

        lastSlotNames = new string[slotTexts.Length];

        playerInventory.OnInventoryChanged += UpdateUI;
        playerInventory.OnInventoryOpened += ShowInventoryTemporarily;

        targetPosition = closedPosition;
        inventoryPanel.anchoredPosition = closedPosition;

        isInitialized = true;

        UpdateUI();
    }

    private void Update() {
        if (!isInitialized)
            return;

        inventoryPanel.anchoredPosition = Vector2.Lerp(
            inventoryPanel.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );

        if (networkInventory == null ||
            networkInventory.Object == null ||
            !networkInventory.Object.IsValid)
            return;

        // Comprobamos si cambiaron los objetos sincronizados.
        for (int i = 0; i < slotTexts.Length; i++) {
            string itemName = networkInventory.GetItemNameInSlot(i);

            if (lastSlotNames[i] != itemName) {
                UpdateUI();
                ShowInventoryTemporarily();
                break;
            }
        }
    }

    private void UpdateUI() {
        if (!isInitialized)
            return;

        if (networkInventory == null ||
            networkInventory.Object == null ||
            !networkInventory.Object.IsValid)
            return;

        int selectedSlot = playerInventory.GetSelectedSlot();

        for (int i = 0; i < slotTexts.Length; i++) {
            string itemName = networkInventory.GetItemNameInSlot(i);

            slotTexts[i].text = itemName;

            lastSlotNames[i] = itemName;

            if (i == selectedSlot) {
                slotImages[i].color = selectedColor;
            } else {
                slotImages[i].color = normalColor;
            }
        }
    }

    private void ShowInventoryTemporarily() {
        if (!isInitialized)
            return;

        CancelInvoke(nameof(CloseInventory));

        targetPosition = openPosition;

        Invoke(nameof(CloseInventory), closeDelay);
    }

    private void CloseInventory() {
        targetPosition = closedPosition;
    }

    private void UnsubscribeEvents() {
        if (playerInventory == null)
            return;

        playerInventory.OnInventoryChanged -= UpdateUI;
        playerInventory.OnInventoryOpened -= ShowInventoryTemporarily;
    }

    private void OnDestroy() {
        UnsubscribeEvents();
        CancelInvoke(nameof(CloseInventory));
    }


    public void SetRoleStyle(int roleIndex) {
        Sprite background = null;

        switch (roleIndex) {
            case 0: // Engineer
                background = engineerBackground;
                normalColor = engineerNormalColor;
                selectedColor = engineerSelectedColor;
                break;

            case 1: // Hacker
                background = hackerBackground;
                normalColor = hackerNormalColor;
                selectedColor = hackerSelectedColor;
                break;

            case 2: // Explorer
                background = explorerBackground;
                normalColor = explorerNormalColor;
                selectedColor = explorerSelectedColor;
                break;

            default:
                Debug.LogWarning("[INVENTORY] Personaje desconocido: " + roleIndex);
                return;
        }

        if (inventoryBackground != null && background != null) {
            inventoryBackground.sprite = background;
            inventoryBackground.color = Color.white;
        }

        // Actualizar los colores si el inventario ya está inicializado.
        if (isInitialized)
            UpdateUI();
    }

}
