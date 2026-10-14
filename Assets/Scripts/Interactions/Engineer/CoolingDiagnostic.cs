using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolingDiagnostic : DiagnosableBase {
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData  collingTask;


    protected override void OnScanComplete() {
        Debug.Log("Cooling diagnosticado");

        taskManager.AddTask(collingTask);
    }
}
