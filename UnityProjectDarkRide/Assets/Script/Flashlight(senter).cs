using UnityEngine;

public class EquipableFlashlight : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("Drag objek Cylinder (FlashlightMesh) ke sini")]
    [SerializeField] private GameObject flashlightMesh;

    [Tooltip("Drag objek Spot Light ke sini")]
    [SerializeField] private Light flashlightSpotLight;

    [Header("Audio Settings (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip equipSFX; // SFX ambil/simpan senter
    [SerializeField] private AudioClip clickSFX; // SFX klik saklar lampu

    [Header("Keybindings")]
    [SerializeField] private KeyCode equipKey = KeyCode.F;              // Tombol F untuk Equip/Unequip
    [SerializeField] private KeyCode alternateEquipKey = KeyCode.Alpha1; // Atau Tombol 1

    // Status Internal Senter
    private bool isEquipped = false; // Default: FALSE (Tangan Kosong saat spawn)
    private bool isLightOn = false;  // Default: FALSE (Lampu Mati)

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        // Pengecekan keamanan jika slot Inspector lupa ditarik
        if (flashlightMesh == null)
            Debug.LogError("[Flashlight Error] Slot 'Flashlight Mesh' di Inspector masih KOSONG/NONE! Harap drag objek Cylinder ke slot ini.");

        if (flashlightSpotLight == null)
            Debug.LogError("[Flashlight Error] Slot 'Flashlight Spot Light' di Inspector masih KOSONG/NONE! Harap drag objek Spot Light ke slot ini.");

        // Set status awal game: Tangan KOSONG (Mesh tersembunyi & lampu mati)
        UpdateFlashlightState();
    }

    private void Update()
    {
        // 1. INTERAKSI EQUIP / UNEQUIP (Tekan tombol F atau 1)
        if (Input.GetKeyDown(equipKey) || Input.GetKeyDown(alternateEquipKey))
        {
            ToggleEquip();
        }

        // 2. INTERAKSI SAKLAR LAMPU (Klik Kiri)
        if (Input.GetMouseButtonDown(0))
        {
            // HANYA BISA DIKLIK JIKA SENTER SEDANG DI-EQUIP!
            if (isEquipped)
            {
                ToggleLightSwitch();
            }
        }
    }

    /// <summary>
    /// Mengeluarkan atau menyimpan senter ke kantong
    /// </summary>
    public void ToggleEquip()
    {
        isEquipped = !isEquipped;

        // Jika senter disimpan (Unequip), lampu otomatis dimatikan
        if (!isEquipped)
        {
            isLightOn = false;
        }

        // Tampilkan pesan di Console Unity untuk verifikasi
        Debug.Log("[Flashlight] Status Equip Berubah: " + (isEquipped ? "SENTER DI-EQUIP (Di Tangan)" : "SENTER DI-UNEQUIP (Tangan Kosong)"));

        // Play SFX Equip/Unequip
        if (audioSource != null && equipSFX != null)
        {
            audioSource.PlayOneShot(equipSFX);
        }

        UpdateFlashlightState();
    }

    /// <summary>
    /// Menyalakan / mematikan saklar lampu senter via Klik Kiri
    /// </summary>
    public void ToggleLightSwitch()
    {
        isLightOn = !isLightOn;

        // Tampilkan pesan di Console Unity untuk verifikasi
        Debug.Log("[Flashlight] Saklar Lampu: " + (isLightOn ? "LAMPU NYALA (ON)" : "LAMPU MATI (OFF)"));

        // Play SFX Klik Saklar
        if (audioSource != null && clickSFX != null)
        {
            audioSource.PlayOneShot(clickSFX);
        }

        UpdateFlashlightState();
    }

    /// <summary>
    /// Update tampilan visual 3D Mesh dan Cahaya Spot Light
    /// </summary>
    private void UpdateFlashlightState()
    {
        // Sembunyikan / Munculkan 3D Mesh Cylinder
        if (flashlightMesh != null)
        {
            flashlightMesh.SetActive(isEquipped);
        }

        // Nyalakan / Matikan komponen cahaya Spot Light
        if (flashlightSpotLight != null)
        {
            flashlightSpotLight.enabled = isEquipped && isLightOn;
        }
    }
}