using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.SceneManagement;

// MonoBehaviour = un script normal de Unity que se puede poner en un GameObject.
// INetworkRunnerCallbacks = una interfaz de Fusion con "avisos" (eventos) de red:
// cuándo se conecta un jugador, cuándo se desconecta, cuándo hay que mandar el
// input, etc. Al escribir ": MonoBehaviour, INetworkRunnerCallbacks" le decimos
// a C# "este script va a escuchar esos avisos", y por eso más abajo vas a ver
// un montón de funciones "OnAlgo(...)" que hay que implementar sí o sí
// (aunque estén vacías) porque la interfaz las exige.
public class NetworkManagerFusion : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Personajes (el orden importa: 0=Ingeniero, 1=Hacker, 2=Científico, 3=Explorador)")]
    // Un prefab por personaje. NetworkPrefabRef es como un "GameObject", pero
    // apuntando a un prefab registrado en el sistema de red de Fusion (cada
    // uno tiene que tener el componente Network Object). El número de cada
    // casillero es el que manda cada botón de ChooseCharacter.
    public NetworkPrefabRef[] characterPrefabs;

    [Header("Escenas")]
    // Nombre exacto de la escena de juego (tiene que estar en Build Settings).
    // La buscamos por nombre y no por número, así agregar o reordenar
    // escenas no rompe nada.
    [SerializeField] private string gameplaySceneName = "Gameplay";

    // Esta variable va a guardar la instancia del NetworkRunner una vez
    // que la creamos. La necesitamos para poder usarla en varias funciones.
    private NetworkRunner _runner;

    // Diccionario que asocia cada jugador (PlayerRef = un ID único por jugador
    // en la partida) con el GameObject que le corresponde en la escena.
    // Nos sirve para poder destruir su personaje si se desconecta.
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private void Awake()
    {
        // Por defecto, Unity frena (o ralentiza muchisimo) el juego cuando su
        // ventana pierde el foco. Si probas Host + Client en la misma PC, una
        // de las dos ventanas SIEMPRE esta sin foco, entonces esa instancia
        // simula el juego en camara lenta y las posiciones no coinciden.
        // Con esto ambas siguen corriendo normal aunque no las estes mirando.
        Application.runInBackground = true;
    }

    // ---------------------------------------------------------
    //  LÓGICA PRINCIPAL DE CONEXIÓN
    // ---------------------------------------------------------

    // Se llama desde CharacterSelect (escena ChooseCharacter) apenas el
    // jugador elige su personaje. El modo (Host/Client) y el código de sala
    // los dejó guardados el MainMenu en GameSession.
    public void StartConnection()
    {
        // Si ya hay un runner conectándose, ignoramos clics repetidos.
        if (_runner != null) return;

        if (string.IsNullOrEmpty(GameSession.RoomCode))
        {
            Debug.LogWarning("No hay un código de sala guardado. Volvé al menú principal y escribilo.");
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }

        StartGame(GameSession.Mode, GameSession.RoomCode);
    }

    // async void: esta función corre en paralelo sin trabar el juego mientras
    // espera la respuesta del servidor de Photon.
    private async void StartGame(GameMode mode, string roomCode)
    {
        // El número de la escena de juego en Build Settings, buscado por nombre.
        int gameplayIndex = GetBuildIndexByName(gameplaySceneName);
        if (gameplayIndex < 0)
        {
            Debug.LogError($"No encontré la escena '{gameplaySceneName}' en Build Settings. Agregala en File > Build Settings.");
            return;
        }

        // Creamos el NetworkRunner: el componente que maneja toda la conexión.
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true; // este runner manda el input del jugador local

        // Permite a Fusion cambiar de escena de forma sincronizada.
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode,             // Host o Client
            SessionName = roomCode,      // todos los que usen este código van a la MISMA sala
            Scene = SceneRef.FromIndex(gameplayIndex),
            SceneManager = sceneManager,

            // El "ConnectionToken" viaja al Host junto con la conexión. Acá
            // mandamos 1 solo byte: el número del personaje elegido. El Host
            // lo lee en OnPlayerJoined para saber qué prefab spawnearte.
            ConnectionToken = new byte[] { (byte)GameSession.CharacterIndex }
        };

        var result = await _runner.StartGame(startGameArgs);

        if (!result.Ok)
        {
            Debug.LogError($"Fusion: no se pudo iniciar la partida. Motivo: {result.ShutdownReason}");

            // Limpiamos lo que creamos y volvemos al menú, para que el jugador
            // pueda corregir el código (por ejemplo, si la sala no existía).
            Destroy(sceneManager);
            Destroy(_runner);
            _runner = null;
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            return;
        }

        // Este objeto (el que tiene el NetworkRunner) tiene que sobrevivir
        // al cambio a la escena Gameplay, porque ahí vive la conexión de red.
        DontDestroyOnLoad(gameObject);
    }

    // Busca el número de una escena en Build Settings a partir de su nombre.
    private static int GetBuildIndexByName(string sceneName)
    {
        int count = UnityEngine.SceneManagement.SceneManager.sceneCountInBuildSettings;
        for (int i = 0; i < count; i++)
        {
            string path = UnityEngine.SceneManagement.SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
            {
                return i;
            }
        }
        return -1;
    }

    // ---------------------------------------------------------
    //  SPAWN DE JUGADORES
    // ---------------------------------------------------------

    // Fusion llama esta función automáticamente cada vez que un jugador
    // (vos incluido) termina de conectarse a la sala.
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // IMPORTANTE: solo el servidor (el Host) tiene autoridad para crear
        // objetos de red compartidos. Si dejáramos que el Client también
        // spawnee, tendríamos jugadores duplicados.
        if (runner.IsServer)
        {
            // Buscamos los puntos de spawn por Tag en vez de depender de un
            // array arrastrado a mano en el Inspector. Esto evita el
            // problema de que NetworkManager (en MainMenu) no puede
            // "ver" objetos que viven en la escena Gameplay hasta que esa
            // escena ya esté cargada — y en este punto del código,
            // gracias al NetworkSceneManagerDefault, ya lo está.
            GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

            Vector3 spawnPosition = Vector3.zero;
            if (spawnPointObjects.Length > 0)
            {
                int index = player.RawEncoded % spawnPointObjects.Length;
                spawnPosition = spawnPointObjects[index].transform.position;
            }
            else
            {
                // Aviso útil por si te olvidaste de ponerle la Tag a algún
                // SpawnPoint — así no te quedás preguntando por qué
                // aparecen todos en el origen sin ningún mensaje de alerta.
                Debug.LogWarning("No se encontró ningún objeto con la Tag 'SpawnPoint' en la escena. Los jugadores van a aparecer en (0,0,0).");
            }

            // --- ELEGIR EL PREFAB SEGÚN EL PERSONAJE ---
            if (characterPrefabs == null || characterPrefabs.Length == 0)
            {
                Debug.LogError("El array 'Character Prefabs' está vacío en el NetworkManagerFusion.");
                return;
            }

            int characterIndex = 0;

            if (player == runner.LocalPlayer)
            {
                // El Host: su elección está guardada en esta misma máquina.
                characterIndex = GameSession.CharacterIndex;
            }
            else
            {
                // Un cliente: su elección llegó en el ConnectionToken.
                // Devuelve null si no mandó ninguno.
                byte[] token = runner.GetPlayerConnectionToken(player);
                if (token != null && token.Length > 0)
                {
                    characterIndex = token[0];
                }
            }

            // Si el número no corresponde a ningún prefab, usamos el primero.
            if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
            {
                Debug.LogWarning($"Personaje {characterIndex} fuera de rango; se usa el 0.");
                characterIndex = 0;
            }

            // Runner.Spawn crea el objeto en TODAS las máquinas conectadas
            // de forma sincronizada (no es un Instantiate normal de Unity).
            // El parámetro "player" le dice a Fusion "este jugador es el
            // dueño de este objeto" (Input Authority) — así luego el script
            // de movimiento sabe si tiene que escuchar el teclado local o no.
            NetworkObject networkPlayerObject = runner.Spawn(
                characterPrefabs[characterIndex],
                spawnPosition,
                Quaternion.identity,
                player
            );

            // Guardamos la referencia para poder limpiarla después si el
            // jugador se desconecta.
            _spawnedCharacters.Add(player, networkPlayerObject);
        }
    }

    // Se llama cuando un jugador se desconecta de la partida.
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // Si el servidor tenía guardado el objeto de este jugador, lo destruimos
        // (esto también lo saca de las pantallas de todos los demás jugadores).
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    // ---------------------------------------------------------
    //  RECOLECCIÓN DE INPUT (se llama automáticamente cada tick de red)
    // ---------------------------------------------------------

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // Acá armamos el "paquete de input" de ESTE frame usando las teclas
        // que el jugador está apretando en ESTE momento, y se lo entregamos
        // a Fusion para que lo mande al resto de la partida.
        var data = new NetworkInputData();

        data.moveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"), // A/D o flechas izquierda/derecha
            Input.GetAxisRaw("Vertical")    // W/S o flechas arriba/abajo
        );

        data.lookYaw = Input.GetAxis("Mouse X");
        data.lookPitch = Input.GetAxis("Mouse Y");

        // GetButton (no GetButtonDown) porque queremos saber si se mantiene
        // apretado. Qué acción dispara este botón (arma, herramienta, lo
        // que definas vos) es responsabilidad de OTRO script, no de este.
        data.useButtonPressed = Input.GetButton("Fire1"); // Click izquierdo por defecto

        // Sprint mientras se mantiene apretada la tecla. CONFIRMAR: usé Shift
        // izquierdo; si tu GameInput usa otra tecla, cambiala acá.
        data.sprintPressed = Input.GetKey(KeyCode.LeftShift);

        // Salto con Espacio. CONFIRMAR: cambiá la tecla si tu GameInput usa otra.
        // Leemos si está MANTENIDA (GetKey) y no el instante en que se aprieta
        // (GetKeyDown): OnInput no corre en cada frame y podría perderse la
        // pulsación. El script detecta el momento exacto del apretado por su cuenta.
        data.jumpPressed = Input.GetKey(KeyCode.Space);

        input.Set(data); // Le entregamos el paquete armado a Fusion
    }

    // ---------------------------------------------------------
    //  El resto de estas funciones las exige la interfaz
    //  INetworkRunnerCallbacks, pero no las necesitamos para
    //  este proyecto todavía. Las dejamos vacías a propósito.
    // ---------------------------------------------------------
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}