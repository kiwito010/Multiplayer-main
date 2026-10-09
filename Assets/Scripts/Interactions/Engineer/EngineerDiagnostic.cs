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

    private float currentScanTime;
    private IDiagnosable currentDiagnosable;

    private void Awake() {
        gameInput = GetComponent<GameInput>();
    }

    private void Update() {
        if (!gameInput.GetDiagnosticPressed()) { 
            playerCamera.fieldOfView = normalFov;
            ResetScan();
            return;
        }

        playerCamera.fieldOfView = diagnosticFov;

        if (Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit raycastHit,
            diagnosticDistance,
            diagnosticLayerMask)) {
            if (raycastHit.transform.TryGetComponent<IDiagnosable>(out IDiagnosable diagnosable)) { 
                Scan(diagnosable);
            }
        }
    }

    private void Scan(IDiagnosable diagnosable) {
        if (diagnosable.IsDiagnosed()) {
            ResetScan();
            return;
        }

        if (currentDiagnosable != diagnosable) { 
            currentDiagnosable = diagnosable;
            currentScanTime = 0;
        }

        currentScanTime += Time.deltaTime;

        if (currentScanTime >= scanTime) { 
            diagnosable.OnScanComplete();
            ResetScan();
        }
    }

    private void ResetScan() {
        currentScanTime = 0f;
        currentDiagnosable = null;
    }
}
