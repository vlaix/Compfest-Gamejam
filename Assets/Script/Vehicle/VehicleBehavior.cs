using UnityEngine;

public class VehicleBehavior : MonoBehaviour
{
    private bool isDead = false;


    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Cek apakah menabrak sesama mobil dan statusnya belum hancur
        if (collision.gameObject.CompareTag("Vehicle") && !isDead)
        {
            isDead = true; 

            // Tandai juga mobil yang ditabrak agar script dia tidak memicu pengurangan nyawa lagi
            VehicleBehavior mobilLain = collision.gameObject.GetComponent<VehicleBehavior>();
            if (mobilLain != null)
            {
                mobilLain.isDead = true;
            }

            // Panggil GameManager untuk mengurangi nyawa (cukup 1 kali)
            if (GameManager.Instance != null)
            {
                GameManager.Instance.KurangiNyawa();
            }

            // Hancurkan kedua mobil secara langsung
            Destroy(collision.gameObject);
            Destroy(gameObject);
        }
    }

    private void OnBecameInvisible()
    {
        // Pastikan mobil keluar map dalam keadaan hidup (bukan karena terlempar akibat tabrakan)
        if (!isDead)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TambahSkor();
            }
            
            // Hancurkan mobil yang sudah keluar layar
            Destroy(gameObject);
        }
    }
}