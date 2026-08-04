using UnityEngine;

/// <summary>
/// Chaos Conductor - NodeBehaviour
/// Ditempel ke prefab node (Turn Left, Turn Right, Stop).
/// Mendeteksi kendaraan yang lewat lewat trigger, lalu memicu
/// reaksi yang sesuai di VehicleController milik kendaraan itu.
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

    private void OnTriggerEnter2D(Collider2D other)
    {
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
    }
}