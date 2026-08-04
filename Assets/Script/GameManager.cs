using UnityEngine;

/// <summary>
/// Chaos Conductor - GameManager
/// Pusat kontrol state game: skor, nyawa, dan kondisi game over.
/// Menggunakan pola Singleton agar bisa diakses dari script manapun
/// lewat GameManager.Instance.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ------------------------------------------------------------
    // SINGLETON
    // ------------------------------------------------------------
    public static GameManager Instance;

    [Header("Game State")]
    public int score = 0;
    public int lives = 3;

    private bool isGameOver = false;

    void Awake()
    {
        // Pastikan hanya ada 1 GameManager aktif di scene.
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("GameManager: instance lain terdeteksi, menghapus duplikat.");
            Destroy(gameObject);
        }
    }

    // ------------------------------------------------------------
    // SKOR
    // ------------------------------------------------------------

    /// <summary>
    /// Dipanggil saat kendaraan berhasil keluar layar dengan selamat (lihat VehicleBehaviour.OnBecameInvisible).
    /// </summary>
    public void TambahSkor()
    {
        if (isGameOver) return;

        score++;
        Debug.Log($"[GameManager] Skor bertambah! Skor sekarang: {score}");
    }

    // ------------------------------------------------------------
    // NYAWA
    // ------------------------------------------------------------

    /// <summary>
    /// Dipanggil saat terjadi kecelakaan/kegagalan (lihat VehicleBehaviour.OnCollisionEnter2D).
    /// </summary>
    public void KurangiNyawa()
    {
        if (isGameOver) return;

        lives--;
        Debug.Log($"[GameManager] Nyawa berkurang! Sisa nyawa: {lives}");

        if (lives <= 0)
        {
            HandleGameOver();
        }
    }

    // ------------------------------------------------------------
    // GAME OVER
    // ------------------------------------------------------------

    private void HandleGameOver()
    {
        isGameOver = true;
        Debug.Log($"[GameManager] GAME OVER! Skor akhir: {score}");

        // Hentikan waktu di seluruh game (semua Update yang bergantung pada Time.deltaTime
        // otomatis "berhenti" secara visual, physics juga ikut berhenti).
        Time.timeScale = 0f;

        // TODO: tampilkan UI "Game Over" / panel restart di sini kalau sudah ada sistem UI-nya.
    }

    /// <summary>
    /// Helper opsional untuk cek dari script lain apakah game sudah berakhir.
    /// </summary>
    public bool IsGameOver()
    {
        return isGameOver;
    }
}