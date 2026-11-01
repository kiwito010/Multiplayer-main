using System.Collections.Generic;
using System.Threading.Tasks;
using Fusion;
using Fusion.Sockets;
using UnityEngine;

// Averigua si existe una sala con cierto código SIN unirse a ella.
//
// Photon no permite preguntar "¿existe esta sala?" directamente. Lo que sí
// permite es entrar al "lobby" (la lista pública de salas abiertas) y leer
// esa lista. Eso hacemos: nos conectamos un momento al lobby, buscamos el
// código y nos desconectamos.
//
// No hace falta ponerlo en ningún objeto: se usa desde código con
//     bool? existe = await RoomLookup.RoomExists("codigo");
// Devuelve true (existe), false (no existe) o null (no se pudo verificar).
public class RoomLookup : MonoBehaviour, INetworkRunnerCallbacks
{
    // Última lista de salas que nos mandó Photon.
    private List<SessionInfo> _sessions;

    // Momento en que llegó la primera lista (-1 = todavía no llegó nada).
    private float _firstListTime = -1f;

    public static async Task<bool?> RoomExists(string roomCode, float timeoutSeconds = 10f, float extraWaitSeconds = 1.5f)
    {
        // Un objeto temporal con su propio NetworkRunner, que se borra al terminar.
        var holder = new GameObject("RoomLookup (temporal)");
        var runner = holder.AddComponent<NetworkRunner>();
        var lookup = holder.AddComponent<RoomLookup>();

        // Le decimos a Fusion a mano que este script escucha sus avisos.
        // Sin esto, el aviso con la lista de salas puede no llegar nunca.
        runner.AddCallbacks(lookup);

        try
        {
            // Entramos al lobby donde Photon lista las salas creadas como Host.
            StartGameResult joined = await runner.JoinSessionLobby(SessionLobby.ClientServer);
            Debug.Log($"[RoomLookup] Entrar al lobby terminó. Ok = {joined.Ok}");

            if (!joined.Ok)
            {
                Debug.LogWarning("[RoomLookup] No se pudo entrar al lobby: " + joined.ErrorMessage);
                return null;
            }

            float start = Time.realtimeSinceStartup;

            while (Time.realtimeSinceStartup - start < timeoutSeconds)
            {
                if (lookup.HasSession(roomCode))
                {
                    Debug.Log($"[RoomLookup] La sala '{roomCode}' existe.");
                    return true;
                }

                // Ya llegó la lista y la sala no está. Esperamos un margen
                // por si la primera lista llegó incompleta antes de decir que no existe.
                if (lookup._firstListTime >= 0f &&
                    Time.realtimeSinceStartup - lookup._firstListTime >= extraWaitSeconds)
                {
                    Debug.Log($"[RoomLookup] La sala '{roomCode}' NO está en la lista de {lookup._sessions.Count} sala(s).");
                    return false;
                }

                await Task.Yield(); // espera un frame y vuelve a mirar
            }

            // Pasó el tiempo y nunca llegó una lista: no pudimos verificar.
            Debug.LogWarning($"[RoomLookup] Pasaron {timeoutSeconds} segundos y Photon nunca mandó la lista de salas.");
            return null;
        }
        finally
        {
            // Pase lo que pase, cerramos la conexión temporal.
            if (runner != null)
            {
                await runner.Shutdown();
            }

            if (holder != null)
            {
                Destroy(holder);
            }
        }
    }

    private bool HasSession(string roomCode)
    {
        if (_sessions == null) return false;

        for (int i = 0; i < _sessions.Count; i++)
        {
            if (_sessions[i].Name == roomCode) return true;
        }

        return false;
    }

    // Photon llama a esta función cada vez que cambia la lista de salas.
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) 
    {
        // Copiamos la lista porque Photon puede reutilizar la original.
        _sessions = new List<SessionInfo>(sessionList);

        var names = new List<string>();
        foreach (SessionInfo session in _sessions) names.Add(session.Name);
        Debug.Log($"[RoomLookup] Llegó la lista de salas: {_sessions.Count} sala(s) -> [{string.Join(", ", names)}]");

        if (_firstListTime < 0f)
        {
            _firstListTime = Time.realtimeSinceStartup;
        }
    }

    // ---------------------------------------------------------
    //  El resto de los avisos de la interfaz no se usan acá.
    // ---------------------------------------------------------
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}