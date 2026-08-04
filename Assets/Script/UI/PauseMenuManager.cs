using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections; // Wajib ditambahkan untuk Coroutine

/// <summary>
/// Chaos Conductor - PauseMenuManager
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject pausePanel;

    [Header("Referensi Script Lain")]
    public PlayerInteraction playerInteraction;

    [Header("Audio")]
    public AudioSource sfxSource;
    public AudioClip buttonClickSfx;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool sedangModeSlowMo = playerInteraction != null && playerInteraction.IsPlacingNode;
            if (sedangModeSlowMo) return;

            TogglePause();
        }
    }

    private void TogglePause()
    {
        if (isPaused) ResumeGame();
        else PauseGame();
    }

    public void PauseGame()
    {
        isPaused = true;
        if (pausePanel != null) pausePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    public void ResumeGame()
    {
        PlayButtonSFX(); // Mainkan suara klik saat resume
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void LoadMainMenu()
    {
        StartCoroutine(LoadMainMenuRoutine());
    }

    private IEnumerator LoadMainMenuRoutine()
    {
        PlayButtonSFX(); // Mainkan suara klik saat mau kembali ke menu
        Time.timeScale = 1f; // Reset timeScale dulu
        yield return new WaitForSecondsRealtime(0.2f); // Tunggu suara selesai
        SceneManager.LoadScene(0);
    }

    public void PlayButtonSFX()
    {
        if (sfxSource != null && buttonClickSfx != null)
        {
            sfxSource.PlayOneShot(buttonClickSfx);
        }
    }
}