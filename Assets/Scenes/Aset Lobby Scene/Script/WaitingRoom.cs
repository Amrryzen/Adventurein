using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WaitingRoom : MonoBehaviourPunCallbacks
{
    [Header("Referensi")]
    [SerializeField] private PanelManager panels;
    [SerializeField] private GameObject panelRuangTunggu;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI textNamaRoom;
    [SerializeField] private Transform containerSlot;
    [SerializeField] private PlayerSlotUI prefabSlot;
    [SerializeField] private Button tombolStart;

    [Header("Game")]
    [SerializeField] private string namaSceneGame = "Game Scene";
    [SerializeField] private int minimalPemain = 2;

    private readonly Dictionary<int, PlayerSlotUI> slots = new();

    public override void OnJoinedRoom()
    {
        panels.Show(panelRuangTunggu.name);
        BersihkanSlot();

        foreach (Player p in PhotonNetwork.PlayerList)
            TambahSlot(p);

        RefreshInfo();
    }

    public override void OnLeftRoom()
    {
        BersihkanSlot();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        TambahSlot(newPlayer);
        RefreshInfo();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (slots.TryGetValue(otherPlayer.ActorNumber, out PlayerSlotUI slot))
        {
            slots.Remove(otherPlayer.ActorNumber);
            slot.Hilangkan();
        }

        RefreshInfo();
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        foreach (PlayerSlotUI slot in slots.Values)
            slot.Refresh();

        RefreshInfo();
    }

    public void OnStartButtonClicked()
    {
        if (!PhotonNetwork.IsMasterClient) return;
        if (PhotonNetwork.CurrentRoom.PlayerCount < minimalPemain) return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.LoadLevel(namaSceneGame);
    }

    public void OnLeaveButtonClicked()
    {
        PhotonNetwork.LeaveRoom();
    }

    private void TambahSlot(Player player)
    {
        if (slots.ContainsKey(player.ActorNumber)) return;

        PlayerSlotUI slot = Instantiate(prefabSlot, containerSlot);
        slot.Bind(player);
        slots[player.ActorNumber] = slot;
    }

    private void BersihkanSlot()
    {
        foreach (PlayerSlotUI slot in slots.Values)
            if (slot != null) Destroy(slot.gameObject);

        slots.Clear();
    }

    private void RefreshInfo()
    {
        if (!PhotonNetwork.InRoom) return;

        Room room = PhotonNetwork.CurrentRoom;
        textNamaRoom.text = $"{room.Name}  |  Player {room.PlayerCount}/{room.MaxPlayers}";

        tombolStart.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        tombolStart.interactable = room.PlayerCount >= minimalPemain;
    }
}