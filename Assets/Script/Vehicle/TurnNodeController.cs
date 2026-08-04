using UnityEngine;
using UnityEngine.InputSystem; 

public class TurnNodeController : MonoBehaviour
{
    [Header("Pengaturan Klik")]
    public float clickRadius = 1.0f; 

    [Header("Pengaturan Slow Motion")]
    public float slowMotionScale = 0.2f;
    private float defaultFixedDeltaTime = 0.02f;

    // STATUS MODE BERHENTI
    public bool isStopMode = false;
    private SpriteRenderer spriteRenderer;
    private Color originalColor;

    private bool isBeingDragged = false;
    private Quaternion initialRotation;

    void Start()
    {
        initialRotation = transform.rotation;
        
        // Simpan warna asli agar bisa dikembalikan nanti
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
        }
    }

    void Update()
    {
        if (Mouse.current == null) return;

        Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(mouseScreenPosition);
        float distance = Vector2.Distance(mousePos, transform.position);

        // 1. KLIK KIRI (Drag & Slow Motion)
        if (Mouse.current.leftButton.wasPressedThisFrame && distance <= clickRadius)
        {
            isBeingDragged = true;
            Time.timeScale = slowMotionScale;
            Time.fixedDeltaTime = defaultFixedDeltaTime * Time.timeScale;
        }

        if (Mouse.current.leftButton.isPressed && isBeingDragged)
        {
            Vector2 direction = mousePos - (Vector2)transform.position;
            
            if (direction != Vector2.zero)
            {
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle - 90f, Vector3.forward);
            }
        }

        if (Mouse.current.leftButton.wasReleasedThisFrame && isBeingDragged)
        {
            isBeingDragged = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = defaultFixedDeltaTime;

            float snappedAngle = Mathf.Round(transform.eulerAngles.z / 90f) * 90f;
            transform.rotation = Quaternion.Euler(0, 0, snappedAngle);
        }

        // 2. KLIK KANAN (Toggle Mode Berhenti)
        if (Mouse.current.rightButton.wasPressedThisFrame && distance <= clickRadius)
        {
            isStopMode = !isStopMode; // Tukar status (true jadi false, false jadi true)
            
            if (spriteRenderer != null)
            {
                // Ubah ke warna merah jika aktif, kembalikan ke warna asli jika dimatikan
                spriteRenderer.color = isStopMode ? Color.red : originalColor;
            }
        }
    }

    // Dipanggil oleh mobil setelah menabrak
    public void ResetStopMode()
    {
        isStopMode = false;
        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void ResetToInitialRotation()
    {
        transform.rotation = initialRotation;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, clickRadius);
    }
}