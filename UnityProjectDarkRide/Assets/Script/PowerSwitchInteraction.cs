using UnityEngine;

/// <summary>
/// Dipasang pada Cube Sakelar di dalam PanelBox.
/// Berfungsi untuk mematikan / menghidupkan listrik ruangan (Toggle ON/OFF).
/// </summary>
public class PowerSwitchInteractable : MonoBehaviour, IInteractable
{
    [Header("Power Switch Status")]
    [SerializeField] private bool isPowerOn = true;

    [Header("Visual Lever / Switch Mesh (Opsional)")]
    [SerializeField] private Transform switchLeverTransform;
    [SerializeField] private Vector3 onRotation = new Vector3(0f, 0f, 30f);
    [SerializeField] private Vector3 offRotation = new Vector3(0f, 0f, -30f);

    // --- IMPLEMENTASI INTERFACE IINTERACTABLE ---

    public string GetInteractPrompt()
    {
        return isPowerOn ? "Tekan [E] Matikan Sakelar Listrik (OFF)" : "Tekan [E] Nyalakan Sakelar Listrik (ON)";
    }

    public float HoldDuration => 0f; // Tap [E] Instan

    public void OnInteract()
    {
        // Toggle Sakelar ON <-> OFF
        isPowerOn = !isPowerOn;
        
        Debug.Log($"[POWER SWITCH] Sakelar Listrik diputar ke posisi: {(isPowerOn ? "ON (NYALA)" : "OFF (MATI)")}");

        // Rotasi visual tuas sakelar jika ada
        if (switchLeverTransform != null)
        {
            switchLeverTransform.localRotation = Quaternion.Euler(isPowerOn ? onRotation : offRotation);
        }
    }

    // Getter Status Listrik yang dibaca oleh LampBulbFixture
    public bool IsPowerOn => isPowerOn;
}