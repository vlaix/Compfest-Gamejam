using UnityEngine;

/// <summary>
/// Chaos Conductor - NodeBehaviour
/// Ditempel ke prefab node (Turn Left, Turn Right, Stop).
/// Mendeteksi kendaraan yang lewat lewat trigger, lalu memicu
/// reaksi yang sesuai di VehicleController milik kendaraan itu.
///
/// PENTING (setup prefab):
/// - Collider2D pada prefab node WAJIB dicentang "Is Trigger"
/// - Set field "Node Type" di Inspector sesuai jenis prefab:
///     turnNodeLeftPrefab  -> TurnLeft
///     turnNodeRightPrefab -> TurnRight
///     stopNodePrefab      -> Stop
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class NodeBehaviour : MonoBehaviour
{
    public enum NodeType
    {
        TurnLeft,
        TurnRight,
        Stop
    }

    [Header("Tipe Node")]
    public NodeType nodeType;

    [Header("Pengaturan Stop Node")]
    [Tooltip("Hanya dipakai kalau Node Type = Stop. Lama kendaraan berhenti sebelum jalan lagi otomatis.")]
    public float stopDuration = 3f;

    [Header("Sekali Pakai")]
    [Tooltip("Kalau dicentang, node akan hilang (Destroy) setelah dipakai satu kali oleh kendaraan.")]
    public bool destroyAfterUse = true;

    // Pengaman: mencegah node kepakai 2x dalam frame yang sama sebelum Destroy() benar-benar berjalan
    // (Destroy di Unity baru efektif di akhir frame, bukan seketika).
    private bool hasBeenUsed = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasBeenUsed) return; // sudah terpakai, abaikan trigger tambahan

        VehicleController vehicle = other.GetComponent<VehicleController>();
        if (vehicle == null) return; // bukan kendaraan, abaikan

        switch (nodeType)
        {
            case NodeType.TurnLeft:
                vehicle.TriggerTurn(true);
                break;

            case NodeType.TurnRight:
                vehicle.TriggerTurn(false);
                break;

            case NodeType.Stop:
                vehicle.TriggerTemporaryStop(stopDuration);
                break;
        }

        if (destroyAfterUse)
        {
            hasBeenUsed = true;
            Destroy(gameObject);
        }
    }
}