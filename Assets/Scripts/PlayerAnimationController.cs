using Fusion;
using UnityEngine;

public class PlayerAnimatorController : NetworkBehaviour
{
    private Animator _animator;
    private Vector3 _lastPosition;
    private bool _isCrouching = false;

    public override void Spawned()
    {
        // Busca el Animator en el modelo 3D hijo
        _animator = GetComponentInChildren<Animator>();
        
        // Guardamos la posición inicial para calcular la velocidad real
        _lastPosition = transform.position;
    }

    public override void FixedUpdateNetwork()
    {
        // Asegura que solo el jugador local ejecute sus animaciones en red
        if (Object != null && !Object.HasInputAuthority) return;

        UpdateAnimations();
    }

    private void UpdateAnimations()
    {
        if (_animator == null) return;

        // 1. VELOCIDAD REAL (Independiente de si usas CharacterController o transform)
        Vector3 currentPosition = transform.position;
        Vector3 displacement = currentPosition - _lastPosition;
        
        // Calculamos la velocidad en el plano horizontal (X y Z) dividida por el tiempo de red
        Vector3 horizontalVelocity = new Vector3(displacement.x, 0f, displacement.z) / Runner.DeltaTime;
        _animator.SetFloat("Speed", horizontalVelocity.magnitude);
        
        // Actualizamos la última posición para el siguiente frame
        _lastPosition = currentPosition;

        // 2. AGACHADO TIPO TOGGLE (Presionar Ctrl una vez para agacharse, otra para levantarse)
        if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
        {
            _isCrouching = !_isCrouching;
        }
        _animator.SetBool("IsCrouching", _isCrouching);

        // 3. SALTO (Trigger con Espacio)
        if (Input.GetKeyDown(KeyCode.Space))
        {
            _animator.SetTrigger("Jump");
        }
    }
}