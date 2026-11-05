using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class NetworkCoolantSpawner : MonoBehaviour {

    // VARIABLES:

    [Header("Refrigerantes")]
    [SerializeField] private NetworkObject coolantPrefab;

    [Header("Puntos de Spawn")]
    [SerializeField] private Transform[] spawnPoints;

    [SerializeField] private int maxCoolants = 3;

    private readonly List<NetworkObject> activeCoolants =
        new List<NetworkObject>();

    private readonly List<int> occupiedSpawnPoints =
        new List<int>();

    private NetworkRunner runner;

    // MÉTODOS:

    public void StartSpawning(NetworkRunner networkRunner) {
        if (networkRunner == null || !networkRunner.IsServer)
            return;

        if (coolantPrefab == null || spawnPoints.Length == 0) {
            Debug.LogWarning("Falta configurar el spawner de refrigerantes");
            return;
        }

        runner = networkRunner;

        for (int i = 0; i < maxCoolants; i++) {
            SpawnCoolant();
        }
    }

    private void SpawnCoolant() {
        if (runner == null || !runner.IsServer)
            return;

        if (activeCoolants.Count >= maxCoolants)
            return;

        List<int> availablePoints = new List<int>();

        for (int i = 0; i < spawnPoints.Length; i++) {
            if (spawnPoints[i] != null &&
                !occupiedSpawnPoints.Contains(i)) {
                availablePoints.Add(i);
            }
        }

        if (availablePoints.Count == 0)
            return;

        int randomIndex = Random.Range(0, availablePoints.Count);
        int spawnIndex = availablePoints[randomIndex];

        Transform spawnPoint = spawnPoints[spawnIndex];

        NetworkObject coolant = runner.Spawn(
            coolantPrefab,
            spawnPoint.position,
            spawnPoint.rotation
        );

        if (coolant == null)
            return;

        activeCoolants.Add(coolant);
        occupiedSpawnPoints.Add(spawnIndex);

        Debug.Log("Refrigerante generado en Spawn_" + (spawnIndex + 1));
    }

    public void NotifyCoolantConsumed(NetworkObject coolant) {
        if (runner == null || !runner.IsServer)
            return;

        int index = activeCoolants.IndexOf(coolant);

        if (index < 0)
            return;

        activeCoolants.RemoveAt(index);
        occupiedSpawnPoints.RemoveAt(index);

        SpawnCoolant();
    }

    public void StopSpawning() {
        // La limpieza de los inventarios se agregará
        // antes de conectar este método al hackeo.
    }
}
