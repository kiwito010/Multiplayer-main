using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Maneja la conexión de red durante TODO el recorrido:
//   ChooseCharacter (se conecta) -> LobbyUI (sala de espera) -> Gameplay (partida).
// Se crea en ChooseCharacter y sobrevive al cambio de escena (DontDestroyOnLoad).
public class NetworkManagerFusion : MonoBehaviour, INetworkRunnerCallbacks
{
    // Acceso rápido desde otras escenas (por ejemplo, desde el LobbyManager).
    public static NetworkManagerFusion Instance { get; private set; }

    [Header("Personajes (0=Ingeniero, 1=Hacker, 2=Exploradora)")]
    public NetworkPrefabRef[] characterPrefabs;

    [Header("Escenas (los nombres tienen que estar en Build Settings)")]
    [SerializeField] private string lobbySceneName = "LobbyUI";
    [SerializeField] private string gameplaySceneName = "GameTest1";
    [SerializeField] private string mainMenuSceneName = "Main Menu";
    [SerializeField] private string titleSceneName = "TitleScreen";

    [Header("Sala")]
    // Cuántos jugadores hacen falta para empezar, y también el máximo que entra en la sala.
    [SerializeField] private int playersPerGame = 3;

    [Header("Salir de la partida (mantener apretada la tecla, solo en el juego)")]
    [SerializeField] private KeyCode leaveKey = KeyCode.Escape;
    [SerializeField] private float leaveHoldDuration = 2f;
    private float _leaveHoldTimer = 0f;

    private NetworkRunner _runner;

    // Solo lo usa el Host: qué personaje tiene cada jugador y qué objeto le tocó.
    private readonly Dictionary<PlayerRef, int> _characterByPlayer = new Dictionary<PlayerRef, int>();
    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private bool _connecting;          // la conexión se está haciendo ahora
    private bool _connected;           // hay una sesión activa (lobby o partida)
    private bool _persistent;          // ya pasó a DontDestroyOnLoad
    private bool _gameStarted;         // el Host apretó "Empezar"
    private bool _gameplaySceneReady;  // la escena de juego ya cargó (solo Host)
    private bool _leaving;

    public int CharacterCount => characterPrefabs != null ? characterPrefabs.Length : 0;

    // Cuántos jugadores hacen falta para empezar.
    public int PlayersNeeded => playersPerGame;

    // Cuántos jugadores hay conectados ahora mismo (el Host incluido).
    public int ConnectedPlayers
    {
        get
        {
            if (_runner == null) return 0;

            int count = 0;
            foreach (PlayerRef player in _runner.ActivePlayers) count++;
            return count;
        }
    }

    private void Awake()
    {
        // Si ya hay un manager que sobrevive entre escenas, este es un duplicado.
        if (Instance != null && Instance != this && Instance._persistent)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        // Evita que el juego vaya en cámara lenta al perder el foco en pruebas de varias ventanas.
        Application.runInBackground = true;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        // La tecla de salida solo vale dentro de la partida (en el lobby hay botón "Salir").
        if (!_connected || _leaving) return;
        if (SceneManager.GetActiveScene().name != gameplaySceneName) return;

        if (Input.GetKey(leaveKey))
        {
            _leaveHoldTimer += Time.deltaTime;
            if (_leaveHoldTimer >= leaveHoldDuration)
            {
                ExitToScene(mainMenuSceneName, null, true);
            }
        }
        else
        {
            _leaveHoldTimer = 0f;
        }
    }

    // ---------------------------------------------------------
    //  CONEXIÓN
    // ---------------------------------------------------------

