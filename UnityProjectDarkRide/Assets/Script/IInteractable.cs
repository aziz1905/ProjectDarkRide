public interface IInteractable
{
    // Teks Prompt UI ("Tekan [E] Ambil Fuse" ATAU "Tahan [E] Lap Cermin")
    string GetInteractPrompt();

    // Durasi Tahan dalam detik (0 = TAP Instan | > 0 = HOLD Tahan)
    float HoldDuration { get; }

    // Eksekusi aksi saat interaksi sukses
    void OnInteract();
}