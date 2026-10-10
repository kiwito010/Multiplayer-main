using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolingDiagnostic : DiagnosableBase {

    protected override void OnScanComplete() {
        Debug.Log("Cooling diagnosticado");
    }
}
