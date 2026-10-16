using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLook : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float lookSensitivity = 1f;

    private float cameraPitch;

    private GameInput gameInput;

    private void Awake() {
        gameInput = GetComponent<GameInput>();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update() {
        Vector2 lookInput = gameInput.GetLookVector();

        float yaw = lookInput.x * lookSensitivity;
        float pitch = lookInput.y * lookSensitivity;

        transform.Rotate(Vector3.up, yaw);

        cameraPitch -= pitch;
        cameraPitch = Mathf.Clamp(cameraPitch, -90f, 90f);

        cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

    }
}
