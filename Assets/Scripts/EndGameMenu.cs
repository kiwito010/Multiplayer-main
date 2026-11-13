using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

public class EndGameMenu : MonoBehaviour
{
    public NetworkRunner runner;

    public void OnClickContinuar()
    {
        if (runner != null)
        {
            runner.Shutdown();
        }
        
        SceneManager.LoadScene("TitleScreen");
    }
}