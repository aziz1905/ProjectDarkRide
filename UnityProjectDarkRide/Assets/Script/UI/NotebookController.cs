using UnityEngine;

public class NotebookController : MonoBehaviour
{
    [Header("Notebook UI References")]
    [SerializeField] private GameObject notebookPanel; // Drag NotebookPanel ke sini
    [SerializeField] private KeyCode toggleKey = KeyCode.Tab; // Tombol TAB

    [Header("SFX (Opsional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openNotebookSFX;
    [SerializeField] private AudioClip closeNotebookSFX;

    private bool isNotebookOpen = false;

    private void Start()
    {
        // Pastikan Notebook mati di awal game
        if (notebookPanel != null)
        {
            notebookPanel.SetActive(false);
        }
        isNotebookOpen = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleNotebook();
        }
    }

    public void ToggleNotebook()
    {
        if (notebookPanel == null) return;

        isNotebookOpen = !isNotebookOpen;
        notebookPanel.SetActive(isNotebookOpen);

        // Play SFX suara halaman buku (jika ada)
        if (audioSource != null)
        {
            AudioClip clipToPlay = isNotebookOpen ? openNotebookSFX : closeNotebookSFX;
            if (clipToPlay != null) audioSource.PlayOneShot(clipToPlay);
        }

        Debug.Log(isNotebookOpen ? "[NOTEBOOK TAB] Menampilkan Catatan Maintenance." : "[NOTEBOOK TAB] Menyimpan Catatan Maintenance.");
    }

    public bool IsNotebookOpen => isNotebookOpen;
}
