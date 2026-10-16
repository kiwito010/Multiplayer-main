using UnityEngine;

public class PickableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName;

    public string GetItemName()
    {
        return itemName;
    }

    public bool TryPickup(PlayerInventory playerInventory)
    {
        bool pickedUp = playerInventory.TryAddItem(this);

        if (pickedUp)
        {
            gameObject.SetActive(false);
        }

        return pickedUp;
    }

    public void Interact()
    {
    }
}