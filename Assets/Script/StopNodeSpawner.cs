using UnityEngine;
using UnityEngine.InputSystem;

public class StopNodeSpawner : MonoBehaviour
{
    [Header("Pengaturan Spawn")]
    [Tooltip("Masukkan prefab Stop Node ke sini")]
    public GameObject stopNodePrefab;
    
    [Tooltip("Layer khusus untuk objek jalan yang bisa diklik")]
    public LayerMask roadLayerMask;

    void Update()
    {
        if (Mouse.current == null) return;

        // Mendeteksi saat klik kanan ditekan
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Mouse.current.position.ReadValue());

            // Mengecek apakah kursor menyentuh objek dengan layer jalan
            Collider2D hit = Physics2D.OverlapPoint(mousePos, roadLayerMask);

            if (hit != null)
            {
                // Ambil titik tengah dari tile jalan yang diklik agar posisinya pas di tengah (snap)
                Vector2 spawnPos = hit.transform.position;
                
                // Cek apakah di posisi tersebut sudah ada Stop Node yang aktif
                bool nodeExists = false;
                Collider2D[] colliders = Physics2D.OverlapCircleAll(spawnPos, 0.2f);
                
                foreach (Collider2D col in colliders)
                {
                    if (col.CompareTag("StopNode"))
                    {
                        // Jika sudah ada, hancurkan (fitur untuk membatalkan mode berhenti)
                        Destroy(col.gameObject);
                        nodeExists = true;
                        break;
                    }
                }

                // Jika belum ada Stop Node di titik tersebut, spawn yang baru
                if (!nodeExists)
                {
                    Instantiate(stopNodePrefab, spawnPos, Quaternion.identity);
                }
            }
        }
    }
}