    // La llama CharacterSelect al apretar "Seleccionar".
    public async void StartConnection()
    {
        // Evita conectar dos veces (doble click).
        if (_connecting || _connected) return;

        if (string.IsNullOrEmpty(GameSession.RoomCode))
        {
            GameSession.Message = "No hay un código de sala. Escribilo en el menú principal.";
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        _connecting = true;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameSession.Mode,
            SessionName = GameSession.RoomCode,
            SceneManager = sceneManager,

            // Máximo de jugadores de la sala (el Host incluido). Solo vale al CREARLA:
            // cuando está llena, Photon rechaza a quien quiera entrar.
            PlayerCount = playersPerGame,

            // El personaje elegido viaja al Host con la conexión.
            ConnectionToken = new byte[] { (byte)Mathf.Max(0, GameSession.CharacterIndex) }
        };

        var result = await _runner.StartGame(startGameArgs);

        if (this == null) return;
        _connecting = false;

        if (!result.Ok)
        {
            Debug.LogError($"Fusion: no se pudo iniciar la partida. Motivo: {result.ShutdownReason}");

            string reason = result.ShutdownReason.ToString();
            if (reason == "GameNotFound")
            {
                GameSession.Message = "No existe ninguna sala con ese código.";
            }
            else if (reason == "GameIdAlreadyExists")
            {
                GameSession.Message = "Ya existe una sala con ese código. Probá con otro.";
            }
            else if (reason == "GameIsFull" || reason.Contains("Full"))
            {
                GameSession.Message = $"La sala ya está llena ({playersPerGame}/{playersPerGame} jugadores).";
            }
            else
            {
                GameSession.Message = $"No se pudo conectar a la sala ({reason}).";
            }

            Destroy(sceneManager);
            Destroy(_runner);
            _runner = null;

            // El código se escribe en el menú principal, así que se vuelve ahí a corregirlo.
            SceneManager.LoadScene(mainMenuSceneName);
            return;
        }

        Debug.Log("¡Conexión exitosa a la sala!");
        _connected = true;
        _persistent = true;
        DontDestroyOnLoad(gameObject);

        // El Host lleva a todos al lobby (los clientes siguen la escena del Host).
        if (_runner.IsServer)
        {
            _runner.LoadScene(lobbySceneName);
        }
    }

    // La llama el LobbyManager cuando el Host aprieta "Empezar".
    // Devuelve true si la partida empezó; false si todavía no se puede
    // (por ejemplo, faltan jugadores).
    public bool BeginGame()
    {
        if (_runner == null || !_runner.IsServer || _gameStarted) return false;

        // Sin todos los jugadores no se empieza, aunque alguien fuerce el botón.
        if (ConnectedPlayers < playersPerGame) return false;

        _gameStarted = true;
        _gameplaySceneReady = false;

        // Carga la escena de juego para todos. Los jugadores se crean cuando
        // termina de cargar (ver OnSceneLoadDone).
        _runner.LoadScene(gameplaySceneName);
        return true;
    }

    // ---------------------------------------------------------
    //  PERSONAJES (solo el Host decide)
    // ---------------------------------------------------------

    public int GetCharacter(PlayerRef player)
    {
        return _characterByPlayer.TryGetValue(player, out int index) ? index : -1;
    }

    public bool IsCharacterTaken(int index, PlayerRef except)
    {
        foreach (var pair in _characterByPlayer)
        {
            if (pair.Key != except && pair.Value == index) return true;
        }
        return false;
    }

    // true si se pudo asignar (el personaje existe y nadie más lo tiene).
    public bool TrySetCharacter(PlayerRef player, int index)
    {
        if (index < 0 || index >= CharacterCount) return false;
        if (IsCharacterTaken(index, player)) return false;

        _characterByPlayer[player] = index;
        return true;
    }

    // Al entrar: se intenta darle el personaje que pidió; si está ocupado, el primero libre.
    private bool AssignCharacterOnJoin(PlayerRef player, int requested)
    {
        int count = CharacterCount;
        if (count == 0) return false;

        requested = Mathf.Clamp(requested, 0, count - 1);
        if (TrySetCharacter(player, requested)) return true;

        for (int i = 0; i < count; i++)
        {
            if (TrySetCharacter(player, i)) return true;
        }

        return false; // no queda ningún personaje libre
    }

    // ---------------------------------------------------------
    //  JUGADORES: ENTRAN AL LOBBY, APARECEN EN LA PARTIDA
    // ---------------------------------------------------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        if (CharacterCount == 0)
        {
            Debug.LogError("El array 'Character Prefabs' está vacío en el NetworkManagerFusion.");
            return;
        }

        // Qué personaje pidió: el Host lo tiene guardado acá; un cliente lo mandó en el token.
        int requested = 0;
        if (player == runner.LocalPlayer)
        {
            requested = GameSession.CharacterIndex;
        }
        else
        {
            byte[] token = runner.GetPlayerConnectionToken(player);
            if (token != null && token.Length > 0) requested = token[0];
        }

        if (!AssignCharacterOnJoin(player, requested))
        {
            // Sala llena (hay tantos jugadores como personajes).
            StartCoroutine(KickAfterDelay(runner, player));
            return;
        }

