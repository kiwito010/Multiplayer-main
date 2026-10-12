using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class DiagnosableBase : MonoBehaviour, IDiagnosable
{
    [SerializeField] private float scanTime = 2f;
    [SerializeField] private float scanLoseSpeed = 0.5f;

    private float currentScanTime;
    private bool isDiagnosed;
    private bool isBeingScanned;

    private void Update() {
        LoseScanProgress();
    }

    public void AddScanProgress(float amount) { 
        if (isDiagnosed)
            return;

        currentScanTime += amount;
        Debug.Log(gameObject.name + " progreso: " + GetScanProgress());

        if (currentScanTime >= scanTime) { 
            currentScanTime = scanTime;
            isDiagnosed = true;

            OnScanComplete();
        }
    }

    private void LoseScanProgress() {
        if (isDiagnosed || isBeingScanned)
            return;

        if (currentScanTime <= 0f)
            return;

        currentScanTime -= scanLoseSpeed * Time.deltaTime;
    }

    public void SetBeingScanned(bool isBeingScanned) { 
        this.isBeingScanned = isBeingScanned;
    }

    public float GetScanProgress() {
        return currentScanTime / scanTime;
    }

    public bool IsDiagnosed() {
        return isDiagnosed;
    }

    protected abstract void OnScanComplete();
}
