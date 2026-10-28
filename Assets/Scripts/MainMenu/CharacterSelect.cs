using UnityEngine;
using UnityEngine.SceneManagement;

// Va en la escena ChooseCharacter. Cada boton de personaje llama a
// OnCharacterSelected con su numero, y eso inicia la conexion.
public class CharacterSelect : MonoBehaviour
{
    [Header("Referencias")]
    // El NetworkManagerFusion que ahora vive en ESTA escena.
    [SerializeField] private NetworkManagerFusion networkManager;

    [SerializeField] private string mainMenuScene = "MainMenu";

    // Conectar al OnClick() de cada boton, poniendo el numero a mano en el
    // Inspector: 0 = Ingeniero, 1 = Hacker, 2 = Cientifico, 3 = Explorador.
    // El orden tiene que coincidir con el array "Character Prefabs".
    public void OnCharacterSelected(int characterIndex)
    {
        GameSession.CharacterIndex = characterIndex;
        networkManager.StartConnection();
    }

    // Opcional: boton "Volver" para cambiar el codigo de sala.
    public void OnBackPressed()
    {
        SceneManager.LoadScene(mainMenuScene);
    }
}
