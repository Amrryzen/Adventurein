using System;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ItemRoom : MonoBehaviour
{
    public TextMeshProUGUI textNama;
    public TextMeshProUGUI textSlot;
    public Button tombolJoin;
    public Button tombolFull;

    public void Setup(RoomInfo room, Action<string> onJoin, Action onFull)
    {
        bool penuh = room.PlayerCount >= room.MaxPlayers;

        textNama.text = room.Name;
        textSlot.text = room.PlayerCount + "/" + room.MaxPlayers;

        tombolJoin.gameObject.SetActive(!penuh);
        tombolFull.gameObject.SetActive(penuh);

        string namaRoom = room.Name;
        tombolJoin.onClick.RemoveAllListeners();
        tombolJoin.onClick.AddListener(() => onJoin(namaRoom));

        tombolFull.onClick.RemoveAllListeners();
        tombolFull.onClick.AddListener(() => onFull());

        if (GameAudio.Instance != null)
        {
            GameAudio.Instance.RegisterButton(tombolJoin);
            GameAudio.Instance.RegisterButton(tombolFull);
        }
    }
}