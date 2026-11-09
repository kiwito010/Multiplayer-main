using UnityEngine;

public class Mission1TaskController : MonoBehaviour {
    [Header("Referencias")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private RepairableComponent generator;
    [SerializeField] private FuseBox fuseBox;
    [SerializeField] private NetworkComputer computer;
    private MissionAnnouncementUI missionAnnouncementUI;

    [Header("Objetos de misión")]
    [SerializeField] private NetworkPickableItem magnifyingGlass;
    [SerializeField] private NetworkPickableItem pliers;
    [SerializeField] private NetworkPickableItem newCable;
    [SerializeField] private NetworkPickableItem insulatingTape;
    [SerializeField] private NetworkPickableItem fuse;

    private bool magnifyingGlassTaskCompleted;
    private bool pliersTaskCompleted;
    private bool cableTaskCompleted;
    private bool tapeTaskCompleted;
    private bool fusePickupTaskCompleted;

    private TaskData mission1Task;
    private TaskData mission2Task;
    private TaskData escapeTask;

    private TaskData diagnoseTask;
    private TaskData magnifyingGlassTask;

    private TaskData repairTask;
    private TaskData pliersTask;
    private TaskData cableTask;
    private TaskData tapeTask;

    private TaskData fuseTask;
    private TaskData findFuseTask;

    private TaskData installFuseTask;

    private TaskData activatePowerTask;

    private bool initialized;
    private int currentStage;

    public int CurrentStage => initialized ? currentStage : -1;

    private TaskData CreateTask(
        string text,
        TaskPriority priority) {
        return new TaskData {
            taskName = text,
            description = text,
            priority = priority
        };
    }

    private void InitializeTasks() {
        mission1Task = CreateTask(
            "Restablecer el suministro eléctrico",
            TaskPriority.Mision
        );

        mission2Task = CreateTask(
            "Hackear la computadora",
            TaskPriority.Mision
        );

        escapeTask = CreateTask(
            "Escapar de las instalaciones",
            TaskPriority.Mision
        );

        diagnoseTask = CreateTask(
            "Diagnosticar la falla del generador",
            TaskPriority.Principal
        );

        magnifyingGlassTask = CreateTask(
            "Buscar la lupa",
            TaskPriority.Secundaria
        );

        repairTask = CreateTask(
            "Reparar el generador",
            TaskPriority.Principal
        );

        pliersTask = CreateTask(
            "Buscar el alicate",
            TaskPriority.Secundaria
        );

        cableTask = CreateTask(
            "Buscar un cable nuevo",
            TaskPriority.Secundaria
        );

        tapeTask = CreateTask(
            "Buscar cinta aisladora",
            TaskPriority.Secundaria
        );

        fuseTask = CreateTask(
            "Reemplazar el fusible",
            TaskPriority.Principal
        );

        findFuseTask = CreateTask(
            "Buscar el fusible",
            TaskPriority.Secundaria
        );

        installFuseTask = CreateTask(
            "Instalar el fusible en la caja",
            TaskPriority.Secundaria
        );

        activatePowerTask = CreateTask(
            "Accionar los interruptores del generador",
            TaskPriority.Principal
        );

        taskManager.AddTask(mission1Task, false);
        taskManager.AddTask(diagnoseTask);
        taskManager.AddTask(magnifyingGlassTask);

        initialized = true;
        currentStage = 0;
    }

    private void Update() {
        if (generator == null)
            return;

        // Esperar a que Fusion inicialice el generador.
        if (generator.Object == null || !generator.Object.IsValid)
            return;

        if (taskManager == null)
            taskManager = FindFirstObjectByType<TaskManager>();

        if (taskManager == null)
            return;

        // Esperar a que exista el HUD local para que
        // las primeras notificaciones puedan mostrarse.
        if (!initialized) {
            if (FindFirstObjectByType<TaskNotificationUI>() == null)
                return;

            missionAnnouncementUI = FindFirstObjectByType<MissionAnnouncementUI>();

            if (missionAnnouncementUI == null)
                return;

            InitializeTasks();

            ShowMissionAnnouncement(
                "MISIÓN 01",
                "Restablecer el suministro eléctrico"
            );
        }

        // Completar la tarea de la lupa cuando Fusion
        // confirme que algún jugador la recogió.
        if (!magnifyingGlassTaskCompleted &&
            magnifyingGlass != null &&
            magnifyingGlass.Object != null &&
            magnifyingGlass.Object.IsValid &&
            magnifyingGlass.WasEverPickedUp) {

            magnifyingGlassTaskCompleted = true;
            taskManager.CompleteTask(magnifyingGlassTask);
        }
        
        // Comprobar herramientas recogidas.
        // Las tareas se completan cuando ya fueron agregadas.
        if (currentStage >= 1) {
            if (!pliersTaskCompleted &&
                pliers != null &&
                pliers.Object != null &&
                pliers.Object.IsValid &&
                pliers.WasEverPickedUp) {
                pliersTaskCompleted = true;
                taskManager.CompleteTask(pliersTask);
            }

            if (!cableTaskCompleted &&
                newCable != null &&
                newCable.Object != null &&
                newCable.Object.IsValid &&
                newCable.WasEverPickedUp) {
                cableTaskCompleted = true;
                taskManager.CompleteTask(cableTask);
            }

            if (!tapeTaskCompleted &&
                insulatingTape != null &&
                insulatingTape.Object != null &&
                insulatingTape.Object.IsValid &&
                insulatingTape.WasEverPickedUp) {
                tapeTaskCompleted = true;
                taskManager.CompleteTask(tapeTask);
            }
        }

        // Etapa 1: diagnóstico completado.
        if (currentStage == 0 && generator.IsDiagnosed()) {
            
            Debug.Log("[MISION 1] Diagnóstico completado. Actualizando tareas.");
            
            taskManager.CompleteTask(diagnoseTask);

            taskManager.AddTask(repairTask);
            taskManager.AddTask(pliersTask);
            taskManager.AddTask(cableTask);
            taskManager.AddTask(tapeTask);

            currentStage = 1;
        }

        // Etapa del fusible: alguien lo recogió.
        if (currentStage >= 2 &&
            !fusePickupTaskCompleted &&
            fuse != null &&
            fuse.Object != null &&
            fuse.Object.IsValid &&
            fuse.WasEverPickedUp) {

            fusePickupTaskCompleted = true;

            taskManager.CompleteTask(findFuseTask);
            taskManager.AddTask(installFuseTask);

            Debug.Log("[MISION 1] Fusible recogido.");
        }

        // Etapa 2: generador reparado.
        if (currentStage == 1 && generator.IsCableRepairCompleted()) {
            Debug.Log("[MISION 1] Generador reparado. Actualizando tareas.");
            taskManager.CompleteTask(repairTask);

            taskManager.AddTask(fuseTask);
            taskManager.AddTask(findFuseTask);

            currentStage = 2;
        }

        // Etapa 3: fusible instalado en la caja.
        if (currentStage == 2 &&
            fuseBox != null &&
            fuseBox.Object != null &&
            fuseBox.Object.IsValid &&
            fuseBox.fuseInstalled) {

            Debug.Log("[MISION 1] Fusible instalado. Actualizando tareas.");

            // Por si el fusible fue instalado antes de
            // que se registrara la tarea de recogida.
            taskManager.CompleteTask(findFuseTask);

            taskManager.CompleteTask(installFuseTask);
            taskManager.CompleteTask(fuseTask);

            taskManager.AddTask(activatePowerTask);

            currentStage = 3;
        }


        // Etapa 4: los tres botones fueron activados
        // y se completó la Misión 1.
        if (currentStage == 3 && generator.IsRepaired()) {

            Debug.Log(
                "[MISION 1] Suministro eléctrico restablecido."
            );

            taskManager.CompleteTask(activatePowerTask);

            // Desbloquear el objetivo de la Misión 2.
            taskManager.CompleteTask(mission1Task);
            taskManager.AddTask(mission2Task, false);

            ShowMissionAnnouncement(
                "MISIÓN 02",
                "Hackear la computadora"
            );

            currentStage = 4;
        }

        // Etapa 5: hackeo completado.
        if (currentStage == 4 &&
            computer != null &&
            computer.Object != null &&
            computer.Object.IsValid &&
            computer.IsCompleted) {

            Debug.Log("[MISION 2] Hackeo completado.");

            taskManager.CompleteTask(mission2Task);

            taskManager.AddTask(escapeTask, false);

            ShowMissionAnnouncement(
                "OBJETIVO FINAL",
                "Escapar de las instalaciones"
            );

            currentStage = 5;
        }
    }

    private void ShowMissionAnnouncement(string label, string title) {

        if (missionAnnouncementUI == null)
            missionAnnouncementUI =
                FindFirstObjectByType<MissionAnnouncementUI>();

        if (missionAnnouncementUI == null) {
            Debug.LogWarning(
                "[Mission1TaskController] No se encontró MissionAnnouncementUI."
            );
            return;
        }

        missionAnnouncementUI.ShowMission(label, title);
    }
}
