using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public abstract class DiagnosableBase : NetworkBehaviour, IDiagnosable
{
    [SerializeField] private float scanTime = 2f;
    [SerializeField] private float scanLoseSpeed = 0.5f;

    [Networked] private float currentScanTime { get; set; }
    [Networked] private NetworkBool isDiagnosed { get; set; }
    [Networked] private NetworkBool isBeingScanned { get; set; }

    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority)
            return;

        LoseScanProgress();
    }

    public void AddScanProgress(float amount) {
        if (!Object.HasStateAuthority)
            return;

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
        if (!Object.HasStateAuthority)
            return;
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
