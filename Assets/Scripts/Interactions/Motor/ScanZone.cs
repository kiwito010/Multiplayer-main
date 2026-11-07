using UnityEngine;

public class ScanZone : MonoBehaviour {
    [SerializeField] private DiagnosableBase target;

    public IDiagnosable GetTarget() {
        return target;
    }
}