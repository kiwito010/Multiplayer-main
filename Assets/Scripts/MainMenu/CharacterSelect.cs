using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en la escena ChooseCharacter.
public class CharacterSelect : MonoBehaviour
{
    [Header("Conexión")]
    [SerializeField] private NetworkManagerFusion networkManager;
    [SerializeField] private string mainMenuScene = "Main Menu";

    [Header("Botones de personaje (0=Ingeniero, 1=Hacker, 2=Científico, 3=Exploradora)")]
    [SerializeField] private Button[] characterButtons;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.6f, 0.6f, 1f);
    [SerializeField] private GameObject[] selectedMarkers;

    [Header("Botón Seleccionar")]
    [SerializeField] private Button confirmButton;
    [SerializeField] private Color confirmDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Imagen de la derecha")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortrait;
    [SerializeField] private Sprite[] characterPortraits;

    [Header("Paneles de descripción por personaje")]
    [SerializeField] private GameObject[] characterDescriptionPanels;

    [Header("Popup de Estado / Barra de Carga (Al confirmar personaje)")]
    [SerializeField] private GameObject statusPanel;  // El panel completo de carga
    [SerializeField] private Slider loadingSlider;    // La barra de carga (opcional)
    [SerializeField] private TMP_Text statusText;     // El texto que dice "Conectando..." con puntos

    [Header("Popup de Avisos (ej. personaje ocupado)")]
    [SerializeField] private GameObject messagePanel; 
    [SerializeField] private TMP_Text messageText;    

    [Header("Aviso animado (sube desde abajo)")]
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string warningMessage = "Elegí un personaje primero";
    [SerializeField] private float slideDistance = 120f;
    [SerializeField] private float riseTime = 0.35f;
    [SerializeField] private float holdTime = 1.2f;
    [SerializeField] private float fallTime = 0.5f;

    private int _selectedIndex = -1;
    private Color[] _characterNormalColors;
    private Color _confirmNormalColor = Color.white;

    private RectTransform _warningRect;
    private Vector2 _warningRest;
    private Color _warningBaseColor = Color.white;
    private Coroutine _warningRoutine;
    private bool _isConnecting;

    private void Awake()
    {
        if (characterButtons != null)
        {
            _characterNormalColors = new Color[characterButtons.Length];
            for (int i = 0; i < characterButtons.Length; i++)
            {
                bool valid = characterButtons[i] != null && characterButtons[i].image != null;
                _characterNormalColors[i] = valid ? characterButtons[i].image.color : Color.white;
            }
        }

        if (confirmButton != null && confirmButton.image != null)
        {
            _confirmNormalColor = confirmButton.image.color;
        }

        if (warningText != null)
        {
            _warningRect = warningText.rectTransform;
            _warningRest = _warningRect.anchoredPosition;
            _warningBaseColor = warningText.color;
            warningText.gameObject.SetActive(false);
        }
    }

    private void Start()
    {
        if (statusPanel != null) statusPanel.SetActive(false);
        if (messagePanel != null) messagePanel.SetActive(false);

        bool hasMessage = !string.IsNullOrEmpty(GameSession.Message);
        if (messagePanel != null) messagePanel.SetActive(hasMessage);

        if (hasMessage)
        {
            if (messageText != null) messageText.text = GameSession.Message;
            GameSession.Message = ""; 
        }

        _selectedIndex = -1;
        RefreshUI();
    }

    public void OnCharacterSelected(int characterIndex)
    {
        if (_isConnecting) return; // Evita cambiar si ya está conectando

        _selectedIndex = (_selectedIndex == characterIndex) ? -1 : characterIndex;
        RefreshUI();
    }

    // --- BOTÓN SELECCIONAR ---
    [Header("Escena del Lobby")]
    [SerializeField] private string lobbySceneName = "LobbyUI";

    public void OnConfirmPressed()
{
    if (_isConnecting) return;

    if (_selectedIndex < 0)
    {
        ShowWarning();
        return;
    }

    GameSession.CharacterIndex = _selectedIndex;

    _isConnecting = true;
    if (statusPanel != null) statusPanel.SetActive(true);

    StartCoroutine(AnimateLoadingAndDots("Conectando"));

    // Llamamos a la conexión. El NetworkManager se encargará de pasar a la LobbyUI cuando esté listo.
    networkManager.StartConnection();
}

    public void OnBackPressed()
    {
        if (_isConnecting) return;
        SceneManager.LoadScene(mainMenuScene);
    }

    public void OnMessageAcceptPressed()
    {
        if (messagePanel != null)
        {
            messagePanel.SetActive(false);
        }
    }

    // Coroutine que anima la barra y los puntos suspensivos
    private IEnumerator AnimateLoadingAndDots(string baseMessage)
    {
        if (loadingSlider != null) loadingSlider.value = 0f;

        float progress = 0f;
        int dotCount = 0;
        float dotTimer = 0f;

        while (_isConnecting)
        {
            if (loadingSlider != null)
            {
                progress += Time.deltaTime * 1.5f;
                if (progress > 0.9f) progress = 0.9f;
                loadingSlider.value = progress;
            }

            dotTimer += Time.deltaTime;
            if (dotTimer >= 0.4f)
            {
                dotTimer = 0f;
                dotCount = (dotCount + 1) % 4;

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

    private void ShowWarning()
    {
        if (warningText == null) return;

        if (_warningRoutine != null) StopCoroutine(_warningRoutine);
        _warningRoutine = StartCoroutine(PlayWarning());
    }

    private IEnumerator PlayWarning()
    {
        warningText.text = warningMessage;
        warningText.gameObject.SetActive(true);

        Vector2 below = _warningRest + Vector2.down * slideDistance;

        for (float t = 0f; t < riseTime; t += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(t / riseTime);
            float eased = 1f - Mathf.Pow(1f - progress, 3f);

            _warningRect.anchoredPosition = Vector2.Lerp(below, _warningRest, eased);
            SetWarningAlpha(eased);
            yield return null;
        }

        _warningRect.anchoredPosition = _warningRest;
        SetWarningAlpha(1f);

        yield return new WaitForSecondsRealtime(holdTime);

        for (float t = 0f; t < fallTime; t += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(t / fallTime);
            float eased = progress * progress;

            _warningRect.anchoredPosition = Vector2.Lerp(_warningRest, below, eased);
            SetWarningAlpha(1f - progress);
            yield return null;
        }

        _warningRect.anchoredPosition = _warningRest;
        warningText.gameObject.SetActive(false);
        _warningRoutine = null;
    }

    private void SetWarningAlpha(float alpha)
    {
        Color color = _warningBaseColor;
        color.a = _warningBaseColor.a * alpha;
        warningText.color = color;
    }

    private void RefreshUI()
    {
        bool hasSelection = _selectedIndex >= 0;

        if (characterButtons != null)
        {
            for (int i = 0; i < characterButtons.Length; i++)
            {
                if (characterButtons[i] == null || characterButtons[i].image == null) continue;
                characterButtons[i].image.color = (i == _selectedIndex) ? selectedButtonColor : _characterNormalColors[i];
            }
        }

        if (selectedMarkers != null)
        {
            for (int i = 0; i < selectedMarkers.Length; i++)
            {
                if (selectedMarkers[i] != null) selectedMarkers[i].SetActive(i == _selectedIndex);
            }
        }

        if (confirmButton != null && confirmButton.image != null)
        {
            confirmButton.image.color = hasSelection ? _confirmNormalColor : confirmDisabledColor;
        }

        if (portraitImage != null)
        {
            Sprite sprite = defaultPortrait;

            if (hasSelection && characterPortraits != null &&
                _selectedIndex < characterPortraits.Length && characterPortraits[_selectedIndex] != null)
            {
                sprite = characterPortraits[_selectedIndex];
            }

            portraitImage.sprite = sprite;
        }

        if (characterDescriptionPanels != null)
        {
            for (int i = 0; i < characterDescriptionPanels.Length; i++)
            {
                if (characterDescriptionPanels[i] != null)
                {
                    characterDescriptionPanels[i].SetActive(i == _selectedIndex);
                }
            }
        }
    }
}