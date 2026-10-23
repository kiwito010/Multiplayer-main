using Fusion;
using UnityEngine;

// Botones de "mantener apretado" que viajan por la red. Fusion detecta solo
// cuáles se apretaron en cada tick (NetworkButtons.GetPressed).
public enum PlayerButtons
{
    Jump = 0,
    Crouch = 1,
    Sprint = 2
}

// Esta estructura representa "el input de un jugador en un instante".
// Fusion no sincroniza el MOVIMIENTO directamente: sincroniza el INPUT
// y cada máquina simula el resultado usando ese mismo input.
public struct NetworkInputData : INetworkInput
{
    // Vector de movimiento en el plano XZ (WASD).
    public Vector2 moveDirection;

    // Movimiento del mouse ACUMULADO desde el tick anterior (en X para girar
    // el cuerpo, en Y para mirar arriba/abajo).
    public float lookYaw;
    public float lookPitch;

    // Botón genérico de "usar" (click izquierdo), usado por PlayerNetworkInteraction.
    public NetworkBool useButtonPressed;

    // Salto, agacharse y sprint.
    public NetworkButtons buttons;
}