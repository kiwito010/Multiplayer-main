using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PlayerAnimationController : NetworkBehaviour
{
    private Animator _animator;
    
    // Si usas un CharacterController o Rigidbody para mover al jugador
    private CharacterController _characterController; 

    public override void Spawned()
    {
        // Buscamos el componente Animator (suele estar en el modelo hijo del prefab)
        _animator = GetComponentInChildren<Animator>();
        _characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        // IMPORTANTE: En juegos multijugador con Photon Fusion, 
        // solo queremos que el cliente local mueva sus propias animaciones visuales 
        // para evitar conflictos de predicción, o dejamos que el componente NetworkAnimator lo sincronice.
        if (Object != null && !Object.HasInputAuthority) 
            return;

        if (_animator == null) return;

        UpdateAnimatorParameters();
    }

    private void UpdateAnimatorParameters()
    {
        // 1. ACTUALIZAR SPEED (Blend Tree)
        // Calculamos la velocidad en el plano horizontal (X y Z), ignorando la altura (Y)
        Vector3 horizontalVelocity = new Vector3(_characterController.velocity.x, 0f, _characterController.velocity.z);
        float currentSpeed = horizontalVelocity.magnitude;
        
        // Enviamos el valor al parámetro "Speed" del Animator
        _animator.SetFloat("Speed", currentSpeed);

        // 2. ACTUALIZAR JUMP (Trigger)
        // Si el jugador presiona espacio, disparamos el Trigger del salto
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _animator.SetTrigger("Jump");
        }

        // 3. ACTUALIZAR IS CROUCHING (Bool)
        // Verificamos si mantiene presionada la tecla para agacharse (ej: C o LeftControl)
        bool isCrouching = Input.GetKey(KeyCode.C);
        _animator.SetBool("IsCrouching", isCrouching);
    }
}
