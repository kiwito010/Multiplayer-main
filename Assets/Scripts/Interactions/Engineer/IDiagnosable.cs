using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDiagnosable {
    void AddScanProgress(float amount);
    void SetBeingScanned(bool isBeingScanned);

    float GetScanProgress();
    bool IsDiagnosed();
}
