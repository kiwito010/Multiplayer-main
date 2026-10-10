using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineerDiagnostic : MonoBehaviour
{
    //VARIABLES:
    //Referencias
    private GameInput gameInput;
    [SerializeField] private Camera playerCamera;

    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float diagnosticFov = 35f;

    //otras
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask diagnosticLayerMask;
    [SerializeField] private float diagnosticDistance = 2f;
    [SerializeField] private float scanTime = 5;
    [SerializeField] private float scanLoseSpeed = 1f;

    private float currentScanTime;
    private IDiagnosable currentDiagnosable;

    private void Awake() {
        gameInput = GetComponent<GameInput>();
    }

    private void Update() {
        if (!gameInput.GetDiagnosticPressed()) { 
            playerCamera.fieldOfView = normalFov;
            LoseScanProgress();
            return;
        }

        playerCamera.fieldOfView = diagnosticFov;

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit raycastHit, diagnosticDistance, diagnosticLayerMask)) {
            if (raycastHit.transform.TryGetComponent<IDiagnosable>(out IDiagnosable diagnosable)) { 
                Scan(diagnosable);
            }
        }
        LoseScanProgress();
    }

    private void Scan(IDiagnosable diagnosable) {
        if (diagnosable.IsDiagnosed()) {
            return;
        }

        if (currentDiagnosable != diagnosable) { 
            currentDiagnosable = diagnosable;
            currentScanTime = 0;
        }

        currentScanTime += Time.deltaTime;

        if (currentScanTime >= scanTime) { 
            currentScanTime = scanTime;
            diagnosable.OnScanComplete();
        }
    }

    private void ResetScan() {
        currentScanTime = 0f;
        currentDiagnosable = null;
    }

    private void LoseScanProgress() {
        if (currentScanTime > 0f) { 
            currentScanTime -= scanLoseSpeed * Time.deltaTime;

            if (currentScanTime < 0f) {
                currentScanTime = 0f; 
                currentDiagnosable = null;
            }
        }
    }
}
