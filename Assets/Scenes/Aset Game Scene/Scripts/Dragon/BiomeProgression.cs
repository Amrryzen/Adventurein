using System;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// Menyimpan level bioma saat ini (1-3) dan menyinkronkannya ke semua pemain.
/// Pola sama seperti DoorController: client -> MasterClient (validasi) -> semua client.
/// Letakkan satu di scene (GameObject kosong + PhotonView).
/// </summary>
[RequireComponent(typeof(PhotonView))]
public class BiomeProgression : MonoBehaviourPunCallbacks
{
    private const int LevelMaks = 3;

    public static BiomeProgression Instance { get; private set; }

    [SerializeField, Range(1, LevelMaks)] private int levelAwal = 1;

    public int LevelSaatIni { get; private set; }
    public event Action<int> LevelBerubah;

    private void Awake()
    {
        Instance = this;
        LevelSaatIni = levelAwal;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Boleh dipanggil dari client mana pun.</summary>
    public void MintaGantiLevel(int level)
    {
        level = Mathf.Clamp(level, 1, LevelMaks);
        if (level == LevelSaatIni) return;

        photonView.RPC(nameof(RPC_MintaLevel), RpcTarget.MasterClient, level);
    }

    [PunRPC]
    private void RPC_MintaLevel(int level, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (info.Sender == null) return;
        if (level < 1 || level > LevelMaks || level == LevelSaatIni) return;

        // Buffered: pemain yang masuk belakangan menerima level terakhir.
        photonView.RPC(nameof(RPC_SetLevel), RpcTarget.AllBuffered, level);
    }

    [PunRPC]
    private void RPC_SetLevel(int level)
    {
        if (level < 1 || level > LevelMaks) return;
        LevelSaatIni = level;
        LevelBerubah?.Invoke(level);
    }

    public override void OnMasterClientSwitched(Photon.Realtime.Player pemainBaru)
    {
        if (!PhotonNetwork.IsMasterClient) return;
        photonView.RPC(nameof(RPC_SetLevel), RpcTarget.AllBuffered, LevelSaatIni);
    }
}
