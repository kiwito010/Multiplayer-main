using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StaminaUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private Slider staminaSlider;
    [SerializeField] private PlayerMovementSingle playerMovement;

    private void Update() {
        staminaSlider.value = playerMovement.GetStaminaNormalized();

        content.SetActive(playerMovement.ShouldShowStaminaUI());
    }
}
