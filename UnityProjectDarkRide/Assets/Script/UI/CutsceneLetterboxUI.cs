using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Cutscene Letterbox Manager (Sinematic Black Bars Top & Bottom).
/// Otomatis membuat & mengontrol bilah hitam atas/bawah layaknya efek film saat cutscene berjalan.
/// </summary>
public class CutsceneLetterboxUI : MonoBehaviour
{
    private static CutsceneLetterboxUI _instance;
    public static CutsceneLetterboxUI Instance => _instance;

    [Header("Letterbox Settings")]
    [Tooltip("Tinggi relatif bilah hitam dari tinggi layar (misal 0.12 = 12% layar atas & 12% layar bawah)")]
    [SerializeField] private float barHeightNormalized = 0.12f;

    [Tooltip("Kecepatan transisi bar slide in/out (detik)")]
    [SerializeField] private float transitionDuration = 0.6f;

    private CanvasGroup canvasGroup;
    private RectTransform topBar;
    private RectTransform bottomBar;
    private Coroutine activeTransition;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
        }

        AutoSetupUI();
    }

    private void AutoSetupUI()
    {
        // Setup Canvas jika ditaruh di GameObject tersendiri atau di bawah Canvas utama
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 99; // Di bawah Fade Fader Screen, tapi di atas UI standar
            gameObject.AddComponent<CanvasScaler>();
            gameObject.AddComponent<GraphicRaycaster>();
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        RectTransform rootRt = GetComponent<RectTransform>();
        if (rootRt != null)
        {
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.one;
        }

        // Top Bar
        Transform topObj = transform.Find("TopLetterboxBar");
        if (topObj == null)
        {
            GameObject obj = new GameObject("TopLetterboxBar");
            obj.transform.SetParent(transform, false);
            Image img = obj.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            topBar = obj.GetComponent<RectTransform>();
        }
        else
        {
            topBar = topObj.GetComponent<RectTransform>();
        }

        topBar.anchorMin = new Vector2(0f, 1f - barHeightNormalized);
        topBar.anchorMax = Vector2.one;
        topBar.offsetMin = Vector2.zero;
        topBar.offsetMax = Vector2.zero;

        // Bottom Bar
        Transform bottomObj = transform.Find("BottomLetterboxBar");
        if (bottomObj == null)
        {
            GameObject obj = new GameObject("BottomLetterboxBar");
            obj.transform.SetParent(transform, false);
            Image img = obj.AddComponent<Image>();
            img.color = Color.black;
            img.raycastTarget = false;
            bottomBar = obj.GetComponent<RectTransform>();
        }
        else
        {
            bottomBar = bottomObj.GetComponent<RectTransform>();
        }

        bottomBar.anchorMin = Vector2.zero;
        bottomBar.anchorMax = new Vector2(1f, barHeightNormalized);
        bottomBar.offsetMin = Vector2.zero;
        bottomBar.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// Menampilkan bilah hitam cinematic (letterbox bar) LANGSUNG TANPA FADE (INSTANT).
    /// </summary>
    public static void Show(float duration = 0f)
    {
        EnsureInstance();
        if (_instance != null)
        {
            _instance.ToggleLetterbox(true, duration);
        }
    }

    /// <summary>
    /// Sembunyikan bilah hitam cinematic DENGAN FADE FADE-OUT MULUS DI AKHIR.
    /// </summary>
    public static void Hide(float duration = -1f)
    {
        if (_instance != null)
        {
            float dur = duration >= 0f ? duration : _instance.transitionDuration;
            _instance.ToggleLetterbox(false, dur);
        }
    }

    private static void EnsureInstance()
    {
        if (_instance == null)
        {
            CutsceneLetterboxUI existing = FindObjectOfType<CutsceneLetterboxUI>();
            if (existing != null)
            {
                _instance = existing;
            }
            else
            {
                GameObject go = new GameObject("CutsceneLetterboxCanvas");
                _instance = go.AddComponent<CutsceneLetterboxUI>();
            }
        }
    }

    public void ToggleLetterbox(bool show, float duration)
    {
        if (activeTransition != null) StopCoroutine(activeTransition);
        activeTransition = StartCoroutine(AnimateLetterboxRoutine(show, duration));
    }

    private IEnumerator AnimateLetterboxRoutine(bool show, float duration)
    {
        float targetAlpha = show ? 1f : 0f;
        float startAlpha = canvasGroup.alpha;
        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, Mathf.SmoothStep(0f, 1f, t));
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;
        activeTransition = null;
    }
}
