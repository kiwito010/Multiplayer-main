using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoolingDiagnostic : MonoBehaviour, IDiagnosable {
    private bool isDiagnosed;

    public void OnScanComplete() {
        if (isDiagnosed)
            return;

        isDiagnosed = true;

        Debug.Log("Cooling diagnosticado");
    }

    public bool IsDiagnosed() { 
        return isDiagnosed;
    }
}
