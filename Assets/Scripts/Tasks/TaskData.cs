using System;
using UnityEngine;

public enum TaskPriority {
    Principal,
    Secundaria
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
