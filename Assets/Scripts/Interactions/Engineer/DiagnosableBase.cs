using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DiagnosableBase : MonoBehaviour, IDiagnosable
{
    [SerializeField] private float scanTime = 2f;

    private float currentScanTime;
    private bool isDiagnosed;

    public void AddScanProgress(float amount) { 
        if (isDiagnosed)
            return;

        currentScanTime += amount;

        if (currentScanTime >= scanTime) { 
            currentScanTime = scanTime;
            isDiagnosed = true;

            OnScanComplete();
        }
    }

    public void LoseScabProgress(float amount) {
        if (isDiagnosed)
            return;

        currentScanTime -= amount;

        if (currentScanTime < scanTime) { 
            currentScanTime = 0f;
        }
    }

    public float GetScanProgress() {
        return currentScanTime / scanTime;
    }

    public bool IsDiagnosed() {
        return isDiagnosed;
    }

    protected abstract void OnScanComplete();
}
