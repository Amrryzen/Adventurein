using Photon.Pun;
using UnityEngine;

public class TesOfflineBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] private string karakter = "Dragon";
    [SerializeField] private GameObject spawner;

    private void Start()
    {
        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.NickName = "Tester";
        KarakterPemain.Set(karakter);

        if (!PhotonNetwork.CreateRoom("tes"))
        {
            Debug.LogError("Room offline 'tes' gagal dibuat.");
        }
    }

    public override void OnJoinedRoom()
    {
        if (spawner == null)
        {
            Debug.LogError("GameObject Spawner belum diatur pada TesOfflineBootstrap.", this);
            return;
        }

        spawner.SetActive(true);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room offline 'tes' gagal dibuat ({returnCode}): {message}", this);
    }
}
