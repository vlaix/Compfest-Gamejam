using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Wajib ditambahkan untuk Coroutine

/// <summary>
/// Chaos Conductor - MainMenuManager
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSfx;

    public void StartGame()
    {
        StartCoroutine(StartGameRoutine());
    }

    private IEnumerator StartGameRoutine()
    {
        PlayButtonSFX();
        // Tunggu 0.2 detik (realtime) agar suara klik selesai berbunyi
        yield return new WaitForSecondsRealtime(0.2f);
        SceneManager.LoadScene(1);
    }

    public void QuitGame()
    {
        StartCoroutine(QuitGameRoutine());
    }

    private IEnumerator QuitGameRoutine()
    {
        PlayButtonSFX();
        yield return new WaitForSecondsRealtime(0.2f);
        Debug.Log("Quit ditekan (hanya bekerja di build, tidak menutup Editor)");
        Application.Quit();
    }

    // Fungsi untuk mainkan SFX (bisa dipanggil mandiri jika perlu)
    public void PlayButtonSFX()
    {
        if (sfxSource != null && buttonClickSfx != null)
        {
            sfxSource.PlayOneShot(buttonClickSfx);
        }
    }
}