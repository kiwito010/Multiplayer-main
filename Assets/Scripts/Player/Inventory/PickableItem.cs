using UnityEngine;

public class PickableItem : MonoBehaviour, IInteractable
{
    [SerializeField] private string itemName;

    private Collider itemCollider;
    private Rigidbody itemRigidbody;

    [SerializeField] private float freezeDelay = 0.5f;
    [SerializeField] private float maxVelocityToFreeze = 0.1f;
    [SerializeField] private float maxAngularVelocityToFreeze = 0.1f;

    private bool touchingGround;
    private bool freezeScheduled;

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

        CancelInvoke(nameof(TryFreezeItem));
        freezeScheduled = false;

        itemRigidbody.constraints = RigidbodyConstraints.None;

        itemRigidbody.AddForce(throwDirection * throwForce, ForceMode.Impulse);
    }

    private void OnCollisionEnter(Collision collision) {
        if (collision.gameObject.layer == LayerMask.NameToLayer("Ground")){
            touchingGround = true;

            if (!freezeScheduled) { 
                freezeScheduled = true;
                Invoke(nameof(TryFreezeItem), freezeDelay);
            }
        }
    }

    private void OnCollisionExit(Collision collision) {
        touchingGround = false;
    }

    private void TryFreezeItem() {
        freezeScheduled = false;

        if (!touchingGround)
            return;

        if (itemRigidbody.velocity.magnitude > maxVelocityToFreeze ||
            itemRigidbody.angularVelocity.magnitude > maxAngularVelocityToFreeze) {
            freezeScheduled = true;
            Invoke(nameof(TryFreezeItem), freezeDelay);
            return;
        }

        itemRigidbody.constraints = RigidbodyConstraints.FreezeAll;
    }
}