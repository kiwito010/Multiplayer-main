using Fusion;
using UnityEngine;

// Esta estructura representa "el input de un jugador en un instante".
// Fusion no sincroniza el MOVIMIENTO directamente: sincroniza el INPUT
// (que teclas apretaste, hacia donde miraba el mouse) y cada maquina
// (la tuya, la del servidor, la de tus rivales) simula el resultado
// usando ese mismo input.
public struct NetworkInputData : INetworkInput
{
    // Vector de movimiento en el plano XZ.
    public Vector2 moveDirection;

    // Rotacion horizontal acumulada del mouse (girar el cuerpo).
    public float lookYaw;

    // Rotacion vertical del mouse (mirar arriba/abajo, solo afecta camara).
    public float lookPitch;

    // Boton generico de "usar/interactuar". Que hace exactamente al
    // apretarlo es decision tuya (arma, herramienta, lo que sea) — este
    // struct solo transporta el dato por la red, no le pone lógica encima.
    public NetworkBool useButtonPressed;
}
