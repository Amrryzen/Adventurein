using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class RoomListUI : MonoBehaviourPunCallbacks
{
    [SerializeField] private Transform content;
    [SerializeField] private ItemRoom prefabItemRoom;
    [SerializeField] private PesanUI pesan;

    private readonly Dictionary<string, RoomInfo> rooms = new();

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        foreach (RoomInfo room in roomList)
        {
            if (room.RemovedFromList || !room.IsOpen || !room.IsVisible)
                rooms.Remove(room.Name);
            else
                rooms[room.Name] = room;
        }

        Render();
    }

    public override void OnJoinedLobby()
    {
        rooms.Clear();
        Render();
    }

    public override void OnLeftLobby()
    {
        rooms.Clear();
    }

    private void Render()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        foreach (RoomInfo room in rooms.Values)
        {
            ItemRoom item = Instantiate(prefabItemRoom, content);
            item.Setup(room, namaRoom => PhotonNetwork.JoinRoom(namaRoom), () => pesan.Tampilkan("Room sudah penuh!"));
        }
    }
}