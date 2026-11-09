
using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour {

    private List<TaskData> activeTasks = new List<TaskData>();
    private List<TaskData> completedTasks = new List<TaskData>();

    public event Action OnTasksChanged;
    public event Action<TaskData> OnTaskAdded;
    public event Action<TaskData> OnTaskCompleted;

    public void AddTask(TaskData task, bool showNotification = true) {
        if (task == null)
            return;

        if (activeTasks.Contains(task))
            return;

        task.isCompleted = false;

        completedTasks.Remove(task);
        activeTasks.Add(task);

        Debug.Log("NUEVA TAREA: " + task.taskName);

        OnTasksChanged?.Invoke();

        if (showNotification)
            OnTaskAdded?.Invoke(task);
    }

    public void CompleteTask(TaskData task, bool showNotification = true) {

        if (task == null)
            return;

        if (!activeTasks.Remove(task))
            return;

        task.isCompleted = true;
        completedTasks.Add(task);

        Debug.Log("TAREA COMPLETADA: " + task.taskName);

        OnTasksChanged?.Invoke();

        if (showNotification)
            OnTaskCompleted?.Invoke(task);
    }

    public List<TaskData> GetActiveTasks() {
        return activeTasks;
    }

    public List<TaskData> GetCompletedTasks() {
        return completedTasks;
    }
}
