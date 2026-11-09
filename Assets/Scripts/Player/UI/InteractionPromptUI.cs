
using Fusion;
using TMPro;
using UnityEngine;

public class InteractionPromptUI : MonoBehaviour {
    [Header("Interfaz")]
    [SerializeField] private GameObject promptPanel;
    [SerializeField] private TextMeshProUGUI promptText;

    [Header("Estabilidad")]
    [SerializeField] private float hideDelay = 0.12f;

    [Header("Colores")]
    [SerializeField] private Color normalTextColor = Color.white;
    [SerializeField] private Color busyTextColor = Color.gray;
    [SerializeField]
    private Color blockedTextColor = new Color(1f, 0.35f, 0.35f, 1f);

    private PlayerNetworkInteraction playerInteraction;
    private MissionAnnouncementUI missionAnnouncementUI;
    private float lastDetectedTime;

    private PickableOutline currentOutline;

    public void Initialize(PlayerNetworkInteraction interaction) {
        playerInteraction = interaction;

        PlayerHUD hud = GetComponentInParent<PlayerHUD>();

        if (hud != null) {
            missionAnnouncementUI =
                hud.GetComponentInChildren<MissionAnnouncementUI>(true);
        }

        HidePrompt();
    }

    private void Update() {
        if (promptPanel == null)
            return;

        // Ocultar el texto y el contorno durante
        // minijuegos, menús y anuncios de misión.
        if (playerInteraction == null || IsUIModeActive()) {
            HidePrompt();
            return;
        }

        bool detected = playerInteraction.TryGetLookTarget(
            out NetworkObject target
        );

        if (detected && target != null) {
            // Si la interacción ya fue completada,
            // ocultar el texto y el contorno.
            if (ShouldHideBecauseCompleted(target)) {
                HidePrompt();
                return;
            }

            lastDetectedTime = Time.unscaledTime;

            // Activar el contorno del objeto apuntado.
            UpdateOutline(target);

            // Actualizar el texto y su color.
            UpdatePromptText(target);

            if (!promptPanel.activeSelf)
                promptPanel.SetActive(true);
        } else {
            // El contorno desaparece inmediatamente
            // cuando dejamos de apuntar al objeto.
            UpdateOutline(null);

            // El texto mantiene el retraso de 0.12
            // segundos para evitar el titileo.
            if (promptPanel.activeSelf &&
                Time.unscaledTime - lastDetectedTime > hideDelay) {
                HidePrompt();
            }
        }
    }

    private void UpdatePromptText(NetworkObject target) {
        if (promptText == null)
            return;

        // 1. OCUPADO: gris.
        if (TryGetBusyMessage(target, out string busyMessage)) {
            promptText.text = busyMessage;
            promptText.color = busyTextColor;
            return;
        }

        // 2. BLOQUEADO POR MISIÓN: rojo.
        if (TryGetMissionBlockedMessage(
            target, out string blockedMessage)) {
            promptText.text = blockedMessage;
            promptText.color = blockedTextColor;
            return;
        }

        // 3. DISPONIBLE: blanco.
        InteractionPromptTarget promptTarget =
            target.GetComponentInChildren<InteractionPromptTarget>(true);

        string interactionText = "Interactuar";

        if (promptTarget != null)
            interactionText = promptTarget.GetInteractionText();

        promptText.text = "E · " + interactionText;
        promptText.color = normalTextColor;
    }

    private bool ShouldHideBecauseCompleted(NetworkObject target) {
        if (target == null)
            return false;

        // Caja de fusibles: dejar de mostrar una vez instalado.
        FuseBox fuseBox =
            target.GetComponentInChildren<FuseBox>(true);

        if (fuseBox != null &&
            fuseBox.Object != null &&
            fuseBox.Object.IsValid &&
            fuseBox.fuseInstalled) {
            return true;
        }

        // Botones: dejar de mostrar cuando los tres
        // jugadores completaron la activación.
        CooperativeButton button =
            target.GetComponentInChildren<CooperativeButton>(true);

        if (button != null && button.IsActivationCompleted())
            return true;

        // Computadora: dejar de mostrar si ya fue hackeada.
        NetworkComputer computer =
            target.GetComponentInChildren<NetworkComputer>(true);

        if (computer != null &&
            computer.Object != null &&
            computer.Object.IsValid &&
            computer.IsCompleted) {
            return true;
        }

        return false;
    }

    private bool TryGetBusyMessage(
        NetworkObject target,
        out string message) {
        message = null;

        if (target == null || playerInteraction == null)
            return false;

        if (playerInteraction.Object == null ||
            !playerInteraction.Object.IsValid)
            return false;

        PlayerRef localPlayer =
            playerInteraction.Object.InputAuthority;

        NetworkComputer computer =
            target.GetComponentInChildren<NetworkComputer>(true);

        if (computer != null && computer.IsBusyFor(localPlayer)) {
            message = "Computadora en uso";
            return true;
        }

        RepairableComponent generator =
            target.GetComponentInChildren<RepairableComponent>(true);

        if (generator != null && generator.IsBusyFor(localPlayer)) {
            message = "Generador en uso";
            return true;
        }

        return false;
    }

    private bool TryGetMissionBlockedMessage(
        NetworkObject target,
        out string message) {
        message = null;

        if (target == null)
            return false;

        // El depósito solo se habilita durante el hackeo.
        NetworkCoolantDeposit deposit =
            target.GetComponentInChildren<NetworkCoolantDeposit>(true);

        if (deposit != null) {
            if (!deposit.IsHackActive()) {
                message = "Esperando el inicio del hackeo";
                return true;
            }

            return false;
        }

        InteractionMissionGate gate =
            target.GetComponentInChildren<InteractionMissionGate>(true);

        if (gate == null)
            return false;

        return gate.TryGetBlockedMessage(out message);
    }

    private bool IsUIModeActive() {
        // No mostrar durante minijuegos o menús
        // que liberan el cursor.
        if (Cursor.lockState != CursorLockMode.Locked)
            return true;

        // No mostrar durante anuncios de misión.
        if (missionAnnouncementUI != null &&
            missionAnnouncementUI.IsAnnouncementVisible)
            return true;

        return false;
    }

    private void HidePrompt() {
        if (promptPanel != null && promptPanel.activeSelf)
            promptPanel.SetActive(false);

        if (currentOutline != null) {
            currentOutline.SetHighlighted(false);
            currentOutline = null;
        }
    }

    private void UpdateOutline(NetworkObject target) {
        PickableOutline newOutline = null;

        if (target != null) {
            newOutline = target.GetComponentInChildren<PickableOutline>(true);
        }

        if (currentOutline == newOutline)
            return;

        if (currentOutline != null)
            currentOutline.SetHighlighted(false);

        currentOutline = newOutline;

        if (currentOutline != null)
            currentOutline.SetHighlighted(true);
    }
}
