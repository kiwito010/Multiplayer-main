using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerAnimatorController : NetworkBehaviour
{
    private Animator _animator;
    private CharacterController _characterController;

    public override void Spawned()
    {
        // Busca el Animator en los hijos (el modelo 3D del personaje)
        _animator = GetComponentInChildren<Animator>();
        
        // Busca el CharacterController para medir la velocidad de movimiento
        _characterController = GetComponent<CharacterController>();
    }

    public override void FixedUpdateNetwork()
    {
        // En Photon Fusion, aseguramos que solo el jugador local actualice sus propias animaciones
        if (Object != null && !Object.HasInputAuthority) return;

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        // 1. Velocidad horizontal para los Blend Trees (Locomoción y Agachado)
        float currentSpeed = 0f;
        if (_characterController != null)
        {
            Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
            currentSpeed = horizontalVelocity.magnitude;
        }
        _animator.SetFloat("Speed", currentSpeed);

        // 2. Estado de agachado (Bool) - Tecla C
        bool isCrouching = Input.GetKey(KeyCode.C);
        _animator.SetBool("IsCrouching", isCrouching);

        // 3. Salto (Trigger) - Tecla Espacio
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _animator.SetTrigger("Jump");
        }
    }
}