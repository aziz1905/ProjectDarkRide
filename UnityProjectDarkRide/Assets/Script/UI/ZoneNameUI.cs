using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// UI Display Nama & Judul Zona Baru saat Cutscene Perpindahan Zona.
/// Teks muncul dengan animasi Fade In & Fade Out mulus di layar.
/// </summary>
public class ZoneNameUI : MonoBehaviour
{
    private static ZoneNameUI _instance;
    public static ZoneNameUI Instance => _instance;

    [Header("UI Text References")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI subTitleText;

    private Coroutine activeDisplayRoutine;

    private void Awake()
    {
        if (_instance == null) _instance = this;
        AutoSetupUI();
    }

    private void AutoSetupUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 95; // Di bawah Fader layar, tapi di atas UI Game
            gameObject.AddComponent<UnityEngine.UI.CanvasScaler>();
            gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
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

        if (titleText == null)
        {
            Transform tObj = transform.Find("ZoneTitleText");
            if (tObj == null)
            {
                GameObject go = new GameObject("ZoneTitleText");
                go.transform.SetParent(transform, false);
                titleText = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                titleText = tObj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (titleText != null)
        {
            titleText.fontSize = 36;
            titleText.fontStyle = FontStyles.Bold;
            titleText.alignment = TextAlignmentOptions.Center;
            titleText.color = Color.white;
            RectTransform rt = titleText.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.45f);
            rt.anchorMax = new Vector2(0.9f, 0.6f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        if (subTitleText == null)
        {
            Transform stObj = transform.Find("ZoneSubTitleText");
            if (stObj == null)
            {
                GameObject go = new GameObject("ZoneSubTitleText");
                go.transform.SetParent(transform, false);
                subTitleText = go.AddComponent<TextMeshProUGUI>();
            }
            else
            {
                subTitleText = stObj.GetComponent<TextMeshProUGUI>();
            }
        }

        if (subTitleText != null)
        {
            subTitleText.fontSize = 20;
            subTitleText.fontStyle = FontStyles.Italic;
            subTitleText.alignment = TextAlignmentOptions.Center;
            subTitleText.color = new Color(0.85f, 0.85f, 0.85f, 1f);
            RectTransform rt = subTitleText.rectTransform;
            rt.anchorMin = new Vector2(0.1f, 0.38f);
            rt.anchorMax = new Vector2(0.9f, 0.46f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
    }

    public void ShowZoneTitle(string title, string subTitle, float duration)
    {
        if (activeDisplayRoutine != null) StopCoroutine(activeDisplayRoutine);
        activeDisplayRoutine = StartCoroutine(DisplayZoneTitleRoutine(title, subTitle, duration));
    }

    private IEnumerator DisplayZoneTitleRoutine(string title, string subTitle, float duration)
    {
        if (titleText != null) titleText.text = title;
        if (subTitleText != null) subTitleText.text = subTitle;

        float fadeInDuration = 0.8f;
        float fadeOutDuration = 0.8f;
        float holdDuration = Mathf.Max(0.5f, duration - fadeInDuration - fadeOutDuration);

        // Fade In
        float timer = 0f;
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1f;

        // Hold
        yield return new WaitForSeconds(holdDuration);

        // Fade Out
        timer = 0f;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0f;
        activeDisplayRoutine = null;
    }
}
