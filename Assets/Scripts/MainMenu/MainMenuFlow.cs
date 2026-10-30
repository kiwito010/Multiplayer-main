using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

// Va en la escena MainMenu. Reemplaza a las funciones de los botones que
// antes estaban en NetworkManagerFusion: ahora "Crear" y "Unirse" ya NO se
// conectan directo, solo guardan lo que escribiste y te llevan a elegir
// personaje. La conexion real ocurre despues, en ChooseCharacter.
public class MainMenuFlow : MonoBehaviour
{
    [Header("Campos de texto (arrastrar desde la Hierarchy)")]
    public TMP_InputField createRoomInput; // el de "Crear codigo..."
    public TMP_InputField joinRoomInput;   // el de "Insertar codigo..."

    [Header("Escena siguiente")]
    [SerializeField] private string chooseCharacterScene = "ChooseCharacter";

    [Header("Popup de error")]
    [SerializeField] private GameObject errorPanel; // el panel completo del popup
    [SerializeField] private TMP_Text errorText;    // el texto donde se escribe el mensaje

    private void Start()
    {
        if (errorPanel == null) return;

        // Si el juego dejó un mensaje (por ejemplo "no existe esa sala" o
        // "personaje ocupado"), mostramos el popup. Si no, queda oculto.
        bool hasMessage = !string.IsNullOrEmpty(GameSession.Message);
        errorPanel.SetActive(hasMessage);

        if (hasMessage)
        {
            if (errorText != null) errorText.text = GameSession.Message;
            GameSession.Message = ""; // se muestra una sola vez
        }
    }

    // Conectar al OnClick() del botón "Aceptar" del popup.
    public void OnErrorAcceptPressed()
    {
        if (errorPanel != null) errorPanel.SetActive(false);
    }

    // Conectar al OnClick() del boton "Crear".
    public void OnHostButtonPressed()
    {
        GoToCharacterSelect(GameMode.Host, createRoomInput.text);
    }

    // Conectar al OnClick() del boton "Unirse".
    public void OnJoinButtonPressed()
    {
        GoToCharacterSelect(GameMode.Client, joinRoomInput.text);
    }

    public void OnBackButtonPressed()
    {
        SceneManager.LoadScene("TitleScreen");
    }

    private void GoToCharacterSelect(GameMode mode, string rawCode)
    {
        // Trim() saca espacios del principio y del final.
        string code = rawCode.Trim();

        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Escribi un codigo de sala antes de continuar.");
            return;
        }

        // Guardamos lo elegido para usarlo mas adelante.
        GameSession.Mode = mode;
        GameSession.RoomCode = code;

        SceneManager.LoadScene(chooseCharacterScene);
    }
}