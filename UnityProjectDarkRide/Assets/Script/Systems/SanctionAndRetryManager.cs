using UnityEngine;
using TMPro;
using System;

/// <summary>
/// Pengelola Sanksi Laporan & Retry System (Dark Ride Maintenance).
/// Production-Ready: Clean & 0% Spam Log.
/// </summary>
public class SanctionAndRetryManager : MonoBehaviour
{
    private static SanctionAndRetryManager _instance;
    public static SanctionAndRetryManager Instance => _instance;

    [Header("Retry Limit Settings")]
    [Tooltip("Jumlah maksimal Retry yang diperbolehkan sebelum Game Over (Default: 5x)")]
    [SerializeField] private int maxRetries = 5;
    [SerializeField] private int currentRetryCount = 0;

    [Header("Scene Respawn References")]
    [Tooltip("Drag GameObject RespawnPoint di Scene (Titik tempat player berdiri di awal game & saat retry)")]
    [SerializeField] private Transform stationRespawnPoint;

    [Header("UI Sanction Display (Opsional)")]
    [Tooltip("Teks UI jumlah sanksi (Opsional)")]
    [SerializeField] private TextMeshProUGUI sanctionCountText;

    [Header("SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sanctionWarningSFX;
    [SerializeField] private AudioClip retryFadeSFX;

    // Events
    public event Action<int, int> OnSanctionAdded;
    public event Action OnGameOverTriggered;

    public int CurrentRetryCount => currentRetryCount;
    public int MaxRetries => maxRetries;
    public bool IsMaxSanctionReached => currentRetryCount >= maxRetries;

    private void Awake()
    {
        if (_instance == null) _instance = this;
    }

    private void Start()
    {
        UpdateSanctionUI();
        UpdateBloodOverlay();

        RespawnPlayerAndCartPreserveInventory();

        if (ShiftTimerManager.Instance != null)
        {
            ShiftTimerManager.Instance.OnShiftEnded += HandleShiftTimeoutFailure;
        }
    }

    private void OnDestroy()
    {
        if (ShiftTimerManager.Instance != null)
        {
            ShiftTimerManager.Instance.OnShiftEnded -= HandleShiftTimeoutFailure;
        }
    }

    private void HandleShiftTimeoutFailure()
    {
        IssueSanction("Shift Malam Habis (06:00 AM) - Tugas Maintenance Belum Selesai");
    }

    public void IssueSanction(string reasonMessage = "Pelanggaran Laporan Anomali / Kematian")
    {
        if (IsMaxSanctionReached) return;

        currentRetryCount++;

        UpdateSanctionUI();
        UpdateBloodOverlay();

        OnSanctionAdded?.Invoke(currentRetryCount, maxRetries);

        if (sanctionWarningSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(sanctionWarningSFX);
        }

        if (currentRetryCount >= maxRetries)
        {
            TriggerGameOverSequence(reasonMessage);
        }
        else
        {
            TriggerRetryFadeSequence();
        }
    }

    private void UpdateBloodOverlay()
    {
        if (BloodSplatterOverlay.Instance != null)
        {
            BloodSplatterOverlay.Instance.UpdateBloodIntensity(currentRetryCount, maxRetries);
        }
    }

    private void TriggerRetryFadeSequence()
    {
        GameInputLock.LockInput();

        if (retryFadeSFX != null && audioSource != null)
        {
            audioSource.PlayOneShot(retryFadeSFX);
        }

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToBlackAndExecute(() =>
            {
                ExecuteFullShiftRetryReset();
            }, 0.8f, 0.8f, 0.5f);
        }
        else
        {
            ExecuteFullShiftRetryReset();
            GameInputLock.UnlockInput();
        }
    }

    private void ExecuteFullShiftRetryReset()
    {
        if (ShiftTimerManager.Instance != null)
        {
            ShiftTimerManager.Instance.ResetShiftTimer();
        }

        if (TaskManager.Instance != null)
        {
            TaskManager.Instance.ResetAllTasks();
        }

        ToolBeltManager toolBelt = FindObjectOfType<ToolBeltManager>();
        if (toolBelt != null)
        {
            toolBelt.ResetToolBelt();
        }

        RespawnPlayerAndCartPreserveInventory();
        GameInputLock.UnlockInput();
    }

    private void RespawnPlayerAndCartPreserveInventory()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) playerObj = FindObjectOfType<PlayerMovement>()?.gameObject;
        if (playerObj == null) playerObj = GameObject.Find("playerDay3");

        Vector3 targetPos = stationRespawnPoint != null ? stationRespawnPoint.position : new Vector3(-75.8f, 0.1f, 66.6f);
        Quaternion targetRot = stationRespawnPoint != null ? stationRespawnPoint.rotation : Quaternion.identity;

        if (playerObj != null)
        {
            DarkRideCartController cart = FindObjectOfType<DarkRideCartController>();
            if (cart != null && cart.IsPlayerSeated)
            {
                cart.SendMessage("UnboardPlayer", SendMessageOptions.DontRequireReceiver);
            }

            CharacterController cc = playerObj.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            playerObj.transform.position = targetPos;
            playerObj.transform.rotation = targetRot;

            if (cc != null) cc.enabled = true;
        }
    }

    private void TriggerGameOverSequence(string finalReason)
    {
        GameInputLock.LockInput();
        OnGameOverTriggered?.Invoke();

        if (ScreenFader.Instance != null)
        {
            ScreenFader.Instance.FadeToBlack(1.5f, () =>
            {
                GameOverUIController gameOverUI = FindObjectOfType<GameOverUIController>();
                if (gameOverUI != null)
                {
                    gameOverUI.ShowGameOverScreen(finalReason);
                }
            });
        }
        else
        {
            GameOverUIController gameOverUI = FindObjectOfType<GameOverUIController>();
            if (gameOverUI != null)
            {
                gameOverUI.ShowGameOverScreen(finalReason);
            }
        }
    }

    private void UpdateSanctionUI()
    {
        if (sanctionCountText != null)
        {
            sanctionCountText.text = $"Sanksi: <b>{currentRetryCount}</b> / {maxRetries}";
        }
    }

    public void ResetSanctions()
    {
        currentRetryCount = 0;
        UpdateSanctionUI();
        UpdateBloodOverlay();
    }
}
