using UnityEngine;

public class TaskNotificationUI : MonoBehaviour {
    private TaskManager taskManager;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private TaskNotification notificationPrefab;

    private void OnEnable() {
        ConnectTaskManager();
    }

    private void Start() {
        ConnectTaskManager();
    }

    private void OnDisable() {
        if (taskManager == null)
            return;

        taskManager.OnTaskAdded -= ShowNotification;

        taskManager.OnTaskCompleted -= ShowCompletedNotification;
    }

    private void ConnectTaskManager() {
        if (taskManager == null) {
            taskManager = FindFirstObjectByType<TaskManager>();
        }

        if (taskManager == null) {
            Debug.LogWarning(
                "[TaskNotificationUI] No se encontró TaskManager en la escena."
            );
            return;
        }

        // Evitar suscripciones duplicadas.
        taskManager.OnTaskAdded -= ShowNotification;
        taskManager.OnTaskAdded += ShowNotification;

        taskManager.OnTaskCompleted -= ShowCompletedNotification;
        taskManager.OnTaskCompleted += ShowCompletedNotification;

        Debug.Log("[TaskNotificationUI] Conectado a TaskManager.");
    }

    private void ShowNotification(TaskData task) {
        TaskNotification notification =
            Instantiate(notificationPrefab, notificationContainer);

        notification.SetTask(task, false);
    }

    private void ShowCompletedNotification(TaskData task) {
        TaskNotification notification =
            Instantiate(notificationPrefab, notificationContainer);

        notification.SetTask(task, true);
    }
}
