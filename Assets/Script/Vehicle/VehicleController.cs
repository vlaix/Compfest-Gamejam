using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class VehicleController : MonoBehaviour
{
    public enum VehicleType { RedCar, BlueCar, YellowTruck, GreenAmbulance }
    
    [Header("Identitas Kendaraan")]
    public VehicleType vehicleType = VehicleType.RedCar;

    [Header("Pergerakan")]
    public float speed = 5f;

    [Header("Sensor Depan (Raycast2D)")]
    public float raycastDistance = 2f;
    public float stoppingDistance = 1.5f;
    public LayerMask vehicleLayerMask;

    [Header("Khusus GreenAmbulance")]
    public float ambulanceRadius = 4f;

    [Header("Pengaturan Belokan")]
    [Tooltip("Sesuaikan dengan ukuran 1 grid/kotak di Scene kamu")]
    public float gridSize = 1f; 

    // STATE INTERNAL
    private Rigidbody2D rb;
    private bool isStoppedByTraffic;   
    private bool isStoppedByAmbulance; 

    private bool pendingTurn = false;
    private float distanceToTurn = 0f;
    private float targetTurnAngle = 0f;

    // TAMBAHAN STATE INTERNAL UNTUK TIMER
    private bool isStoppedByNode = false;
    private float nodeStopTimer = 0f;

    private float honkTimer = 0f;

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

    // MEMBACA ARAH MONCONG SEGITIGA
    private void OnTriggerEnter2D(Collider2D collision)
    {
        // 1. CEK JIKA MENYENTUH NODE BERHENTI DI JALAN LURUS
        if (collision.CompareTag("StopNode"))
        {
            StopNodeController stopNode = collision.GetComponent<StopNodeController>();
            if (stopNode != null && stopNode.isStopMode)
            {
                if (vehicleType != VehicleType.YellowTruck)
                {
                    isStoppedByNode = true;
                    nodeStopTimer = 2f; 

                    if (AudioManager.Instance != null) AudioManager.Instance.PlayBrake();
                }
                stopNode.ResetStopMode(); 
            }
            // Keluar dari fungsi karena node ini tidak untuk belok
            return; 
        }

        // 2. CEK JIKA MENYENTUH NODE BELOK DI PEREMPATAN
        if (collision.CompareTag("TurnNode") && !pendingTurn)
        {
            TurnNodeController turnNode = collision.GetComponent<TurnNodeController>();
            if (turnNode == null) return;

            // Fitur berhenti juga tetap bisa dipakai di perempatan
            if (turnNode.isStopMode)
            {
                if (vehicleType != VehicleType.YellowTruck)
                {
                    isStoppedByNode = true;
                    nodeStopTimer = 2f; 
                }
                turnNode.ResetStopMode(); 
            }

            Vector2 carDirection = transform.up;
            Vector2 nodeDirection = collision.transform.up;

            float angleDiff = Vector2.SignedAngle(carDirection, nodeDirection);
            angleDiff = Mathf.Round(angleDiff);

            if (angleDiff == 90f) 
            {
                pendingTurn = true;
                distanceToTurn = 2.65f * gridSize;
                targetTurnAngle = 90f;
            }
            else if (angleDiff == -90f)
            {
                pendingTurn = true;
                distanceToTurn = 3.65f * gridSize;
                targetTurnAngle = -90f;
            }
            
            turnNode.ResetToInitialRotation();
        }
    }

    // PERILAKU PER TIPE
    private void HandleRedCar()
    {
        isStoppedByTraffic = false;
    }

    private void HandleBlueCar()
    {
        // FIX: Offset dimajukan sedikit agar tidak mendeteksi badannya sendiri
        float rayStartOffset = 1.15f; 
        Vector2 startPos = (Vector2)transform.position + (Vector2)transform.up * rayStartOffset;
        
        RaycastHit2D hit = Physics2D.Raycast(startPos, transform.up, raycastDistance, vehicleLayerMask);

        if (hit.collider != null && hit.distance <= stoppingDistance)
        {
            isStoppedByTraffic = true;
            
            // Cek apakah jeda klakson sudah lewat (setiap 2 detik sekali)
            if (Time.time >= honkTimer)
            {
                if (AudioManager.Instance != null) AudioManager.Instance.PlayHonk();
                honkTimer = Time.time + 2f;
            }
        }
            else
        {
            isStoppedByTraffic = false;
        }
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