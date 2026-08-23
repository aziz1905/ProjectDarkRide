using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pengelola Efek Transisi Layar Hitam (Screen Fade Black).
/// Auto-Setup Fullscreen: Otomatis meretangkan RectTransform menjadi 100% Fullscreen tanpa kotak kecil.
/// 0% CPU Overhead & Smooth Lerp CanvasGroup.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;
    public static ScreenFader Instance => _instance;

    [Header("UI Canvas References")]
    [SerializeField] private CanvasGroup faderCanvasGroup;
    [SerializeField] private Image faderImage;

    [Header("Default Fade Speed")]
    [SerializeField] private float defaultFadeDuration = 1.2f;

    private Coroutine activeFadeRoutine;

    // Public Property untuk mengecek apakah layar sedang dalam kondisi Hitam / Fade
    public bool IsFadingOrBlack => (faderCanvasGroup != null && faderCanvasGroup.alpha > 0.05f);

    private void Awake()
    {
        if (_instance == null) _instance = this;

        AutoSetupCanvasGroup();
    }

    private void AutoSetupCanvasGroup()
    {
        if (faderCanvasGroup == null)
        {
            faderCanvasGroup = GetComponent<CanvasGroup>();
            if (faderCanvasGroup == null) faderCanvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        // 🎯 OTOMATIS MERETANGKAN RECTTRANSFORM PARENT JADI 100% FULLSCREEN (AKHIRI KOTAK KECIL DI TENGAH LAYAR)
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

            // 🎯 OTOMATIS MERETANGKAN RECTTRANSFORM IMAGE JADI 100% FULLSCREEN
            RectTransform rt = faderImage.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.one;
        }

        faderCanvasGroup.blocksRaycasts = false;
        faderCanvasGroup.alpha = 0f; // Transparan di awal game
    }

    /// <summary>
    /// Transisi Layar Menjadi Hitam Pekat (Fade to Black)
    /// </summary>
    public void FadeToBlack(float duration = -1f, System.Action onComplete = null)
    {
        float targetDuration = duration > 0f ? duration : defaultFadeDuration;
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeRoutine(1f, targetDuration, onComplete));
    }

    /// <summary>
    /// Transisi Layar Kembali Terang (Fade from Black)
    /// </summary>
    public void FadeFromBlack(float duration = -1f, System.Action onComplete = null)
    {
        float targetDuration = duration > 0f ? duration : defaultFadeDuration;
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FadeRoutine(0f, targetDuration, onComplete));
    }

    /// <summary>
    /// Perintah Sekali Jalan: Layar Hitam -> Eksekusi Action (Respawn) -> Layar Kembali Terang
    /// </summary>
    public void FadeToBlackAndExecute(System.Action onBlackScreen, float fadeOutSpeed = 0.8f, float fadeInSpeed = 0.8f, float holdBlackTime = 0.5f)
    {
        if (activeFadeRoutine != null) StopCoroutine(activeFadeRoutine);
        activeFadeRoutine = StartCoroutine(FullSequenceRoutine(onBlackScreen, fadeOutSpeed, fadeInSpeed, holdBlackTime));
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

    private IEnumerator FullSequenceRoutine(System.Action onBlackScreen, float fadeOutSpeed, float fadeInSpeed, float holdBlackTime)
    {
        // 1. Fade Out ke Layar Hitam
        yield return FadeRoutine(1f, fadeOutSpeed, null);

        // 2. Tahan di Layar Hitam & Eksekusi Reset/Respawn
        if (holdBlackTime > 0f)
        {
            yield return new WaitForSeconds(holdBlackTime);
        }

        onBlackScreen?.Invoke();

        // 3. Fade In Kembali Terang
        yield return FadeRoutine(0f, fadeInSpeed, null);
    }
}
