using Fusion;
using UnityEngine;

public class RepairableComponent : DiagnosableBase, IInteractable {
    [SerializeField] private string componentName;

    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData repairTask;

    [SerializeField] private MonoBehaviour repairMinigameBehaviour;

    [Networked] public NetworkBool cableInstalled { get; private set; }


    [Networked] public int repairPhaseIndex { get; private set; }

    [Networked] public NetworkBool leftCut { get; private set; }
    [Networked] public NetworkBool rightCut { get; private set; }

    public void RegisterCableCut(bool isLeft) {
        if (!Object.HasStateAuthority)
            return;

        if (!IsDiagnosed() || isRepaired)
            return;

        if (cableInstalled)
            return;

        if (isLeft) {
            leftCut = true;
        } else {
            rightCut = true;
        }

        if (leftCut && rightCut) {
            repairPhaseIndex = 1;
        }
    }
    [Networked] public NetworkBool brokenCableRemoved { get; private set; }

    public void RegisterBrokenCableRemoved() {
        if (!Object.HasStateAuthority)
            return;

        if (!IsDiagnosed() || isRepaired)
            return;

        if (!leftCut || !rightCut)
            return;

        if (brokenCableRemoved)
            return;

        brokenCableRemoved = true;
        repairPhaseIndex = 2;
    }

    [Networked] public NetworkBool cableTwistedLeft { get; private set; }
    [Networked] public NetworkBool cableTwistedRight { get; private set; }

    public void RegisterCableTwist(bool isLeft) {
        if (!Object.HasStateAuthority)
            return;

        if (!IsDiagnosed() || isRepaired)
            return;

        if (!cableInstalled || repairPhaseIndex != 3)
            return;

        if (isLeft) {
            cableTwistedLeft = true;
        } else {
            cableTwistedRight = true;
        }

        if (cableTwistedLeft && cableTwistedRight) {
            repairPhaseIndex = 4;
        }
    }

    [Networked] public NetworkBool leftWrapped { get; private set; }
    [Networked] public NetworkBool rightWrapped { get; private set; }

    private IRepairMinigame repairMinigame;
    private bool isRepaired;

    public void Interact() { 
        
    }

    private void Awake() {
        repairMinigame = repairMinigameBehaviour as IRepairMinigame;
    }

    protected override void OnScanComplete() {
        Debug.Log(componentName + " diagnosticado");

        taskManager.AddTask(repairTask);
    }

    public void Repair() {
        if (isRepaired)
            return;

        isRepaired = true;

        taskManager.CompleteTask(repairTask);

        Debug.Log(componentName + " reparado");
    }

    public bool IsRepaired() {
        return isRepaired;
    }

    public void OpenRepairMinigame(PlayerInventory inventory) {
        Debug.Log("INTENTANDO ABRIR MINIJUEGO");
        if (!IsDiagnosed()) {
            Debug.Log("Este componente todavia no fue diagnosticado");
            return;
        }

        if (isRepaired)
            return;

        if (repairMinigame == null) {
            Debug.LogError("No hay un minijuego de reparacion asignado");
            return;
        }

        repairMinigame.Open(this, inventory);
    }

    public bool TryInstallCable(NetworkInventoryState inventory) {
        if (!Object.HasStateAuthority)
            return false;

        if (!IsDiagnosed() || isRepaired || cableInstalled)
            return false;

        if (inventory == null || !inventory.Object.HasStateAuthority)
            return false;

        if (!inventory.TryConsumeItem(ItemType.Cable, out NetworkObject cable))
            return false;

        cableInstalled = true;
        repairPhaseIndex = 3;

        // El material ya fue consumido: lo retiramos de la partida.
        Runner.Despawn(cable);

        Debug.Log("Cable nuevo instalado");

        return true;
    }

    //Cual es el jugador esta reparando el cable
    [Networked] public PlayerRef repairingPlayer { get; private set; }

    public bool TryBeginRepair(PlayerRef player) {
        if (!Object.HasStateAuthority)
            return false;

        if (player == PlayerRef.None || isRepaired)
            return false;

        if (repairingPlayer != PlayerRef.None)
            return false;

        repairingPlayer = player;
        return true;
    }

    //Desvincular el jugador que esta arreglando el motor del estado repairingPlayer
    public void EndRepair(PlayerRef player) {
        if (!Object.HasStateAuthority)
            return;

        if (repairingPlayer != player)
            return;

        repairingPlayer = PlayerRef.None;
    }
}