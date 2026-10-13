using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TaskManager : MonoBehaviour
{
    public void AddTask(TaskData task) {
        Debug.Log("NUEVA TAREA: " + task.taskName);
        Debug.Log(task.description);
    }
}
