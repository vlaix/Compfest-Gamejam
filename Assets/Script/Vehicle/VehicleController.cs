using UnityEngine;

/// <summary>
/// Chaos Conductor - VehicleController
/// Script tunggal & modular untuk semua tipe kendaraan di tahap graybox.
/// Logika tiap tipe dipisah ke method HandleXxx() supaya mudah dibaca & di-tweak saat jam.
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    // ------------------------------------------------------------
    // ENUM: Tipe kendaraan (poin 1 & 4)
    // ------------------------------------------------------------
    public enum VehicleType
    {
        RedCar,
        BlueCar,
        YellowTruck,
        GreenAmbulance
    }

    [Header("Identitas Kendaraan")]
    public VehicleType vehicleType = VehicleType.RedCar;

    [Header("Pergerakan")]
    [Tooltip("Klik kanan komponen ini > Reset untuk auto-isi speed default sesuai tipe.")]
    public float speed = 5f;

    [Header("Sensor Depan (Raycast2D)")]
    [Tooltip("Panjang raycast ke arah depan (transform.up).")]
    public float raycastDistance = 2f;
    [Tooltip("Jarak minimal ke kendaraan lain sebelum BlueCar berhenti.")]
    public float stoppingDistance = 1.5f;
    [Tooltip("Set ke layer 'Vehicle' agar raycast/overlap hanya mendeteksi kendaraan lain.")]
    public LayerMask vehicleLayerMask;

    [Header("Khusus GreenAmbulance")]
    [Tooltip("Radius efek: BlueCar di dalam radius ini akan dipaksa minggir/berhenti.")]
    public float ambulanceRadius = 4f;

    [Header("Pengaturan Belokan")]
    [Tooltip("Ukuran 1 grid di dalam Unity scene kamu.")]
    public float gridSize = 1f; 

    // ------------------------------------------------------------
    // STATE INTERNAL
    // ------------------------------------------------------------
    private Rigidbody2D rb;
    public bool isStoppedByTraffic;   
    public bool isStoppedByAmbulance; 

    // Variabel untuk melacak penundaan belokan
    private bool pendingTurn = false;
    private float distanceToTurn = 0f;
    private float targetTurnAngle = 0f;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = 0f;                              
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; 
    }

    void Reset()
    {
        ApplyDefaultSpeedByType();
    }

    private void ApplyDefaultSpeedByType()
    {
        switch (vehicleType)
        {
            case VehicleType.RedCar: speed = 8f; break; 
            case VehicleType.BlueCar: speed = 4f; break; 
            case VehicleType.YellowTruck: speed = 1.5f; break; 
            case VehicleType.GreenAmbulance: speed = 6f; break; 
        }
    }

    void Update()
    {
        switch (vehicleType)
        {
            case VehicleType.RedCar:
                HandleRedCar();
                break;
            case VehicleType.BlueCar:
                isStoppedByAmbulance = false;
                HandleBlueCar();
                break;
            case VehicleType.YellowTruck:
                HandleYellowTruck();
                break;
            case VehicleType.GreenAmbulance:
                isStoppedByTraffic = false;
                isStoppedByAmbulance = false;
                break;
        }
    }

    void LateUpdate()
    {
        if (vehicleType == VehicleType.GreenAmbulance)
        {
            HandleGreenAmbulance();
        }
    }

    void FixedUpdate()
    {
        bool bolehJalan = !isStoppedByTraffic && !isStoppedByAmbulance;
        rb.linearVelocity = bolehJalan ? (Vector2)transform.up * speed : Vector2.zero;
        
        // Proses penundaan belokan (kiri atau kanan)
        if (pendingTurn && bolehJalan)
        {
            // Kurangi jarak tempuh sesuai kecepatan dan waktu
            distanceToTurn -= speed * Time.fixedDeltaTime;
            
            if (distanceToTurn <= 0f)
            {
                // Eksekusi rotasi sesuai target sudut
                transform.Rotate(0, 0, targetTurnAngle);
                pendingTurn = false;
            }
        }
    }

    // ------------------------------------------------------------
    // DETEKSI NODE PEREMPATAN
    // ------------------------------------------------------------
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Pastikan triangle memiliki Tag "TurnNode" dan belum ada belokan yang antre
        if (collision.CompareTag("TurnNode") && !pendingTurn)
        {
            // 0 = Lurus, 1 = Kiri, 2 = Kanan
            int randomDir = Random.Range(0, 3);

            if (randomDir == 1) 
            {
                // Kiri: Maju 1 grid dulu baru belok
                pendingTurn = true;
                distanceToTurn = 1.05f * gridSize;
                targetTurnAngle = 90f;
            }
            else if (randomDir == 2)
            {
                // Kanan: Maju 2 grid dulu baru belok
                pendingTurn = true;
                distanceToTurn = 1.6f * gridSize;
                targetTurnAngle = -90f;
            }
            // Jika randomDir == 0 (Lurus), pendingTurn tetap false dan mobil tidak berotasi
        }
    }

    // ------------------------------------------------------------
    // PERILAKU PER TIPE (poin 4 - "The Order")
    // ------------------------------------------------------------

    private void HandleRedCar()
    {
        isStoppedByTraffic = false;
    }

    private void HandleBlueCar()
    {
        // Sesuaikan nilai 0.6f ini dengan ukuran mobilmu, 
        // pastikan posisinya berada tepat di luar collider depan mobil.
        float rayStartOffset = 0.6f; 
        Vector2 startPos = (Vector2)transform.position + (Vector2)transform.up * rayStartOffset;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, transform.up, raycastDistance, vehicleLayerMask);

        // Debug sekarang dimulai dari moncong mobil
        Debug.DrawRay(startPos, transform.up * raycastDistance, hit.collider != null ? Color.red : Color.green);

        isStoppedByTraffic = (hit.collider != null && hit.distance <= stoppingDistance);
    }

    private void HandleYellowTruck()
    {
        isStoppedByTraffic = false;
    }

    private void HandleGreenAmbulance()
    {
        Collider2D[] nearby = Physics2D.OverlapCircleAll(transform.position, ambulanceRadius, vehicleLayerMask);

        foreach (Collider2D col in nearby)
        {
            VehicleController other = col.GetComponent<VehicleController>();
            if (other != null && other.vehicleType == VehicleType.BlueCar)
            {
                other.ForceStopByAmbulance(true);
            }
        }
    }

    public void ForceStopByAmbulance(bool stop)
    {
        isStoppedByAmbulance = stop;
    }

    void OnDrawGizmosSelected()
    {
        if (vehicleType == VehicleType.GreenAmbulance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, ambulanceRadius);
        }
    }
}