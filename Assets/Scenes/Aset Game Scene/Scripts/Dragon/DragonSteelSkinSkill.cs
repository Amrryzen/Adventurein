using UnityEngine;

/// <summary>
/// LEVEL 3 - STEEL SKIN (C), sekarang dengan STAMINA dan PARRY.
///  - Dragon membeku dan massanya naik drastis: kebal angin/patukan, jadi tameng statis (toggle atau hold).
///  - STAMINA: terkuras selama aktif, terisi lagi saat nonaktif (setelah jeda). Habis = baja retak, lepas paksa.
///    Jadi Dragon tidak bisa jadi tameng selamanya; tim harus mengatur waktu bersembunyi.
///  - PARRY: bila Steel Skin baru diaktifkan tepat sebelum serangan mendarat (jendela parry),
///    serangan ditangkis total: musuh pingsan dan sebagian stamina kembali. Blok biasa menguras stamina.
/// Hazard (mis. BurungPelatuk) memanggil Terima(...) di client Dragon.
/// </summary>
public class DragonSteelSkinSkill : DragonSkillBase
{
    public enum HasilBlok { Gagal, Blok, Parry }

    public override DragonSkillType Tipe => DragonSkillType.SteelSkin;

    [SerializeField] private bool modeToggle = true;
    [Tooltip("Jika false, Steel Skin hanya bisa diaktifkan saat Dragon menyentuh tanah.")]
    [SerializeField] private bool bolehDiUdara = false;
    [SerializeField] private float pengaliMassa = 50f;

    [Header("Stamina (satuan: detik bertahan)")]
    [SerializeField, Min(0.5f)] private float staminaMaks = 5f;
    [SerializeField] private float isiUlangPerDetik = 1.2f;
    [Tooltip("Jeda setelah dilepas sebelum stamina mulai terisi.")]
    [SerializeField] private float jedaIsiUlang = 0.8f;
    [Tooltip("Stamina minimal untuk bisa mengaktifkan Steel Skin.")]
    [SerializeField] private float staminaMinimum = 1f;

    [Header("Blok & Parry")]
    [Tooltip("Detik setelah aktivasi di mana serangan dihitung PARRY.")]
    [SerializeField] private float jendelaParry = 0.35f;
    [SerializeField] private float refundParry = 2f;
    [SerializeField] private float biayaBlok = 0.6f;

    private float stamina;
    private float waktuAktif;
    private float isiUlangMulai;

    public bool Aktif { get; private set; }
    public float StaminaNormal => staminaMaks <= 0f ? 0f : Mathf.Clamp01(stamina / staminaMaks);
    public bool JendelaParryAktif => Aktif && Time.time - waktuAktif <= jendelaParry;

    private void Awake()
    {
        stamina = staminaMaks;
    }

    public override void OnUpdateSkill()
    {
        if (modeToggle)
        {
            if (Masukan.DitekanSteelSkin) Atur(!Aktif);
        }
        else
        {
            if (Masukan.DitekanSteelSkin) Atur(true);
            else if (Masukan.DilepasSteelSkin) Atur(false);
        }

        float dt = Time.deltaTime;
        if (Aktif)
        {
            stamina -= dt;
            if (stamina <= 0f)
            {
                stamina = 0f;
                DragonDebug.Log("Steel Skin retak: stamina habis");
                Atur(false);
            }
        }
        else if (Time.time >= isiUlangMulai)
        {
            stamina = Mathf.Min(staminaMaks, stamina + isiUlangPerDetik * dt);
        }
    }

    public override void OnKeluar()
    {
        Atur(false);
    }

    /// <summary>
    /// Dipanggil hazard di client Dragon saat serangan mendarat. Parry = tepat setelah aktivasi.
    /// </summary>
    public HasilBlok Terima(Vector3 titikSerangan)
    {
        if (!Aktif) return HasilBlok.Gagal;

        if (JendelaParryAktif)
        {
            stamina = Mathf.Min(staminaMaks, stamina + refundParry);
            Jaringan.KirimParry(titikSerangan);
            DragonDebug.Log("Steel Skin: PARRY!");
            return HasilBlok.Parry;
        }

        stamina = Mathf.Max(0f, stamina - biayaBlok);
        DragonDebug.Log("Steel Skin: serangan diblok");
        return HasilBlok.Blok;
    }

    private void Atur(bool aktif)
    {
        if (aktif == Aktif) return;

        if (aktif)
        {
            if (!bolehDiUdara && !Fisik.MenyentuhTanah())
            {
                DragonDebug.Log("Steel Skin ditolak: Dragon di udara");
                return;
            }

            if (stamina < staminaMinimum)
            {
                DragonDebug.Log($"Steel Skin belum siap: stamina {stamina:0.0}/{staminaMaks:0.0}");
                return;
            }

            waktuAktif = Time.time;
        }
        else
        {
            isiUlangMulai = Time.time + jedaIsiUlang;
        }

        Aktif = aktif;
        Fisik.KunciBadan(aktif, pengaliMassa);
        Jaringan.KirimSkillAktif(Tipe, aktif);
    }
}