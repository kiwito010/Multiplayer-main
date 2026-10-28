using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CableRepairMinigame : MonoBehaviour, IRepairMinigame {
    private enum RepairPhase {
        CutBrokenCable,
        RemoveBrokenCable,
        PlaceNewCable,
        TwistEnds,
        Wrap,
        Complete
    }

    [SerializeField] private GameObject content;

    //Components:
    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject tape;

        //ChangeCable
        [SerializeField] private GameObject changeLeftCable;
        [SerializeField] private GameObject changeRightCable;
        [SerializeField] private GameObject changeLeftTwistedCable;
        [SerializeField] private GameObject changeRightTwistedCable;
        //

        //NewCable
        [SerializeField] private GameObject leftCable;
        [SerializeField] private GameObject rightCable;
        [SerializeField] private GameObject leftTwistedCable;
        [SerializeField] private GameObject rightTwistedCable;
        //

    //

    //[SerializeField] private TextMeshProUGUI instructionText;

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameInput gameInput;

    private RepairPhase currentPhase;


    //CablePanels
    [SerializeField] private GameObject cutPanel;
    [SerializeField] private GameObject changePanel;
    [SerializeField] private GameObject wrapPanel;
    //

    //Signs
    [SerializeField] private GameObject cutSign;
    [SerializeField] private GameObject removeSign;
    [SerializeField] private GameObject placeSign;
    [SerializeField] private GameObject twistSign;
    [SerializeField] private GameObject wrapSign;
    [SerializeField] private GameObject repairedSign;
    //

    private RepairableComponent currentComponent;
    private PlayerInventory currentInventory;

    [SerializeField] private GameObject leftCutPoint;
    [SerializeField] private GameObject rightCutPoint;

    private bool leftCut;
    private bool rightCut;

    private bool brokenCableRemoved;

    private bool repairStarted;

    [SerializeField] private GameObject leftTwistPoint;
    [SerializeField] private GameObject rightTwistPoint;

    private bool leftTwisted;
    private bool rightTwisted;

    [SerializeField] private RectTransform leftWrapCenter;
    [SerializeField] private RectTransform rightWrapCenter;

    [SerializeField] private GameObject leftWrappedPanel;
    [SerializeField] private GameObject rightWrappedPanel;

    private bool leftWrapped;
    private bool rightWrapped;

    [SerializeField] private float requiredWrapRotation = 720f;

    private float leftWrapRotation;
    private float rightWrapRotation;

    private float previousTapeAngle;
    private bool isWrapping;
    
    //required objects
    [SerializeField] private GameObject requiredItemMessage;
    [SerializeField] private TextMeshProUGUI requiredItemMessageText;

    private RectTransform currentWrapCenter;
    private bool wrappingLeft;

    private string requiredCableName = "New Cable";
    private string requiredPliersName = "Alicate";
    private string requiredTapeName = "Tape";
    
    [SerializeField] private RectTransform wrapProgressRect;
    [SerializeField] private Image wrapProgressImage;
    [SerializeField] private Vector2 wrapProgressOffset;


    private void Update() {
        if (content.activeSelf && gameInput.GetExitPressed()) {
            ExitRepair();
        }
    }

    public void Open(
        RepairableComponent component,
        PlayerInventory inventory) {
        currentComponent = component;
        currentInventory = inventory;

        if (!repairStarted) {
            currentPhase = RepairPhase.CutBrokenCable;

            leftCut = false;
            rightCut = false;

            brokenCableRemoved = false;

            repairStarted = true;
        }

        content.SetActive(true);

        playerManager.EnterUIMode();

        UpdatePhaseUI();
    }

    public void ExitRepair() {
        playerManager.ExitUIMode();

        content.SetActive(false);
    }

    private void UpdatePhaseUI() {
        //componentes
        brokenCable.SetActive(!(currentPhase == RepairPhase.CutBrokenCable));
        newCable.SetActive(currentPhase == RepairPhase.CutBrokenCable || currentPhase == RepairPhase.RemoveBrokenCable || currentPhase == RepairPhase.PlaceNewCable || currentPhase == RepairPhase.TwistEnds);
        //newCable
        leftCable.SetActive(!leftTwisted);
        rightCable.SetActive(!rightTwisted);
        leftTwistedCable.SetActive(leftTwisted);
        rightTwistedCable.SetActive(rightTwisted);
        //

        //ChangePanel
        changeLeftCable.SetActive(!leftTwisted);
        changeRightCable.SetActive(!rightTwisted);
        changeLeftTwistedCable.SetActive(leftTwisted);
        changeRightTwistedCable.SetActive(rightTwisted);
        //
        tape.SetActive(HasRequiredTape());

        //paneles + signs
        cutPanel.SetActive(currentPhase == RepairPhase.CutBrokenCable);
        cutSign.SetActive(currentPhase == RepairPhase.CutBrokenCable);
        changePanel.SetActive(currentPhase == RepairPhase.RemoveBrokenCable || currentPhase == RepairPhase.PlaceNewCable || currentPhase == RepairPhase.TwistEnds);
        removeSign.SetActive(currentPhase == RepairPhase.RemoveBrokenCable);
        placeSign.SetActive (currentPhase == RepairPhase.PlaceNewCable);
        twistSign.SetActive(currentPhase == RepairPhase.TwistEnds);
        wrapPanel.SetActive(currentPhase == RepairPhase.Wrap || currentPhase == RepairPhase.Complete);
        wrapSign.SetActive(currentPhase == RepairPhase.Wrap);
        repairedSign.SetActive(currentPhase == RepairPhase.Complete);

        leftCutPoint.SetActive(currentPhase == RepairPhase.CutBrokenCable && !leftCut);
        rightCutPoint.SetActive(currentPhase == RepairPhase.CutBrokenCable && !rightCut);

        leftTwistPoint.SetActive(currentPhase == RepairPhase.TwistEnds && !leftTwisted);
        rightTwistPoint.SetActive(currentPhase == RepairPhase.TwistEnds && !rightTwisted);

        leftWrappedPanel.SetActive(currentPhase == RepairPhase.Wrap && leftWrapped || currentPhase == RepairPhase.Complete);

        rightWrappedPanel.SetActive(currentPhase == RepairPhase.Wrap && rightWrapped || currentPhase == RepairPhase.Complete);

        //UpdateInstruction();
    }

    /*private void UpdateInstruction() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                instructionText.text =
                    "Corta las puntas del cable roto";
                break;

            case RepairPhase.RemoveBrokenCable:
                instructionText.text =
                    "Retira el cable roto";
                break;

            case RepairPhase.PlaceNewCable:
                instructionText.text =
                    "Coloca el cable nuevo";
                break;

            case RepairPhase.TwistEnds:
                instructionText.text = "Conecta las puntas del cable";
                break;

            case RepairPhase.Wrap:
                instructionText.text =
                    "Encinta la reparacion";
                break;

            case RepairPhase.Complete:
                instructionText.text =
                    "REPARACION COMPLETA";
                break;
        }
    }*/

    public void CutLeftSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (leftCut)
            return;

        if (!HasRequiredPliers()) {
            ShowMessage("ALICATE NECESARIO");
            return;
        }

        leftCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    public void CutRightSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (rightCut)
            return;

        if (!HasRequiredPliers()) {
            ShowMessage("ALICATE NECESARIO");
            return;
        }

        rightCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    private void CheckCutsComplete() {
        if (leftCut && rightCut) {
            CompleteCurrentPhase();
        }
    }

    public bool CanRemoveBrokenCable() {
        return currentPhase == RepairPhase.RemoveBrokenCable;
    }

    public void BrokenCableRemoved() {
        if (currentPhase != RepairPhase.RemoveBrokenCable)
            return;

        brokenCableRemoved = true;

        CompleteCurrentPhase();
    }

    public bool CanPlaceNewCable() {
        return brokenCableRemoved;
    }

    public void CompleteCurrentPhase() {
        switch (currentPhase) {
            case RepairPhase.CutBrokenCable:
                currentPhase = RepairPhase.RemoveBrokenCable;
                break;

            case RepairPhase.RemoveBrokenCable:
                currentPhase = RepairPhase.PlaceNewCable;
                break;

            case RepairPhase.PlaceNewCable:
                currentPhase = RepairPhase.TwistEnds;
                break;

            case RepairPhase.TwistEnds:
                currentPhase = RepairPhase.Wrap;
                break;

            case RepairPhase.Wrap:
                currentPhase = RepairPhase.Complete;
                CompleteRepair();
                return;
        }

        UpdatePhaseUI();
    }

    public void NewCablePlaced() {
        if (!brokenCableRemoved)
            return;

        if (currentPhase != RepairPhase.PlaceNewCable)
            return;

        if (!HasRequiredCable())
            return;

        CompleteCurrentPhase();
    }

    public void TwistLeftSide() {
        if (currentPhase != RepairPhase.TwistEnds)
            return;

        if (leftTwisted)
            return;

        leftTwisted = true;

        UpdatePhaseUI();
        CheckTwistsComplete();
    }

    public void TwistRightSide() {
        if (currentPhase != RepairPhase.TwistEnds)
            return;

        if (rightTwisted)
            return;

        rightTwisted = true;

        UpdatePhaseUI();
        CheckTwistsComplete();
    }

    private void CheckTwistsComplete() {
        if (leftTwisted && rightTwisted) {
            CompleteCurrentPhase();
        }
    }

    public void StartWrapping(Vector2 tapePosition) {
        if (currentPhase != RepairPhase.Wrap)
            return;

        if (!HasRequiredTape())
            return;

        isWrapping = true;

        UpdateCurrentWrapTarget(tapePosition);

        previousTapeAngle = GetTapeAngle(
            tapePosition,
            currentWrapCenter
        );

        wrapProgressRect.gameObject.SetActive(true);

        UpdateWrapProgressUI(tapePosition);
    }


    public void UpdateWrapping(Vector2 tapePosition) {
        if (!isWrapping)
            return;

        UpdateCurrentWrapTarget(tapePosition);

        float currentTapeAngle = GetTapeAngle(
            tapePosition,
            currentWrapCenter
        );

        float angleDifference = Mathf.DeltaAngle(
            previousTapeAngle,
            currentTapeAngle
        );

        previousTapeAngle = currentTapeAngle;

        if (angleDifference < 0f) {
            float rotationAmount = Mathf.Abs(angleDifference);

            if (wrappingLeft) {
                leftWrapRotation += rotationAmount;

                if (leftWrapRotation >= requiredWrapRotation) {
                    leftWrapped = true;

                    UpdatePhaseUI();

                    ShowMessage("Punta izquierda encintada");
                }
            } else {
                rightWrapRotation += rotationAmount;

                if (rightWrapRotation >= requiredWrapRotation) {
                    rightWrapped = true;

                    UpdatePhaseUI();

                    ShowMessage("Punta derecha encintada");
                }
            }
        }

        if (leftWrapped && rightWrapped) {
            isWrapping = false;

            currentInventory.RemoveItem(requiredTapeName);

            CompleteCurrentPhase();
        }

        UpdateWrapProgressUI(tapePosition);
    }

    private void UpdateCurrentWrapTarget(Vector2 tapePosition) {
        float distanceToLeft = Vector2.Distance(tapePosition, leftWrapCenter.position);

        float distanceToRight = Vector2.Distance(tapePosition, rightWrapCenter.position);

        if (!leftWrapped &&
            distanceToLeft < distanceToRight) {
            currentWrapCenter = leftWrapCenter;
            wrappingLeft = true;
        } else if (!rightWrapped) {
            currentWrapCenter = rightWrapCenter;
            wrappingLeft = false;
        }
    }

    public void StopWrapping() {
        isWrapping = false;
        wrapProgressRect.gameObject.SetActive(false);
    }

    private float GetTapeAngle(Vector2 tapePosition, RectTransform center) {
        Vector2 direction = tapePosition - (Vector2)center.position;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void CompleteRepair() {
        currentComponent.Repair();

        UpdatePhaseUI();
    }

    private void UpdateWrapProgressUI(Vector2 tapePosition) {
        wrapProgressRect.position =
            tapePosition + wrapProgressOffset;

        float currentRotation;

        if (wrappingLeft) {
            currentRotation = leftWrapRotation;
        } else {
            currentRotation = rightWrapRotation;
        }

        wrapProgressImage.fillAmount =
            currentRotation / requiredWrapRotation;

        Debug.Log("Mostrando progreso");
    }

    private void ShowMessage(string message) {
        requiredItemMessageText.text = message;
        requiredItemMessage.SetActive(true);

        CancelInvoke(nameof(HideRequiredItemMessage));
        Invoke(nameof(HideRequiredItemMessage), 2f);
    }

    private void HideRequiredItemMessage() {
        requiredItemMessage.SetActive(false);
    }

    public void CheckCableRequired() {
        if (currentPhase != RepairPhase.PlaceNewCable)
            return;

        if (HasRequiredCable())
            return;

        ShowMessage("CABLE NECESARIO");
    }

    private bool HasRequiredCable()
{
        if (currentInventory == null)
        {
            Debug.Log("currentInventory es null");
            return false;
        }

        bool hasCable = currentInventory.HasItem(requiredCableName);

        Debug.Log("Buscando cable: " + requiredCableName);
        Debug.Log("Tiene cable: " + hasCable);
            
        return hasCable;
    }


    private bool HasRequiredPliers() {
        if (currentInventory == null)
            return false;

        return currentInventory.HasItem(requiredPliersName);
    }

    public void CheckTapeRequired() {
        if (currentPhase != RepairPhase.Wrap)
            return;

        if (HasRequiredTape())
            return;

        ShowMessage("TAPE NECESARIA");
    }

    private bool HasRequiredTape() {
        if (currentInventory == null)
            return false;

        return currentInventory.HasItem(requiredTapeName);
    }
}
