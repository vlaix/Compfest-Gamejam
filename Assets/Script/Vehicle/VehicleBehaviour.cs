using UnityEngine;

public class VehicleBehaviour : MonoBehaviour
{

    private void OnCollisionEnter2D(Collision2D collision)
        {
            // Mengecek apakah objek yang ditabrak memiliki Tag "Vehicle"
            // Pastikan kamu membuat dan memasang Tag "Vehicle" di Unity untuk semua mobil
            if (collision.gameObject.CompareTag("Vehicle"))
            {
                // Panggil fungsi dari script utama game kamu untuk mengurangi nyawa pemain di sini
                // Contoh: GameManager.Instance.KurangiNyawa();
                
                // Hancurkan objek mobil ini setelah tabrakan
                Destroy(gameObject);
            }
        }

    private void OnBecameInvisible()
    {
        // Panggil fungsi dari script utama game kamu untuk menambah skor di sini
        // Contoh: GameManager.Instance.TambahSkor();
        
        // Hancurkan objek mobil yang sudah keluar layar agar memori tidak penuh
        Destroy(gameObject);
    }
    
}
