using UnityEngine;

/// <summary>
/// Trigger Area di Lantai Lorong untuk memicu Lampu Mati Mendadak (1x Eksekusi).
/// Saat pemain melangkah melewati trigger ini, lampu-lampu lorong di zona tersebut
/// otomatis berkedip dan mati total selama X detik lalu menyala normal kembali.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LampAnomalyTrigger : MonoBehaviour
{
    [Header("Target Lamps")]
    [Tooltip("Drag 1 atau lebih lampu lorong yang akan mati saat pemain lewat")]
    [SerializeField] private AmbientLampAnomaly[] targetLamps;

    [Header("Blackout Settings")]
    [Tooltip("Berapa detik lampu mati sebelum menyala kembali")]
    [SerializeField] private float blackoutDuration = 2.0f;

    [Tooltip("CENTANG agar event horor ini hanya terjadi 1x saja saat dilewati")]
    [SerializeField] private bool onlyTriggerOnce = true;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip spookySound; // Suara hembusan angin / suara tegangan listrik

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        // Periksa apakah yang melangkah adalah Player
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            hasTriggered = true;
            Debug.Log($"<color=yellow>[LAMP ANOMALY TRIGGER] Pemain melewati trigger '{gameObject.name}'! Mematikan lampu selama {blackoutDuration} detik.</color>");

            // Matikan seluruh lampu target secara serentak
            if (targetLamps != null)
            {
                foreach (AmbientLampAnomaly lamp in targetLamps)
                {
                    if (lamp != null)
                    {
                        lamp.TriggerBlackout(blackoutDuration);
                    }
                }
            }

            if (audioSource != null && spookySound != null)
            {
                audioSource.PlayOneShot(spookySound);
            }

            // Jika hanya 1x seumur hidup, nonaktifkan trigger ini
            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}
