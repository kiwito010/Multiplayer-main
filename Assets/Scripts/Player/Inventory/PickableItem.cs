using UnityEngine;

public class PickableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName;

    private Collider itemCollider;
    private Rigidbody itemRigidbody;

    private void Awake() {
        itemCollider = GetComponent<Collider>();
        itemRigidbody = GetComponent<Rigidbody>();
    }

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

    public void Equip(Transform handPoint) {
        transform.SetParent(handPoint);

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        itemCollider.enabled = false;

        itemRigidbody.isKinematic = true;
        itemRigidbody.useGravity = false;

        gameObject.SetActive(true);
    }

    public void Unequip() {
        itemCollider.enabled = true;

        gameObject.SetActive(false);
    }

    public void Drop(Vector3 dropPosition, Vector3 throwDirection, float throwForce) {
        transform.SetParent(null);

        transform.position = dropPosition;

        gameObject.SetActive(true);

        itemCollider.enabled = true;

        itemRigidbody.isKinematic = false;
        itemRigidbody.useGravity = true;

        itemRigidbody.AddForce(
            throwDirection * throwForce,
            ForceMode.Impulse
        );
    }
}