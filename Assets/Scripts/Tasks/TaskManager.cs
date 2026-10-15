using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    private List<TaskData> activeTasks = new List<TaskData>();
    public event System.Action OnTasksChanged;
    public event System.Action<TaskData> OnTaskAdded;

    public void AddTask(TaskData task) {
        activeTasks.Add(task);
        Debug.Log("NUEVA TAREA: " + task.taskName);
        Debug.Log("DESCRIPCION: " + task.description);
        Debug.Log("TAREAS ACTIVAS: " + activeTasks.Count);

        OnTasksChanged?.Invoke();
        OnTaskAdded?.Invoke(task);
    }

    public List<TaskData> GetActiveTasks() {
        return activeTasks;
    }
}
