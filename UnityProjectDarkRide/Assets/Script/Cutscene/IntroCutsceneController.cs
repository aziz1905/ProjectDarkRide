using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controller untuk Intro Cutscene narasi cerita di atas layar hitam pekat.
/// Dilengkapi efek typewriter per huruf, pengaturan durasi di Inspector,
/// Title Card di akhir narasi, tombol skip/percepat, dan auto-lock kontrol player via GameInputLock.
/// </summary>
public class IntroCutsceneController : MonoBehaviour
{
    [Header("=== DAFTAR KALIMAT CERITA (INSPECTOR) ===")]
    [Tooltip("Daftar kalimat cerita yang akan ditampilkan satu per satu dengan efek mesin tik.")]
    [TextArea(3, 10)]
    [SerializeField]
    private List<string> storyLines = new List<string>()
    {
        "Ada sebuah tempat wahana tua yang sudah lama di tinggal di tengah hutan(dirumorkan tempat itu di tinggalkan karena banyak sekali kejadian bahkan di bilang mistis),",
        "kemudian dibeli oleh pengusaha yang ingin membuat tempat ini dihidupkan Kembali dengan vibes yang sama seperti yang dulu.",
        "Tetapi sangat disayangkan setelah di beli dan ingin di operasikan  Kembali banyak komponen yang susah di cari sehingga membiarkan yang ada.",
        "Dikarenakan kondisinya seperti ini banyak sekali kejadian yang membuat wahan selalu di maintenance setiap malam.",
        "dan bangunan memerlukan staff yang mengcheck bangunan setiap malam..."
    };

    [Header("=== REFERENSI UI CANVAS ===")]
    [Tooltip("CanvasGroup utama dari seluruh layar intro (digunakan untuk fade out akhir ke gameplay).")]
    [SerializeField] private CanvasGroup mainCanvasGroup;

    [Tooltip("Komponen TextMeshPro tempat teks narasi cerita ditampilkan.")]
    [SerializeField] private TextMeshProUGUI storyText;

    [Tooltip("CanvasGroup khusus teks cerita (untuk fade in/out antar kalimat secara halus).")]
    [SerializeField] private CanvasGroup textCanvasGroup;

    [Tooltip("Objek Title Card (judul game / logo) yang akan muncul setelah semua kalimat narasi selesai.")]
    [SerializeField] private GameObject titleCardObject;

    [Tooltip("CanvasGroup untuk Title Card jika ingin efek fade in/out judul game.")]
    [SerializeField] private CanvasGroup titleCardCanvasGroup;

    [Tooltip("Petunjuk tombol di pojok layar (contoh: 'Tekan SPACE untuk lanjut / percepat'). Opsional.")]
    [SerializeField] private TextMeshProUGUI skipHintText;

    [Header("=== PENGATURAN WAKTU / TIMING ===")]
    [Tooltip("Jeda waktu antar karakter saat mengetik dalam detik (Default: 0.04s).")]
    [SerializeField] private float typingSpeed = 0.04f;

    [Tooltip("Jeda ekstra saat menemukan tanda baca titik, koma, atau tanda seru/tanya (Default: 0.15s).")]
    [SerializeField] private float punctuationPause = 0.15f;

    [Tooltip("Berapa lama teks diam terbaca setelah selesai diketik sebelum berganti ke kalimat berikutnya (Default: 2.5s).")]
    [SerializeField] private float sentenceDisplayDuration = 2.5f;

    [Tooltip("Kecepatan fade in dan fade out antar kalimat cerita (Default: 0.4s).")]
    [SerializeField] private float sentenceFadeDuration = 0.4f;

    [Tooltip("Jeda layar hitam kosong setelah cerita selesai sebelum Judul muncul. Dibuat singkat agar tidak menunggu lama (Default: 0.2s).")]
    [SerializeField] private float delayBeforeTitleCard = 0.2f;

    [Tooltip("Kecepatan munculnya Judul Game (Default: 0.4s).")]
    [SerializeField] private float titleFadeInDuration = 0.4f;

    [Tooltip("Durasi Title Card tampil di layar sebelum game dimulai (Default: 3.0s).")]
    [SerializeField] private float titleCardDuration = 3.0f;

    [Tooltip("Kecepatan transisi layar hitam membuka ruangan game di akhir cutscene (Default: 1.2s).")]
    [SerializeField] private float finalFadeOutDuration = 1.2f;

    [Header("=== AUDIO (OPSIONAL) ===")]
    [SerializeField] private AudioSource audioSource;
    [Tooltip("Suara ketukan tik mesin tik setiap huruf muncul.")]
    [SerializeField] private AudioClip typewriterSFX;
    [Tooltip("Suara gong / sting seram saat Title Card muncul.")]
    [SerializeField] private AudioClip titleStingSFX;

    [Header("=== OPSI LAINNYA ===")]
    [Tooltip("Hancurkan GameObject Canvas ini setelah cutscene selesai agar menghemat memori.")]
    [SerializeField] private bool destroyOnComplete = true;

    // State internal
    private bool isCutsceneRunning = false;
    private bool skipRequested = false;
    private bool hasLockedPlayer = false;
    private Coroutine cutsceneCoroutine;

    private void Awake()
    {
        // Pastikan CanvasGroup utama ada dan menutupi layar sejak frame pertama
        if (mainCanvasGroup == null)
        {
            mainCanvasGroup = GetComponent<CanvasGroup>();
            if (mainCanvasGroup == null)
            {
                mainCanvasGroup = gameObject.AddComponent<CanvasGroup>();
            }
        }

        mainCanvasGroup.alpha = 1f;
        mainCanvasGroup.blocksRaycasts = true;

        if (titleCardObject != null)
        {
            titleCardObject.SetActive(false);
        }

        if (storyText != null)
        {
            storyText.text = "";
        }
    }

