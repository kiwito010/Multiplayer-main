
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TaskNotification : MonoBehaviour {
    [Header("Referencias")]
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI taskText;
    [SerializeField] private Image background;

    [Header("Tamaños")]
    [SerializeField] private float mainFontSize = 23f;
    [SerializeField] private float secondaryFontSize = 20f;

    [Header("Duraciones")]
    [SerializeField] private float mainVisibleTime = 6f;
    [SerializeField] private float secondaryVisibleTime = 4f;
    [SerializeField] private float completedVisibleTime = 4f;

    public void SetTask(TaskData task, bool completed = false) {
        if (task == null || headerText == null || taskText == null)
            return;

        bool isMain = task.priority == TaskPriority.Principal;

        if (completed) {
            headerText.text = "TAREA COMPLETADA";

            taskText.text =
                "<color=#B0B0B0><s>" +
                task.description +
                "</s></color>";
        } else {
            headerText.text = isMain
                ? "OBJETIVO PRINCIPAL"
                : "NUEVA TAREA";

            taskText.text = task.description;
        }

        taskText.fontSize = isMain
            ? mainFontSize
            : secondaryFontSize;

        taskText.fontStyle = isMain
            ? FontStyles.Bold
            : FontStyles.Normal;

        if (background != null) {
            background.color =
                new Color(0.05f, 0.06f, 0.08f, 0.88f);
        }

        float duration = completed
            ? completedVisibleTime
            : isMain
                ? mainVisibleTime
                : secondaryVisibleTime;

        StartCoroutine(HideAfterTime(duration));
    }

    public void SetHint(string message) {
        if (headerText == null || taskText == null)
            return;

        headerText.text = "INDICACIÓN";
        taskText.text = message;

        taskText.fontSize = secondaryFontSize;
        taskText.fontStyle = FontStyles.Normal;

        StartCoroutine(HideAfterTime(7f));
    }

    private IEnumerator HideAfterTime(float duration) {
        yield return new WaitForSecondsRealtime(duration);
        Destroy(gameObject);
    }
}
