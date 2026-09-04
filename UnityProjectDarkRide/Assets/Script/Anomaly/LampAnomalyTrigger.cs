using UnityEngine;

/// <summary>
/// Trigger Lantai Lorong untuk Memicu Anomali pada Ruangan.
/// 100% Bersih & 0% Spam Console Log.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LampAnomalyTrigger : MonoBehaviour
{
    [Header("Target Rooms to Trigger (Drag Objek Ruangan ke Sini)")]
    [Tooltip("Drag 1 atau lebih Objek Ruangan (misal: 'ruangan 1', 'ruangan 2')")]
    [SerializeField] private GameObject[] targetRooms;

    [Header("Trigger Settings")]
    [Tooltip("Berapa detik efek anomali (kedip/mati) berlangsung saat trigger ini diinjak")]
    [SerializeField] private float duration = 3.5f;

    [Tooltip("CENTANG agar trigger ini hanya terjadi 1x saat pertama kali diinjak")]
    [SerializeField] private bool onlyTriggerOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            rb = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        bool isPlayer = other.CompareTag("Player") 
            || other.GetComponentInParent<CharacterController>() != null 
            || other.transform.root.CompareTag("Player")
            || other.name.ToLower().Contains("player")
            || other.transform.root.name.ToLower().Contains("player")
            || other.name.ToLower().Contains("cart")
            || other.name.ToLower().Contains("camera");

        if (isPlayer)
        {
            hasTriggered = true;
            ExecuteTriggerOnRooms();

            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    public void ExecuteTriggerOnRooms()
    {
        if (targetRooms == null || targetRooms.Length == 0) return;

        foreach (GameObject room in targetRooms)
        {
            if (room == null) continue;

            AmbientLampAnomaly anomaly = room.GetComponent<AmbientLampAnomaly>();
            if (anomaly == null) anomaly = room.GetComponentInChildren<AmbientLampAnomaly>();

            if (anomaly != null)
            {
                anomaly.TriggerAnomaly(duration);
            }
        }
    }
}
