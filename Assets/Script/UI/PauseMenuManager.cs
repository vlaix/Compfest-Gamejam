using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Chaos Conductor - PauseMenuManager
/// Toggle Pause/Resume lewat ESC, tapi menghindari bentrok dengan PlayerInteraction
/// yang juga memakai ESC untuk membatalkan mode tahan-klik (slow motion).
/// </summary>
public class PauseMenuManager : MonoBehaviour
{
    [Header("Referensi UI")]
    public GameObject pausePanel;

    [Header("Referensi Script Lain")]
    [Tooltip("Dipakai untuk cek apakah pemain sedang menahan klik (mode slow-mo), supaya ESC tidak bentrok.")]
    public PlayerInteraction playerInteraction;

    private bool isPaused = false;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // PENTING: PlayerInteraction JUGA memakai ESC untuk membatalkan mode tahan-klik (slow motion).
            // Kalau pemain sedang menahan klik (isPlacingNode true), ESC di frame ini adalah
            // milik PlayerInteraction (cancel placement) -> Pause Menu diabaikan dulu.
            // Baru kalau pemain TIDAK sedang menahan klik, ESC dianggap untuk toggle Pause Menu.
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
        isPaused = false;
        if (pausePanel != null) pausePanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public void LoadMainMenu()
    {
        Time.timeScale = 1f; // penting: reset dulu biar scene Main Menu tidak ikut freeze
        SceneManager.LoadScene(0);
    }
}