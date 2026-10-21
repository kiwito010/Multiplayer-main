using UnityEngine;

// Maneja el botón "Salir" y el cartel de confirmación "¿Estás seguro?".
// No tiene nada que ver con la red, es solo UI de menú.
//
// IMPORTANTE: este script tiene que estar en un objeto que esté SIEMPRE
// activo (por ejemplo un Empty llamado "ExitMenu"), NO dentro del cartel.
// Si lo pusieras en el cartel, cuando el cartel está oculto el script
// también queda apagado y no podría volver a mostrarlo.
public class ExitMenu : MonoBehaviour
{
    [Header("Referencias de UI")]
    public GameObject confirmPanel; // El cartel completo de "¿Estás seguro?"

    private void Start()
    {
        // Arrancamos con el cartel oculto, así no tapa el menú al abrir el juego.
        confirmPanel.SetActive(false);
    }

    // Conectar al OnClick() del botón "Salir" del menú.
    public void OnExitButtonPressed()
    {
        confirmPanel.SetActive(true); // Mostramos el cartel
    }

    // Conectar al OnClick() del botón "No" del cartel.
    public void OnCancelPressed()
    {
        confirmPanel.SetActive(false); // Cerramos el cartel y seguimos en el menú
    }

    // Conectar al OnClick() del botón "Sí" del cartel.
    public void OnConfirmPressed()
    {
        // Application.Quit() cierra el juego, pero SOLO funciona en un build
        // (el .exe). Dentro del Editor de Unity no hace nada, y por eso
        // parecería que "no anda". Con esta condición, en el Editor
        // detenemos el modo Play, y en el build cerramos de verdad.
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
