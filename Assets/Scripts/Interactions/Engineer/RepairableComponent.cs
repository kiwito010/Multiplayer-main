using UnityEngine;

public class RepairableComponent : DiagnosableBase, IInteractable {
    [SerializeField] private string componentName;

    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData repairTask;

    [SerializeField] private MonoBehaviour repairMinigameBehaviour;

    private IRepairMinigame repairMinigame;
    private bool isRepaired;

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

    public void Interact() { 
        
    }
}