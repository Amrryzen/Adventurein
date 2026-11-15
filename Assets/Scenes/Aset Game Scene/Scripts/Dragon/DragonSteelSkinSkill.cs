using UnityEngine;

/// <summary>
/// LEVEL 3 - STEEL SKIN.
/// Dragon membeku di tempat dan massanya naik drastis sehingga tidak terdorong angin / patukan burung,
/// berfungsi sebagai tameng statis untuk pemain 2. Mode toggle (tekan E) atau hold (tahan E).
/// </summary>
public class DragonSteelSkinSkill : DragonSkillBase
{
    public override DragonSkillType Tipe => DragonSkillType.SteelSkin;

    [SerializeField] private bool modeToggle = true;
    [Tooltip("Jika false, Steel Skin hanya bisa diaktifkan saat Dragon menyentuh tanah.")]
    [SerializeField] private bool bolehDiUdara = false;
    [SerializeField] private float pengaliMassa = 50f;

    public bool Aktif { get; private set; }

    public override void OnUpdateSkill()
    {
        if (modeToggle)
        {
            if (Masukan.Ditekan) Atur(!Aktif);
        }
        else
        {
            if (Masukan.Ditekan) Atur(true);
            else if (Masukan.Dilepas) Atur(false);
        }
    }

    public override void OnKeluar()
    {
        Atur(false);
    }

    private void Atur(bool aktif)
    {
        if (aktif == Aktif) return;
        if (aktif && !bolehDiUdara && !Fisik.MenyentuhTanah()) return;

        Aktif = aktif;
        Fisik.KunciBadan(aktif, pengaliMassa);
        Jaringan.KirimSkillAktif(Tipe, aktif);

        // OPSIONAL (butuh patch kecil di PlayerController, lihat penjelasan):
        // GetComponent<PlayerController>().GerakDikunci = aktif;
    }
}
