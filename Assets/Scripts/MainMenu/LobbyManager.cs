using System.Collections.Generic;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en un objeto de la escena LobbyUI que tenga TAMBIÉN un componente NetworkObject.
public class LobbyManager : NetworkBehaviour
{
    // Cantidad máxima de jugadores que se pueden mostrar (se indexa por PlayerId).
    private const int MaxPlayers = 8;
    private const int MaxVisibleChatLines = 8;

    [Header("Escena (solo se usa si probás el lobby sin conexión)")]
    [SerializeField] private string titleSceneName = "TitleScreen";

    [Header("UI del Lobby")]
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Transform playerListContent;   // contenedor de la lista
    [SerializeField] private GameObject playerEntryPrefab;  // una fila (puede ser un prefab o la fila de ejemplo de la escena)
    [SerializeField] private Button startGameButton;        // solo lo ve y lo usa el Host
    [SerializeField] private Button changeColorButton;      // cambiar de personaje
    [SerializeField] private Button leaveButton;

    [Header("Botón Empezar")]
    // Color del botón mientras no estén todos los jugadores (sigue recibiendo el click para avisar por qué).
    [SerializeField] private Color startDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Popup de confirmación (Salir)")]
    [SerializeField] private GameObject leaveConfirmPopup;
    [SerializeField] private Button leaveYesButton;   // opcional: si lo asignás, no hace falta conectarlo a mano
    [SerializeField] private Button leaveNoButton;    // opcional

    [Header("Chat por texto (apretar Enter)")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private GameObject chatDisplayPanel;
    [SerializeField] private TMP_Text chatLogText;
    [SerializeField] private float messageDuration = 5f;  // cuánto se ve cada mensaje
    [SerializeField] private float fadeDuration = 1f;     // cuánto tarda en desaparecer

    [Header("Personajes (0=Ingeniero, 1=Hacker, 2=Exploradora)")]
    [SerializeField] private Sprite[] characterPortraits;
    [SerializeField] private string[] characterNames = { "Ingeniero", "Hacker", "Exploradora" };

    [Header("Filas de jugadores")]
    [SerializeField] private float rowSpacing = 10f;   // separación entre una fila y la de abajo

    // ---------------------------------------------------------
    //  ESTADO
    // ---------------------------------------------------------
    // Personaje de cada jugador MÁS UNO (0 = ninguno), indexado por PlayerId.
    // Lo escribe el Host y lo leen todos para dibujar la lista.
    [Networked, Capacity(MaxPlayers)] private NetworkArray<int> CharacterPlusOne => default;

    private readonly List<GameObject> _entries = new List<GameObject>();
    private readonly bool[] _activeScratch = new bool[MaxPlayers];
    private float _refreshTimer;
    private Color _startNormalColor = Color.white;
    private bool _leaving;

    private struct ChatLine
    {
        public string text;
        public float time;
    }

    private readonly List<ChatLine> _chatLines = new List<ChatLine>();
    private readonly System.Text.StringBuilder _builder = new System.Text.StringBuilder();
    private bool _chatOpen;
    private int _ignoreEnterFrame = -1;

    private bool IsReady => Object != null && Object.IsValid && Runner != null;

    // ---------------------------------------------------------
    //  ARRANQUE: la UI se conecta ACÁ, sin depender de la red
    // ---------------------------------------------------------
    private void Awake()
    {
        EnsureEventSystem();
        ResolveRowTemplate();
        ReportMissingReferences();
        WireButtons();
        PrepareChatUI();

        if (leaveConfirmPopup != null) leaveConfirmPopup.SetActive(false);

        if (startGameButton != null && startGameButton.image != null)
        {
            _startNormalColor = startGameButton.image.color;
        }

        // Solo el Host ve "Empezar". Hasta saber quién es el Host, queda oculto.
        if (startGameButton != null) startGameButton.gameObject.SetActive(false);
    }

    public override void Spawned()
    {
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(Object.HasStateAuthority);
        }

        RefreshPing();
        RefreshPlayerCount();
        RefreshPlayerList();
    }

