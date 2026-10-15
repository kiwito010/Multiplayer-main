using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableDiagnostic : DiagnosableBase
{
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData cableTask;

    protected override void OnScanComplete() {
        Debug.Log("Cable diagnosticado");

        taskManager.AddTask(cableTask);
    }
}
