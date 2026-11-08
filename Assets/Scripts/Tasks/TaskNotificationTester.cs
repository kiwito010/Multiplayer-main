using UnityEngine;

public class TaskNotificationTester : MonoBehaviour {
    [SerializeField] private TaskManager taskManager;

    private TaskData testTask;

    private void Update() {
        if (taskManager == null)
            return;

        // F8: crear tarea principal.
        if (Input.GetKeyDown(KeyCode.F8)) {
            testTask = new TaskData {
                taskName = "Reparar generador",
                description = "Restablecer el generador",
                priority = TaskPriority.Principal
            };

            taskManager.AddTask(testTask);
        }

        // F9: completar tarea.
        if (Input.GetKeyDown(KeyCode.F9) && testTask != null) {
            taskManager.CompleteTask(testTask);
        }
    }
}