        // Si la partida ya empezó y la escena ya cargó (entró tarde), aparece directo.
        if (_gameStarted && _gameplaySceneReady)
        {
            SpawnCharacter(runner, player);
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        _characterByPlayer.Remove(player);

        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }
    }

    // Fusion lo llama cuando termina de cargar una escena (la del lobby, y después la del juego).
    // ACÁ es donde aparecen todos los jugadores al empezar la partida.
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer || !_gameStarted) return;

        // Ignoramos la carga del lobby: solo nos interesa cuando ya cargó la escena de juego.
        Scene gameplay = SceneManager.GetSceneByName(gameplaySceneName);
        if (!gameplay.isLoaded) return;

        _gameplaySceneReady = true;

        foreach (PlayerRef player in runner.ActivePlayers)
        {
            SpawnCharacter(runner, player);
        }
    }

    private void SpawnCharacter(NetworkRunner runner, PlayerRef player)
    {
        // Ya tiene su personaje.
        if (_spawnedCharacters.ContainsKey(player)) return;

        int characterIndex = GetCharacter(player);
        if (characterIndex < 0 || characterIndex >= CharacterCount) characterIndex = 0;

        GameObject[] spawnPointObjects = GameObject.FindGameObjectsWithTag("SpawnPoint");

        Vector3 spawnPosition = Vector3.zero;
        if (spawnPointObjects.Length > 0)
        {
            int index = player.RawEncoded % spawnPointObjects.Length;
            spawnPosition = spawnPointObjects[index].transform.position;
        }
        else
        {
            Debug.LogWarning("No se encontró ningún objeto con la Tag 'SpawnPoint'. Los jugadores aparecerán en (0,0,0).");
        }

        NetworkObject networkPlayerObject = runner.Spawn(
            characterPrefabs[characterIndex],
            spawnPosition,
            Quaternion.identity,
            player
        );

        runner.SetPlayerObject(player, networkPlayerObject);
        _spawnedCharacters[player] = networkPlayerObject;
    }

    private System.Collections.IEnumerator KickAfterDelay(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.5f);

        if (runner != null)
        {
            runner.Disconnect(player);
        }
    }

    // ---------------------------------------------------------
    //  SALIR / CIERRE
    // ---------------------------------------------------------

    // La llama el botón "Salir" del lobby (después del "¿Estás seguro?").
    public void LeaveToTitle()
    {
        ExitToScene(titleSceneName, null, true);
    }

    // Fusion avisa que la sesión terminó (por ejemplo, el Host cerró la sala).
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (!_connected || _leaving) return;
        ExitToScene(mainMenuSceneName, "Se cerró la sala o se perdió la conexión.", false);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (!_connected || _leaving) return;
        ExitToScene(mainMenuSceneName, "Se cerró la sala o se perdió la conexión.", true);
    }

    private async void ExitToScene(string sceneName, string message, bool shutdownRunner)
    {
        if (_leaving) return;
        _leaving = true;
        _connected = false;
        _gameStarted = false;

        GameSession.Message = message ?? "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        NetworkRunner runner = _runner;
        if (shutdownRunner && runner != null)
        {
            await runner.Shutdown();
        }

        SceneManager.LoadScene(sceneName);

        if (this != null)
        {
            Destroy(gameObject);
        }
    }

    // ---------------------------------------------------------
    //  RECOLECCIÓN DE INPUT
    // ---------------------------------------------------------


    public void OnInput(NetworkRunner runner, NetworkInput input) {
        var data = new NetworkInputData();

        NetworkObject localPlayer =
            runner.GetPlayerObject(runner.LocalPlayer);

        if (localPlayer == null) {
            input.Set(data);
            return;
        }

        GameInput gameInput = localPlayer.GetComponent<GameInput>();

        if (gameInput == null) {
            input.Set(data);
            return;
        }

        // Respetar el bloqueo de movimiento al abrir una UI.
        if (!gameInput.IsMovementEnabled()) {
            input.Set(data);
            return;
        }

        // Movimiento y cámara.
        data.moveDirection = gameInput.GetMovementVector();

        Vector2 lookInput = gameInput.GetLookVector();

        data.lookYaw = lookInput.x;
        data.lookPitch = lookInput.y;

        // Acciones del jugador.
        data.useButtonPressed = gameInput.GetInteractHeld();
        data.sprintPressed = gameInput.GetSprintPressed();
        data.jumpPressed = gameInput.GetJumpHeld();
        data.crouchPressed = gameInput.GetCrouchHeld();
        data.pronePressed = gameInput.GetProneHeld();

        input.Set(data);
    }


    // Callbacks vacíos obligatorios de la interfaz INetworkRunnerCallbacks
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ReadOnlySpan<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}