using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Status Pemain")]
    public int nyawa = 3; 
    private int skor = 0; 
    
    // Status ini bisa dibaca oleh script lain (seperti spawner)
    public bool isGameOver = false; 

    [Header("UI Referensi")]
    public TextMeshProUGUI teksSkor;
    public TextMeshProUGUI teksNyawa;
    
    [Tooltip("Masukkan panel/objek image pop-up Game Over dari Canvas ke sini")]
    public GameObject gameOverPanel; 

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Pastikan waktu berjalan normal saat mulai ulang game
        Time.timeScale = 1f; 
        isGameOver = false;

        // Sembunyikan panel pop-up di awal permainan
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        UpdateUI();
    }

    public void TambahSkor()
    {
        // Jangan tambah skor kalau sudah kalah
        if (isGameOver) return; 

        skor += 1;
        UpdateUI();
    }

    public void KurangiNyawa()
    {
        // Cegah nyawa terus berkurang setelah kalah
        if (isGameOver) return; 

        nyawa -= 1;
        UpdateUI();

        if (nyawa <= 0)
        {
            TriggerGameOver();
        }
    }

    private void TriggerGameOver()
    {
        isGameOver = true;
        Debug.Log("GAME OVER!");

        // Munculkan pop-up / image kekalahan
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        // Hentikan semua pergerakan physics dan waktu
        Time.timeScale = 0f;
    }

    private void UpdateUI()
    {
        if (teksSkor != null)
        {
            teksSkor.text = "Skor: " + skor.ToString();
        }
        
        if (teksNyawa != null)
        {
            teksNyawa.text = "Nyawa: " + nyawa.ToString();
        }
    }
}