    private void Start()
    {
        // Kunci input WASD & Kamera player agar tidak bisa bergerak selama intro
        LockPlayerInput();

        // Mulai alur cutscene
        cutsceneCoroutine = StartCoroutine(PlayIntroCutsceneRoutine());
    }

    private void Update()
    {
        if (!isCutsceneRunning) return;

        // Pemain menekan Spasi, Enter, atau Klik Kiri untuk mempercepat / skip kalimat
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetMouseButtonDown(0))
        {
            skipRequested = true;
        }
    }

    private void LockPlayerInput()
    {
        if (!hasLockedPlayer)
        {
            GameInputLock.LockInput();
            hasLockedPlayer = true;
        }
    }

    private void UnlockPlayerInput()
    {
        if (hasLockedPlayer)
        {
            GameInputLock.UnlockInput();
            hasLockedPlayer = false;
        }
    }

    private IEnumerator PlayIntroCutsceneRoutine()
    {
        isCutsceneRunning = true;

        // Berikan sedikit jeda hening di awal (0.5 detik)
        yield return new WaitForSeconds(0.5f);

        // 1. Loop setiap kalimat cerita
        for (int i = 0; i < storyLines.Count; i++)
        {
            string currentLine = storyLines[i];
            if (string.IsNullOrWhiteSpace(currentLine)) continue;

            // Reset teks dan siapkan alpha
            if (storyText != null) storyText.text = "";
            if (textCanvasGroup != null) textCanvasGroup.alpha = 1f;

            // Efek Typewriter
            skipRequested = false;

            for (int charIndex = 0; charIndex < currentLine.Length; charIndex++)
            {
                // Jika pemain menekan skip saat sedang ngetik, langsung tamatkan kalimat seketika
                if (skipRequested)
                {
                    if (storyText != null) storyText.text = currentLine;
                    skipRequested = false;
                    break;
                }

                if (storyText != null)
                {
                    storyText.text += currentLine[charIndex];
                }

                // Mainkan SFX ketik jika ada
                if (audioSource != null && typewriterSFX != null && !char.IsWhiteSpace(currentLine[charIndex]))
                {
                    audioSource.pitch = Random.Range(0.95f, 1.05f);
                    audioSource.PlayOneShot(typewriterSFX, 0.6f);
                }

                char currentChar = currentLine[charIndex];
                if (currentChar == '.' || currentChar == ',' || currentChar == '?' || currentChar == '!')
                {
                    yield return new WaitForSeconds(punctuationPause);
                }
                else
                {
                    yield return new WaitForSeconds(typingSpeed);
                }
            }

            skipRequested = false;

            // 2. Tahan kalimat di layar agar terbaca oleh pemain
            float timer = 0f;
            while (timer < sentenceDisplayDuration)
            {
                if (skipRequested)
                {
                    skipRequested = false;
                    break;
                }
                timer += Time.deltaTime;
                yield return null;
            }

            // 3. Fade Out kalimat
            if (textCanvasGroup != null)
            {
                yield return FadeCanvasGroup(textCanvasGroup, 1f, 0f, sentenceFadeDuration);
            }
        }

        // Kosongkan teks cerita
        if (storyText != null) storyText.text = "";

        // Jeda singkat layar hitam sebelum Judul muncul (bisa diatur di Inspector, default 0.2 detik)
        if (delayBeforeTitleCard > 0f)
        {
            float waitTimer = 0f;
            while (waitTimer < delayBeforeTitleCard)
            {
                if (skipRequested)
                {
                    skipRequested = false;
                    break;
                }
                waitTimer += Time.deltaTime;
                yield return null;
            }
        }

        // Sembunyikan hint skip jika ada
        if (skipHintText != null) skipHintText.gameObject.SetActive(false);

        // 4. TAMPILKAN TITLE CARD (JIKA ADA)
        if (titleCardObject != null)
        {
            titleCardObject.SetActive(true);

            if (audioSource != null && titleStingSFX != null)
            {
                audioSource.PlayOneShot(titleStingSFX, 0.8f);
            }

            if (titleCardCanvasGroup != null)
            {
                yield return FadeCanvasGroup(titleCardCanvasGroup, 0f, 1f, titleFadeInDuration);
            }

            // Tahan Title Card di layar
            float titleTimer = 0f;
            skipRequested = false;
            while (titleTimer < titleCardDuration)
            {
                if (skipRequested)
                {
                    skipRequested = false;
                    break;
                }
                titleTimer += Time.deltaTime;
                yield return null;
            }

            if (titleCardCanvasGroup != null)
            {
                yield return FadeCanvasGroup(titleCardCanvasGroup, 1f, 0f, 0.6f);
            }

            titleCardObject.SetActive(false);
        }

        // 5. TRANSISI KE GAMEPLAY (FADE OUT LAYAR HITAM)
        if (mainCanvasGroup != null)
        {
            yield return FadeCanvasGroup(mainCanvasGroup, 1f, 0f, finalFadeOutDuration);
        }

        // 6. BUKA KONTROL PEMAIN (PLAYER INPUT UNLOCK)
        UnlockPlayerInput();
        isCutsceneRunning = false;

        // 7. BERSIHKAN / NONAKTIFKAN INTRO CANVAS
        if (destroyOnComplete)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        if (cg == null || duration <= 0f) yield break;

        float elapsed = 0f;
        cg.alpha = from;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }

        cg.alpha = to;
    }

    private void OnDestroy()
    {
        // Pengaman: Jika objek hancur mendadak, pastikan kontrol pemain tidak terkunci selamanya
        UnlockPlayerInput();
    }
}