    private void EnsureEventSystem()
    {
        // Sin EventSystem ningún botón de la escena recibe clicks.
        if (FindObjectOfType<EventSystem>() == null)
        {
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            Debug.LogWarning("[LobbyManager] La escena no tenía EventSystem; se creó uno. Conviene agregarlo en la escena (UI > Event System).");
        }
    }

    // Encuentra la fila de ejemplo y su contenedor aunque no estén asignados en el Inspector.
    private void ResolveRowTemplate()
    {
        // 1) Hay contenedor pero no fila: la fila de ejemplo es su primer hijo.
        if (playerEntryPrefab == null && playerListContent != null && playerListContent.childCount > 0)
        {
            playerEntryPrefab = playerListContent.GetChild(0).gameObject;
        }

        // 2) No hay ninguno: buscamos en la escena el texto de ejemplo "Xms" y usamos su fila.
        if (playerEntryPrefab == null)
        {
            foreach (TMP_Text text in FindObjectsOfType<TMP_Text>(true))
            {
                if (text.text.Trim().ToLowerInvariant() == "xms" && text.transform.parent != null)
                {
                    playerEntryPrefab = text.transform.parent.gameObject;
                    break;
                }
            }
        }

        // 3) Hay fila (de la escena) pero no contenedor: el contenedor es su padre.
        if (playerEntryPrefab != null && playerListContent == null && playerEntryPrefab.scene.IsValid())
        {
            playerListContent = playerEntryPrefab.transform.parent;
        }

        if (playerEntryPrefab == null || playerListContent == null)
        {
            Debug.LogError("[LobbyManager] No encuentro la fila de ejemplo de la lista. Arrastrá tu fila (la que dice 'Xms') al campo 'Player Entry Prefab'.");
            return;
        }

        // Si la fila de ejemplo es un objeto de la escena, se usa como molde y se oculta.
        if (playerEntryPrefab.scene.IsValid())
        {
            playerEntryPrefab.SetActive(false);
        }

        Debug.Log($"[LobbyManager] Molde de fila: '{playerEntryPrefab.name}' | contenedor: '{playerListContent.name}'");
    }

    private void ReportMissingReferences()
    {
        var missing = new List<string>();
        if (pingText == null) missing.Add("Ping Text");
        if (playerListContent == null) missing.Add("Player List Content");
        if (playerEntryPrefab == null) missing.Add("Player Entry Prefab");
        if (startGameButton == null) missing.Add("Start Game Button");
        if (changeColorButton == null) missing.Add("Change Color Button");
        if (leaveButton == null) missing.Add("Leave Button");
        if (leaveConfirmPopup == null) missing.Add("Leave Confirm Popup");
        if (chatInputField == null) missing.Add("Chat Input Field");
        if (chatLogText == null) missing.Add("Chat Log Text");

        if (missing.Count > 0)
        {
            Debug.LogWarning("[LobbyManager] Faltan asignar en el Inspector: " + string.Join(", ", missing));
        }
    }

