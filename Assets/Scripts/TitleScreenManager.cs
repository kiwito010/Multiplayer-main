using UnityEngine;
using UnityEngine.SceneManagement;

// Este script no tiene nada de red todavía — es solo navegación de escenas,
// como cualquier juego de un solo jugador. Fusion recién entra en juego
// más adelante, cuando desde el MainMenu se aprieta "Crear Partida" o
// "Unirse".
public class TitleScreenManager : MonoBehaviour
{
    // La conectamos al OnClick() del botón "Jugar".
    public void OnPlayButtonPressed()
    {
        // SceneManager.LoadScene carga otra escena por su nombre. Tiene que
        // coincidir EXACTO con el nombre del archivo de la escena (sin
        // ".unity") y esa escena tiene que estar agregada en el
        // File > Build Settings, si no, esto va a tirar un error en runtime
        // diciendo que no la encuentra.
        SceneManager.LoadScene("MainMenu");
    }
}
