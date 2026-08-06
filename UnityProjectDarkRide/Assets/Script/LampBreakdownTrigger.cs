using UnityEngine;

/// <summary>
/// Trigger Zone yang bisa merusakkan BANYAK LAMPU sekaligus dalam 1 zona saat player lewat.
/// </summary>
public class LampBreakdownTrigger : MonoBehaviour
{
    [Header("Target Lamp Fixtures (Bisa Banyak Lampu)")]
    [Tooltip("Drag 1 atau lebih Objek Lampu yang ingin dirusakkan di zona ini saat player lewat.")]
    [SerializeField] private LampBulbFixture[] targetLamps;

    [Header("Audio & Visual FX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sparkSoundEffect; 
    [SerializeField] private ParticleSystem sparkParticles; 

    [Header("Trigger Settings")]
    [SerializeField] private bool triggerOnlyOnce = true;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && triggerOnlyOnce) return;

        // Deteksi Objek Player
        bool isPlayer = other.CompareTag("Player") || 
                         other.GetComponent<PlayerMovement>() != null || 
                         other.GetComponentInParent<PlayerMovement>() != null ||
                         other.GetComponent<PlayerInteraction>() != null;

        if (isPlayer)
        {
            hasTriggered = true;
            Debug.Log("<color=yellow>[TRIGGER SUCCESS] Player melintasi area trigger lampu!</color>");

            // 1. Merusakkan SELURUH Lampu di dalam Daftar (Array)
            if (targetLamps != null && targetLamps.Length > 0)
            {
                foreach (var lamp in targetLamps)
                {
                    if (lamp != null)
                    {
                        lamp.SetBrokenState(true);
                        Debug.Log($"[LAMP BROKEN] Lampu '{lamp.gameObject.name}' di zona ini sekarang BERKEDIP/RUSAK!");
                    }
                }
            }
            else
            {
                Debug.LogError("[TRIGGER ERROR] Daftar 'Target Lamps' di Inspector masih KOSONG! Harap masukkan minimal 1 lampu.");
            }

            // 2. Play Sound FX
            if (audioSource != null && sparkSoundEffect != null)
            {
                audioSource.PlayOneShot(sparkSoundEffect);
            }

            // 3. Play Spark Particles
            if (sparkParticles != null)
            {
                sparkParticles.Play();
            }
        }
    }
}