using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Va en la escena ChooseCharacter.
//  - Los botones de personaje SELECCIONAN (no conectan).
//  - Tocar el personaje elegido lo deselecciona; tocar otro cambia la elección.
//  - El botón "Seleccionar" es el que inicia la conexión.
public class CharacterSelect : MonoBehaviour
{
    [Header("Conexión")]
    // El NetworkManagerFusion que vive en ESTA escena.
    [SerializeField] private NetworkManagerFusion networkManager;
    [SerializeField] private string mainMenuScene = "MainMenu";

    [Header("Botones de personaje (el orden importa: 0=Ingeniero, 1=Hacker, 2=Científico, 3=Exploradora)")]
    [SerializeField] private Button[] characterButtons;
    [SerializeField] private Color normalButtonColor = Color.white;
    [SerializeField] private Color selectedButtonColor = new Color(1f, 0.6f, 0.6f, 1f);

    // Opcional: un objeto por botón (por ejemplo un borde o una marca) que se
    // prende solo en el personaje elegido. Sirve si el cambio de color no se nota.
    [SerializeField] private GameObject[] selectedMarkers;

    [Header("Botón Seleccionar")]
    [SerializeField] private Button confirmButton;

    [Header("Imagen de la derecha")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Sprite defaultPortrait;      // cuando no hay nada elegido
    [SerializeField] private Sprite[] characterPortraits; // una por personaje, en el mismo orden

    [Header("Pop up (se ve solo mientras hay un personaje elegido)")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TMP_Text popupTitleText;        // opcional
    [SerializeField] private TMP_Text popupDescriptionText;  // opcional
    [SerializeField] private string[] characterNames = { "Ingeniero", "Hacker", "Científico", "Exploradora" };
    [SerializeField, TextArea] private string[] characterDescriptions;

    [Header("Avisos")]
    [SerializeField] private TMP_Text messageText;

    // Personaje elegido en pantalla (-1 = ninguno). Todavía NO se conectó nada.
    private int _selectedIndex = -1;

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
    // Mantiene el nombre de antes, así tu conexión actual de botones sigue valiendo.
    public void OnCharacterSelected(int characterIndex)
    {
        // Si tocás el que ya estaba elegido, se deselecciona; si no, se elige este.
        _selectedIndex = (_selectedIndex == characterIndex) ? -1 : characterIndex;

        RefreshUI();
    }

    // Conectar al OnClick() del botón "Seleccionar": recién acá se inicia la conexión.
    public void OnConfirmPressed()
    {
        if (_selectedIndex < 0)
        {
            if (messageText != null) messageText.text = "Elegí un personaje primero.";
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

                characterButtons[i].image.color = (i == _selectedIndex) ? selectedButtonColor : normalButtonColor;
            }
        }

        if (selectedMarkers != null)
        {
            for (int i = 0; i < selectedMarkers.Length; i++)
            {
                if (selectedMarkers[i] != null) selectedMarkers[i].SetActive(i == _selectedIndex);
            }
        }

        // "Seleccionar" solo se puede apretar si hay un personaje elegido.
        if (confirmButton != null)
        {
            confirmButton.interactable = hasSelection;
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

        // Pop up: visible solo con un personaje elegido.
        if (popupPanel != null)
        {
            popupPanel.SetActive(hasSelection);
        }

        if (hasSelection)
        {
            if (popupTitleText != null && characterNames != null && _selectedIndex < characterNames.Length)
            {
                popupTitleText.text = characterNames[_selectedIndex];
            }

            if (popupDescriptionText != null && characterDescriptions != null && _selectedIndex < characterDescriptions.Length)
            {
                popupDescriptionText.text = characterDescriptions[_selectedIndex];
            }
        }
    }
}