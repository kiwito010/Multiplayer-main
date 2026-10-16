using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalCharacterSwitcher : MonoBehaviour
{
    [SerializeField] private GameObject engineer;
    [SerializeField] private GameObject explorer;

    [SerializeField] private GameObject engineerInventoryUI;
    [SerializeField] private GameObject explorerInventoryUI;

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
        
        engineerInventoryUI.SetActive(true);
        explorerInventoryUI.SetActive(false);

        controllingEngineer = true;
    }

    private void SetExplorerActive() {
        engineer.SetActive(false);
        explorer.SetActive(true);

        engineerInventoryUI.SetActive(false);
        explorerInventoryUI.SetActive(true);

        controllingEngineer = false;
    }
}
