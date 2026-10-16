using UnityEngine;

public class RepairableComponent : DiagnosableBase, IInteractable {
    [SerializeField] private string componentName;

    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData repairTask;

    [SerializeField] private string requiredItemName;

    private bool isRepaired;

    protected override void OnScanComplete() {
        Debug.Log(componentName + " diagnosticado");

        taskManager.AddTask(repairTask);
    }

    public bool CanRepairWith(PickableItem item) {
        if (!IsDiagnosed())
            return false;

        if (isRepaired)
            return false;

        if (item == null)
            return false;

        return item.GetItemName() == requiredItemName;
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

    public void Interact() { 
        
    }
}