using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

// Barra de stamina. Funciona en las dos versiones del juego:
//  - Escenas de un jugador: arrastrá el PlayerMovementSingle en el campo opcional.
//  - Multijugador: queda vacío y usa solo al jugador local (PlayerMovement.Local).
public class StaminaUI : MonoBehaviour
{
    [SerializeField] private GameObject content; // Tiene que ser un HIJO, no este mismo objeto
    [SerializeField] private Slider staminaSlider;

    [FormerlySerializedAs("playerMovement")]
    [SerializeField] private PlayerMovementSingle playerMovementSingle; // opcional

    // Visibilidad forzada desde afuera (por ejemplo, Computer.cs al usar la terminal).
    private bool _forceHidden;

    // Vuelve al comportamiento normal: la barra se muestra solo cuando corresponde.
    public void EnableNormalVisibility()
    {
        _forceHidden = false;
    }

    // Oculta la barra hasta que se llame a EnableNormalVisibility().
    public void ForceHide()
    {
        _forceHidden = true;
        content.SetActive(false);
    }

    private void Update()
    {
        if (_forceHidden) return;

        float normalized;
        bool show;

        if (playerMovementSingle != null)
        {
            normalized = playerMovementSingle.GetStaminaNormalized();
            show = playerMovementSingle.ShouldShowStaminaUI();
        }
        else if (PlayerMovement.Local != null)
        {
            normalized = PlayerMovement.Local.GetStaminaNormalized();
            show = PlayerMovement.Local.ShouldShowStaminaUI();
        }
        else
        {
            content.SetActive(false);
            return;
        }

        staminaSlider.value = normalized;
        content.SetActive(show);
    }
}