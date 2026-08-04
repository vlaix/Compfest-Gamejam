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
    private bool isStoppedByTraffic;   
    private bool isStoppedByAmbulance;

    // Variabel untuk melacak berhenti
    private bool isStoppedByNode = false;
    private float nodeStopTimer = 0f; 

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
        // KURANGI TIMER JIKA SEDANG DITAHAN NODE
        if (isStoppedByNode)
        {
            nodeStopTimer -= Time.fixedDeltaTime;
            if (nodeStopTimer <= 0f)
            {
                isStoppedByNode = false; // Waktu habis, mobil boleh jalan lagi
            }
        }

        // Kendaraan boleh jalan jika tidak ditahan oleh apa pun (termasuk timer node)
        bool bolehJalan = !isStoppedByTraffic && !isStoppedByAmbulance && !isStoppedByNode;
        rb.linearVelocity = bolehJalan ? (Vector2)transform.up * speed : Vector2.zero;
        
        // KOMPENSASI JARAK BELOK
        if (pendingTurn && bolehJalan)
        {
            distanceToTurn -= speed * Time.fixedDeltaTime;
            
            if (distanceToTurn <= 0f)
            {
                float kelebihanJarak = -distanceToTurn;
                transform.position -= transform.up * kelebihanJarak;
                transform.Rotate(0, 0, targetTurnAngle);
                transform.position += transform.up * kelebihanJarak;
                
                pendingTurn = false;
            }
        }
    }

    // ------------------------------------------------------------
    // DETEKSI NODE PEREMPATAN
    // ------------------------------------------------------------
private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("TurnNode") && !pendingTurn)
        {
            TurnNodeController node = collision.GetComponent<TurnNodeController>();
            if (node == null) return;

            // CEK APAKAH SEGITIGA BERWARNA MERAH (MODE BERHENTI)
            if (node.isStopMode)
            {
                // Truk kuning kebal terhadap efek berhenti
                if (vehicleType != VehicleType.YellowTruck)
                {
                    isStoppedByNode = true;
                    nodeStopTimer = 2f; // Tahan selama 2 detik
                }
                
                // Segitiga tetap dikembalikan warnanya agar kereset, 
                // meskipun yang menabrak adalah truk kuning
                node.ResetStopMode(); 
            }

            // KALKULASI ARAH BELOKAN
            Vector2 carDirection = transform.up;
            Vector2 nodeDirection = collision.transform.up;

            float angleDiff = Vector2.SignedAngle(carDirection, nodeDirection);
            angleDiff = Mathf.Round(angleDiff);

            if (angleDiff == 90f) 
            {
                pendingTurn = true;
                distanceToTurn = 0.62f * gridSize;
                targetTurnAngle = 90f;
            }
            else if (angleDiff == -90f)
            {
                pendingTurn = true;
                distanceToTurn = 1.1f * gridSize;
                targetTurnAngle = -90f;
            }
            
            // Kembalikan rotasi segitiga seperti semula
            node.ResetToInitialRotation();
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