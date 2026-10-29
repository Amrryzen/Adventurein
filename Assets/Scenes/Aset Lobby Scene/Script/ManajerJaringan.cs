using System.Text.RegularExpressions;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class ManajerJaringan : MonoBehaviourPunCallbacks
{
    private const int BatasMaxPemain = 4;

    [Header("Referensi")]
    [SerializeField] private PanelManager panels;
    [SerializeField] private PesanUI pesan;
    [SerializeField] private TextMeshProUGUI textStatus;

    [Header("Panel")]
    [SerializeField] private GameObject panelLogin;
    [SerializeField] private GameObject panelMenuRoom;

    [Header("Login")]
    [SerializeField] private TMP_InputField inputNama;

    [Header("Buat Room")]
    [SerializeField] private TMP_InputField inputNamaRoom;
    [SerializeField] private TMP_Dropdown dropdownMaxPlayer;

    private ClientState? statusTerakhir;

    private void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;

        if (!PhotonNetwork.IsConnected)
        {
            panels.Show(panelLogin.name);
            return;
        }

        if (PhotonNetwork.InLobby)
        {
            panels.Show(panelMenuRoom.name);
        }
        else if (!PhotonNetwork.InRoom && PhotonNetwork.IsConnectedAndReady)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void Update()
    {
        if (textStatus == null || PhotonNetwork.NetworkClientState == statusTerakhir) return;

        statusTerakhir = PhotonNetwork.NetworkClientState;
        textStatus.text = "Status Koneksi: " + statusTerakhir;
    }

    public void OnLoginButtonClicked()
    {
        string nama = inputNama.text.Trim();

        if (string.IsNullOrEmpty(nama))
        {
            pesan.Tampilkan("Nama pemain tidak boleh kosong!");
            return;
        }

        if (PhotonNetwork.IsConnected) return;

        PhotonNetwork.NickName = nama;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void OnCreateRoomButtonClicked()
    {
        string namaRoom = inputNamaRoom.text.Trim();
        if (string.IsNullOrEmpty(namaRoom))
            namaRoom = $"Room {Random.Range(1000, 9999)}";

        var options = new RoomOptions { MaxPlayers = (byte)AmbilMaxPemain() };
        PhotonNetwork.CreateRoom(namaRoom, options);
    }

    public void OnJoinRandomButtonClicked()
    {
        PhotonNetwork.JoinRandomRoom();
    }

    private int AmbilMaxPemain()
    {
        string teksOpsi = dropdownMaxPlayer.options[dropdownMaxPlayer.value].text;
        int.TryParse(Regex.Match(teksOpsi, @"\d+").Value, out int hasil);
        return Mathf.Clamp(hasil, 1, BatasMaxPemain);
    }

    public override void OnConnectedToMaster()
    {
        if (!PhotonNetwork.InLobby)
            PhotonNetwork.JoinLobby();
        else
            panels.Show(panelMenuRoom.name);
    }

    public override void OnJoinedLobby()
    {
        panels.Show(panelMenuRoom.name);
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        panels.Show(panelLogin.name);
        pesan.Tampilkan("Terputus: " + cause);
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        pesan.Tampilkan("Gagal membuat room: " + message);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        pesan.Tampilkan("Gagal join: " + message);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        pesan.Tampilkan("Tidak ada room yang tersedia.");
    }
}