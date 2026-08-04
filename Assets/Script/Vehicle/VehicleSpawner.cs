using UnityEngine;

/// <summary>
/// Chaos Conductor - VehicleSpawner
/// Memunculkan kendaraan secara berkala di titik spawn acak,
/// dengan rotasi mengikuti SpawnPoint agar transform.up di VehicleController
/// otomatis mengarah ke arah yang benar.
/// </summary>
public class VehicleSpawner : MonoBehaviour
{
    [Header("Sumber Kendaraan & Titik Spawn")]
    [Tooltip("Kumpulan prefab kendaraan (RedCar, BlueCar, YellowTruck, GreenAmbulance, dll).")]
    public GameObject[] vehiclePrefabs;

    [Tooltip("Titik-titik spawn. Rotasi Transform ini menentukan arah gerak kendaraan.")]
    public Transform[] spawnPoints;

    [Header("Timer Spawn")]
    [Tooltip("Jeda waktu (detik) antar kemunculan kendaraan.")]
    public float spawnInterval = 2f;

    private float timer;

    void Update()
    {
        // Cek apakah game sudah over melalui GameManager
        if (GameManager.Instance != null && GameManager.Instance.isGameOver)
        {
            // Hentikan proses spawn jika sudah kalah
            return; 
        }

        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnVehicle();
        }
    }

    private void SpawnVehicle()
    {
        if (vehiclePrefabs.Length == 0 || spawnPoints.Length == 0)
        {
            Debug.LogWarning("VehicleSpawner: vehiclePrefabs atau spawnPoints masih kosong!");
            return;
        }

        GameObject chosenPrefab = vehiclePrefabs[Random.Range(0, vehiclePrefabs.Length)];
        Transform chosenSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Instantiate(chosenPrefab, chosenSpawnPoint.position, chosenSpawnPoint.rotation);
    }

    void OnDrawGizmos()
    {
        if (spawnPoints == null) return;

        Gizmos.color = Color.cyan;
        foreach (Transform sp in spawnPoints)
        {
            if (sp == null) continue;
            Gizmos.DrawSphere(sp.position, 0.2f);
            Gizmos.DrawLine(sp.position, sp.position + sp.up * 1.5f);
        }
    }
}