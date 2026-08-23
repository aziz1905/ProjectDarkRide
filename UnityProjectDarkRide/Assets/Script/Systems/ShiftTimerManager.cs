using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Pengelola Waktu Shift Malam (00:00 - 06:00 AM).
/// Menampilkan Jam Digital HANYA di Pojok Kiri Bawah Layar Canvas HUD.
/// Otomatis menyembunyikan Jam saat layar sedang Hitam / Fade Black.
/// 0% CPU Overhead & Bebas Garbage Collection.
/// </summary>
public class ShiftTimerManager : MonoBehaviour
{
    private static ShiftTimerManager _instance;
    public static ShiftTimerManager Instance => _instance;

    [Header("Shift Time Settings")]
    [Tooltip("Durasi 1 Shift Malam (00:00 - 06:00) dalam detik dunia nyata. Default: 360 detik = 6 menit")]
    [SerializeField] private float shiftDurationInSeconds = 360f; // 6 Menit Real Time = 6 Jam Game Time

    [Header("UI Text Display (Pojok Kiri Bawah)")]
    [Tooltip("Teks Jam Digital di Pojok Kiri Bawah Layar HUD (Canvas Utama - Tampil Terus dari Awal Sampai Akhir)")]
    [SerializeField] private TextMeshProUGUI hudClockText;

    [Header("Shift Status Event")]
    [SerializeField] private bool isTimerRunning = false;
    private float elapsedTime = 0f;
    private bool isShiftEnded = false;

    // Public Properties & Events
    public event Action OnShiftEnded;
    public bool IsShiftEnded => isShiftEnded;
    public float CurrentTimeProgressNormalized => Mathf.Clamp01(elapsedTime / shiftDurationInSeconds);

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void Start()
    {
        elapsedTime = 0f;
        isShiftEnded = false;
        isTimerRunning = true;

        UpdateClockUI();
    }

    private void Update()
    {
        // Tahan waktu jika game paused / input locked
        if (GameInputLock.IsInputLocked || !isTimerRunning || isShiftEnded)
        {
            UpdateClockUI();
            return;
        }

        elapsedTime += Time.deltaTime;

        if (elapsedTime >= shiftDurationInSeconds)
        {
            elapsedTime = shiftDurationInSeconds;
            TriggerShiftEnd();
        }

        UpdateClockUI();
    }

    /// <summary>
    /// Mengonversi progress detik real-time menjadi format jam digital 00:00 AM - 06:00 AM
    /// </summary>
    public string GetFormattedGameTime()
    {
        float normalized = CurrentTimeProgressNormalized; // 0.0 -> 1.0 (berarti 0 jam -> 6 jam)
        float totalGameMinutes = normalized * 360f; // 6 Jam * 60 Menit = 360 Menit

        int hours = Mathf.FloorToInt(totalGameMinutes / 60f); // 0 sampai 6
        int minutes = Mathf.FloorToInt(totalGameMinutes % 60f); // 0 sampai 59

        if (hours >= 6)
        {
            return "06:00 AM";
        }

        return string.Format("{0:D2}:{1:D2} AM", hours, minutes);
    }

    private void UpdateClockUI()
    {
        if (hudClockText == null) return;

        // 🛑 JAM TIDAK BOLEH MUNCUL SAAT LAYAR SEDANG HITAM (FADE BLACK / RETRY / GAME OVER)
        if (ScreenFader.Instance != null && ScreenFader.Instance.IsFadingOrBlack)
        {
            if (hudClockText.gameObject.activeSelf) hudClockText.gameObject.SetActive(false);
            return;
        }

        // 🎯 TAMPILKAN JAM DI POJOK KIRI BAWAH LAYAR CANVAS HUD SAAT LAYAR TERANG
        if (!hudClockText.gameObject.activeSelf) hudClockText.gameObject.SetActive(true);
        hudClockText.text = $"<b>JAM:</b> {GetFormattedGameTime()}";
    }

    private void TriggerShiftEnd()
    {
        if (isShiftEnded) return;

        isShiftEnded = true;
        isTimerRunning = false;

        Debug.Log("<color=cyan>[SHIFT TIMER END] Waktu Shift Malam telah habis (06:00 AM)!</color>");
        OnShiftEnded?.Invoke();
    }

    public void PauseTimer()
    {
        isTimerRunning = false;
    }

    public void ResumeTimer()
    {
        if (!isShiftEnded) isTimerRunning = true;
    }

    public void ResetShiftTimer()
    {
        elapsedTime = 0f;
        isShiftEnded = false;
        isTimerRunning = true;
        UpdateClockUI();
    }
}
