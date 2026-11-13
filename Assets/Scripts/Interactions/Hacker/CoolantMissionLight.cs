using UnityEngine;

public class CoolantMissionLight : MonoBehaviour {

    [Header("Referencias")]
    [SerializeField] private MissionItemLight missionItemLight;

    private NetworkComputer networkComputer;
    private bool? lastState;

    private void Start() {
        networkComputer = FindFirstObjectByType<NetworkComputer>();
    }

    private void Update() {
        if (networkComputer == null) {
            networkComputer = FindFirstObjectByType<NetworkComputer>();
            return;
        }

        if (missionItemLight == null)
            return;

        if (networkComputer.Object == null ||
            !networkComputer.Object.IsValid)
            return;

        bool shouldHighlight =
            networkComputer.IsHackRunning &&
            !networkComputer.IsCompleted;

        if (lastState.HasValue &&
            lastState.Value == shouldHighlight)
            return;

        lastState = shouldHighlight;

        missionItemLight.SetHighlighted(shouldHighlight);
    }

    private void OnDisable() {
        if (missionItemLight != null)
            missionItemLight.SetHighlighted(false);

        lastState = null;
    }
}
