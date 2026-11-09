using System;
using UnityEngine;

public enum TaskPriority {
    Principal = 0,
    Secundaria = 1,
    Mision = 2
}

[Serializable]
public class TaskData {
    public string taskName;

    [TextArea(2, 4)]
    public string description;

    public TaskPriority priority = TaskPriority.Principal;

    [HideInInspector]
    public bool isCompleted;
}
