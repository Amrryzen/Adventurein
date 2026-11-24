using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// LAYER 3 - NETWORK SYNC (PUN 2).
/// Satu-satunya skrip skill yang memanggil RPC. Pola: Owner menerapkan efek lokal langsung,
/// lalu mengirim RPC ke pemain lain (RpcTarget.Others). Pemain lain hanya menerima event
/// untuk visual/status; mereka TIDAK menjalankan logika fisika skill.
/// Pemain yang bergabung belakangan menerima status aktif lewat OnPlayerEnteredRoom
/// (tidak memakai buffered RPC agar tidak menumpuk).
/// </summary>
public class DragonSkillNetwork : MonoBehaviourPunCallbacks
{
    /// <summary>(jenis skill, aktif?) - dipicu di owner DAN remote.</summary>
    public event Action<DragonSkillType, bool> SkillAktifBerubah;

    /// <summary>Titik dunia tempat pukulan mengenai target - dipicu di owner DAN remote.</summary>
    public event Action<Vector3> PukulanDipicu;

    /// <summary>Pukulan + tenaga isi (0..1; 1 = Hantaman Penuh) - dipicu di owner DAN remote.</summary>
    public event Action<Vector3, float> PukulanBerkekuatan;

    /// <summary>Steel Skin berhasil mem-parry serangan di titik ini - dipicu di owner DAN remote.</summary>
    public event Action<Vector3> ParryDipicu;

    private readonly bool[] statusAktif = new bool[4];

    public bool IsAktif(DragonSkillType tipe)
    {
        int i = (int)tipe;
        return i >= 1 && i < statusAktif.Length && statusAktif[i];
    }

    // ---------------- API untuk owner ----------------

    public void KirimSkillAktif(DragonSkillType tipe, bool aktif)
    {
        if (!photonView.IsMine) return;

        Terapkan(tipe, aktif); // efek lokal langsung (tanpa menunggu jaringan)
        photonView.RPC(nameof(RPC_SkillAktif), RpcTarget.Others, (int)tipe, aktif);
    }

    public void KirimPukulan(Vector3 titik) => KirimPukulan(titik, 0f);

    public void KirimPukulan(Vector3 titik, float tenaga)
    {
        if (!photonView.IsMine) return;

        PukulanDipicu?.Invoke(titik);
        PukulanBerkekuatan?.Invoke(titik, tenaga);
        photonView.RPC(nameof(RPC_Pukulan), RpcTarget.Others, titik, tenaga);
    }

    public void KirimParry(Vector3 titik)
    {
        if (!photonView.IsMine) return;

        ParryDipicu?.Invoke(titik);
        photonView.RPC(nameof(RPC_Parry), RpcTarget.Others, titik);
    }

    // ---------------- Penerima RPC ----------------

    [PunRPC]
    private void RPC_SkillAktif(int tipe, bool aktif, PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        Terapkan((DragonSkillType)tipe, aktif);
    }

    [PunRPC]
    private void RPC_Pukulan(Vector3 titik, float tenaga, PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        PukulanDipicu?.Invoke(titik);
        PukulanBerkekuatan?.Invoke(titik, tenaga);
    }

    [PunRPC]
    private void RPC_Parry(Vector3 titik, PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        ParryDipicu?.Invoke(titik);
    }

    // ---------------- Internal ----------------

    private bool PengirimAdalahOwner(PhotonMessageInfo info)
    {
        return info.Sender != null && info.Sender.ActorNumber == photonView.OwnerActorNr;
    }

    private void Terapkan(DragonSkillType tipe, bool aktif)
    {
        int i = (int)tipe;
        if (i < 1 || i >= statusAktif.Length) return;
        if (statusAktif[i] == aktif) return;

        statusAktif[i] = aktif;
        SkillAktifBerubah?.Invoke(tipe, aktif);
    }

    /// <summary>Kirim status skill yang sedang aktif ke pemain yang baru masuk room.</summary>
    public override void OnPlayerEnteredRoom(Player pemainBaru)
    {
        if (!photonView.IsMine) return;

        for (int i = 1; i < statusAktif.Length; i++)
        {
            if (statusAktif[i])
                photonView.RPC(nameof(RPC_SkillAktif), pemainBaru, i, true);
        }
    }
}