using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerInteraction : MonoBehaviour
{
    //Fusion
    private NetworkObject networkObject;
    //

    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask interactLayerMask;
    [SerializeField] private PlayerInventory playerInventory;

    private GameInput gameInput;

    private float interactionDistance = 2f;

    public event EventHandler<InteractEventArgs> OnInteract;

    private void Awake() {
        networkObject = GetComponent<NetworkObject>();

        gameInput = GetComponent<GameInput>();  
    }

    private void Update() {
        if (networkObject == null || !networkObject.IsValid || !networkObject.HasInputAuthority)
            return;

        if (gameInput.GetInteractPressed()) {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit raycastHit, interactionDistance, interactLayerMask)) {
                if (raycastHit.transform.TryGetComponent<IInteractable>(out IInteractable interactable)) { 
                    OnInteract?.Invoke(this, new InteractEventArgs(interactable, playerInventory));
                }
            }
        }
    }
}
