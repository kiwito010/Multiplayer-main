using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCharacterSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject engineer;
    [SerializeField] private GameObject explorer;

    private bool controllingEngineer = true;

    private void Start() {
        SetEngineerActive();
    }

    private void Update() {
        if (Input.GetKeyDown(KeyCode.Tab)) {
            if (controllingEngineer) {
                SetExplorerActive();
            } else {
                SetEngineerActive();
            }
        }
    }

    private void SetEngineerActive() {
        engineer.SetActive(true);
        explorer.SetActive(false);

        controllingEngineer = true;
    }

    private void SetExplorerActive() {
        engineer.SetActive(false);
        explorer.SetActive(true);

        controllingEngineer = false;
    }
}
