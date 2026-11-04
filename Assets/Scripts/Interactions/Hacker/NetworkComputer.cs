using Fusion;
using UnityEngine;

public class NetworkComputer : NetworkBehaviour {

    // ESTADOS DE RED:
    private ComputerUI localComputerUI;

    [Networked] public NetworkBool IsCompleted { get; private set; }

    [Networked] public PlayerRef ActiveHacker { get; private set; }

    [Header("Temperatura")]
    [SerializeField] private float phase1HeatPerSecond = 1f;

    [SerializeField] private float qteErrorHeat = 15f;

    [Networked] public float Temperature { get; private set; }

    [Networked] public NetworkBool IsHackRunning { get; private set; }

    [Header("Enfriamiento")]
    [SerializeField] private float overheatCooldown = 15f;
    [SerializeField] private float exitCooldown = 5f;

    [Networked] public TickTimer CooldownTimer { get; private set; }

    // MÉTODOS:

    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += HandleInteraction;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= HandleInteraction;
    }

    public override void FixedUpdateNetwork() {
        if (!Object.HasStateAuthority)
            return;

        // Comprobar si terminó el enfriamiento.
        if (CooldownTimer.Expired(Runner)) {
            FinishCooldown();
        }

        // No aumentar la temperatura si está apagada.
        if (CooldownTimer.IsRunning)
            return;

        if (!IsHackRunning || IsCompleted)
            return;

        Temperature = Mathf.Min(100f, Temperature + phase1HeatPerSecond * Runner.DeltaTime);

        // Comprobar sobrecalentamiento.
        if (Temperature >= 100f) {
            PlayerRef hacker = ActiveHacker;

            StartCooldown(overheatCooldown);

            RPC_ForceCloseComputer(hacker);
            return;
        }

        // Debug temporal.
        if (Runner.Tick % 60 == 0) {
            Debug.Log(
                "Temperatura de la computadora: " +
                Temperature.ToString("F1") + "%"
            );
        }
    }

    private void HandleInteraction(PlayerRef player, NetworkObject target) {
        if (!Object.HasStateAuthority)
            return;

        if (target != Object)
            return;

        if (IsCompleted)
            return;

        if (CooldownTimer.IsRunning)
            return;

        if (ActiveHacker != PlayerRef.None)
            return;

        ActiveHacker = player;

        Debug.Log("Jugador " + player + " comenzó a usar la computadora");

        RPC_OpenComputer(player);
    }

    public bool IsBeingUsed() {
        return ActiveHacker != PlayerRef.None;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_OpenComputer(PlayerRef hacker) {
        if (Runner.LocalPlayer != hacker)
            return;

        NetworkObject playerObject = Runner.GetPlayerObject(hacker);

        if (playerObject == null) {
            Debug.LogWarning("No se encontró el jugador local.");
            return;
        }

        PlayerHUDSpawner hudSpawner =
            playerObject.GetComponent<PlayerHUDSpawner>();

        if (hudSpawner == null) {
            Debug.LogWarning("PlayerHUDSpawner no encontrado.");
            return;
        }

        PlayerHUD hud = hudSpawner.GetHUD();

        if (hud == null) {
            Debug.LogWarning("PlayerHUD no encontrado.");
            return;
        }

        ComputerUI computerUI = hud.GetComputerUI();

        if (computerUI == null) {
            Debug.LogWarning("ComputerUI no asignado en PlayerHUD.");
            return;
        }

        if (localComputerUI != null) {
            localComputerUI.OnHackStarted -= HandleHackStarted;
            localComputerUI.OnHackError -= HandleHackError;
        }

        localComputerUI = computerUI;
        localComputerUI.OnHackStarted += HandleHackStarted;
        localComputerUI.OnHackError += HandleHackError;

        computerUI.ShowNetwork(this);

        Debug.Log("ComputerUI abierta para el jugador local.");
    }

    public void RequestExit() {
        if (Object.HasStateAuthority) {
            if (ActiveHacker != Runner.LocalPlayer)
                return;

            if (IsHackRunning && !IsCompleted) {
                StartCooldown(exitCooldown);
            } else {
                IsHackRunning = false;
                ActiveHacker = PlayerRef.None;
            }

            return;
        }

        RPC_RequestExit();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestExit(RpcInfo info = default) {
        if (!Object.HasStateAuthority)
            return;

        if (ActiveHacker != info.Source)
            return;

        if (IsHackRunning && !IsCompleted) {
            StartCooldown(exitCooldown);
        } else {
            IsHackRunning = false;
            ActiveHacker = PlayerRef.None;
        }

        Debug.Log("Jugador salió de la computadora");
    }

    public void RequestCompleteHack() {
        if (Object.HasStateAuthority) {
            if (ActiveHacker != Runner.LocalPlayer)
                return;

            if (IsCompleted)
                return;

            if (!IsHackRunning || CooldownTimer.IsRunning)
                return;

            IsCompleted = true;
            IsHackRunning = false;

            Debug.Log("Hackeo completado por el Host");
            return;
        }
        RPC_CompleteHack();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_CompleteHack(RpcInfo info = default) {
        if (!Object.HasStateAuthority)
            return;

        if (IsCompleted)
            return;

        if (ActiveHacker != info.Source)
            return;

        if (!IsHackRunning || CooldownTimer.IsRunning)
            return;

        IsCompleted = true;
        IsHackRunning = false;

        Debug.Log("Hackeo completado en multiplayer por " + info.Source);
    }

    public void RequestStartHack() {
        if (Object.HasStateAuthority) {
            if (ActiveHacker == Runner.LocalPlayer &&
                !IsCompleted && !CooldownTimer.IsRunning) {
                IsHackRunning = true;
            }

            return;
        }

        RPC_RequestStartHack();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartHack(RpcInfo info = default) {
        if (!Object.HasStateAuthority)
            return;

        if (IsCompleted || CooldownTimer.IsRunning || ActiveHacker != info.Source)
            return;

        IsHackRunning = true;
    }

    private void HandleHackStarted() {
        RequestStartHack();
    }

    public void DetachLocalComputerUI(ComputerUI computerUI) {
        if (localComputerUI != computerUI)
            return;

        localComputerUI.OnHackStarted -= HandleHackStarted;
        localComputerUI.OnHackError -= HandleHackError;
        localComputerUI = null;
    }

    private void StartCooldown(float duration) {
        if (!Object.HasStateAuthority)
            return;

        IsHackRunning = false;
        ActiveHacker = PlayerRef.None;

        CooldownTimer = TickTimer.CreateFromSeconds(Runner, duration);

        Debug.Log("Computadora apagada durante " + duration + " segundos");
    }

    private void FinishCooldown() {
        Temperature = 0f;
        CooldownTimer = TickTimer.None;

        Debug.Log("Computadora enfriada y disponible");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ForceCloseComputer(PlayerRef hacker) {
        if (Runner.LocalPlayer != hacker)
            return;

        if (localComputerUI == null)
            return;

        localComputerUI.ExitComputer();
    }

    public void RequestQTEError() {
        if (Object.HasStateAuthority) {
            ApplyQTEError(Runner.LocalPlayer);
            return;
        }

        RPC_RequestQTEError();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestQTEError(RpcInfo info = default) {
        ApplyQTEError(info.Source);
    }

    private void ApplyQTEError(PlayerRef player) {
        if (!Object.HasStateAuthority)
            return;

        if (IsCompleted || !IsHackRunning || CooldownTimer.IsRunning)
            return;

        if (ActiveHacker != player)
            return;

        Temperature = Mathf.Min(100f, Temperature + qteErrorHeat);

        Debug.Log(
            "Error de QTE. Temperatura: " +
            Temperature.ToString("F1") + "%"
        );
    }

    private void HandleHackError() {
        RequestQTEError();
    }
}
