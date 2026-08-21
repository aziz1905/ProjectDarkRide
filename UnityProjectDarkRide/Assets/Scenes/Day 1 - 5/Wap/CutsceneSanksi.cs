using System.Collections;
using UnityEngine;
using TMPro;

public class CutsceneSanksi : MonoBehaviour
{
    public CanvasGroup canvasGroup;
    public TextMeshProUGUI laporanText;

    [TextArea(3, 10)]
    public string isiLaporan = "Laporan Sanksi: Pelanggaran telah dicatat...";

    public float fadeSpeed = 1f;
    public float typingSpeed = 0.05f;

    void Start()
    {
        canvasGroup.alpha = 0f;
        laporanText.text = "";
        StartCoroutine(MulaiCutscene());
    }

    IEnumerator MulaiCutscene()
    {
        // 1. Fade-In
        while (canvasGroup.alpha < 1f)
        {
            canvasGroup.alpha += Time.deltaTime * fadeSpeed;
            yield return null;
        }

        // 2. Teks Berjalan (Typewriter)
        foreach (char letter in isiLaporan.ToCharArray())
        {
            laporanText.text += letter;
            yield return new WaitForSeconds(typingSpeed);
        }

        yield return new WaitForSeconds(2f);

        // 3. Fade-Out
        while (canvasGroup.alpha > 0f)
        {
            canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
            yield return null;
        }
    }
}