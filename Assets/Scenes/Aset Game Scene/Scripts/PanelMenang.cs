using System.Collections;
using TMPro;
using UnityEngine;

public class PanelMenang : MonoBehaviour
{
    public static PanelMenang Instance { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private TextMeshProUGUI teks;
    [SerializeField] private MenuPause menu;
    [SerializeField] private string pesan = "Kalian menang!";
    [SerializeField] private float jedaKeLobby = 7f;

    private void Awake()
    {
        Instance = this;
        PlayerController.PermainanSelesai = false;
        panel.SetActive(false);
    }

    public void Tampil()
    {
        if (PlayerController.PermainanSelesai) return;

        PlayerController.PermainanSelesai = true;
        teks.text = pesan;
        panel.SetActive(true);
        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayWin();
        StartCoroutine(KeLobby());
    }

    private IEnumerator KeLobby()
    {
        yield return new WaitForSeconds(jedaKeLobby);
        menu.KeLobby();
    }
}