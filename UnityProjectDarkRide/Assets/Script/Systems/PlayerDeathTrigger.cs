using UnityEngine;

/// <summary>
/// Trigger 3D Bahaya / Kematian Player / Sanksi Anomali.
/// Production-Ready: Clean & 0% Spam Log.
/// </summary>
public class PlayerDeathTrigger : MonoBehaviour
{
    [Header("Death / Sanction Reason")]
    [Tooltip("Alasan Kematian / Sanksi yang tampil di layar saat Game Over (misal: 'Tersengat Listrik Tegangan Tinggi' atau 'Diserang Animatronik')")]
    [SerializeField] private string deathReason = "Diserang Animatronik / Area Bahaya Terlarang";

    [Header("Trigger Mode")]
    [Tooltip("Jika CENTANG, trigger ini hanya bisa memicu kematian 1x")]
    [SerializeField] private bool triggerOnce = false;
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnce && hasTriggered) return;

        if (other.CompareTag("Player") || other.GetComponent<PlayerMovement>() != null || other.name.ToLower().Contains("player"))
        {
            TriggerPlayerDeath();
        }
    }

    public void TriggerPlayerDeath()
    {
        if (triggerOnce && hasTriggered) return;
        hasTriggered = true;

        if (SanctionAndRetryManager.Instance != null)
        {
            SanctionAndRetryManager.Instance.IssueSanction(deathReason);
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
    }
}
