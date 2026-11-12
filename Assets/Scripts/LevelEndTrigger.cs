using UnityEngine;
using Fusion;

public class LevelEndTrigger : NetworkBehaviour
{
    private int playersInTrigger = 0;
    private const int TotalPlayers = 3; // Ajustalo según la cantidad de jugadores
    private bool levelFinished = false;

    [SerializeField] private SceneRef targetScene; // Te permitirá seleccionar la escena desde el Inspector

    private void OnTriggerEnter(Collider other)
    {
        // Solo el Host (StateAuthority) maneja la lógica de cambio de escena en red
        if (Object.HasStateAuthority && !levelFinished && other.CompareTag("Player"))
        {
            playersInTrigger++;
            Debug.Log($"Jugadores en la zona: {playersInTrigger}/{TotalPlayers}");

            if (playersInTrigger >= TotalPlayers)
            {
                levelFinished = true;
                
                // Carga la escena de manera sincronizada en red usando SceneRef
                Runner.LoadScene(targetScene);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (Object.HasStateAuthority && !levelFinished && other.CompareTag("Player"))
        {
            playersInTrigger = Mathf.Max(0, playersInTrigger - 1);
        }
    }
}