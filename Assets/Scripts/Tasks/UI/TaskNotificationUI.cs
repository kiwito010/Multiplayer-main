using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskNotificationUI : MonoBehaviour
{
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private Transform notificationContainer;
    [SerializeField] private TaskNotification notificationPrefab;

    private void OnEnable() {
        taskManager.OnTaskAdded += ShowNotification;
    }

    private void OnDisable() {
        taskManager.OnTaskAdded -= ShowNotification;
    }

    private void ShowNotification(TaskData task) {
        TaskNotification notification = Instantiate(notificationPrefab, notificationContainer);

        notification.SetTask(task);
    }
}
