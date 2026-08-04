using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Chaos Conductor - MainMenuManager
/// Ditempel ke GameObject apa saja di scene Main Menu (misal GameObject kosong "MenuManager").
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public void StartGame()
    {
        SceneManager.LoadScene(1); // Scene index 1 = scene gameplay
    }

    public void QuitGame()
    {
        Debug.Log("Quit ditekan (hanya bekerja di build, tidak menutup Editor)");
        Application.Quit();
    }
}