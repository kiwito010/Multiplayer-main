using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private PlayerMovementSingle playerMovement;

    private bool forceHidden;

    private void Update() {
        staminaSlider.value = playerMovement.GetStaminaNormalized();

        if (forceHidden)
            return;

        content.SetActive(playerMovement.ShouldShowStaminaUI());
    }

    public void Hide() { 
        forceHidden = true;
        content.SetActive(false);
    }

    public void EnableNormalVisibility() { 
        forceHidden = false;
        content.SetActive(playerMovement.ShouldShowStaminaUI());
    }
}
