using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class TaskJournalUI : MonoBehaviour {
    [Header("Interfaz")]
    [SerializeField] private GameObject journalPanel;
    [SerializeField] private TextMeshProUGUI tasksText;

    private TaskManager taskManager;
    private bool wasOpen;

    private void Start() {
        taskManager = FindFirstObjectByType<TaskManager>();

        if (journalPanel != null)
            journalPanel.SetActive(false);

        if (taskManager == null) {
            Debug.LogWarning("[TaskJournalUI] No se encontró TaskManager.");
        }
    }

    private void Update() {
        if (journalPanel == null || Keyboard.current == null)
            return;

        bool tabHeld = Keyboard.current.tabKey.isPressed;

        if (tabHeld != wasOpen) {
            wasOpen = tabHeld;
            journalPanel.SetActive(tabHeld);

            if (tabHeld)
                RefreshTasks();
        }

        if (tabHeld)
            RefreshTasks();
    }

    private void RefreshTasks() {
        if (taskManager == null)
            taskManager = FindFirstObjectByType<TaskManager>();

        if (taskManager == null || tasksText == null)
            return;

        StringBuilder builder = new StringBuilder();
        var tasks = taskManager.GetActiveTasks();

        // 1. Misiones
        foreach (TaskData task in tasks) {
            if (task == null ||
                task.priority != TaskPriority.Mision)
                continue;

            builder.AppendLine(
                "<size=145%><b><color=#8DCBFF>" +
                task.description +
                "</color></b></size>"
            );
            builder.AppendLine();
        }

        // 2. Objetivos principales
        foreach (TaskData task in tasks) {
            if (task == null ||
                task.priority != TaskPriority.Principal)
                continue;

            builder.AppendLine(
                "<size=115%><b>● " +
                task.description +
                "</b></size>"
            );
            builder.AppendLine();
        }

        // 3. Tareas secundarias
        foreach (TaskData task in tasks) {
            if (task == null ||
                task.priority != TaskPriority.Secundaria)
                continue;

            builder.AppendLine(
                "<size=90%><color=#CCCCCC>○ " +
                task.description +
                "</color></size>"
            );
        }

        if (tasks.Count == 0)
            builder.AppendLine("No hay tareas activas.");

        tasksText.text = builder.ToString();
    }

    private void OnDisable() {
        wasOpen = false;

        if (journalPanel != null)
            journalPanel.SetActive(false);
    }
}