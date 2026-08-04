using System.Collections;
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
    [Tooltip("Jarak ekstra (di luar tepi collider sendiri) sebelum raycast mulai ditembakkan, biar tidak nyangkut ke collider sendiri.")]
    public float raycastOriginPadding = 0.1f;

    [Header("Khusus GreenAmbulance")]
    [Tooltip("Radius efek: BlueCar di dalam radius ini akan dipaksa minggir/berhenti.")]
    public float ambulanceRadius = 4f;

    [Header("Interaksi Node (Turn / Stop)")]
    [Tooltip("Kecepatan rotasi (derajat/detik) saat kendaraan berbelok halus akibat Turn Node.")]
    public float turnSpeed = 180f;

    // ------------------------------------------------------------
    // STATE INTERNAL
    // ------------------------------------------------------------
    private Rigidbody2D rb;
    private bool isStoppedByTraffic;   
    private bool isStoppedByAmbulance;

    // Variabel untuk melacak berhenti
    private bool isStoppedByNode = false;
    private float nodeStopTimer = 0f; 

    private bool isTurning;            // true selama proses belok halus akibat Turn Node
    private float targetRotationZ;     // sudut rotasi (derajat) yang dituju saat berbelok

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // GetComponentInChildren dipakai supaya tetap ketemu walau BoxCollider2D-nya
        // ditaruh di child object (misal parent kosong + child untuk sprite & collider).
        vehicleCollider = GetComponentInChildren<Collider2D>();
        rb.gravityScale = 0f;                              // top-down, tidak butuh gravity
        rb.constraints = RigidbodyConstraints2D.FreezeRotation; // biar box tidak muter random pas nabrak
    }

    // Dipanggil Editor saat komponen pertama kali ditambahkan / lewat context menu "Reset"
    void Reset()
    {
        ApplyDefaultSpeedByType();
    }

    private void ApplyDefaultSpeedByType()
    {
        switch (vehicleType)
        {
            case VehicleType.RedCar: speed = 8f; break; // cepat
            case VehicleType.BlueCar: speed = 4f; break; // sedang
            case VehicleType.YellowTruck: speed = 1.5f; break; // sangat lambat
            case VehicleType.GreenAmbulance: speed = 6f; break; // cepat, tidak direm siapapun
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
                // Reset flag ambulance duluan di sini, nanti GreenAmbulance yang men-set ulang
                // di LateUpdate (biar urutan eksekusi antar GameObject tidak jadi masalah).
                isStoppedByAmbulance = false;
                HandleBlueCar();
                break;

            case VehicleType.YellowTruck:
                HandleYellowTruck();
                break;

            case VehicleType.GreenAmbulance:
                // Logika deteksi radius dilakukan di LateUpdate, lihat di bawah.
                isStoppedByTraffic = false;
                isStoppedByAmbulance = false;
                break;
        }
    }

    // LateUpdate berjalan SETELAH semua Update() objek lain selesai.
    // Ini memastikan flag "dipaksa berhenti" dari Ambulance tidak ke-overwrite
    // oleh reset flag milik BlueCar sendiri, apapun urutan Script Execution Order-nya.
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
    // BELOK HALUS (dipicu oleh Turn Node, lihat TriggerTurn())
    // Rotasi kendaraan didekatkan sedikit demi sedikit ke targetRotationZ
    // setiap FixedUpdate, bukan langsung snap 90 derajat.
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
        // RedCar cuek total sama raycast, tidak pernah ngerem.
        // Kalau nabrak, itu jadi tanggung jawab script collision terpisah nanti.
        isStoppedByTraffic = false;
    }

    private void HandleBlueCar()
    {
        // PENTING: origin raycast digeser ke tepi depan collider sendiri + padding tambahan
        // (bukan dari transform.position yang berada di TENGAH collider). Kalau tidak digeser,
        // raycast bisa "menabrak" collider dirinya sendiri di jarak ~0 dan mobil biru
        // terkunci berhenti selamanya sejak spawn.
        float forwardOffset = (vehicleCollider != null ? vehicleCollider.bounds.extents.y : 0.1f) + raycastOriginPadding;
        Vector2 origin = (Vector2)transform.position + (Vector2)transform.up * forwardOffset;

        RaycastHit2D hit = Physics2D.Raycast(origin, transform.up, raycastDistance, vehicleLayerMask);

        // Pengaman tambahan: kalau entah kenapa raycast masih kena diri sendiri
        // (misal collider anak/child lain dari objek yang sama), abaikan hasil itu.
        if (hit.collider != null && hit.collider.transform.IsChildOf(transform))
        {
            hit = default;
        }

        // Debug visual di Scene view: merah = ada halangan, hijau = jalur aman
        Debug.DrawRay(origin, transform.up * raycastDistance,
            hit.collider != null ? Color.red : Color.green);

        isStoppedByTraffic = (hit.collider != null && hit.distance <= stoppingDistance);
    }

    private void HandleYellowTruck()
    {
        // Truk kuning tidak peduli raycast sama sekali, cuma melaju pelan terus.
        isStoppedByTraffic = false;
    }

    private void HandleGreenAmbulance()
    {
        // Cari semua kendaraan dalam radius efek ambulance
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

    /// <summary>
    /// Dipanggil dari GreenAmbulance untuk memaksa BlueCar minggir/berhenti.
    /// Public supaya bisa diakses antar-script.
    /// </summary>
    public void ForceStopByAmbulance(bool stop)
    {
        isStoppedByAmbulance = stop;
    }

    // ------------------------------------------------------------
    // DIPANGGIL DARI NodeBehaviour (Turn Node & Stop Node)
    // ------------------------------------------------------------

    /// <summary>
    /// Memicu belokan halus 90 derajat. Kendaraan tetap jalan selama berbelok,
    /// rotasinya didekatkan bertahap tiap FixedUpdate lewat HandleSmoothTurning().
    /// </summary>
    /// <param name="turnLeft">true = belok kiri (+90�), false = belok kanan (-90�)</param>
    public void TriggerTurn(bool turnLeft)
    {
        float delta = turnLeft ? 90f : -90f;
        targetRotationZ = rb.rotation + delta;
        isTurning = true;
    }

    /// <summary>
    /// Memicu berhenti sementara selama "duration" detik, lalu jalan lagi otomatis.
    /// Kalau kendaraan kena Stop Node lagi sebelum timer habis, timer di-reset dari awal.
    /// </summary>
    public void TriggerTemporaryStop(float duration)
    {
        if (stopNodeRoutine != null)
        {
            StopCoroutine(stopNodeRoutine);
        }
        stopNodeRoutine = StartCoroutine(StopNodeRoutine(duration));
    }

    private IEnumerator StopNodeRoutine(float duration)
    {
        isStoppedByNode = true;
        yield return new WaitForSeconds(duration);
        isStoppedByNode = false;
        stopNodeRoutine = null;
    }

    // Gizmo bantu visual radius ambulance di Scene view (tidak muncul saat build)
    void OnDrawGizmosSelected()
    {
        if (vehicleType == VehicleType.GreenAmbulance)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, ambulanceRadius);
        }
    }
}