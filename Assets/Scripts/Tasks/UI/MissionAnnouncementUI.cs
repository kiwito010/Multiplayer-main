using System.Collections;
using TMPro;
using UnityEngine;

public class MissionAnnouncementUI : MonoBehaviour {
    [Header("Anuncio central")]
    [SerializeField] private GameObject announcementPanel;
    [SerializeField] private TextMeshProUGUI missionLabel;
    [SerializeField] private TextMeshProUGUI missionTitle;

    [Header("Duraciones")]
    [SerializeField] private float announcementDuration = 7f;
    

    private Coroutine announcementCoroutine;
    private Coroutine hintCoroutine;
    
    public bool IsAnnouncementVisible =>
        announcementPanel != null &&
        announcementPanel.activeInHierarchy;

    private void Awake() {
        if (announcementPanel != null)
            announcementPanel.SetActive(false);
    }

    public void ShowMission(string label, string title) {
        if (announcementPanel == null ||
            missionLabel == null ||
            missionTitle == null) {
            Debug.LogWarning(
                "[MissionAnnouncementUI] Faltan referencias del anuncio."
            );
            return;
        }

        if (announcementCoroutine != null)
            StopCoroutine(announcementCoroutine);

        missionLabel.text = label;
        missionTitle.text = title;

        announcementPanel.SetActive(true);

        announcementCoroutine = StartCoroutine(
            HideAnnouncementAfterDelay()
        );
    }
    private IEnumerator HideAnnouncementAfterDelay() {
        yield return new WaitForSecondsRealtime(
            announcementDuration
        );

        announcementPanel.SetActive(false);
        announcementCoroutine = null;
    }
    private void OnDisable() {
        if (announcementPanel != null)
            announcementPanel.SetActive(false);
    }

}
