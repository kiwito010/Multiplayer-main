using Fusion;
using UnityEngine;
using System.Collections;

public class FuseBox : NetworkBehaviour {
    // REFERENCIAS:

    [SerializeField] private RepairableComponent repairableComponent;

    [Header("Animación de la puerta")]
    [SerializeField] private Animator doorAnimator;

    [SerializeField] private AnimationClip termicasClip;

    private bool previousFuseInstalled;
    private Coroutine installationCoroutine;


    private bool previousOpened;

    [SerializeField] private GameObject closedVisual;
    [SerializeField] private GameObject openedVisual;
    [SerializeField] private GameObject installedFuseVisual;

    [SerializeField] private float interactionDistance = 3f;

    // ESTADO COMPARTIDO:

    [Networked] public NetworkBool isOpened { get; private set; }
    [Networked] public NetworkBool fuseInstalled { get; private set; }

    // MÉTODOS:


    public override void Spawned() {
        PlayerNetworkInteraction.OnUsedOnTarget += OnUsedOnTarget;

        previousOpened = isOpened;
        previousFuseInstalled = fuseInstalled;

        if (doorAnimator != null) {
            if (fuseInstalled) {
                // Para jugadores que llegan después:
                // dejar la secuencia en su estado final.
                doorAnimator.Play("ActivatingTermicas", 0, 1f);
                doorAnimator.Update(0f);

                doorAnimator.Play("Closing", 0, 1f);
                doorAnimator.Update(0f);
            } else if (isOpened) {
                // La caja ya estaba abierta.
                doorAnimator.Play("Opening", 0, 1f);
                doorAnimator.Update(0f);
            }
        }

        UpdateVisuals();
    }


    public override void Despawned(NetworkRunner runner, bool hasState) {
        PlayerNetworkInteraction.OnUsedOnTarget -= OnUsedOnTarget;
    }

    private void OnUsedOnTarget(PlayerRef player, NetworkObject target) {
        if (!Object.HasStateAuthority)
            return;

        if (target != Object)
            return;

        Debug.Log("FuseBox: interacción de " + player);

        NetworkObject playerObject = Runner.GetPlayerObject(player);

        if (playerObject == null)
            return;

        float distance = Vector3.Distance(
            playerObject.transform.position,
            transform.position
        );

        Debug.Log("FuseBox: distancia del jugador = " + distance);

        if (distance > interactionDistance) {
            Debug.LogWarning("FuseBox: jugador demasiado lejos");
            return;
        }

        if (!isOpened) {
            // No permitir abrir la caja hasta reparar el cable.
            if (repairableComponent == null ||
                !repairableComponent.cableRepairCompleted)
                return;

            isOpened = true;
            Debug.Log("Caja del fusible abierta");
            return;
        }

        if (fuseInstalled)
            return;

        if (repairableComponent != null &&
            !repairableComponent.cableRepairCompleted)
            return;

        NetworkInventoryState inventory = playerObject.GetComponent<NetworkInventoryState>();

        if (inventory == null)
            return;

        if (!inventory.TryConsumeItem(
            ItemType.Fuse,
            out NetworkObject fuse
        ))
            return;

        fuseInstalled = true;

        if (fuse != null) {
            Runner.Despawn(fuse);
        }

        Debug.Log("Fusible instalado correctamente");
    }



    public override void Render() {
        // La puerta se abre cuando el Host confirma la apertura.
        if (isOpened != previousOpened) {
            previousOpened = isOpened;

            if (isOpened && !fuseInstalled && doorAnimator != null) {
                doorAnimator.SetTrigger("Open");
            }
        }

        // La secuencia de instalación comienza una sola vez.
        if (fuseInstalled != previousFuseInstalled) {
            previousFuseInstalled = fuseInstalled;

            if (fuseInstalled) {
                if (installationCoroutine != null)
                    StopCoroutine(installationCoroutine);

                installationCoroutine = StartCoroutine(PlayInstallationSequence());
            }
        }

        UpdateVisuals();
    }

    private IEnumerator PlayInstallationSequence() {
        if (doorAnimator == null || termicasClip == null) {
            Debug.LogWarning("[FuseBox] Faltan referencias de animación.");
            installationCoroutine = null;
            yield break;
        }

        // Ejecutar la animación de las térmicas.
        doorAnimator.Play("ActivatingTermicas", 0, 0f);

        // Esperar a que termine.
        yield return new WaitForSecondsRealtime(
            termicasClip.length
        );

        // Cerrar la puerta automáticamente.
        doorAnimator.Play("Closing", 0, 0f);

        installationCoroutine = null;
    }


    private void UpdateVisuals() {
        // La puerta ahora se controla mediante el Animator.

        // Conservamos la visualización del fusible instalado.
        if (installedFuseVisual != null) {
            installedFuseVisual.SetActive(fuseInstalled);
        }
    }
}
