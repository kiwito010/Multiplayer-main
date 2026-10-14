using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TaskUI : MonoBehaviour
{
    [SerializeField] private TaskManager taskManager;
    [SerializeField] private TextMeshProUGUI taskText;

    private void OnEnable() {
        taskManager.OnTasksChanged += UpdateTaskUI;
        UpdateTaskUI();
    }

    private void OnDisable() {
        taskManager.OnTasksChanged -= UpdateTaskUI;
    }

    private void UpdateTaskUI() {
        string text = "OBJETIVOS\n";

        foreach (TaskData task in taskManager.GetActiveTasks()) {
            text += "- " + task.taskName + "\n";
        }
        taskText.text = text;
    }
}
