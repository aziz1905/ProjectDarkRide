using System.Collections;
using UnityEngine;

/// <summary>
/// Anomali: MuseumLaughterAnomaly (Suara Tertawa Misterius di Area Museum).
/// Fitur:
/// 1. Trigger saat Player atau Cart memasuki area tertentu di Museum.
/// 2. Spatial 3D Audio: Suara tawa terdengar berasal dari titik spesifik (patung/lukisan/sudut lorong).
/// 3. Jeda delay alami (opsional) sebelum suara mulai berbisik/tertawa agar tidak kaku.
/// 4. Variasi Pitch & Volume halus agar terdengar organik.
/// 5. Sekali picu (Trigger Once) untuk menjaga suasana ketegangan.
/// </summary>
[RequireComponent(typeof(Collider))]
public class MuseumLaughterAnomaly : MonoBehaviour
{
    [Header("Audio Settings")]
    [Tooltip("AudioSource untuk memutar suara. Jika kosong, script otomatis mencari di objek ini atau soundOrigin")]
    [SerializeField] private AudioSource audioSource;

    [Tooltip("Koleksi klip suara tertawa (bisa dimasukkan 1 atau lebih untuk variasi acak)")]
    [SerializeField] private AudioClip[] laughClips;

    [Tooltip("Titik sumber suara 3D (misal: ditaruh di patung atau lukisan). Jika kosong, memakai posisi objek ini")]
    [SerializeField] private Transform soundOrigin;

    [Header("Timing & Playback")]
    [Tooltip("Jeda waktu sebelum suara tawa mulai berbunyi setelah trigger diinjak (detik)")]
    [SerializeField] private float delayBeforeLaugh = 0.8f;

    [Tooltip("Volume suara (0.0 sampai 1.0)")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.85f;

    [Tooltip("Variasi pitch acak halus agar tidak monoton (Min, Max)")]
    [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

    [Header("Trigger Conditions")]
    [Tooltip("CENTANG jika anomali tawa ini hanya terjadi 1x")]
    [SerializeField] private bool onlyTriggerOnce = true;

    private bool hasTriggered = false;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;

        SetupAudioSource();
    }

    private void SetupAudioSource()
    {
        if (audioSource == null)
        {
            if (soundOrigin != null)
            {
                audioSource = soundOrigin.GetComponent<AudioSource>();
                if (audioSource == null) audioSource = soundOrigin.gameObject.AddComponent<AudioSource>();
            }
            else
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
            }
        }

        // Pastikan AudioSource diset ke Full 3D Sound
        audioSource.spatialBlend = 1.0f; // 100% 3D Audio
        audioSource.playOnAwake = false;
        audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        audioSource.minDistance = 2.0f;
        audioSource.maxDistance = 25.0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce) return;

        bool isPlayerOrCart = other.CompareTag("Player")
            || other.GetComponentInParent<CharacterController>() != null
            || other.name.ToLower().Contains("player")
            || other.transform.root.name.ToLower().Contains("player")
            || other.name.ToLower().Contains("cart")
            || other.name.ToLower().Contains("camera");

        if (isPlayerOrCart)
        {
            hasTriggered = true;
            StartCoroutine(PlayLaughterRoutine());

            if (onlyTriggerOnce)
            {
                Collider col = GetComponent<Collider>();
                if (col != null) col.enabled = false;
            }
        }
    }

    private IEnumerator PlayLaughterRoutine()
    {
        if (delayBeforeLaugh > 0f)
        {
            yield return new WaitForSeconds(delayBeforeLaugh);
        }

        if (audioSource != null && laughClips != null && laughClips.Length > 0)
        {
            // Pilih klip tawa secara acak jika ada lebih dari satu
            AudioClip clip = laughClips[Random.Range(0, laughClips.Length)];
            if (clip != null)
            {
                audioSource.pitch = Random.Range(pitchRange.x, pitchRange.y);
                audioSource.volume = volume;
                audioSource.clip = clip;
                audioSource.Play();
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Gambar batas collider trigger
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.35f);
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }

        // Gambar garis ke titik sumber suara jika berbeda tempat
        if (soundOrigin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(transform.position, soundOrigin.position);
            Gizmos.DrawWireSphere(soundOrigin.position, 0.5f);
        }
    }
}
