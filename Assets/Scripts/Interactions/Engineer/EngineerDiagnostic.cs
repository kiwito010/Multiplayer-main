using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineerDiagnostic : MonoBehaviour
{
    //VARIABLES:
    //Referencias
    private GameInput gameInput;
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraTransform;

    [SerializeField] private ScanUI scanUI;
    
    //FOV
    [SerializeField] private float normalFov = 60f;
    [SerializeField] private float diagnosticFov = 35f;

    //otras
    [SerializeField] private LayerMask diagnosticLayerMask;
    [SerializeField] private float diagnosticDistance = 2f;
    
    [SerializeField] private float scanLoseSpeed = 1f;

    //Lupa
    private bool hasMagnifyingGlass;

    private IDiagnosable currentDiagnosable;

    public event System.Action<bool> OnMagnifyingGlassStateChanged;


    private void Awake() {
        gameInput = GetComponent<GameInput>();
    }

    private void Update() {
        if (!hasMagnifyingGlass || !gameInput.GetDiagnosticPressed()) { 
            playerCamera.fieldOfView = normalFov;
            StopCurrentScan();
            scanUI.HideAll();
            return;
        }

        playerCamera.fieldOfView = diagnosticFov;
        scanUI.ShowCrosshair();

        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out RaycastHit raycastHit, diagnosticDistance, diagnosticLayerMask)) {
            if (raycastHit.transform.TryGetComponent<IDiagnosable>(out IDiagnosable diagnosable)) { 
                Scan(diagnosable);
                return;
            }
        }
        StopCurrentScan();
    }

    private void Scan(IDiagnosable diagnosable) {
        if (currentDiagnosable != diagnosable) {
            StopCurrentScan();

            currentDiagnosable = diagnosable;
            currentDiagnosable.SetBeingScanned(true);

            if (!currentDiagnosable.IsDiagnosed()) {
                scanUI.ResetScanFeedback();
            }
        }
        currentDiagnosable.AddScanProgress(Time.deltaTime);

        scanUI.ShowScanFeedback(currentDiagnosable.GetScanProgress(), currentDiagnosable.IsDiagnosed());
    }

    private void StopCurrentScan() {
        if (currentDiagnosable == null)
            return;

        currentDiagnosable.SetBeingScanned(false);
        currentDiagnosable = null;
    }

    public void GiveMagnifyingGlass() { 
        hasMagnifyingGlass = true;
        OnMagnifyingGlassStateChanged?.Invoke(true);

        Debug.Log("ENGINEER AHORA TIENE LUPA");
    }

    public bool HasMagnifyingGlass() {
        return hasMagnifyingGlass;
    }
}
