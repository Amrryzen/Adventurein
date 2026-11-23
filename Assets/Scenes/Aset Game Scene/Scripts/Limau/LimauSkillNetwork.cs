using System;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

/// <summary>
/// LAYER 3 - NETWORK SYNC (PUN 2).
/// Satu-satunya skrip skill Limau yang memanggil RPC. Pola: owner menerapkan efek lokal langsung,
/// lalu RPC ke pemain lain (RpcTarget.Others). Semua client (owner + remote) menerima EVENT yang sama
/// sehingga handler fisika bentuk, visual, dan aura memakai satu jalur.
/// Late join: status aktif dikirim ulang ke pemain baru lewat OnPlayerEnteredRoom.
/// </summary>
public class LimauSkillNetwork : MonoBehaviourPunCallbacks
{
    /// <summary>Inflate / Bioluminesensi aktif atau nonaktif.</summary>
    public event Action<LimauSkillType, bool> SkillAktifBerubah;

    /// <summary>Sutra terpasang. Transform non-null = sutra menempel ke teman (mengikuti geraknya).</summary>
    public event Action<Vector3, Transform> SutraTerpasang;

    public event Action SutraLepas;

    private readonly bool[] statusAktif = new bool[4];
    private bool sutraAktif;
    private Vector3 titikSutra;
    private int idTargetSutra;
    private TargetSutra targetDitarik; // hanya terisi di client pemilik teman yang sedang ditarik

    public bool IsAktif(LimauSkillType tipe) =>
        tipe == LimauSkillType.GrapplingSilk ? sutraAktif : statusAktif[(int)tipe];

    // ---------------- API untuk owner ----------------

    public void KirimSkillAktif(LimauSkillType tipe, bool aktif)
    {
        if (!photonView.IsMine) return;

        TerapkanSkillAktif(tipe, aktif);
        photonView.RPC(nameof(RPC_SkillAktif), RpcTarget.Others, (int)tipe, aktif);
    }

    /// <param name="target">PhotonView teman yang ditarik; null untuk titik gantung statis.</param>
    /// <param name="kecepatanTarik">&gt; 0 = teman ditarik ke Limau (dijalankan di client teman).</param>
    public void KirimSutraTerpasang(Vector3 titik, PhotonView target, float kecepatanTarik)
    {
        if (!photonView.IsMine) return;

        int id = target != null ? target.ViewID : 0;
        TerapkanSutraTerpasang(titik, id, kecepatanTarik);
        photonView.RPC(nameof(RPC_SutraTerpasang), RpcTarget.Others, titik, id, kecepatanTarik);
    }

    public void KirimSutraLepas()
    {
        if (!photonView.IsMine) return;

        TerapkanSutraLepas();
        photonView.RPC(nameof(RPC_SutraLepas), RpcTarget.Others);
    }

    // ---------------- Penerima RPC ----------------

    [PunRPC]
    private void RPC_SkillAktif(int tipe, bool aktif, PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        TerapkanSkillAktif((LimauSkillType)tipe, aktif);
    }

    [PunRPC]
    private void RPC_SutraTerpasang(Vector3 titik, int idTarget, float kecepatanTarik, PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        TerapkanSutraTerpasang(titik, idTarget, kecepatanTarik);
    }

    [PunRPC]
    private void RPC_SutraLepas(PhotonMessageInfo info)
    {
        if (!PengirimAdalahOwner(info)) return;
        TerapkanSutraLepas();
    }

    // ---------------- Internal ----------------

    private bool PengirimAdalahOwner(PhotonMessageInfo info)
    {
        return info.Sender != null && info.Sender.ActorNumber == photonView.OwnerActorNr;
    }

    private void TerapkanSkillAktif(LimauSkillType tipe, bool aktif)
    {
        int i = (int)tipe;
        if (i < 1 || i > 2) return; // GrapplingSilk punya jalur sendiri
        if (statusAktif[i] == aktif) return;

        statusAktif[i] = aktif;
        SkillAktifBerubah?.Invoke(tipe, aktif);
    }

    private void TerapkanSutraTerpasang(Vector3 titik, int idTarget, float kecepatanTarik)
    {
        sutraAktif = true;
        titikSutra = titik;
        idTargetSutra = idTarget;

        Transform tujuan = null;
        if (idTarget != 0)
        {
            PhotonView tv = PhotonView.Find(idTarget);
            if (tv != null)
            {
                TargetSutra ts = tv.GetComponent<TargetSutra>();
                tujuan = ts != null ? ts.TitikTambat : tv.transform;

                // Hanya client PEMILIK teman yang menjalankan tarikan (fisikanya ada di sana).
                if (ts != null && kecepatanTarik > 0f && tv.IsMine && ts.MulaiTarik(transform, kecepatanTarik))
                    targetDitarik = ts;
            }
        }

        SutraTerpasang?.Invoke(titik, tujuan);
    }

    private void TerapkanSutraLepas()
    {
        if (!sutraAktif && targetDitarik == null) return;

        sutraAktif = false;
        idTargetSutra = 0;

        if (targetDitarik != null)
        {
            targetDitarik.Berhenti();
            targetDitarik = null;
        }

        SutraLepas?.Invoke();
    }

    private void OnDestroy()
    {
        // Limau keluar/hancur saat teman sedang ditarik -> bebaskan teman.
        if (targetDitarik != null) targetDitarik.Berhenti();
    }

    /// <summary>Kirim status yang sedang aktif ke pemain yang baru masuk room.</summary>
    public override void OnPlayerEnteredRoom(Player pemainBaru)
    {
        if (!photonView.IsMine) return;

        for (int i = 1; i <= 2; i++)
        {
            if (statusAktif[i])
                photonView.RPC(nameof(RPC_SkillAktif), pemainBaru, i, true);
        }

        // kecepatan 0 = hanya visual, jangan memulai tarikan baru.
        if (sutraAktif)
            photonView.RPC(nameof(RPC_SutraTerpasang), pemainBaru, titikSutra, idTargetSutra, 0f);
    }
}