    private void WireButtons()
    {
        if (startGameButton != null) startGameButton.onClick.AddListener(OnStartGameClicked);
        if (changeColorButton != null) changeColorButton.onClick.AddListener(OnChangeCharacterClicked);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeavePressed);
        if (leaveYesButton != null) leaveYesButton.onClick.AddListener(OnConfirmLeaveYes);
        if (leaveNoButton != null) leaveNoButton.onClick.AddListener(OnConfirmLeaveNo);
    }

    // ---------------------------------------------------------
    //  CADA FRAME
    // ---------------------------------------------------------
    private void Update()
    {
        HandleChatInput();
        UpdateChatLines();

        if (!IsReady) return;

        // La lista y el ping no hace falta redibujarlos cada frame.
        _refreshTimer -= Time.unscaledDeltaTime;
        if (_refreshTimer <= 0f)
        {
            _refreshTimer = 0.25f;
            RefreshPing();
            RefreshPlayerCount();
            RefreshPlayerList();
        }
    }

    // El Host copia el personaje de cada jugador a un dato de red para que
    // todos puedan dibujarlo en la lista.
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            SyncCharactersToNetwork();
        }
    }

    private void SyncCharactersToNetwork()
    {
        NetworkManagerFusion manager = NetworkManagerFusion.Instance;
        if (manager == null) return;

        for (int i = 0; i < MaxPlayers; i++) _activeScratch[i] = false;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            int id = player.PlayerId;
            if (id < 0 || id >= MaxPlayers) continue;

            _activeScratch[id] = true;

            int value = manager.GetCharacter(player) + 1;
            if (CharacterPlusOne[id] != value) CharacterPlusOne.Set(id, value);
        }

        // Los que se fueron dejan de tener personaje.
        for (int i = 0; i < MaxPlayers; i++)
        {
            if (!_activeScratch[i] && CharacterPlusOne[i] != 0) CharacterPlusOne.Set(i, 0);
        }
    }

    // ---------------------------------------------------------
    //  LISTA DE JUGADORES
    // ---------------------------------------------------------
    private void RefreshPing()
    {
        if (pingText != null && IsReady)
        {
            pingText.text = $"{PingFor(Runner.LocalPlayer)} ms";
        }
    }

    // Muestra cuántos jugadores hay y apaga "Empezar" mientras falten.
    private void RefreshPlayerCount()
    {
        if (!IsReady) return;

        int count = 0;
        foreach (PlayerRef player in Runner.ActivePlayers) count++;

        NetworkManagerFusion manager = NetworkManagerFusion.Instance;
        int needed = manager != null ? manager.PlayersNeeded : 3;

        // El botón sigue recibiendo el click (para poder avisar por qué no empieza),
        // pero se ve apagado hasta que estén todos.
        if (startGameButton != null && startGameButton.image != null)
        {
            startGameButton.image.color = count >= needed ? _startNormalColor : startDisabledColor;
        }
    }

    private int PingFor(PlayerRef player)
    {
        try
        {
            return Mathf.RoundToInt((float)Runner.GetPlayerRtt(player) * 1000f);
        }
        catch
        {
            return 0;
        }
    }

    private void RefreshPlayerList()
    {
        if (playerListContent == null || playerEntryPrefab == null || !IsReady) return;

        var players = new List<PlayerRef>(Runner.ActivePlayers);
        players.Sort((a, b) => a.PlayerId.CompareTo(b.PlayerId));

        // Una fila por jugador: se crean o borran las que sobren.
        while (_entries.Count < players.Count)
        {
            _entries.Add(Instantiate(playerEntryPrefab, playerListContent));
        }
        while (_entries.Count > players.Count)
        {
            Destroy(_entries[_entries.Count - 1]);
            _entries.RemoveAt(_entries.Count - 1);
        }

        for (int i = 0; i < players.Count; i++)
        {
            FillEntry(_entries[i], players[i]);
        }

        ArrangeRows();
    }

    // Pone cada fila debajo de la anterior. Si el contenedor ya tiene un Layout Group
    // (Vertical Layout Group), él las ordena solo y esto no hace nada.
    private void ArrangeRows()
    {
        if (playerListContent.GetComponent<LayoutGroup>() != null) return;

        RectTransform template = playerEntryPrefab.GetComponent<RectTransform>();
        if (template == null) return;

        float step = template.rect.height + rowSpacing;

        for (int i = 0; i < _entries.Count; i++)
        {
            RectTransform rect = _entries[i].GetComponent<RectTransform>();
            if (rect != null) rect.anchoredPosition = template.anchoredPosition + Vector2.down * step * i;
        }
    }

    // Completa una fila: ping, nombre del rol y avatar del rol.
    private void FillEntry(GameObject entry, PlayerRef player)
    {
        entry.SetActive(true);

        int roleIndex = GetCharacterOf(player);
        string roleName = roleIndex >= 0 ? CharacterLabel(roleIndex) : "Eligiendo...";
        if (IsHostPlayer(player)) roleName += " <color=#FF3B30>[HOST]</color>";

        int pingMs = PingFor(player);
        string ping = $"{pingMs}ms";

        // --- Textos: uno es el ping ("Xms") y el otro el nombre del rol ---
        TMP_Text[] texts = entry.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text pingTxt = null;
        TMP_Text roleTxt = null;

        foreach (TMP_Text t in texts)
        {
            if (pingTxt == null && (t.name.ToLowerInvariant().Contains("ping") || t.text.TrimEnd().EndsWith("ms")))
            {
                pingTxt = t;
            }
        }

        foreach (TMP_Text t in texts)
        {
            if (t != pingTxt) { roleTxt = t; break; }
        }

        if (pingTxt != null)
        {
            pingTxt.text = ping;
            pingTxt.color = PingColor(pingMs);
        }
        if (roleTxt != null) roleTxt.text = (pingTxt == null) ? $"{ping}   {roleName}" : roleName;

        // --- Avatar: la imagen chica de la fila (el fondo de la fila no cuenta) ---
        Image avatar = null;
        float smallestArea = float.MaxValue;

        foreach (Image img in entry.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject == entry) continue;

            if (img.name.ToLowerInvariant().Contains("avatar"))
            {
                avatar = img;
                break;
            }

            float area = img.rectTransform.rect.width * img.rectTransform.rect.height;
            if (area < smallestArea)
            {
                smallestArea = area;
                avatar = img;
            }
        }

        if (avatar != null)
        {
            bool hasSprite = characterPortraits != null && roleIndex >= 0 && roleIndex < characterPortraits.Length
                             && characterPortraits[roleIndex] != null;

            // Si hay imagen para este rol se pone; si no, el avatar queda como está.
            if (hasSprite)
            {
                avatar.sprite = characterPortraits[roleIndex];
                avatar.color = Color.white;
            }
        }
    }

    // true si este jugador es el que creó la sala.
    private bool IsHostPlayer(PlayerRef player)
    {
        if (!IsReady) return false;

        if (Runner.IsServer && player == Runner.LocalPlayer) return true; // vos sos el Host
        return player == Object.StateAuthority;                            // el Host, visto desde un cliente
    }

    // Verde = buen ping, naranja = regular, rojo = malo.
    private Color PingColor(int ms)
    {
        if (ms < 80) return new Color(0.55f, 1f, 0.25f);
        if (ms < 150) return new Color(1f, 0.65f, 0.1f);
        return new Color(1f, 0.25f, 0.2f);
    }

    // -1 si todavía no se sabe.
    private int GetCharacterOf(PlayerRef player)
    {
        int id = player.PlayerId;
        if (id < 0 || id >= MaxPlayers) return -1;
        return CharacterPlusOne[id] - 1;
    }

    private string GetPlainName(PlayerRef player)
    {
        return $"Jugador {player.PlayerId}";
    }

    private string CharacterLabel(int index)
    {
        if (characterNames != null && index >= 0 && index < characterNames.Length) return characterNames[index];
        return $"personaje {index}";
    }

    // ---------------------------------------------------------
    //  CAMBIAR DE PERSONAJE (el Host valida que no esté ocupado)
    // ---------------------------------------------------------
    private void OnChangeCharacterClicked()
    {
        if (!IsReady) return;

        if (Object.HasStateAuthority)
        {
            ServerCycleCharacter(Runner.LocalPlayer);
        }
        else
        {
            RPC_RequestCycleCharacter(Runner.LocalPlayer);
        }
    }

    // Un cliente le pide al Host que le cambie el personaje.
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestCycleCharacter(PlayerRef who)
    {
        ServerCycleCharacter(who);
    }

    // Pasa al siguiente personaje que esté libre.
    private void ServerCycleCharacter(PlayerRef player)
    {
        NetworkManagerFusion manager = NetworkManagerFusion.Instance;
        if (manager == null || manager.CharacterCount == 0) return;

        int count = manager.CharacterCount;
        int current = manager.GetCharacter(player);
        if (current < 0) current = count - 1;

        for (int step = 1; step < count; step++)
        {
            int candidate = (current + step) % count;

            if (manager.TrySetCharacter(player, candidate))
            {
                SyncCharactersToNetwork();
                RPC_System($"{GetPlainName(player)} ahora es {CharacterLabel(candidate)}.");
                return;
            }
        }

        RPC_System("No hay otros personajes libres.");
    }

    // ---------------------------------------------------------
    //  CHAT
    // ---------------------------------------------------------
    private void PrepareChatUI()
    {
        if (chatInputField != null)
        {
            chatInputField.lineType = TMP_InputField.LineType.SingleLine;
            chatInputField.characterLimit = 100;
            chatInputField.onSubmit.AddListener(OnChatSubmit);
            chatInputField.onEndEdit.AddListener(OnChatEndEdit);

            // El campo solo se ve mientras escribís.
            chatInputField.gameObject.SetActive(false);
        }

        if (chatLogText != null)
        {
            chatLogText.raycastTarget = false;
            chatLogText.text = "";
        }

        // El panel de mensajes es solo para leer: no tiene que tapar los botones.
        if (chatDisplayPanel != null)
        {
            foreach (Graphic graphic in chatDisplayPanel.GetComponentsInChildren<Graphic>(true))
            {
                if (chatInputField != null && graphic.transform.IsChildOf(chatInputField.transform)) continue;
                graphic.raycastTarget = false;
            }
        }
    }

    private void HandleChatInput()
    {
        if (chatInputField == null) return;

        bool enterPressed = Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);

        if (!_chatOpen)
        {
            // Enter abre el chat (salvo que justo se acabe de enviar un mensaje).
            if (enterPressed && Time.frameCount > _ignoreEnterFrame)
            {
                OpenChat();
            }
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseChat();
        }
    }

    private void OpenChat()
    {
        _chatOpen = true;
        chatInputField.gameObject.SetActive(true);
        chatInputField.text = "";
        UpdateChatPanelVisibility();

        // Enfocamos el campo un frame DESPUÉS de activarlo. Justo al activarlo, Unity
        // todavía no lo tiene listo y el foco no "pega" (había que hacer click).
        StartCoroutine(FocusChatNextFrame());
    }

    private System.Collections.IEnumerator FocusChatNextFrame()
    {
        yield return null;

        if (!_chatOpen || chatInputField == null) yield break;

        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(chatInputField.gameObject);
        }

        chatInputField.ActivateInputField();
    }

    private void CloseChat()
    {
        _chatOpen = false;
        chatInputField.DeactivateInputField();
        chatInputField.gameObject.SetActive(false);
        UpdateChatPanelVisibility();
    }

    // Enter con el campo abierto: manda el mensaje y cierra.
    private void OnChatSubmit(string text)
    {
        // Evita que ESTE mismo Enter vuelva a abrir el chat en el mismo frame.
        _ignoreEnterFrame = Time.frameCount;

        string message = text != null ? text.Trim() : "";
        if (message.Length > 0 && IsReady)
        {
            RPC_Chat(Runner.LocalPlayer, message);
        }

        CloseChat();
    }

    // Si hacés click afuera o apretás Esc, el campo se cierra.
    private void OnChatEndEdit(string text)
    {
        if (_chatOpen) CloseChat();
    }

    // Cualquiera puede mandar un mensaje y le llega a TODOS (incluido él).
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_Chat(PlayerRef sender, string message)
    {
        AddChatLine($"<b>{GetPlainName(sender)}:</b> {Sanitize(message)}");
    }

    // Avisos del sistema (por ejemplo, "Jugador 2 ahora es Hacker").
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_System(string text)
    {
        AddChatLine($"<i>{Sanitize(text)}</i>");
    }

    // Evita que alguien escriba etiquetas de formato de TextMeshPro.
    private string Sanitize(string text)
    {
        return text.Replace("<", "<\u200B");
    }

    private void AddChatLine(string line)
    {
        _chatLines.Add(new ChatLine { text = line, time = Time.unscaledTime });

        while (_chatLines.Count > MaxVisibleChatLines)
        {
            _chatLines.RemoveAt(0);
        }
    }

    // Cada mensaje se ve un rato y después se desvanece solo.
    private void UpdateChatLines()
    {
        if (chatLogText == null) return;

        float now = Time.unscaledTime;
        float lifetime = messageDuration + fadeDuration;
        _chatLines.RemoveAll(line => now - line.time > lifetime);

        if (_chatLines.Count == 0)
        {
            if (chatLogText.text.Length > 0) chatLogText.text = "";
            UpdateChatPanelVisibility();
            return;
        }

        _builder.Clear();
        foreach (ChatLine line in _chatLines)
        {
            float age = now - line.time;
            float alpha = age <= messageDuration
                ? 1f
                : Mathf.Clamp01(1f - (age - messageDuration) / Mathf.Max(0.01f, fadeDuration));

            string hex = Mathf.RoundToInt(alpha * 255f).ToString("X2");
            _builder.Append("<alpha=#").Append(hex).Append('>').Append(line.text).Append('\n');
        }

        chatLogText.text = _builder.ToString();
        UpdateChatPanelVisibility();
    }

    private void UpdateChatPanelVisibility()
    {
        if (chatDisplayPanel == null) return;

        // Si el campo de escribir está DENTRO del panel, el panel no puede apagarse mientras escribís.
        bool inputInsidePanel = chatInputField != null && chatInputField.transform.IsChildOf(chatDisplayPanel.transform);
        bool show = _chatLines.Count > 0 || (_chatOpen && inputInsidePanel);

        if (chatDisplayPanel.activeSelf != show) chatDisplayPanel.SetActive(show);
    }

    // ---------------------------------------------------------
    //  EMPEZAR (solo el Host)
    // ---------------------------------------------------------
    private void OnStartGameClicked()
    {
        if (!IsReady || !Object.HasStateAuthority) return;

        NetworkManagerFusion manager = NetworkManagerFusion.Instance;
        if (manager == null)
        {
            Debug.LogError("[LobbyManager] No encuentro el NetworkManagerFusion. ¿Pasaste por ChooseCharacter?");
            return;
        }

        // Faltan jugadores: se avisa y no se empieza.
        int connected = manager.ConnectedPlayers;
        int needed = manager.PlayersNeeded;

        if (connected < needed)
        {
            AddChatLine($"<i>Hacen falta {needed} jugadores para empezar ({connected}/{needed}).</i>");
            return;
        }

        manager.BeginGame();
    }

    // ---------------------------------------------------------
    //  SALIR (con "¿Estás seguro?")
    // ---------------------------------------------------------
    private void OnLeavePressed()
    {
        if (leaveConfirmPopup != null)
        {
            leaveConfirmPopup.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[LobbyManager] Falta asignar 'Leave Confirm Popup' en el Inspector.");
        }
    }

    // Conectable también a mano desde el Inspector (botón "Sí").
    public void OnConfirmLeaveYes()
    {
        if (_leaving) return;
        _leaving = true;

        NetworkManagerFusion manager = NetworkManagerFusion.Instance;
        if (manager != null)
        {
            // Cierra la conexión como corresponde y vuelve al título.
            manager.LeaveToTitle();
        }
        else
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }

    // Conectable también a mano desde el Inspector (botón "No").
    public void OnConfirmLeaveNo()
    {
        if (leaveConfirmPopup != null)
        {
            leaveConfirmPopup.SetActive(false);
        }
    }
}