using System.Collections;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuFlow : MonoBehaviour
{
    [Header("Campos de texto (arrastrar desde la Hierarchy)")]
    public TMP_InputField createRoomInput;
    public TMP_InputField joinRoomInput;

    [Header("Escena siguiente")]
    [SerializeField] private string chooseCharacterScene = "ChooseCharacter";

    [Header("Popup de error")]
    [SerializeField] private GameObject errorPanel; 
    [SerializeField] private TMP_Text errorText;    

    [Header("Popup de Estado / Barra de Carga")]
    [SerializeField] private GameObject statusPanel;  // El panel completo
    [SerializeField] private Slider loadingSlider;    // La barra de carga (opcional)
    [SerializeField] private TMP_Text statusText;     // El texto que va cambiando con los puntos

    private bool _checking;

    private void Start()
    {
        if (statusPanel != null) statusPanel.SetActive(false);

        if (errorPanel == null) return;

        bool hasMessage = !string.IsNullOrEmpty(GameSession.Message);
        errorPanel.SetActive(hasMessage);

        if (hasMessage)
        {
            if (errorText != null) errorText.text = GameSession.Message;
            GameSession.Message = ""; 
        }
    }

        public void OnBackButtonPressed()
    {
        SceneManager.LoadScene("TitleScreen");
    }

    public void OnErrorAcceptPressed()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    // --- CREAR SALA (HOST) ---
    public async void OnHostButtonPressed()
    {
        if (_checking) return;

        string code = createRoomInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Escribí un código para la sala.");
            return;
        }

        // Comprobamos si ya existe una sala con este código antes de crearla
        _checking = true;
        if (statusPanel != null) statusPanel.SetActive(true);

        Coroutine loadRoutine = StartCoroutine(AnimateLoadingAndDots("Verificando sala"));

        bool? exists = null;
        try
        {
            exists = await RoomLookup.RoomExists(code);
        }
        finally
        {
            if (loadRoutine != null) StopCoroutine(loadRoutine);
            _checking = false;
            if (statusPanel != null) statusPanel.SetActive(false);
        }

        if (this == null) return;

        // Si la sala ya existe, mostramos error y detenemos la creación
        if (exists == true)
        {
            ShowError("Ya existe una sala con ese código. Probá con otro.");
            return;
        }

        // Si no existe (o no se pudo verificar por internet lenta), procedemos a crearla
        GoToCharacterSelect(GameMode.Host, code);
    }

    // --- UNIRSE A SALA (CLIENT) ---
    public async void OnJoinButtonPressed()
    {
        if (_checking) return;

        string code = joinRoomInput.text.Trim();

        if (string.IsNullOrEmpty(code))
        {
            ShowError("Escribí el código de la sala a la que querés unirte.");
            return;
        }

        // Comprobamos que la sala exista ANTES de elegir personaje
        _checking = true;
        if (statusPanel != null) statusPanel.SetActive(true);

        Coroutine loadRoutine = StartCoroutine(AnimateLoadingAndDots("Buscando sala"));

        bool? exists = null;
        try
        {
            exists = await RoomLookup.RoomExists(code);
        }
        finally
        {
            if (loadRoutine != null) StopCoroutine(loadRoutine);
            _checking = false;
            if (statusPanel != null) statusPanel.SetActive(false);
        }

        if (this == null) return;

        if (exists == false)
        {
            ShowError("No existe ninguna sala con ese código. Revisá que esté bien escrito e intentá de nuevo.");
            return;
        }

        if (exists == null)
        {
            Debug.LogWarning("No se pudo verificar la sala; se continúa igual.");
        }

        GoToCharacterSelect(GameMode.Client, code);
    }

    // Coroutine que anima la barra y los puntos suspensivos con el mensaje recibido
    private IEnumerator AnimateLoadingAndDots(string baseMessage)
    {
        if (loadingSlider != null) loadingSlider.value = 0f;

        float progress = 0f;
        int dotCount = 0;
        float dotTimer = 0f;

        while (_checking)
        {
            // 1. Avanzar la barra de carga de forma fluida
            if (loadingSlider != null)
            {
                progress += Time.deltaTime * 1.5f;
                if (progress > 0.9f) progress = 0.9f;
                loadingSlider.value = progress;
            }

            // 2. Animar los puntos cada 0.4 segundos
            dotTimer += Time.deltaTime;
            if (dotTimer >= 0.4f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4; // Cicla entre 0, 1, 2 y 3 puntos

                if (statusText != null)
                {
                    string dots = new string('.', dotCount);
                    statusText.text = baseMessage + dots;
                }
            }

            yield return null;
        }

        if (loadingSlider != null) loadingSlider.value = 1f;
    }

    private void GoToCharacterSelect(GameMode mode, string code)
    {
        GameSession.Mode = mode;
        GameSession.RoomCode = code;
        SceneManager.LoadScene(chooseCharacterScene);
    }

    // Muestra el popup de error con un mensaje.
    private void ShowError(string message)
    {
        if (errorText != null) errorText.text = message;
        if (errorPanel != null) errorPanel.SetActive(true);
    }
}