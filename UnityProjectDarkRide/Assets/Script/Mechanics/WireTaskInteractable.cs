using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Dipasang pada Objek JunctionBox di dinding 3D.
/// Membuka Minigame Sambung Kabel (Among Us) saat pemain menekan [E].
/// Dilengkapi sistem pengaman sakelar listrik jika diaktifkan.
/// </summary>
public class WireTaskInteractable : MonoBehaviour, IInteractable
{
    [Header("Task Settings")]
    [Tooltip("ID Tugas di TaskManager Notebook TAB")]
    [SerializeField] private string taskId = "FIX_WIRE";

    [Header("Safety Power Switch")]
    [Tooltip("Drag Objek Switch / Sakelar jika ingin mewajibkan listrik OFF saat memperbaiki kabel")]
    [SerializeField] private PowerSwitchInteractable powerSwitch;
    [SerializeField] private bool requirePowerOff = true;

    [Header("Sparks Hazard Effect (Opsional)")]
    [Tooltip("Efek percikan api listrik yang akan otomatis mati saat sakelar OFF / kabel selesai")]
    [SerializeField] private GameObject electricSparksObject;

    [Header("Custom Actions (Events)")]
    public UnityEvent OnWireRepaired;

    // Internal State
    private bool isWireFixed = false;

    public bool IsWireFixed => isWireFixed;
    public bool IsSwitchOn => powerSwitch != null && powerSwitch.IsPowerOn;

    private void Update()
    {
        // Matikan percikan api jika sakelar OFF atau kabel sudah selesai diperbaiki
        if (electricSparksObject != null)
        {
            bool shouldSpark = !isWireFixed && IsSwitchOn;
            if (electricSparksObject.activeSelf != shouldSpark)
            {
                electricSparksObject.SetActive(shouldSpark);
            }
        }
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        if (isWireFixed) return "Junction Box [NORMAL]";

        if (WireMinigameController.Instance != null && WireMinigameController.Instance.IsPlaying)
            return ""; // Sembunyikan prompt saat minigame aktif

        // Cek Keselamatan
        if (requirePowerOff && IsSwitchOn)
        {
            return "[BAHAYA] Matikan Sakelar Listrik Terlebih Dahulu!";
        }

        return "Tekan [E] Perbaiki Kabel (Wiring)";
    }

    public float HoldDuration => 0f; // Tap [E] Instan untuk membuka Minigame

    public void OnInteract()
    {
        if (isWireFixed) return;

        // Cek Keselamatan
        if (requirePowerOff && IsSwitchOn)
        {
            Debug.LogWarning("[WIRE BLOCKED] Sakelar masih ON! Matikan sakelar terlebih dahulu!");
            return;
        }

        // Buka Minigame Sambung Kabel!
        if (WireMinigameController.Instance != null)
        {
            WireMinigameController.Instance.StartMinigame(OnMinigameSuccess);
        }
        else
        {
            Debug.LogError("[WIRE ERROR] Objek 'WireMinigameController' tidak ditemukan di Canvas! Pastikan script sudah terpasang di Canvas UI.");
        }
    }

    private void OnMinigameSuccess()
    {
        isWireFixed = true;
        Debug.Log($"[TASK COMPLETED] Tugas Kabel '{gameObject.name}' Selesai 100%!");

        // Laporkan ke TaskManager (Notebook TAB)
        if (!string.IsNullOrEmpty(taskId))
        {
            TaskManager taskMgr = FindObjectOfType<TaskManager>();
            if (taskMgr != null) taskMgr.CompleteTask(taskId);
        }

        // Eksekusi Event Custom jika ada
        OnWireRepaired?.Invoke();
    }
}
