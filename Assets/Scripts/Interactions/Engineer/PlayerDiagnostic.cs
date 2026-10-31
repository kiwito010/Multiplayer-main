using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerDiagnostic : MonoBehaviour
{
    //VARIABLES:
    //Referencias
    private GameInput gameInput;

        //Fusion
        private NetworkObject networkObject;
        private NetworkInventoryState networkInventory;
        private PlayerInventory playerInventory;
        //

    [SerializeField] private Camera playerCamera;
    [SerializeField] private Transform cameraTransform;

    private ScanUI scanUI;
    
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
        
        //Fusion
        networkObject = GetComponent<NetworkObject>();
        networkInventory = GetComponent<NetworkInventoryState>();
        playerInventory = GetComponent<PlayerInventory>();
        //
    }

    private void Update() {
        if (networkObject == null || !networkObject.IsValid || !networkObject.HasInputAuthority)
            return;

        if (scanUI == null || playerCamera == null || cameraTransform == null)
            return;

        if (!HasMagnifyingGlassEquipped() || !gameInput.GetDiagnosticPressed()) {
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

    public void Initialize(ScanUI scanUI) {
        this.scanUI = scanUI;

        if (scanUI != null) {
            scanUI.HideAll();
        }
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

    private bool HasMagnifyingGlassEquipped() {
        if (networkInventory == null || playerInventory == null)
            return false;

        int selectedSlot = playerInventory.GetSelectedSlot();

        if (selectedSlot < 0)
            return false;

        NetworkId itemId = networkInventory.GetItemIdInSlot(selectedSlot);

        if (itemId == default(NetworkId))
            return false;

        if (!networkInventory.Runner.TryFindObject(itemId, out NetworkObject itemObject))
            return false;

        NetworkPickableItem item = itemObject.GetComponent<NetworkPickableItem>();

        if (item == null)
            return false;

        return item.GetItemType() == ItemType.MagnifyingGlass;
    }

}
