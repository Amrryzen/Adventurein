using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuPause : MonoBehaviourPunCallbacks
{
    [SerializeField] private GameObject panel;
    [SerializeField] private string sceneLobby = "Lobby";

    private void Awake()
    {
        PlayerController.InputDikunci = false;
        panel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Atur(!panel.activeSelf);
    }

    public void Lanjut()
    {
        Atur(false);
    }

    public void Keluar()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void KeLobby()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else
        {
            SceneManager.LoadScene(sceneLobby);
        }
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(sceneLobby);
    }

    private void Atur(bool buka)
    {
        panel.SetActive(buka);
        PlayerController.InputDikunci = buka;
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PlayerController.InputDikunci = false;
    }
}