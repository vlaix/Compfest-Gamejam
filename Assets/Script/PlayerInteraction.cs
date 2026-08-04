using UnityEngine;

/// <summary>
/// Chaos Conductor - PlayerInteraction
/// Menangani input pemain untuk menempatkan "node" (Turn Left, Turn Right, Stop)
/// menggunakan mekanik hold-and-drag, dibantu efek slow motion saat menahan klik.
///
/// Klik Kiri (tahan lalu geser) -> Turn Node Left / Right, tergantung arah geser
/// Klik Kanan (tahan lalu lepas) -> Stop Node
/// Escape saat menahan klik      -> Batalkan penempatan
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // ------------------------------------------------------------
    // PREFAB NODE
    // ------------------------------------------------------------
    [Header("Prefab Node")]
    public GameObject turnNodeLeftPrefab;
    public GameObject turnNodeRightPrefab;
    public GameObject stopNodePrefab;

    // ------------------------------------------------------------
    // PENGATURAN SLOW MOTION & GRID
    // ------------------------------------------------------------
    [Header("Slow Motion")]
    [Tooltip("Skala waktu saat mode penempatan node aktif (klik ditahan).")]
    [Range(0.05f, 1f)]
    public float slowMotionScale = 0.2f;

    [Header("Grid Snapping")]
    [Tooltip("Ukuran grid untuk membulatkan posisi world saat Instantiate node.")]
    public float gridSize = 1f;

    [Tooltip("Jarak geser minimum (dalam pixel layar) sebelum dianggap 'geser kiri/kanan' yang valid.")]
    public float dragThreshold = 20f;

    [Header("Preview / Ghost Node")]
    [Tooltip("Transparansi (alpha) sprite saat masih berupa preview, sebelum diletakkan permanen.")]
    [Range(0.1f, 1f)]
    public float previewAlpha = 0.4f;

    // ------------------------------------------------------------
    // STATE INTERNAL
    // ------------------------------------------------------------
    private bool isPlacingNode = false;
    private int activeMouseButton = -1; // 0 = klik kiri, 1 = klik kanan, -1 = tidak ada
    private Vector3 initialClickScreenPos;
    private const float NORMAL_TIME_SCALE = 1f;

    // Preview / ghost object yang mengikuti mouse selama klik ditahan
    private GameObject previewInstance;
    private GameObject previewSourcePrefab; // dipakai untuk cek apakah tipe node berubah (misal dari "belum jelas" ke "Turn Left")

    /// <summary>
    /// Dipakai script lain (misal PauseMenuManager) untuk cek apakah pemain sedang
    /// menahan klik / dalam mode slow-mo, supaya input ESC tidak bentrok.
    /// </summary>
    public bool IsPlacingNode => isPlacingNode;

    void Update()
    {
        // --- Batal (Escape) - dicek duluan supaya prioritas di atas input mouse ---
        if (isPlacingNode && Input.GetKeyDown(KeyCode.Escape))
        {
            CancelPlacement();
            return;
        }

        // --- Klik Kiri: Turn Node (Left/Right) ---
        if (Input.GetMouseButtonDown(0))
        {
            StartPlacement(0);
        }
        else if (Input.GetMouseButtonUp(0) && activeMouseButton == 0)
        {
            ConfirmTurnNodePlacement();
        }

        // --- Klik Kanan: Stop Node ---
        if (Input.GetMouseButtonDown(1))
        {
            StartPlacement(1);
        }
        else if (Input.GetMouseButtonUp(1) && activeMouseButton == 1)
        {
            ConfirmStopNodePlacement();
        }

        // --- Update posisi & tipe preview selama mode penempatan aktif ---
        if (isPlacingNode)
        {
            UpdatePreview();
        }
    }

    // ------------------------------------------------------------
    // MULAI PENEMPATAN (klik ditahan)
    // ------------------------------------------------------------
    private void StartPlacement(int mouseButton)
    {
        isPlacingNode = true;
        activeMouseButton = mouseButton;
        initialClickScreenPos = Input.mousePosition;

        ActivateSlowMotion();
    }

    // ------------------------------------------------------------
    // KONFIRMASI: TURN NODE (klik kiri dilepas)
    // ------------------------------------------------------------
    private void ConfirmTurnNodePlacement()
    {
        Vector3 releaseScreenPos = Input.mousePosition;
        float deltaX = releaseScreenPos.x - initialClickScreenPos.x;

        // Hanya proses kalau geseran melewati threshold, biar klik tanpa geser tidak menempatkan apa-apa
        if (Mathf.Abs(deltaX) >= dragThreshold)
        {
            GameObject prefabToSpawn = (deltaX < 0f) ? turnNodeLeftPrefab : turnNodeRightPrefab;
            Vector3 spawnPosition = GetSnappedWorldPosition(releaseScreenPos);

            Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        }

        DestroyPreview();
        EndPlacement();
    }

    // ------------------------------------------------------------
    // KONFIRMASI: STOP NODE (klik kanan dilepas)
    // ------------------------------------------------------------
    private void ConfirmStopNodePlacement()
    {
        Vector3 releaseScreenPos = Input.mousePosition;
        Vector3 spawnPosition = GetSnappedWorldPosition(releaseScreenPos);

        Instantiate(stopNodePrefab, spawnPosition, Quaternion.identity);

        DestroyPreview();
        EndPlacement();
    }

    // ------------------------------------------------------------
    // BATAL PENEMPATAN
    // ------------------------------------------------------------
    private void CancelPlacement()
    {
        // Tidak Instantiate apapun, cuma reset state & waktu
        DestroyPreview();
        EndPlacement();
    }

    // ------------------------------------------------------------
    // AKHIR MODE PENEMPATAN (dipakai baik saat sukses maupun batal)
    // ------------------------------------------------------------
    private void EndPlacement()
    {
        isPlacingNode = false;
        activeMouseButton = -1;
        DeactivateSlowMotion();
    }

    // ------------------------------------------------------------
    // PREVIEW / GHOST NODE
    // Menampilkan node semi-transparan mengikuti mouse selama klik ditahan,
    // supaya pemain tahu node akan jatuh di kotak grid yang mana.
    // ------------------------------------------------------------
    private void UpdatePreview()
    {
        Vector3 snappedPos = GetSnappedWorldPosition(Input.mousePosition);

        // Tentukan prefab mana yang seharusnya di-preview saat ini
        GameObject targetPrefab = null;

        if (activeMouseButton == 1)
        {
            // Klik kanan -> selalu preview Stop Node
            targetPrefab = stopNodePrefab;
        }
        else if (activeMouseButton == 0)
        {
            // Klik kiri -> tergantung arah geser saat ini (bisa berubah-ubah selama drag)
            float deltaX = Input.mousePosition.x - initialClickScreenPos.x;
            if (Mathf.Abs(deltaX) >= dragThreshold)
            {
                targetPrefab = (deltaX < 0f) ? turnNodeLeftPrefab : turnNodeRightPrefab;
            }
            // Kalau belum melewati threshold, targetPrefab tetap null -> belum ada preview yang jelas
        }

        // Kalau tipe node yang harus di-preview berubah (atau baru pertama kali), buat ulang preview-nya
        if (targetPrefab != previewSourcePrefab)
        {
            DestroyPreview();
            previewSourcePrefab = targetPrefab;

            if (targetPrefab != null)
            {
                previewInstance = Instantiate(targetPrefab, snappedPos, Quaternion.identity);
                SetupPreviewVisual(previewInstance);
            }
        }
        else if (previewInstance != null)
        {
            // Tipe sama, cukup update posisinya saja
            previewInstance.transform.position = snappedPos;
        }
    }

    private void SetupPreviewVisual(GameObject preview)
    {
        // Buat semi-transparan supaya kelihatan beda dari node yang sudah permanen
        SpriteRenderer sr = preview.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            Color c = sr.color;
            c.a = previewAlpha;
            sr.color = c;
        }

        // Matikan collider supaya preview tidak ikut ke-detect oleh kendaraan/sistem lain
        Collider2D col = preview.GetComponentInChildren<Collider2D>();
        if (col != null)
        {
            col.enabled = false;
        }
    }

    private void DestroyPreview()
    {
        if (previewInstance != null)
        {
            Destroy(previewInstance);
        }
        previewInstance = null;
        previewSourcePrefab = null;
    }

    // ------------------------------------------------------------
    // SLOW MOTION HELPER
    // ------------------------------------------------------------
    private void ActivateSlowMotion()
    {
        Time.timeScale = slowMotionScale;
        // fixedDeltaTime ikut diskalakan supaya physics (Rigidbody2D) tetap konsisten
        // dan tidak terasa "patah-patah" saat slow motion aktif.
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
    }

    private void DeactivateSlowMotion()
    {
        Time.timeScale = NORMAL_TIME_SCALE;
        Time.fixedDeltaTime = 0.02f * NORMAL_TIME_SCALE;
    }

    // ------------------------------------------------------------
    // KONVERSI POSISI LAYAR -> WORLD, DIBULATKAN KE TENGAH KOTAK/CELL
    // ------------------------------------------------------------
    private Vector3 GetSnappedWorldPosition(Vector3 screenPos)
    {
        // Asumsi kamera top-down orthographic bernama "MainCamera"
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(screenPos);
        worldPos.z = 0f; // pastikan tetap di plane 2D

        // PENTING: pakai Mathf.Floor + setengah gridSize supaya hasilnya jatuh
        // ke TENGAH kotak (misal cell tilemap 32x32), bukan ke garis/sudut grid.
        // Kalau dulu pakai Mathf.Round, posisi akan nempel ke perpotongan garis.
        float snappedX = Mathf.Floor(worldPos.x / gridSize) * gridSize + (gridSize * 0.5f);
        float snappedY = Mathf.Floor(worldPos.y / gridSize) * gridSize + (gridSize * 0.5f);

        return new Vector3(snappedX, snappedY, 0f);
    }

    // Jaga-jaga: kalau object ini di-disable/destroy saat slow motion aktif,
    // pastikan Time.timeScale tidak nyangkut di 0.2 selamanya.
    void OnDisable()
    {
        if (isPlacingNode)
        {
            DeactivateSlowMotion();
        }
        DestroyPreview();
    }
}