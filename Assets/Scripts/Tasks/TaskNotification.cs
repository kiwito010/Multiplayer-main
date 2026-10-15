using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TaskNotification : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI taskText;
    [SerializeField] private RectTransform rectTransform;

    [SerializeField] private float horizontalPadding = 40f;
    [SerializeField] private float visibleTime = 4f;

    public void SetTask(TaskData task) {
        taskText.text = task.description;

        taskText.ForceMeshUpdate();

        float textWidth = taskText.preferredWidth;

        rectTransform.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth + horizontalPadding);

        StartCoroutine(HideAfterTime());
    }

    private IEnumerator HideAfterTime() {
        yield return new WaitForSeconds(visibleTime);

        Destroy(gameObject);
    }
}
