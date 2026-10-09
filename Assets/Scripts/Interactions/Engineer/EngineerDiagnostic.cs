using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineerDiagnostic : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private LayerMask diagnosticLayerMask;
    [SerializeField] private float diagnosticDistance = 2f;
    [SerializeField] private float scanTime = 2f;

    private float currentScanTime;
    private IDiagnosable currentDiagnosable;

    private void Update() {
        if (Physics.Raycast(
            cameraTransform.position,
            cameraTransform.forward,
            out RaycastHit raycastHit,
            diagnosticDistance,
            diagnosticLayerMask)) { 
            
        }
    }
}
