using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Pengelola Efek Transisi Layar Hitam (Screen Fade Black).
/// Auto-Setup Fullscreen & Auto-Sort Last Sibling.
/// Teks Death Reason & Durasi Tahan Layar Hitam (Hold Black Duration) BISA DIATUR BERSIH DI INSPECTOR!
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;
    public static ScreenFader Instance => _instance;

    [Header("UI Canvas References")]
    [SerializeField] private CanvasGroup faderCanvasGroup;
    [SerializeField] private Image faderImage;
    [SerializeField] private TextMeshProUGUI fadeReasonText;

    [Header("Fade Duration Settings (Atur di Inspector)")]
    [Tooltip("Kecepatan transisi dari/ke layar hitam dalam detik (Default: 0.8s)")]
    [SerializeField] private float defaultFadeDuration = 0.8f;

    [Tooltip("Durasi berapa lama layar TAHAN PEKAT HITAM di tengah-tengah transisi saat teks Death Reason tampil (Default: 2.0s)")]
    [SerializeField] private float defaultHoldBlackDuration = 2.0f;

    private Coroutine activeFadeRoutine;

    // Public Property untuk mengecek apakah layar sedang dalam kondisi Hitam / Fade
    public bool IsFadingOrBlack => (faderCanvasGroup != null && faderCanvasGroup.alpha > 0.05f);

    private void Awake()
    {
        if (_instance == null) _instance = this;
        AutoSetupCanvasGroup();
    }

    private void Start()
    {
        transform.SetAsLastSibling();
        EnsureTextOnTop();
    }

    private void EnsureTextOnTop()
    {
        if (fadeReasonText != null)
        {
            fadeReasonText.transform.SetAsLastSibling();
        }
    }

    private void AutoSetupCanvasGroup()
    {
        if (faderCanvasGroup == null)
        {
            faderCanvasGroup = GetComponent<CanvasGroup>();
            if (faderCanvasGroup == null) faderCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        RectTransform parentRt = GetComponent<RectTransform>();
        if (parentRt != null)
        {
            parentRt.anchorMin = Vector2.zero;
            parentRt.anchorMax = Vector2.one;
            parentRt.offsetMin = Vector2.zero;
            parentRt.offsetMax = Vector2.one;
        }

        if (faderImage == null)
        {
            faderImage = GetComponentInChildren<Image>(true);
            if (faderImage == null)
            {
                GameObject imgObj = new GameObject("FadeImage");
                imgObj.transform.SetParent(transform, false);
                faderImage = imgObj.AddComponent<Image>();
            }
        }

        if (faderImage != null)
        {
            faderImage.color = Color.black;
            faderImage.raycastTarget = false;

            RectTransform rt = faderImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.one;
        }

        if (fadeReasonText == null)
        {
            fadeReasonText = GetComponentInChildren<TextMeshProUGUI>(true);
            if (fadeReasonText == null)
            {
                GameObject txtObj = new GameObject("FadeReasonText");
                txtObj.transform.SetParent(transform, false);
                fadeReasonText = txtObj.AddComponent<TextMeshProUGUI>();
            }
        }

        if (fadeReasonText != null)
        {
            fadeReasonText.transform.SetAsLastSibling();
            fadeReasonText.alignment = TextAlignmentOptions.Center;

            RectTransform txtRt = fadeReasonText.rectTransform;
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.pivot = new Vector2(0.5f, 0.5f);
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;
        }

        faderCanvasGroup.blocksRaycasts = false;
        faderCanvasGroup.alpha = 0f;
    }

    /// <summary>
    /// Transisi Layar Menjadi Hitam Pekat (Fade to Black)
    /// </summary>
    public void FadeToBlack(float duration = -1f, System.Action onComplete = null)
    {
        transform.SetAsLastSibling();
        EnsureTextOnTop();
        float targetDuration = duration > 0f ? duration : defaultFadeDuration;
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeRoutine(1f, targetDuration, onComplete));
    }

    /// <summary>
    /// Transisi Layar Kembali Terang (Fade from Black)
    /// </summary>
    public void FadeFromBlack(float duration = -1f, System.Action onComplete = null)
    {
        transform.SetAsLastSibling();
        EnsureTextOnTop();
        float targetDuration = duration > 0f ? duration : defaultFadeDuration;
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeRoutine(0f, targetDuration, onComplete));
    }

    /// <summary>
    /// Transisi Layar Menjadi Hitam Pekat dengan Durasi Tahan Layar Hitam Dari Inspector
    /// </summary>
    public void FadeToBlackAndExecute(System.Action onBlackScreen, string deathReason = "", float fadeOutSpeed = -1f, float fadeInSpeed = -1f, float holdBlackTime = -1f)
    {
        transform.SetAsLastSibling();
        EnsureTextOnTop();

        float targetFadeOut = fadeOutSpeed > 0f ? fadeOutSpeed : defaultFadeDuration;
        float targetFadeIn = fadeInSpeed > 0f ? fadeInSpeed : defaultFadeDuration;
        float targetHold = holdBlackTime > 0f ? holdBlackTime : defaultHoldBlackDuration;

        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FullSequenceRoutine(onBlackScreen, deathReason, targetFadeOut, targetFadeIn, targetHold));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, System.Action onComplete)
    {
        if (faderCanvasGroup == null) yield break;

        faderCanvasGroup.blocksRaycasts = targetAlpha > 0.1f;
        float startAlpha = faderCanvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            faderCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, timer / duration);
            yield return null;
        }

        faderCanvasGroup.alpha = targetAlpha;
        faderCanvasGroup.blocksRaycasts = targetAlpha > 0.1f;

        onComplete?.Invoke();
        activeFadeRoutine = null;
    }

    private IEnumerator FullSequenceRoutine(System.Action onBlackScreen, string deathReason, float fadeOutSpeed, float fadeInSpeed, float holdBlackTime)
    {
        transform.SetAsLastSibling();
        EnsureTextOnTop();

        if (fadeReasonText != null)
        {
            fadeReasonText.text = "";
        }

        // 1. Fade Out ke Layar Hitam (Teks KOSONG)
        yield return FadeRoutine(1f, fadeOutSpeed, null);

        // 2. 🎯 SAAT LAYAR SUDAH BENAR-BENAR 100% HITAM PEKAT MURNI: TAMPILKAN TEKS DEATH REASON
        if (fadeReasonText != null && !string.IsNullOrEmpty(deathReason))
        {
            fadeReasonText.alignment = TextAlignmentOptions.Center;
            fadeReasonText.text = $"<color=red><b>{deathReason}</b></color>";
        }

        if (holdBlackTime > 0f)
        {
            yield return new WaitForSeconds(holdBlackTime);
        }

        onBlackScreen?.Invoke();

        // 3. HAPUS TEKS KEMBALI KOSONG SEBELUM FADE IN
        if (fadeReasonText != null)
        {
            fadeReasonText.text = "";
        }

        // 4. Fade In Kembali Terang (Teks KOSONG)
        yield return FadeRoutine(0f, fadeInSpeed, null);
    }
}
