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
    [SerializeField] private KeyCode equipKey = KeyCode.F; // Tombol F untuk Equip/Unequip Senter

    // Status Internal Senter
    private bool isEquipped = false; // Default: FALSE (Tangan Kosong saat spawn)
    private bool isLightOn = false;  // Default: FALSE (Lampu Mati)

    private void Start()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (flashlightMesh == null)
            Debug.LogError("[Flashlight Error] Slot 'Flashlight Mesh' di Inspector masih KOSONG! Harap drag objek Cylinder ke slot ini.");

        if (flashlightSpotLight == null)
            Debug.LogError("[Flashlight Error] Slot 'Flashlight Spot Light' di Inspector masih KOSONG! Harap drag objek Spot Light ke slot ini.");

        UpdateFlashlightState();
    }

    private void Update()
    {
        // 1. INTERAKSI EQUIP / UNEQUIP (Tekan tombol F)
        if (Input.GetKeyDown(equipKey))
        {
            ToggleEquip();
        }

        // 2. INTERAKSI SAKLAR LAMPU SENTER (KLIK KANAN / Mouse1)
        if (Input.GetMouseButtonDown(1))
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

        Debug.Log("[Flashlight] Status Equip: " + (isEquipped ? "SENTER DI-EQUIP (Di Tangan)" : "SENTER DI-UNEQUIP (Kantong)"));

        if (audioSource != null && equipSFX != null)
        {
            audioSource.PlayOneShot(equipSFX);
        }

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
    /// Update tampilan visual 3D Mesh dan Cahaya Spot Light
    /// </summary>
    private void UpdateFlashlightState()
    {
        if (flashlightMesh != null)
        {
            flashlightMesh.SetActive(isEquipped);
        }

        if (flashlightSpotLight != null)
        {
            flashlightSpotLight.enabled = isEquipped && isLightOn;
        }
    }
}