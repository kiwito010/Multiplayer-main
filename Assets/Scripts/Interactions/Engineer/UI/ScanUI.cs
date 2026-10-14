using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ScanUI : MonoBehaviour
{
    [SerializeField] private GameObject content;
    [SerializeField] private GameObject crosshair;
    [SerializeField] private GameObject scanFeedback;

    [SerializeField] private Image progressRing;
    [SerializeField] private TextMeshProUGUI scanText;

    [SerializeField] private float completeDelay = 0.2f;
    private bool isScanComplete;
    private bool isCompleting;

    public void ShowCrosshair() { 
        content.SetActive(true);
        crosshair.SetActive(true);
        scanFeedback.SetActive(false);
    }

    public void ShowScanFeedback(float progress, bool isDiagnosed) { 
        content.SetActive(true);
        crosshair.SetActive(true);
        scanFeedback.SetActive(true);

        progressRing.fillAmount = progress;

        if (isDiagnosed) { 
            ShowCompleteState();
            return;
        }

        if (isScanComplete)
            return;

        int percentage = Mathf.RoundToInt(progress * 100f);
        scanText.text = "SCANNING " + percentage + "%";

        if (progress >= 1f && !isCompleting) {
            isCompleting = true;
            StartCoroutine(ShowCompleteRoutine());
        }
    }

    private IEnumerator ShowCompleteRoutine() { 
        yield return new WaitForSeconds(completeDelay);
        
        ShowCompleteState();
        isCompleting = false;
    }

    private void ShowCompleteState() { 
        isScanComplete = true;
        progressRing.fillAmount = 1f;
        scanText.text = "SCAN COMPLETE";
    }

    public void ResetScanFeedback() { 
        StopAllCoroutines();
        isScanComplete = false;
        isCompleting = false;
    }

    public void HideAll() {
        content.SetActive(false);
    }
}
