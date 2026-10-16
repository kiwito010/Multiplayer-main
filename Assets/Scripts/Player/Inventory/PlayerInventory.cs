using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    //VARIABLES:
    //Referencias:
    [SerializeField] private GameInput gameInput;

    [SerializeField] private Transform handPoint;

    [SerializeField] private int maxSlots = 3;

    private PickableItem equippedItem;

    private PickableItem[] slots;
    private int selectedSlot = 0;

    private void Awake() {
        slots = new PickableItem[maxSlots];
    }

    private void Update() {
        if (gameInput.GetInventorySlot1Pressed()) {
            SelectSlot(0);
        }

        if (gameInput.GetInventorySlot2Pressed()) {
            SelectSlot(1);
        }

        if (gameInput.GetInventorySlot3Pressed()) {
            SelectSlot(2);
        }
    }

    public bool TryAddItem(PickableItem item) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) {
                slots[i] = item;

                Debug.Log(
                    item.GetItemName() +
                    " guardado en slot " +
                    i
                );

                return true;
            }
        }

        Debug.Log("Inventario lleno");
        return false;
    }

    public void SelectSlot(int slotIndex) {
        if (slotIndex < 0 || slotIndex >= slots.Length)
            return;

        selectedSlot = slotIndex;

        EquipSelectedItem();

        Debug.Log("Slot seleccionado: " + selectedSlot);
    }

    private void EquipSelectedItem() {
        if (equippedItem != null) {
            equippedItem.gameObject.SetActive(false);
        }

        equippedItem = GetSelectedItem();

        if (equippedItem == null)
            return;

        equippedItem.transform.SetParent(handPoint);

        equippedItem.transform.localPosition = Vector3.zero;
        equippedItem.transform.localRotation = Quaternion.identity;

        equippedItem.gameObject.SetActive(true);
    }

    public PickableItem GetSelectedItem() {
        return slots[selectedSlot];
    }
}
