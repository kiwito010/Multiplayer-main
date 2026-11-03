using Fusion;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkManagerFusion : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Personajes (0=Ingeniero, 1=Hacker, 2=Exploradora)")]
    public NetworkPrefabRef[] characterPrefabs;

    [Header("Escenas")]
    [SerializeField] private string chooseCharacterSceneName = "ChooseCharacter";
    [SerializeField] private string lobbySceneName = "LobbyUI";
    [SerializeField] private string gameplaySceneName = "GameTest1";
    [SerializeField] private string mainMenuSceneName = "Main Menu";

    [Header("Salir de la partida")]
    [SerializeField] private KeyCode leaveKey = KeyCode.Escape;
    [SerializeField] private float leaveHoldDuration = 2f;
    private float _leaveHoldTimer = 0f;

    private NetworkRunner _runner;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private Dictionary<PlayerRef, int> _characterByPlayer = new Dictionary<PlayerRef, int>();

    private bool _inGame;
    private bool _hadLocalPlayer;
    private bool _leaving;

    private void Awake()
    {
        // Evita que el juego vaya en cámara lenta al perder el foco en pruebas de varias ventanas
        Application.runInBackground = true;
    }

    private void Update()
    {
        if (!_inGame || _leaving) return;

        if (!_hadLocalPlayer && PlayerMovement.Local != null)
        {
            _hadLocalPlayer = true;
        }

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
    //  LÓGICA DE CONEXIÓN
    // ---------------------------------------------------------

    public async void StartConnection()
    {
        if (_runner == null)
        {
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        _runner.ProvideInput = true;

        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        var startGameArgs = new StartGameArgs()
        {
            GameMode = GameSession.Mode,
            SessionName = GameSession.RoomCode,
            SceneManager = sceneManager,
            ConnectionToken = new byte[] { (byte)GameSession.CharacterIndex }
        };

        var result = await _runner.StartGame(startGameArgs);

        if (result.Ok)
        {
            Debug.Log("¡Conexión exitosa a la sala!");
            _inGame = true;
            DontDestroyOnLoad(gameObject);

            // Si somos el Host, cargamos la escena del Lobby para todos de forma sincronizada
            if (_runner.IsServer)
            {
                _runner.LoadScene(gameplaySceneName);
            }
        }
        else
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
            else
            {
                GameSession.Message = $"No se pudo conectar a la sala ({reason}).";
            }

            Destroy(sceneManager);
            Destroy(_runner);
            _runner = null;
            SceneManager.LoadScene(chooseCharacterSceneName);
        }
    }

    // ---------------------------------------------------------
    //  SPAWN DE JUGADORES (SOLO EN GAMEPLAY)
    // ---------------------------------------------------------

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        // 🛑 FILTRO CRUCIAL: Si estamos en el Lobby u otra escena, NO spawneamos el prefab 3D ni el HUD
        if (SceneManager.GetActiveScene().name != gameplaySceneName)
        {
            return;
        }

        if (runner.IsServer)
        {
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

            if (characterPrefabs == null || characterPrefabs.Length == 0)
            {
                Debug.LogError("El array 'Character Prefabs' está vacío en el NetworkManagerFusion.");
                return;
            }

            int characterIndex = 0;

            if (player == runner.LocalPlayer)
            {
                characterIndex = GameSession.CharacterIndex;
            }
            else
            {
                byte[] token = runner.GetPlayerConnectionToken(player);
                if (token != null && token.Length > 0)
                {
                    characterIndex = token[0];
                }
            }

            if (characterIndex < 0 || characterIndex >= characterPrefabs.Length)
            {
                characterIndex = 0;
            }

            // Validar personaje ocupado
            if (player != runner.LocalPlayer && _characterByPlayer.ContainsValue(characterIndex))
            {
                StartCoroutine(KickAfterDelay(runner, player));
                return;
            }

            NetworkObject networkPlayerObject = runner.Spawn(
                characterPrefabs[characterIndex],
                spawnPosition,
                Quaternion.identity,
                player
            );

            runner.SetPlayerObject(player, networkPlayerObject);

            _spawnedCharacters.Add(player, networkPlayerObject);
            _characterByPlayer[player] = characterIndex;
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

    // ---------------------------------------------------------
    //  SALIR DE LA PARTIDA Y CIERRE
    // ---------------------------------------------------------

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        if (!_inGame || _leaving) return;
        HandleSessionEnded(runner, false);
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        if (!_inGame || _leaving) return;
        HandleSessionEnded(runner, true);
    }

    private void HandleSessionEnded(NetworkRunner runner, bool shutdownRunner)
    {
        bool rejectedBeforeSpawn = !_hadLocalPlayer && GameSession.Mode == GameMode.Client;

        string message = rejectedBeforeSpawn
            ? "Ese personaje ya está ocupado. Elegí otro."
            : "Se cerró la sala o se perdió la conexión.";

        string targetScene = rejectedBeforeSpawn ? chooseCharacterSceneName : mainMenuSceneName;

        ExitToScene(targetScene, message, shutdownRunner);
    }

    private System.Collections.IEnumerator KickAfterDelay(NetworkRunner runner, PlayerRef player)
    {
        yield return new WaitForSeconds(0.5f);

        if (runner != null)
        {
            runner.Disconnect(player);
        }
    }

    private async void ExitToScene(string sceneName, string message, bool shutdownRunner)
    {
        if (_leaving) return;
        _leaving = true;
        _inGame = false;

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

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        data.moveDirection = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        data.lookYaw = Input.GetAxis("Mouse X");
        data.lookPitch = Input.GetAxis("Mouse Y");

        data.useButtonPressed = Input.GetKey(KeyCode.E);
        data.sprintPressed = Input.GetKey(KeyCode.LeftShift);
        data.jumpPressed = Input.GetKey(KeyCode.Space);

        if (PlayerMovement.Local != null) {
            GameInput gameInput =
                PlayerMovement.Local.GetComponent<GameInput>();

            if (gameInput != null) {
                data.crouchPressed = gameInput.GetCrouchHeld();
            }
        }

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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}