using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;

public class LobbyManager : NetworkBehaviour
{
    [Header("Escenas")]
    [SerializeField] private string gameplaySceneName = "GameTest1";
    [SerializeField] private string titleSceneName = "Main Menu";

    [Header("UI del Lobby (Estilo R.E.P.O.)")]
    [SerializeField] private TMP_Text pingText;
    [SerializeField] private Transform playerListContent; // Contenedor vertical para la lista de jugadores
    [SerializeField] private GameObject playerEntryPrefab; // Prefab con el nombre y retrato del personaje
    [SerializeField] private Button startGameButton; // Solo lo puede apretar el Host
    [SerializeField] private Button changeColorButton; // Cambiar de personaje
    [SerializeField] private Button leaveButton;

    [Header("Popup de Confirmación (Leave)")]
    [SerializeField] private GameObject leaveConfirmPopup;

    [Header("Chat por Texto (Presionar Enter)")]
    [SerializeField] private TMP_InputField chatInputField;
    [SerializeField] private GameObject chatDisplayPanel;
    [SerializeField] private TMP_Text chatLogText;
    [SerializeField] private float messageDuration = 4f;

    [Header("Avatares de Personajes (0=Ingeniero, 1=Hacker, 2=Exploradora)")]
    [SerializeField] private Sprite[] characterPortraits;

    private float _messageTimer;

    public override void Spawned()
    {
        // Si somos clientes, ocultamos el botón Start Game
        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(Object.HasStateAuthority);
            startGameButton.onClick.AddListener(OnStartGameClicked);
        }

        if (changeColorButton != null) changeColorButton.onClick.AddListener(OnChangeColorClicked);
        if (leaveButton != null) leaveButton.onClick.AddListener(OnLeavePressed);

        if (leaveConfirmPopup != null) leaveConfirmPopup.SetActive(false);

        if (chatInputField != null)
        {
            chatInputField.onEndEdit.AddListener(OnChatSubmit);
        }

        UpdatePlayerUI();
    }

    private void Update()
    {
        // Actualizar Ping aproximado
        if (Runner != null && pingText != null)
        {
            int rtt = Mathf.RoundToInt((float)Runner.GetPlayerRtt(Runner.LocalPlayer) * 1000f);
            pingText.text = $"{rtt} ms";
        }

        // Activar el input de chat al presionar Enter
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (chatInputField != null && !chatInputField.isFocused)
            {
                chatInputField.ActivateInputField();
                chatInputField.Select();
            }
        }

        // Desvanecer mensajes del chat pasado el tiempo
        if (chatDisplayPanel != null && chatDisplayPanel.activeSelf)
        {
            _messageTimer -= Time.deltaTime;
            if (_messageTimer <= 0f)
            {
                chatDisplayPanel.SetActive(false);
            }
        }
    }

    // --- CAMBIAR DE PERSONAJE (Cicla entre 0, 1 y 2) ---
    private void OnChangeColorClicked()
    {
        GameSession.CharacterIndex = (GameSession.CharacterIndex + 1) % 3;
        UpdatePlayerUI();
    }

    // --- LISTA DE JUGADORES Y AVATARES ---
    private void UpdatePlayerUI()
    {
        if (playerListContent == null || playerEntryPrefab == null) return;

        foreach (Transform child in playerListContent)
        {
            Destroy(child.gameObject);
        }

        if (Runner != null)
        {
            foreach (var player in Runner.ActivePlayers)
            {
                GameObject entry = Instantiate(playerEntryPrefab, playerListContent);
                
                TMP_Text nameTxt = entry.GetComponentInChildren<TMP_Text>();
                Image avatarImg = entry.transform.Find("AvatarImage")?.GetComponent<Image>();

                if (nameTxt != null) nameTxt.text = $"Player {player.RawEncoded}";

                int charIdx = GameSession.CharacterIndex; 
                if (avatarImg != null && characterPortraits != null && charIdx < characterPortraits.Length)
                {
                    avatarImg.sprite = characterPortraits[charIdx];
                }
            }
        }
    }

    // --- CHAT NATIVO ---
    private void OnChatSubmit(string text)
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && !string.IsNullOrEmpty(text))
        {
            SendChatMessage(text);
            chatInputField.text = "";
            chatInputField.ActivateInputField();
        }
    }

    private void SendChatMessage(string msg)
    {
        if (chatLogText != null && chatDisplayPanel != null)
        {
            chatLogText.text += $"\n{msg}";
            chatDisplayPanel.SetActive(true);
            _messageTimer = messageDuration;
        }
    }

    // --- BOTÓN START GAME (SOLO HOST) ---
    private void OnStartGameClicked()
    {
        if (Object.HasStateAuthority && Runner != null)
        {
            Runner.LoadScene(gameplaySceneName);
        }
    }

    // --- BOTÓN LEAVE Y POP-UP ---
    private void OnLeavePressed()
    {
        if (leaveConfirmPopup != null)
        {
            leaveConfirmPopup.SetActive(true);
        }
    }

    public void OnConfirmLeaveYes()
    {
        if (Runner != null && Runner.IsRunning)
        {
            Runner.Shutdown();
        }
        SceneManager.LoadScene(titleSceneName);
    }

    public void OnConfirmLeaveNo()
    {
        if (leaveConfirmPopup != null)
        {
            leaveConfirmPopup.SetActive(false);
        }
    }
}