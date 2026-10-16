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

    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float dropDistance = 1f;
    [SerializeField] private float throwForce = 3f;

    public event System.Action OnInventoryChanged;

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

        if (gameInput.GetDropPressed()) {
            DropSelectedItem();
        }
    }

    public bool TryAddItem(PickableItem item) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) {
                slots[i] = item;

                OnInventoryChanged?.Invoke();

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

        if (selectedSlot == slotIndex && equippedItem != null) {
            equippedItem.Unequip();
            equippedItem = null;
            return;
        }

        selectedSlot = slotIndex;

        EquipSelectedItem();

        Debug.Log("Slot seleccionado: " + selectedSlot);
    }

    private void EquipSelectedItem() {
        if (equippedItem != null) {
            equippedItem.Unequip();
        }

        equippedItem = GetSelectedItem();

        if (equippedItem == null)
            return;

        equippedItem.Equip(handPoint);
    }

    public void DropSelectedItem() {
        if (equippedItem == null)
            return;

        Vector3 dropPosition =
            cameraTransform.position +
            cameraTransform.forward * dropDistance;

        equippedItem.Drop(
            dropPosition,
            cameraTransform.forward,
            throwForce
        );

        slots[selectedSlot] = null;
        equippedItem = null;

        OnInventoryChanged?.Invoke();
    }

    public void RemoveSelectedItem() {
        if (equippedItem != null) {
            equippedItem.gameObject.SetActive(false);
            equippedItem = null;
        }

        slots[selectedSlot] = null;
        
        OnInventoryChanged?.Invoke();
    }

    public PickableItem GetSelectedItem() {
        return slots[selectedSlot];
    }

    public PickableItem GetItemInSlot(int index) {
        if (index < 0 || index >= slots.Length)
            return null;

        return slots[index];
    }

    public int GetSelectedSlot() {
        return selectedSlot;
    }
}
