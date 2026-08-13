using UnityEngine;

/// <summary>
/// Kontroler Indikator 3 Lampu pada SaklarBox (Merah, Kuning, Hijau).
/// Mengontrol Objek Bola 3D Indikator (GameObject / Mesh) secara bersih tanpa silau:
/// 1. MERAH: Menyala saat Sakelar OFF (Listrik Mati).
/// 2. KUNING: Menyala saat Sakelar ON, tetapi Fuse atau Kabel masih Rusak.
/// 3. HIJAU: Menyala saat Sakelar ON dan Semua Sistem Kelistrikan Normal.
/// </summary>
public class SaklarBoxController : MonoBehaviour
{
    [Header("Switch Reference")]
    [Tooltip("Drag Objek Switch / Sakelar ke sini")]
    [SerializeField] private PowerSwitchInteractable powerSwitch;

    [Header("Connected Maintenance Boxes")]
    [Tooltip("Drag Objek FuseBox ke sini (Opsional)")]
    [SerializeField] private FuseBoxController fuseBox;

    [Tooltip("Drag Objek JunctionBox / WireTask ke sini (Opsional)")]
    [SerializeField] private WireTaskInteractable wireTask;

    [Header("3 Indicator Spheres (3D GameObjects / Meshes)")]
    [Tooltip("Bola Indikator Merah")]
    [SerializeField] private GameObject redIndicator;

    [Tooltip("Bola Indikator Kuning")]
    [SerializeField] private GameObject yellowIndicator;

    [Tooltip("Bola Indikator Hijau")]
    [SerializeField] private GameObject greenIndicator;

    private void Start()
    {
        if (powerSwitch == null)
            powerSwitch = GetComponentInChildren<PowerSwitchInteractable>();

        UpdateIndicators();
    }

    private void Update()
    {
        UpdateIndicators();
    }

    public void UpdateIndicators()
    {
        bool isSwitchOn = powerSwitch != null && powerSwitch.IsPowerOn;
        bool isFuseBroken = fuseBox != null && fuseBox.IsFuseBroken;
        bool isWireBroken = wireTask != null && !wireTask.IsWireFixed;

        // 🔴 1. STATUS MERAH: Sakelar Listrik OFF (Daya Mati)
        if (!isSwitchOn)
        {
            SetIndicators(red: true, yellow: false, green: false);
        }
        // 🟡 2. STATUS KUNING: Sakelar ON, tapi ada Fuse Rusak ATAU Kabel Belum Beres
        else if (isFuseBroken || isWireBroken)
        {
            SetIndicators(red: false, yellow: true, green: false);
        }
        // 🟢 3. STATUS HIJAU: Sakelar ON & Semua Sistem Normal
        else
        {
            SetIndicators(red: false, yellow: false, green: true);
        }
    }

    private void SetIndicators(bool red, bool yellow, bool green)
    {
        if (redIndicator != null) redIndicator.SetActive(red);
        if (yellowIndicator != null) yellowIndicator.SetActive(yellow);
        if (greenIndicator != null) greenIndicator.SetActive(green);
    }
}
