using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

// MonoBehaviour = un script normal de Unity que se puede poner en un GameObject.
// INetworkRunnerCallbacks = una interfaz de Fusion con "avisos" (eventos) de red:
// cuándo se conecta un jugador, cuándo se desconecta, cuándo hay que mandar el
// input, etc. Al escribir ": MonoBehaviour, INetworkRunnerCallbacks" le decimos
// a C# "este script va a escuchar esos avisos", y por eso más abajo vas a ver
// un montón de funciones "OnAlgo(...)" que hay que implementar sí o sí
// (aunque estén vacías) porque la interfaz las exige.
public class NetworkManagerFusion : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Referencias de UI (arrastrar desde la Hierarchy)")]
    // Campo donde el jugador que CREA la sala escribe el código que quiere usar.
    // [FormerlySerializedAs] le dice a Unity "este campo antes se llamaba
    // roomNameInput", así conserva lo que ya tenías arrastrado en el Inspector
    // en vez de dejarlo vacío al cambiar el nombre de la variable.
    [FormerlySerializedAs("roomNameInput")]
    public TMP_InputField createRoomInput;

    // Campo donde el jugador que SE UNE escribe el código de la sala a la que quiere entrar.
    public TMP_InputField joinRoomInput;

    [Header("Prefab del jugador")]
    // NetworkPrefabRef es como un "GameObject" normal, pero apuntando
    // específicamente a un prefab registrado en el sistema de red de Fusion.
    // Fusion lo necesita así (no un GameObject común) para poder instanciarlo
    // de forma sincronizada en todas las máquinas conectadas.
    public NetworkPrefabRef playerPrefab;

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
    //  FUNCIONES QUE SE CONECTAN A LOS BOTONES DEL MENÚ
    // ---------------------------------------------------------

    // Esta función la vamos a enganchar al OnClick() del botón "Crear Partida"
    public void OnHostButtonPressed()
    {
        // Si ya hay un runner conectándose o conectado, ignoramos clics
        // repetidos (por ejemplo, si el jugador hace doble clic sin querer,
        // o aprieta el botón varias veces mientras espera la conexión).
        if (_runner != null) return;

        // Leemos el código del campo de "Crear sala". Trim() le saca los
        // espacios del principio y del final, para que "abc " y "abc" sean
        // el mismo código (si no, un espacio de más haría que no coincidan).
        string code = createRoomInput.text.Trim();

        // Exigimos que haya un código; si no, avisamos y no hacemos nada.
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Escribí un código en el campo de 'Crear sala' antes de apretar Crear.");
            return;
        }

        // GameMode.Host: esta máquina va a ser servidor Y jugador al mismo tiempo.
        // Tiene autoridad total sobre la partida (es el "dueño" de la sala).
        StartGame(GameMode.Host, code);
    }

    // Esta función la vamos a enganchar al OnClick() del botón "Unirse"
    public void OnJoinButtonPressed()
    {
        if (_runner != null) return;

        // Acá leemos el campo de "Unirse a sala", que es otro distinto.
        string code = joinRoomInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Escribí el código de la sala en el campo de 'Unirse a sala' antes de apretar Unirse.");
            return;
        }

        // GameMode.Client: esta máquina se conecta a una sala que ya existe,
        // creada por otro jugador en modo Host.
        StartGame(GameMode.Client, code);
    }

    // ---------------------------------------------------------
    //  LÓGICA PRINCIPAL DE CONEXIÓN
    // ---------------------------------------------------------

    // async void: esta función corre en paralelo sin trabar el juego mientras
    // espera la respuesta del servidor de Photon (conectarse tarda un ratito,
    // y no queremos que el juego se "congele" mientras tanto).
    private async void StartGame(GameMode mode, string roomCode)
    {
        // Creamos un GameObject vacío en tiempo de ejecución (no lo vas a ver
        // en la Hierarchy hasta que le des Play) y le agregamos el componente
        // NetworkRunner: es el objeto que realmente maneja toda la conexión.
        _runner = gameObject.AddComponent<NetworkRunner>();

        // Le decimos al runner "avisame a MÍ (este mismo script) cuando pase
        // algo en la red" — por eso este script implementa INetworkRunnerCallbacks.
        _runner.ProvideInput = true; // Además, este runner va a mandar el input del jugador local

        // NetworkSceneManagerDefault es el componente que le permite a Fusion
        // cargar/cambiar de escena de forma sincronizada entre todos los jugadores.
        var sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        // Armamos el "paquete de argumentos" para iniciar la partida:
        var startGameArgs = new StartGameArgs()
        {
            GameMode = mode, // Host o Client, según qué botón se apretó

            // SessionName es el "nombre de la sala". Todos los jugadores que
            // usen el mismo nombre se van a conectar a la MISMA partida.
            // Acá usamos el código que escribió el jugador (del campo de
            // "Crear" o del de "Unirse", según el botón que apretó).
            SessionName = roomCode,

            Scene = SceneRef.FromIndex(2), // Índice 2 = la escena "Gameplay" (0=TitleScreen, 1=MainMenu, 2=Gameplay)
            SceneManager = sceneManager
        };

        // Esta línea es la que realmente dispara la conexión a los servidores
        // de Photon. "await" significa "esperá acá a que termine, sin trabar
        // el resto del juego mientras tanto".
        var result = await _runner.StartGame(startGameArgs);

        // Si algo salió mal (por ejemplo, no hay internet, o la sala no existe
        // cuando intentás unirte), result.Ok va a ser false.
        if (!result.Ok)
        {
            Debug.LogError($"Fusion: no se pudo iniciar la partida. Motivo: {result.ShutdownReason}");

            // Limpiamos los componentes que creamos y dejamos _runner en null.
            // Si no hiciéramos esto, el chequeo "if (_runner != null) return;"
            // de los botones bloquearía para siempre cualquier reintento
            // (por ejemplo, si te equivocaste de código al unirte).
            Destroy(sceneManager);
            Destroy(_runner);
            _runner = null;
            return;
        }

        // Este objeto (el que tiene el NetworkRunner) tiene que sobrevivir
        // cuando cambiemos de la escena MainMenu a la escena Gameplay,
        // porque ahí vive toda la conexión de red.
        DontDestroyOnLoad(gameObject);
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

            // Runner.Spawn crea el objeto en TODAS las máquinas conectadas
            // de forma sincronizada (no es un Instantiate normal de Unity).
            // El parámetro "player" le dice a Fusion "este jugador es el
            // dueño de este objeto" (Input Authority) — así luego el script
            // de movimiento sabe si tiene que escuchar el teclado local o no.
            NetworkObject networkPlayerObject = runner.Spawn(
                playerPrefab,
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