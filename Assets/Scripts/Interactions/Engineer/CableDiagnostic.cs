using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableDiagnostic : DiagnosableBase
{
    protected override void OnScanComplete() {
        Debug.Log("Cable diagnosticado");
    }
}
