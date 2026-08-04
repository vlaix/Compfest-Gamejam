using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class StopNodeSpawner : MonoBehaviour
{
    [Header("Pengaturan Spawn")]
    public GameObject stopNodePrefab;
    public LayerMask roadLayerMask;
    
    [Header("Referensi Tilemap")]
    [Tooltip("Masukkan objek Tilemap jalan kamu ke sini")]
    public Tilemap roadTilemap; 

    [Header("Pengaturan Cooldown")]
    [Tooltip("Jeda waktu sebelum bisa memunculkan node baru (dalam detik)")]
    public float spawnCooldown = 0.5f;
    
    // Variabel untuk melacak kapan kita boleh klik lagi
    private float nextSpawnTime = 0f;

    void Update()
    {
        if (Mouse.current == null) return;

        // Mendeteksi klik kanan
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            // Cek apakah waktu saat ini masih di bawah batas tunggu
            // Jika ya, batalkan eksekusi di bawahnya
            if (Time.time < nextSpawnTime)
            {
                return; 
            }

            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());
            Collider2D hit = Physics2D.OverlapPoint(mousePos, roadLayerMask);

            if (hit != null && roadTilemap != null)
            {
                Vector3Int cellPosition = roadTilemap.WorldToCell(mousePos);
                Vector2 spawnPos = roadTilemap.GetCellCenterWorld(cellPosition);
                
                bool nodeExists = false;
                Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPos, 0.2f);
                
                foreach (Collider2D col in colliders)
                {
                    if (col.CompareTag("StopNode"))
                    {
                        Destroy(col.gameObject);
                        nodeExists = true;
                        
                        // Setel ulang timer cooldown setelah berhasil menghapus node
                        nextSpawnTime = Time.time + spawnCooldown;
                        break;
                    }
                }

                if (!nodeExists)
                {
                    Instantiate(stopNodePrefab, spawnPos, Quaternion.identity);
                    
                    // Setel ulang timer cooldown setelah berhasil membuat node baru
                    nextSpawnTime = Time.time + spawnCooldown;
                }
            }
        }
    }
}