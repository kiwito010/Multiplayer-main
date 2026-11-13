using UnityEngine;
using Fusion;

public class LevelEndTrigger : NetworkBehaviour
{
    private int playersInTrigger = 0;
    private const int TotalPlayers = 3;
    private bool levelFinished = false;

    [SerializeField] private SceneRef targetScene;

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"[Trigger OK] Algo tocó el trigger: {other.name} (Tag: {other.tag})");

        if (levelFinished) return;

        if (other.CompareTag("Player"))
        {
            playersInTrigger++;
            Debug.Log($"Jugadores en zona: {playersInTrigger}/{TotalPlayers}");

            // Si sos el Host, cargás la escena para todos
            if (Object.HasStateAuthority && playersInTrigger >= TotalPlayers)
            {
                levelFinished = true;
                Runner.LoadScene(targetScene);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInTrigger = Mathf.Max(0, playersInTrigger - 1);
            Debug.Log($"Un jugador salió. Total: {playersInTrigger}/{TotalPlayers}");
        }
    }
}