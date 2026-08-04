using UnityEngine;

public class StopNodeController : MonoBehaviour
{
    // Mode ini selalu true karena objek ini hanya di-spawn saat pemain ingin menghentikan mobil
    public bool isStopMode = true; 

    // Fungsi ini akan dipanggil oleh VehicleController setelah mobil berhenti
    public void ResetStopMode()
    {
        // Langsung hancurkan objek ini agar jalan kembali normal
        Destroy(gameObject);
    }
}