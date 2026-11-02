using Fusion;
using UnityEngine;

public class CooperativeActivation : MonoBehaviour {
    // REFERENCIAS:

    [SerializeField] private RepairableComponent repairableComponent;
    [SerializeField] private FuseBox fuseBox;

    [SerializeField] private CooperativeButton button1;
    [SerializeField] private CooperativeButton button2;
    [SerializeField] private CooperativeButton button3;

    // MÉTODOS:

    public bool CanPress(PlayerRef player, CooperativeButton button) {
        if (repairableComponent == null || fuseBox == null)
            return false;

        if (!repairableComponent.Object.HasStateAuthority)
            return false;

        if (repairableComponent.IsRepaired())
            return false;

        if (!repairableComponent.cableRepairCompleted || !fuseBox.fuseInstalled)
            return false;

        if (button1 == null || button2 == null || button3 == null)
            return false;

        if (player == PlayerRef.None)
            return false;

        // Un jugador no puede mantener dos botones activos.
        CooperativeButton[] buttons = { button1, button2, button3 };

        foreach (CooperativeButton otherButton in buttons) {
            if (otherButton != button &&
                otherButton.IsPressed() &&
                otherButton.pressedBy == player)
                return false;
        }

        return true;
    }

    public void TryComplete() {
        if (repairableComponent == null || fuseBox == null)
            return;

        if (!repairableComponent.Object.HasStateAuthority)
            return;

        if (repairableComponent.IsRepaired())
            return;

        if (!repairableComponent.cableRepairCompleted || !fuseBox.fuseInstalled)
            return;

        if (button1 == null || button2 == null || button3 == null)
            return;

        if (!button1.IsPressed() ||
            !button2.IsPressed() ||
            !button3.IsPressed())
            return;

        PlayerRef player1 = button1.pressedBy;
        PlayerRef player2 = button2.pressedBy;
        PlayerRef player3 = button3.pressedBy;

        if (player1 == PlayerRef.None ||
            player2 == PlayerRef.None ||
            player3 == PlayerRef.None)
            return;

        if (player1 == player2 ||
            player1 == player3 ||
            player2 == player3)
            return;

        repairableComponent.Repair();

        Debug.Log("MISIÓN 1 COMPLETADA: GENERADOR REPARADO");
    }

    public bool IsActivationCompleted() {
        return repairableComponent != null &&
               repairableComponent.IsRepaired();
    }
}
