using UnityEngine;

/// <summary>
/// Kontroler Senter Tangan (Terintegrasi Penuh dengan Slot ToolBeltManager 1-5).
/// - Memegang / Menyimpan Senter diatur otomatis oleh Slot ToolBelt (Tekan tombol angka slotnya, misal 1 atau 2).
/// - KLIK KANAN: Menyalakan / Mematikan saklar lampu Spot Light saat sedang dipegang di tangan.
/// </summary>
public class EquipableFlashlight : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("Drag objek 3D Senter di tangan ke sini")]
    [SerializeField] private GameObject flashlightMesh;

    [Tooltip("Drag objek Spot Light di tangan ke sini")]
    [SerializeField] private Light flashlightSpotLight;

    [Header("Audio Settings (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip clickSFX; // SFX klik saklar lampu

    // Status Internal Senter
    private bool isLightOn = false; // Default: Lampu Selalu Mati di Awal
    private bool wasEquipped = false;

    public bool IsLightOn => isLightOn;
    public bool IsEquipped => flashlightMesh != null && flashlightMesh.activeInHierarchy;
    public Light SpotLightComponent => flashlightSpotLight;

    public void SetLightState(bool state)
    {
        isLightOn = state;
        UpdateFlashlightState();
    }

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        isLightOn = false; // Default: Mati saat game dimulai
        wasEquipped = false;
        UpdateFlashlightState();
    }

    private void Update()
    {
        bool currentlyEquipped = IsEquipped;

        // Setiap kali senter baru dikeluarkan ke tangan: PASTIKAN LAMPU SELALU MATI DI AWAL!
        if (currentlyEquipped && !wasEquipped)
        {
            isLightOn = false;
        }

        // Jika senter disimpan kembali ke sabuk/kantong (Unequip): OTOMATIS MATIKAN SAKLAR LAMPU!
        if (!currentlyEquipped && wasEquipped)
        {
            isLightOn = false;
        }
        wasEquipped = currentlyEquipped;

        // 💡 INTERAKSI SAKLAR LAMPU SENTER (KLIK KANAN / Mouse 1)
        if (Input.GetMouseButtonDown(1))
        {
            // HANYA BISA DIKLIK JIKA SENTER SEDANG DIPEGANG DI TANGAN!
            if (currentlyEquipped)
            {
                ToggleLightSwitch();
            }
        }

        // Sinkronisasi Sinar Lampu dengan Tangan Player
        UpdateFlashlightState();
    }

    /// <summary>
    /// Menyalakan / mematikan saklar lampu senter via KLIK KANAN
    /// </summary>
    public void ToggleLightSwitch()
    {
        isLightOn = !isLightOn;

        Debug.Log("[Flashlight] Saklar Lampu: " + (isLightOn ? "LAMPU NYALA (ON)" : "LAMPU MATI (OFF)"));

        if (audioSource != null && clickSFX != null)
        {
            audioSource.PlayOneShot(clickSFX);
        }

        UpdateFlashlightState();
    }

    /// <summary>
    /// Update tampilan cahaya Spot Light
    /// </summary>
    private void UpdateFlashlightState()
    {
        if (flashlightSpotLight != null)
        {
            // Lampu hanya menyala jika SENTER SEDANG DIPEGANG DI TANGAN & SAKLAR DALAM POSISI ON
            flashlightSpotLight.enabled = IsEquipped && isLightOn;
        }
    }
}