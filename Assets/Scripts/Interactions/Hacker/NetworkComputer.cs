using Fusion;
using UnityEngine;

public class NetworkComputer : NetworkBehaviour {

    // ESTADOS DE RED:
    private ComputerUI localComputerUI;

    [Networked] public NetworkBool IsCompleted { get; private set; }

    [Networked] public PlayerRef ActiveHacker { get; private set; }

    [Header("Temperatura")]
    [SerializeField] private float phase1HeatPerSecond = 1f;
    [SerializeField] private float phase2HeatPerSecond = 1.5f;
    [SerializeField] private float phase3HeatPerSecond = 2f;

    [Networked] public int CurrentHackPhase { get; private set; }

    [SerializeField] private float qteErrorHeat = 15f;

    [SerializeField] private float coolantFreeze = 30f;

    [Networked] public float Temperature { get; private set; }

    [Networked] public NetworkBool IsHackRunning { get; private set; }

    [Header("Enfriamiento")]
    [SerializeField] private float overheatCooldown = 15f;
    [SerializeField] private float exitCooldown = 5f;

    [Networked] public TickTimer CooldownTimer { get; private set; }

    [Header("Refrigerantes")]
    [SerializeField] private NetworkCoolantSpawner coolantSpawner;

    [Header("Misión 2")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TaskData hackTask;

    [SerializeField] private RepairableComponent generator;

    [Networked] public NetworkBool IsUnlocked { get; private set; }

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

        if (!IsUnlocked &&
            generator != null &&
            generator.Object != null &&
            generator.Object.IsValid &&
            generator.IsRepaired()) {

            IsUnlocked = true;

            RegisterHackTask();

            Debug.Log("Misión 2 desbloqueada");
        }
        // Comprobar si terminó el enfriamiento.
        if (CooldownTimer.Expired(Runner)) {
            FinishCooldown();
        }

        // No aumentar la temperatura si está apagada.
        if (CooldownTimer.IsRunning)
            return;

        if (!IsHackRunning || IsCompleted)
            return;

        float heatPerSecond = phase1HeatPerSecond;

        if (CurrentHackPhase == 2) {
            heatPerSecond = phase2HeatPerSecond;
        } else if (CurrentHackPhase == 3) {
            heatPerSecond = phase3HeatPerSecond;
        }

        Temperature = Mathf.Min(100f, Temperature + heatPerSecond * Runner.DeltaTime);
       
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

        if (!IsUnlocked)
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

        PlayerHUDSpawner hudSpawner = playerObject.GetComponent<PlayerHUDSpawner>();

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

            localComputerUI.OnHackPhaseChanged -= HandleHackPhaseChanged;
        }

        localComputerUI = computerUI;
        localComputerUI.OnHackStarted += HandleHackStarted;
        localComputerUI.OnHackError += HandleHackError;

        localComputerUI.OnHackPhaseChanged += HandleHackPhaseChanged;

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

            CompleteHackTask();

            if (coolantSpawner != null)
                coolantSpawner.StopSpawning();

            RPC_ConfirmHackSuccess(ActiveHacker);

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

        CompleteHackTask();

        if (coolantSpawner != null)
            coolantSpawner.StopSpawning();

        RPC_ConfirmHackSuccess(ActiveHacker);

        Debug.Log("Hackeo completado en multiplayer por " + info.Source);
    }

    public void RequestStartHack() {
        if (Object.HasStateAuthority) {
            if (IsUnlocked &&
                ActiveHacker == Runner.LocalPlayer &&
                !IsCompleted &&
                !CooldownTimer.IsRunning) {

                IsHackRunning = true;
                CurrentHackPhase = 1;

                if (coolantSpawner != null)
                    coolantSpawner.StartSpawning(Runner);
            }

            return;
        }

        RPC_RequestStartHack();
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestStartHack(RpcInfo info = default) {
        if (!Object.HasStateAuthority)
            return;

        if (!IsUnlocked ||
            IsCompleted ||
            CooldownTimer.IsRunning ||
            ActiveHacker != info.Source)
            return;

        IsHackRunning = true;
        CurrentHackPhase = 1;

        if (coolantSpawner != null)
            coolantSpawner.StartSpawning(Runner);
    }

    private void HandleHackStarted() {
        RequestStartHack();
    }

    public void DetachLocalComputerUI(ComputerUI computerUI) {
        if (localComputerUI != computerUI)
            return;

        localComputerUI.OnHackStarted -= HandleHackStarted;
        localComputerUI.OnHackError -= HandleHackError;

        localComputerUI.OnHackPhaseChanged -= HandleHackPhaseChanged;
        localComputerUI = null;
    }

    private void StartCooldown(float duration) {
        if (!Object.HasStateAuthority)
            return;

        if (coolantSpawner != null)
            coolantSpawner.StopSpawning();

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

    private void HandleHackPhaseChanged(int phase) {
        RequestHackPhaseChange(phase);
    }

    private void RequestHackPhaseChange(int phase) {
        if (Object.HasStateAuthority) {
            ApplyHackPhaseChange(Runner.LocalPlayer, phase);
            return;
        }

        RPC_RequestHackPhaseChange(phase);
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestHackPhaseChange(int phase, RpcInfo info = default) {
        ApplyHackPhaseChange(info.Source, phase);
    }

    private void ApplyHackPhaseChange(PlayerRef player, int phase) {
        if (!Object.HasStateAuthority)
            return;

        if (ActiveHacker != player || !IsHackRunning ||
            IsCompleted || CooldownTimer.IsRunning)
            return;

        if (phase < 1 || phase > 3)
            return;

        CurrentHackPhase = phase;

        Debug.Log("Fase actual del hackeo: " + CurrentHackPhase);
    }

    public bool TryApplyCoolant() {
        if (!Object.HasStateAuthority)
            return false;

        if (!IsHackRunning || IsCompleted || CooldownTimer.IsRunning)
            return false;

        Temperature = Mathf.Max(0f, Temperature - coolantFreeze);

        Debug.Log("Refrigerante depositado. Temperatura: " +
                  Temperature.ToString("F1") + "%");

        return true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ConfirmHackSuccess(PlayerRef hacker) {
        if (Runner.LocalPlayer != hacker)
            return;

        if (localComputerUI == null)
            return;

        localComputerUI.ConfirmNetworkHackSuccess(this);
    }

    private void RegisterHackTask() {
        if (!Object.HasStateAuthority)
            return;

        if (taskManager == null || hackTask == null)
            return;

        if (taskManager.GetActiveTasks().Contains(hackTask))
            return;

        taskManager.AddTask(hackTask);
    }

    private void CompleteHackTask() {
        if (!Object.HasStateAuthority)
            return;

        if (taskManager == null || hackTask == null)
            return;

        if (!taskManager.GetActiveTasks().Contains(hackTask))
            return;

        taskManager.CompleteTask(hackTask);
    }

}
