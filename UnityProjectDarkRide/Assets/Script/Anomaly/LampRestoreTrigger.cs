using UnityEngine;

/// <summary>
/// Trigger Area di Lantai Lorong untuk MENYALAKAN KEMBALI Lampu Ruangan yang sedang mengalami anomali.
/// Cukup drag 1 atau lebih objek Ruangan target.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LampRestoreTrigger : MonoBehaviour
{
    [Header("Target Rooms to Restore")]
    [Tooltip("Drag 1 atau lebih Parent Ruangan yang akan dipulihkan lampunya saat pemain lewat")]
    [SerializeField] private GameObject[] targetRooms;

    [Header("Restore Settings")]
    [Tooltip("CENTANG jika ingin ada efek percikan kedip listrik saat lampu dinyalakan kembali")]
    [SerializeField] private bool withSparkEffect = true;

    [Tooltip("CENTANG agar trigger pemulih ini hanya berfungsi 1x saja saat dilewati")]
    [SerializeField] private bool onlyTriggerOnce = true;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip powerRestoredSFX;

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            hasTriggered = true;

            if (targetRooms != null)
            {
                foreach (GameObject room in targetRooms)
                {
                    if (room == null) continue;

                    // 1. Pulihkan via AmbientLampAnomaly jika ada
                    AmbientLampAnomaly anomaly = room.GetComponent<AmbientLampAnomaly>();
                    if (anomaly != null)
                    {
                        anomaly.RestoreAllLights(withSparkEffect);
                    }
                    else
                    {
                        // Fallback jika tidak ada script anomali
                        Light[] lights = room.GetComponentsInChildren<Light>(true);
                        foreach (Light l in lights)
                        {
                            if (l != null) l.enabled = true;
                        }
                    }
                }
            }

            if (audioSource != null && powerRestoredSFX != null)
            {
                audioSource.PlayOneShot(powerRestoredSFX);
            }

            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}
