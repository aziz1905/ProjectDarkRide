using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sakelar Listrik Fisik (Power Switch) yang dapat ditekan [E] untuk menyalakan / mematikan aliran listrik (ON / OFF).
/// Mengayunkan tuas/sakelar 3D secara visual dan memutar audio klik.
/// </summary>
public class PowerSwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Power Status")]
    [Tooltip("Status awal sakelar (Default: TRUE / ON saat game mulai)")]
    [SerializeField] private bool isPowerOn = true;

    [Header("Switch Visual Rotation (Opsional)")]
    [Tooltip("Transform tuas/sakelar yang akan berputar naik/turun saat ditekan")]
    [SerializeField] private Transform switchLeverTransform;
    [SerializeField] private Vector3 onLocalRotation = new Vector3(0f, 0f, 0f);
    [SerializeField] private Vector3 offLocalRotation = new Vector3(45f, 0f, 0f);

    [Header("Audio (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip switchToggleSound; // SFX 'Klik' sakelar listrik

    [Header("Events (Opsional)")]
    public UnityEvent<bool> OnPowerStateChanged;

    public bool IsPowerOn => isPowerOn;
    public float HoldDuration => 0f; // Tap [E] Instan

    private void Start()
    {
        if (switchLeverTransform == null)
            switchLeverTransform = transform;

        UpdateSwitchVisual();
    }

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        return isPowerOn ? "Tekan [E] Matikan Listrik (OFF)" : "Tekan [E] Nyalakan Listrik (ON)";
    }

    public void OnInteract()
    {
        // ⚡ TOGGLE POWER ON / OFF
        isPowerOn = !isPowerOn;

        Debug.Log($"[POWER SWITCH] Sakelar Listrik diubah ke: {(isPowerOn ? "ON (MENYALA)" : "OFF (MATI)")}");

        // Play SFX Klik
        if (audioSource != null && switchToggleSound != null)
        {
            audioSource.PlayOneShot(switchToggleSound);
        }

        // Putar visual tuas sakelar
        UpdateSwitchVisual();

        // Eksekusi Event jika ada
        OnPowerStateChanged?.Invoke(isPowerOn);
    }

    private void UpdateSwitchVisual()
    {
        if (switchLeverTransform != null)
        {
            switchLeverTransform.localRotation = Quaternion.Euler(isPowerOn ? onLocalRotation : offLocalRotation);
        }
    }

    public void SetPowerState(bool state)
    {
        isPowerOn = state;
        UpdateSwitchVisual();
    }
}
