using Fusion;
using UnityEngine;

public class ExitDoorController : MonoBehaviour {
    [Header("Referencias")]
    [SerializeField] private NetworkComputer computer;
    [SerializeField] private Animator doorAnimator;

    private bool initialized;
    private bool previousCompleted;

    private void Update() {
        if (computer == null ||
            computer.Object == null ||
            !computer.Object.IsValid ||
            doorAnimator == null)
            return;

        bool completed = computer.IsCompleted;

        // Al iniciar, comprobar si la puerta
        // ya debería estar abierta.
        if (!initialized) {
            initialized = true;
            previousCompleted = completed;

            if (completed) {
                // Si el hackeo ya había terminado,
                // colocar la puerta directamente abierta.
                doorAnimator.Play("Opening", 0, 1f);
                doorAnimator.Update(0f);
            }

            return;
        }

        // Detectar cuando termina el hackeo.
        if (completed && !previousCompleted) {
            doorAnimator.SetTrigger("Open");

            Debug.Log(
                "[EXIT DOOR] Hackeo completado. Abriendo puerta."
            );
        }

        previousCompleted = completed;
    }
}
