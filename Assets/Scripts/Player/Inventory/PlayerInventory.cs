using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerInventory : MonoBehaviour
{
    //VARIABLES:

    //Referencias:
    
    //Fusion
    private NetworkObject networkObject;
    private PlayerNetworkItemDrop networkItemDrop;
    //
    [SerializeField] private GameInput gameInput;

    private NetworkInventoryState networkInventory;

    [SerializeField] private Transform handPoint;
    //

    [SerializeField] private int maxSlots = 3;

    private PickableItem equippedItem;

    private PickableItem[] slots;
    private int selectedSlot = -1;

    [SerializeField] private Transform cameraTransform;

    [SerializeField] private float dropDistance = 1f;
    [SerializeField] private float throwForce = 3f;

    public event System.Action OnInventoryChanged;

    public event System.Action OnInventoryOpened;

    private void Awake() {
        networkObject = GetComponent<NetworkObject>();
        networkItemDrop = GetComponent<PlayerNetworkItemDrop>();
        networkInventory = GetComponent<NetworkInventoryState>();

        slots = new PickableItem[maxSlots];
    }

    private void Update() {
        if (networkObject == null || !networkObject.IsValid || !networkObject.HasInputAuthority)
            return;

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
            if (selectedSlot >= 0 && networkItemDrop != null) {
                networkItemDrop.RequestDrop(selectedSlot);
                DeselectSlot();
            }
        }
    }

    public bool TryAddItem(PickableItem item) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] == null) {
                slots[i] = item;

                OnInventoryChanged?.Invoke();
                OnInventoryOpened?.Invoke();

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

        if (selectedSlot == slotIndex) {
            
            if (equippedItem != null) { 
            equippedItem.Unequip();
            equippedItem = null;
            }

            selectedSlot = -1;

            if (networkInventory != null)
                networkInventory.RPC_SetEquippedSlot(-1);

            OnInventoryChanged?.Invoke();
            return;
        }

        selectedSlot = slotIndex;

        if (networkInventory != null)
            networkInventory.RPC_SetEquippedSlot(selectedSlot);

        OnInventoryChanged?.Invoke();
        OnInventoryOpened?.Invoke();

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

        Vector3 dropPosition = cameraTransform.position + cameraTransform.forward * dropDistance;

        equippedItem.Drop(dropPosition, cameraTransform.forward, throwForce);

        slots[selectedSlot] = null;

        equippedItem = null;
        selectedSlot = -1;

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
        if (selectedSlot < 0 || selectedSlot >= slots.Length)
            return null;

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

    public bool HasItem(string itemName) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] != null) {
                Debug.Log(
                    "Slot " + i +
                    " contiene: " +
                    slots[i].GetItemName()
                );

                if (slots[i].GetItemName() == itemName) {
                    return true;
                }
            }
        }

        return false;
    }

    public bool RemoveItem(string itemName) {
        for (int i = 0; i < slots.Length; i++) {
            if (slots[i] != null &&
                slots[i].GetItemName() == itemName) {
                if (equippedItem == slots[i]) {
                    equippedItem.gameObject.SetActive(false);
                    equippedItem = null;
                }

                slots[i] = null;

                if (selectedSlot == i) {
                    selectedSlot = -1;
                }

                OnInventoryChanged?.Invoke();

                return true;
            }
        }

        return false;
    }

    public void DeselectSlot() {
        if (selectedSlot == -1)
            return;

        if (equippedItem != null) {
            equippedItem.Unequip();
            equippedItem = null;
        }

        selectedSlot = -1;

        if (networkInventory != null)
            networkInventory.RPC_SetEquippedSlot(-1);

        OnInventoryChanged?.Invoke();
    }

}
