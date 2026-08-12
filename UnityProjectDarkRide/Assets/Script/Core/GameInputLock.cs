using UnityEngine;

/// <summary>
/// Sistem Kunci Input Universal (Master Input Lock).
/// Menjadi 1 pintu pusat untuk membekukan WASD & Kamera saat minigame APA PUN aktif
/// tanpa perlu mengedit-edit script PlayerMovement atau FirstPersonCamera lagi di masa depan!
/// </summary>
public static class GameInputLock
{
    private static int activeLockCounter = 0;

    /// <summary>
    /// Apakah input player (WASD & Kamera) sedang terkunci oleh suatu sistem / minigame?
    /// </summary>
    public static bool IsInputLocked => activeLockCounter > 0;

    /// <summary>
    /// Kunci kontrol player dan buka kursor mouse bebas (Dipanggil saat Minigame/Menu APA PUN mulai)
    /// </summary>
    public static void LockInput()
    {
        activeLockCounter++;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        // Debug.Log($"[INPUT LOCK] Input Player Terkunci (Aktif: {activeLockCounter})");
    }

    /// <summary>
    /// Buka kembali kontrol player dan kunci kursor ke tengah layar (Dipanggil saat Minigame selesai)
    /// </summary>
    public static void UnlockInput()
    {
        activeLockCounter = Mathf.Max(0, activeLockCounter - 1);

        if (activeLockCounter == 0)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            // Debug.Log("[INPUT UNLOCK] Input Player Pulih Kembali Normal!");
        }
    }

    /// <summary>
    /// Reset paksa semua status kunci (misal saat reload scene)
    /// </summary>
    public static void ForceUnlockAll()
    {
        activeLockCounter = 0;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
}
