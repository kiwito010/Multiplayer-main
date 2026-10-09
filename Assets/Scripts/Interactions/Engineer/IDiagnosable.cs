using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDiagnosable {
    void OnScanComplete();
    bool IsDiagnosed();
}
