using UnityEngine;

/// <summary>
/// BIOLUMINESENSI (toggle Q).
/// Hanya mengirim status ON/OFF. Cahaya diatur LimauSkillVisuals, sedangkan aura pengusir
/// diatur LimauAura; keduanya berjalan di semua client dari event jaringan yang sama.
/// </summary>
public class LimauBioluminesensiSkill : LimauSkillBase
{
    public override LimauSkillType Tipe => LimauSkillType.Bioluminesensi;

    public bool Aktif { get; private set; }

    public override void OnUpdateSkill()
    {
        if (Masukan.DitekanBioluminesensi) Atur(!Aktif);
    }

    public override void OnKeluar()
    {
        Atur(false);
    }

    private void Atur(bool aktif)
    {
        if (aktif == Aktif) return;

        Aktif = aktif;
        Jaringan.KirimSkillAktif(Tipe, aktif);
    }
}
