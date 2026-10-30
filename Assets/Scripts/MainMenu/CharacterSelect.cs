using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en la escena ChooseCharacter.
//  - Los botones de personaje SELECCIONAN (no conectan).
//  - Tocar el personaje elegido lo deselecciona; tocar otro cambia la elección.
//  - El botón "Seleccionar" es el que inicia la conexión. Si no hay nadie
//    elegido, muestra un aviso que sube desde abajo y vuelve a caer.
public class CharacterSelect : MonoBehaviour
{
    [Header("Conexión")]
    // El NetworkManagerFusion que vive en ESTA escena.
    [SerializeField] private NetworkManagerFusion networkManager;
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Botones de personaje (el orden importa: 0=Ingeniero, 1=Hacker, 3=Exploradora)")]
    [SerializeField] private Button[] characterButtons;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.6f, 0.6f, 1f);

    // Opcional: un objeto por botón (por ejemplo un borde o una marca) que se
    // prende solo en el personaje elegido. Sirve si el cambio de color no se nota.
    [SerializeField] private GameObject[] selectedMarkers;

    [Header("Botón Seleccionar")]
    // IMPORTANTE: dejalo con "Interactable" TILDADO. Un botón desactivado no
    // recibe clicks, y necesitamos el click para mostrar el aviso. Cuando no
    // hay personaje elegido solo lo pintamos de apagado.
    [SerializeField] private Button confirmButton;
    [SerializeField] private Color confirmDisabledColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Header("Imagen de la derecha")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortrait;      // cuando no hay nada elegido
    [SerializeField] private Sprite[] characterPortraits; // una por personaje, en el mismo orden

    [Header("Paneles de descripción por personaje (0=Ingeniero, 1=Hacker, 3=Exploradora)")]
    [SerializeField] private GameObject[] characterDescriptionPanels;

    [Header("Aviso animado (sube desde abajo, se queda un momento y vuelve a caer)")]
    // Un texto TMP puesto donde querés que quede al subir.
    [SerializeField] private TMP_Text warningText;
    [SerializeField] private string warningMessage = "Elegí un personaje primero";
    [SerializeField] private float slideDistance = 120f; // cuántos píxeles sube/baja
    [SerializeField] private float riseTime = 0.35f;     // tiempo que tarda en subir
    [SerializeField] private float holdTime = 1.2f;      // tiempo que se queda quieto
    [SerializeField] private float fallTime = 0.5f;      // tiempo que tarda en caer

    [Header("Avisos que vienen de otras escenas")]
    [SerializeField] private TMP_Text messageText;

    // Personaje elegido en pantalla (-1 = ninguno). Todavía NO se conectó nada.
    private int _selectedIndex = -1;

    // Colores originales de los botones, para no pisarlos.
    private Color[] _characterNormalColors;
    private Color _confirmNormalColor = Color.white;

    // Para animar el aviso.
    private RectTransform _warningRect;
    private Vector2 _warningRest;
    private Color _warningBaseColor = Color.white;
    private Coroutine _warningRoutine;

    private void Awake()
    {
        // Guardamos cómo se ven los botones ahora, para volver a ese color.
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

        // El lugar donde pusiste el texto del aviso es donde queda al subir.
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
        if (messageText != null)
        {
            messageText.text = GameSession.Message;
        }

        // El mensaje se muestra una sola vez.
        GameSession.Message = "";

        _selectedIndex = -1;
        RefreshUI();
    }

    // Conectado al OnClick() de cada botón de personaje, con su número (0 a 3).
    public void OnCharacterSelected(int characterIndex)
    {
        // Si tocás el que ya estaba elegido, se deselecciona; si no, se elige este.
        _selectedIndex = (_selectedIndex == characterIndex) ? -1 : characterIndex;

        RefreshUI();
    }

    // Conectar al OnClick() del botón "Seleccionar".
    public void OnConfirmPressed()
    {
        if (_selectedIndex < 0)
        {
            ShowWarning();
            return;
        }

        GameSession.CharacterIndex = _selectedIndex;
        networkManager.StartConnection();
    }

    // Opcional: botón "Volver" para cambiar el código de sala.
    public void OnBackPressed()
    {
        SceneManager.LoadScene(mainMenuScene);
    }

    // ---------------------------------------------------------
    //  AVISO ANIMADO
    // ---------------------------------------------------------
    private void ShowWarning()
    {
        // Si no armaste el texto del aviso, usamos el texto simple de siempre.
        if (warningText == null)
        {
            if (messageText != null) messageText.text = warningMessage;
            return;
        }

        // Si se aprieta de nuevo mientras anima, arranca otra vez desde abajo.
        if (_warningRoutine != null) StopCoroutine(_warningRoutine);
        _warningRoutine = StartCoroutine(PlayWarning());
    }

    private IEnumerator PlayWarning()
    {
        warningText.text = warningMessage;
        warningText.gameObject.SetActive(true);

        Vector2 below = _warningRest + Vector2.down * slideDistance;

        // 1) SUBE desde abajo, apareciendo (arranca rápido y frena al llegar).
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

        // 2) SE QUEDA un momento.
        yield return new WaitForSecondsRealtime(holdTime);

        // 3) CAE de nuevo hacia abajo (cada vez más rápido) hasta desaparecer.
        for (float t = 0f; t < fallTime; t += Time.unscaledDeltaTime)
        {
            float progress = Mathf.Clamp01(t / fallTime);
            float eased = progress * progress;

            _warningRect.anchoredPosition = Vector2.Lerp(_warningRest, below, eased);
            SetWarningAlpha(1f - progress);
            yield return null;
        }

        // Al terminar se apaga y queda listo para la próxima vez.
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

    // ---------------------------------------------------------
    //  PANTALLA
    // ---------------------------------------------------------
    // Deja la pantalla de acuerdo a lo que está elegido ahora.
    private void RefreshUI()
    {
        bool hasSelection = _selectedIndex >= 0;

        // Botones de personaje: el elegido se marca.
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

        // "Seleccionar": se ve apagado si no hay nadie elegido, pero sigue
        // recibiendo el click para poder mostrar el aviso.
        if (confirmButton != null && confirmButton.image != null)
        {
            confirmButton.image.color = hasSelection ? _confirmNormalColor : confirmDisabledColor;
        }

        // Imagen de la derecha: la del personaje, o la de "nada elegido".
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

        // Paneles de descripción: Se activa únicamente el panel correspondiente al personaje seleccionado.
        if (characterDescriptionPanels != null)
        {
            for (int i = 0; i < characterDescriptionPanels.Length; i++)
            {
                if (characterDescriptionPanels[i] != null)
                {
                    // Se prende solo si coincide con el índice seleccionado
                    characterDescriptionPanels[i].SetActive(i == _selectedIndex);
                }
            }
        }
    }
}