using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour {
    [SerializeField] private PlayerInventory playerInventory;

    [SerializeField] private TextMeshProUGUI[] slotTexts;
    [SerializeField] private Image[] slotImages;

    [SerializeField] private Color normalColor;
    [SerializeField] private Color selectedColor;

    [SerializeField] private GameObject closedInventory;
    [SerializeField] private GameObject openInventory;

    [SerializeField] private float closeDelay = 1f;

    private bool wasInventoryOpen;

    private void OnEnable() {
        playerInventory.OnInventoryChanged += UpdateUI;
    }

    private void OnDisable() {
        playerInventory.OnInventoryChanged -= UpdateUI;
    }

    private void Start() {
        UpdateUI();
    }

    private void UpdateUI() {
        int selectedSlot = playerInventory.GetSelectedSlot();

        bool inventoryOpen = selectedSlot != -1;

        if (inventoryOpen) {
            CancelInvoke(nameof(CloseInventory));

            openInventory.SetActive(true);
            closedInventory.SetActive(false);

            wasInventoryOpen = true;
        } else if (wasInventoryOpen) {
            CancelInvoke(nameof(CloseInventory));
            Invoke(nameof(CloseInventory), closeDelay);

            wasInventoryOpen = false;
        }

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

    private void CloseInventory() {
        openInventory.SetActive(false);
        closedInventory.SetActive(true);
    }
}