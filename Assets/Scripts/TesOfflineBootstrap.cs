using Photon.Pun;
using UnityEngine;

public class TesOfflineBootstrap : MonoBehaviourPunCallbacks
{
    [SerializeField] private string karakterTes = "Limau";
    [SerializeField] private GameObject spawner;
    private bool spawnerSudahAktif;

    private void Awake()
    {
        PlayerController.InputDikunci = false;
        PlayerController.PermainanSelesai = false;
    }

    private void Start()
    {
        // Setelah restart (scene dimuat ulang) room offline masih aktif: spawner harus tetap dinyalakan.
        if (PhotonNetwork.InRoom)
        {
            AktifkanSpawner();
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            return;
        }

        PhotonNetwork.OfflineMode = true;
        PhotonNetwork.NickName = "Tester";
        KarakterPemain.Set(karakterTes);

        if (!PhotonNetwork.CreateRoom("tes"))
        {
            Debug.LogError("Room offline 'tes' gagal dibuat.");
        }
    }

    public override void OnJoinedRoom()
    {
        AktifkanSpawner();
    }

    private void AktifkanSpawner()
    {
        if (spawnerSudahAktif)
            return;

        if (spawner == null)
        {
            Debug.LogError("GameObject Spawner belum diatur pada TesOfflineBootstrap.", this);
            return;
        }

        spawner.SetActive(true);
        spawnerSudahAktif = true;
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"Room offline 'tes' gagal dibuat ({returnCode}): {message}", this);
    }
}