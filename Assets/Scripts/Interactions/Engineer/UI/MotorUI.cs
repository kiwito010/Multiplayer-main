using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotorUI : MonoBehaviour
{
    [SerializeField] private GameObject diagnosticPanel;

    public void Show() {
        gameObject.SetActive(true);
    }

    public void Hide() { 
        gameObject.SetActive(false);
    }

    public void ShowDiagnosticContent() { 
        diagnosticPanel.SetActive(true);
    }
}
