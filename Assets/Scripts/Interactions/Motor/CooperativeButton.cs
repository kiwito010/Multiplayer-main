using Fusion;
using UnityEngine;

public class CooperativeButton : NetworkBehaviour {
    // REFERENCIAS:
    //otros scripts
    [SerializeField] private CooperativeActivation cooperativeActivation;
    //

    [SerializeField] private GameObject normalVisual;
    [SerializeField] private GameObject pressedVisual;

    [SerializeField] private float pressDuration = 0.5f;
    [SerializeField] private float interactionDistance = 3f;

    // ESTADO COMPARTIDO:

    [Networked] public TickTimer pressTimer { get; private set; }
    [Networked] public PlayerRef pressedBy { get; private set; }

    // MÉTODOS:

    public override void Spawned() {
        if (Object.HasStateAuthority) {
            PlayerNetworkInteraction.OnUsedOnTarget += OnUsedOnTarget;
        }

        UpdateVisuals();
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= OnUsedOnTarget;
    }

    private void OnUsedOnTarget(PlayerRef player, NetworkObject target) {
        if (!Object.HasStateAuthority)
            return;

        if (target != Object)
            return;

        if (IsPressed())
            return;

        if (cooperativeActivation == null || !cooperativeActivation.CanPress(player, this))
            return;

        if (!Runner.TryGetPlayerObject(player, out NetworkObject playerObject))
            return;

        if (playerObject == null)
            return;

        float distance = Vector3.Distance(
            playerObject.transform.position,
            transform.position
        );

        if (distance > interactionDistance)
            return;

        pressedBy = player;
        pressTimer = TickTimer.CreateFromSeconds(Runner, pressDuration);

        cooperativeActivation.TryComplete();

        Debug.Log("Botón presionado por " + player);
    }

    public bool IsPressed() {
        return Runner != null &&
               !pressTimer.ExpiredOrNotRunning(Runner);
    }

    public bool IsActivationCompleted() {
        return cooperativeActivation != null &&
               cooperativeActivation.IsActivationCompleted();
    }

    private bool ShouldShowPressedVisual() {
        return IsPressed() ||
               (cooperativeActivation != null &&
                cooperativeActivation.IsActivationCompleted());
    }

    public override void Render() {
        UpdateVisuals();
    }

    private void UpdateVisuals() {
        bool pressed = ShouldShowPressedVisual();

        if (normalVisual != null) {
            normalVisual.SetActive(!pressed);
        }

        if (pressedVisual != null) {
            pressedVisual.SetActive(pressed);
        }
    }
}
