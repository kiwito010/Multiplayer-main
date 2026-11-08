using UnityEngine;

public class Mission1TaskController : MonoBehaviour {
    [Header("Referencias")]
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private RepairableComponent generator;

    private TaskData diagnoseTask;
    private TaskData magnifyingGlassTask;

    private TaskData repairTask;
    private TaskData pliersTask;
    private TaskData cableTask;
    private TaskData tapeTask;

    private TaskData fuseTask;
    private TaskData findFuseTask;

    private bool initialized;
    private int currentStage;

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

            InitializeTasks();
        }

        // Etapa 1: diagnóstico completado.
        Debug.Log("[MISION 1] Diagnóstico completado. Actualizando tareas.");
        if (currentStage == 0 && generator.IsDiagnosed()) {
            taskManager.CompleteTask(diagnoseTask);

            taskManager.AddTask(repairTask);
            taskManager.AddTask(pliersTask);
            taskManager.AddTask(cableTask);
            taskManager.AddTask(tapeTask);

            currentStage = 1;
        }

        // Etapa 2: generador reparado.
        if (currentStage == 1 && generator.IsRepaired()) {
            Debug.Log("[MISION 1] Generador reparado. Actualizando tareas.");
            taskManager.CompleteTask(repairTask);

            taskManager.AddTask(fuseTask);
            taskManager.AddTask(findFuseTask);

            currentStage = 2;
        }
    }
}
