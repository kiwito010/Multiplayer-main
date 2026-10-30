
using Fusion;
using UnityEngine;

public class NetworkPickableItem : NetworkBehaviour {
    // VARIABLES:

    [SerializeField] private GameObject itemVisual;

    [SerializeField] private Collider itemCollider;

    private Rigidbody itemRigidbody;

    [Networked] private NetworkBool isPickedUp { get; set; }
    [Networked] private PlayerRef pickedBy { get; set; }

    // EVENTOS:
    private void Awake() {
        itemRigidbody = GetComponent<Rigidbody>();
    }



    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += PlayerNetworkInteraction_OnUsedOnTarget;

        UpdateVisual();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= PlayerNetworkInteraction_OnUsedOnTarget;
    }


    private void PlayerNetworkInteraction_OnUsedOnTarget(PlayerRef player, NetworkObject target) {
        // Solo la autoridad puede modificar el objeto.
        if (!Object.HasStateAuthority)
            return;

        // Comprobamos que se interactuó con este objeto.
        if (target != Object)
            return;

        // Evitamos recoger el mismo objeto dos veces.
        if (isPickedUp)
            return;

        // Buscamos el personaje que realizó la interacción.
        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return;

        // Buscamos su inventario de red.
        NetworkInventoryState inventory =
            playerObject.GetComponent<NetworkInventoryState>();

        if (inventory == null) {
            Debug.LogWarning("El jugador no tiene NetworkInventoryState");
            return;
        }

        // Intentamos guardar el objeto.
        if (!inventory.TryAddItem(Object)) {
            Debug.Log("El inventario está lleno");
            return;
        }

        // Confirmamos la recogida.
        pickedBy = player;
        isPickedUp = true;

        SetPhysicsEnabled(false);

        Debug.Log("Objeto guardado en el inventario de " + player);
    }


    public override void Render() {
        UpdateVisual();
    }

    private void UpdateVisual() {
        if (itemVisual != null) {
            itemVisual.SetActive(!isPickedUp);
        }

        if (itemCollider != null) {
            itemCollider.enabled = !isPickedUp;
        }
    }

    public GameObject GetItemVisual() {
        return itemVisual;
    }


    public bool TryDrop(PlayerRef player, Vector3 dropPosition, Vector3 throwDirection, float throwForce) {
        if (!Object.HasStateAuthority)
            return false;

        if (!isPickedUp || pickedBy != player)
            return false;

        NetworkTransform networkTransform =
            GetComponent<NetworkTransform>();

        if (networkTransform == null || itemRigidbody == null) {
            Debug.LogWarning("Falta NetworkTransform o Rigidbody en el objeto");
            return false;
        }

        if (throwDirection.sqrMagnitude < 0.001f)
            return false;

        // Reubicamos el objeto mientras todavía está oculto.
        networkTransform.Teleport(dropPosition);

        // Reactivamos el objeto en el mundo.
        pickedBy = PlayerRef.None;
        isPickedUp = false;

        SetPhysicsEnabled(true);
        UpdateVisual();

        // Reiniciamos el movimiento anterior.
        itemRigidbody.velocity = Vector3.zero;
        itemRigidbody.angularVelocity = Vector3.zero;

        // Aplicamos el impulso desde la autoridad.
        itemRigidbody.AddForce(throwDirection.normalized * throwForce, ForceMode.Impulse);

        Debug.Log("Objeto lanzado por " + player);

        return true;
    }


    private void SetPhysicsEnabled(bool enabled) {
        if (!Object.HasStateAuthority || itemRigidbody == null)
            return;

        if (!enabled && !itemRigidbody.isKinematic) {
            itemRigidbody.velocity = Vector3.zero;
            itemRigidbody.angularVelocity = Vector3.zero;
        }

        itemRigidbody.isKinematic = !enabled;
        itemRigidbody.useGravity = enabled;
    }


}
