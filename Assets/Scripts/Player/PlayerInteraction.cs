using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask interactLayerMask;

    private GameInput gameInput;

    private float interactionDistance = 2f;

    public event EventHandler<InteractEventArgs> OnInteract;

    private void Awake() {
        gameInput = GetComponent<GameInput>();  
    }

    private void Update() {
        if (gameInput.GetInteractPressed()) {
            if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit raycastHit, interactionDistance, interactLayerMask)) {
                if (raycastHit.transform.TryGetComponent<IInteractable>(out IInteractable interactable)) { 
                    OnInteract?.Invoke(this, new InteractEventArgs(interactable));
                }
            }
        }
    }
}
