using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

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

    [SerializeField] private GameObject brokenCable;
    [SerializeField] private GameObject newCable;
    [SerializeField] private GameObject socket;
    [SerializeField] private GameObject tape;

    [SerializeField] private TextMeshProUGUI instructionText;

    [SerializeField] private PlayerManager playerManager;
    [SerializeField] private GameInput gameInput;

    private RepairPhase currentPhase;

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
    
    private bool leftWrapped;
    private bool rightWrapped;

    [SerializeField] private float requiredWrapRotation = 720f;

    private float leftWrapRotation;
    private float rightWrapRotation;

    private float previousTapeAngle;
    private bool isWrapping;

    private RectTransform currentWrapCenter;
    private bool wrappingLeft;

    private string requiredCableName = "New Cable";
    [SerializeField] private GameObject cableRequiredMessage;

    private void Update() {
        if (content.activeSelf && gameInput.GetExitPressed()) {
            Close();
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

        cableRequiredMessage.SetActive(false);

        playerManager.EnterUIMode();

        UpdatePhaseUI();
    }

    public void Close() {
        playerManager.ExitUIMode();

        content.SetActive(false);
    }

    private void UpdatePhaseUI() {
        brokenCable.SetActive(true);
        newCable.SetActive(HasRequiredCable());
        tape.SetActive(true);

        leftCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !leftCut
        );

        rightCutPoint.SetActive(
            currentPhase == RepairPhase.CutBrokenCable &&
            !rightCut
        );

        leftTwistPoint.SetActive(
            currentPhase == RepairPhase.TwistEnds &&
            !leftTwisted
        );

        rightTwistPoint.SetActive(
            currentPhase == RepairPhase.TwistEnds &&
            !rightTwisted
        );

        UpdateInstruction();
    }

    private void UpdateInstruction() {
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
    }

    public void CutLeftSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (leftCut)
            return;

        leftCut = true;

        UpdatePhaseUI();
        CheckCutsComplete();
    }

    public void CutRightSide() {
        if (currentPhase != RepairPhase.CutBrokenCable)
            return;

        if (rightCut)
            return;

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

        bool removed =
            currentInventory.RemoveItem(requiredCableName);

        if (!removed)
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

        isWrapping = true;

        UpdateCurrentWrapTarget(tapePosition);

        previousTapeAngle = GetTapeAngle(
            tapePosition,
            currentWrapCenter
        );
    }

    private void UpdateCurrentWrapTarget(Vector2 tapePosition) {
        float distanceToLeft =
            Vector2.Distance(tapePosition, leftWrapCenter.position);

        float distanceToRight =
            Vector2.Distance(tapePosition, rightWrapCenter.position);

        if (!leftWrapped &&
            distanceToLeft < distanceToRight) {
            currentWrapCenter = leftWrapCenter;
            wrappingLeft = true;
        } else if (!rightWrapped) {
            currentWrapCenter = rightWrapCenter;
            wrappingLeft = false;
        }
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
                    Debug.Log("Punta izquierda encintada");
                }
            } else {
                rightWrapRotation += rotationAmount;

                if (rightWrapRotation >= requiredWrapRotation) {
                    rightWrapped = true;
                    Debug.Log("Punta derecha encintada");
                }
            }
        }

        if (leftWrapped && rightWrapped) {
            isWrapping = false;
            CompleteCurrentPhase();
        }
    }

    public void StopWrapping() {
        isWrapping = false;
    }

    private void CompleteWrapSide() {
        if (wrappingLeft) {
            leftWrapped = true;
            Debug.Log("Punta izquierda encintada");
        } else {
            rightWrapped = true;
            Debug.Log("Punta derecha encintada");
        }

        if (leftWrapped && rightWrapped) {
            CompleteCurrentPhase();
        }
    }

    private float GetTapeAngle(Vector2 tapePosition, RectTransform center) {
        Vector2 direction = tapePosition - (Vector2)center.position;

        return Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
    }

    private void CompleteRepair() {
        currentComponent.Repair();

        Close();
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

    public void CheckCableRequired() {
        if (currentPhase != RepairPhase.PlaceNewCable)
            return;

        if (HasRequiredCable())
            return;

        cableRequiredMessage.SetActive(true);

        CancelInvoke(nameof(HideCableRequiredMessage));
        Invoke(nameof(HideCableRequiredMessage), 2f);
    }

    private void HideCableRequiredMessage() {
        cableRequiredMessage.SetActive(false);
    }
}
