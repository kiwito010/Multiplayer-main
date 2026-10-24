using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    [SerializeField] private PlayerInventory playerInventory;

    [SerializeField] private TextMeshProUGUI[] slotTexts;
    [SerializeField] private Image[] slotImages;

    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;

    [SerializeField] private RectTransform inventoryPanel;

    [SerializeField] private Vector2 openPosition;
    [SerializeField] private Vector2 closedPosition;

    [SerializeField] private float moveSpeed = 8f;
    [SerializeField] private float closeDelay = 1f;

    private Vector2 targetPosition;
    private bool closeScheduled;

    [SerializeField] private EngineerDiagnostic engineerDiagnostic;

    [SerializeField] private Image magnifyingGlassImage;

    [SerializeField] private Color magnifyingGlassLockedColor;
    [SerializeField] private Color magnifyingGlassUnlockedColor;

    private void OnEnable() {
        playerInventory.OnInventoryChanged += UpdateUI;
        playerInventory.OnInventoryOpened += ShowInventoryTemporarily;

        engineerDiagnostic.OnMagnifyingGlassStateChanged += EngineerDiagnostic_OnMagnifyingGlassStateChanged;
    }

    private void OnDisable() {
        playerInventory.OnInventoryChanged -= UpdateUI;
        playerInventory.OnInventoryOpened -= ShowInventoryTemporarily;

        engineerDiagnostic.OnMagnifyingGlassStateChanged -= EngineerDiagnostic_OnMagnifyingGlassStateChanged; 
    }

    private void EngineerDiagnostic_OnMagnifyingGlassStateChanged(
    bool hasMagnifyingGlass) {
        if (hasMagnifyingGlass) {
            magnifyingGlassImage.color = magnifyingGlassUnlockedColor;
        } else {
            magnifyingGlassImage.color = magnifyingGlassLockedColor;
        }
    }

    private void Start() {
        targetPosition = closedPosition;
        inventoryPanel.anchoredPosition = closedPosition;

        UpdateUI();

        EngineerDiagnostic_OnMagnifyingGlassStateChanged(engineerDiagnostic.HasMagnifyingGlass());
    }

    private void Update() {
        inventoryPanel.anchoredPosition = Vector2.Lerp(
            inventoryPanel.anchoredPosition,
            targetPosition,
            Time.deltaTime * moveSpeed
        );
    }

    private void UpdateUI() {
        int selectedSlot = playerInventory.GetSelectedSlot();

        for (int i = 0; i < slotTexts.Length; i++) {
            PickableItem item = playerInventory.GetItemInSlot(i);

            if (item == null) {
                slotTexts[i].text = "";
            } else {
                slotTexts[i].text = item.GetItemName();
            }

            if (i == selectedSlot) {
                slotImages[i].color = selectedColor;
            } else {
                slotImages[i].color = normalColor;
            }
        }
    }

    private void ShowInventoryTemporarily() {
        CancelInvoke(nameof(CloseInventory));

        targetPosition = openPosition;

        Invoke(nameof(CloseInventory), closeDelay);
    }

    private void CloseInventory() {
        targetPosition = closedPosition;
        closeScheduled = false;
    }
}