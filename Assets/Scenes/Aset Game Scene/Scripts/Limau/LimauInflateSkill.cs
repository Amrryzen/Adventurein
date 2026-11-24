using UnityEngine;

/// <summary>
/// LEVEL 1 - INFLATE.
/// Toggle (tekan E) atau hold (tahan E). Limau mengembang; bentuk fisiknya diterapkan oleh
/// LimauPhysicsHandler di semua client. Pantulan untuk Dragon ada di LimauBantal.
/// </summary>
public class LimauInflateSkill : LimauSkillBase
{
    public override LimauSkillType Tipe => LimauSkillType.Inflate;

    [SerializeField] private bool modeToggle = true;

    public bool Aktif { get; private set; }

    public override void OnUpdateSkill()
    {
        if (modeToggle)
        {
            if (Masukan.DitekanInflate) Atur(!Aktif);
        }
        else
        {
            if (Masukan.DitekanInflate) Atur(true);
            else if (Masukan.DilepasInflate) Atur(false);
        }
    }

    public override void OnKeluar()
    {
        Atur(false);
    }

    private void Atur(bool aktif)
    {
        if (aktif == Aktif) return;

        if (aktif && !Fisik.BisaMengembang())
        {
            Debug.Log("[Limau] Inflate ditolak: ruang tidak cukup untuk mengembang.", this);
            return;
        }

        Aktif = aktif;
        Jaringan.KirimSkillAktif(Tipe, aktif);
    }
}
