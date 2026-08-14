using UnityEngine;

/// <summary>
/// Trigger Area di Lantai Lorong untuk MENYALAKAN KEMBALI Lampu-lampu yang sedang mati (1x Eksekusi).
/// Sangat cocok dipasang di ujung lorong / pintu keluar zona agar lampu mati dalam durasi sangat lama (1-2 jam)
/// dan baru menyala kembali saat pemain mencapai titik ini.
/// </summary>
[RequireComponent(typeof(Collider))]
public class LampRestoreTrigger : MonoBehaviour
{
    [Header("Target Lamps to Restore")]
    [Tooltip("Drag 1 atau lebih lampu lorong yang akan dinyalakan kembali saat pemain lewat")]
    [SerializeField] private AmbientLampAnomaly[] targetLamps;

    [Header("Restore Settings")]
    [Tooltip("CENTANG jika ingin ada efek percikan listrik (spark) saat lampu menyala kembali")]
    [SerializeField] private bool withSparkEffect = true;

    [Tooltip("CENTANG agar trigger pemulih ini hanya berfungsi 1x saja saat dilewati")]
    [SerializeField] private bool onlyTriggerOnce = true;

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip powerRestoredSFX; // Suara daya pulih / saklar menyala

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        // Periksa apakah yang melangkah adalah Player
        if (other.CompareTag("Player") || other.GetComponent<CharacterController>() != null || other.name.ToLower().Contains("player"))
        {
            hasTriggered = true;
            Debug.Log($"<color=green>[LAMP RESTORE TRIGGER] Pemain melewati trigger '{gameObject.name}'! Menyalakan seluruh lampu target kembali!</color>");

            // Nyalakan kembali seluruh lampu target secara serentak
            if (targetLamps != null)
            {
                foreach (AmbientLampAnomaly lamp in targetLamps)
                {
                    if (lamp != null)
                    {
                        lamp.RestoreLights(withSparkEffect);
                    }
                }
            }

            if (audioSource != null && powerRestoredSFX != null)
            {
                audioSource.PlayOneShot(powerRestoredSFX);
            }

            // Jika hanya 1x seumur hidup, nonaktifkan collider trigger ini
            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }
}
