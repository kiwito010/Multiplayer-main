using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IDiagnosable {
    void AddScanProgress(float amount);
    void LoseScanProgress(float amount);

    float GetScanProgress();
    bool IsDiagnosed();

    void OnScanComplete();
}
