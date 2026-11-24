using System.Collections;
using UnityEngine;

/// <summary>
/// LEVEL 3 - GRAPPLING SILK.
/// Tekan C untuk menembak sutra ke arah bidik. Tahan klik kanan untuk membidik dengan mouse.
///  - Mengenai objek bertag "GrapplePoint": Limau BERAYUN (SpringJoint lokal di client Limau).
///  - Mengenai teman (komponen TargetSutra): teman DITARIK ke Limau (fisikanya dijalankan
///    di client teman, dipicu lewat RPC).
/// Tekan E lagi (atau Lompat saat berayun) untuk melepas.
/// </summary>
public class LimauSilkSkill : LimauSkillBase
{
    public override LimauSkillType Tipe => LimauSkillType.GrapplingSilk;

    [Header("Umum")]
    [SerializeField] private float cooldownTembak = 0.4f;
    [SerializeField] private bool lepasDenganLompat = true;
    [Tooltip("Setelah sutra dilepas, kontrol fisika ditahan sebentar agar momentum ayunan tidak langsung hilang.")]
    [SerializeField] private float jedaMomentum = 0.25f;

    [Header("Menarik teman")]
    [SerializeField] private float kecepatanTarik = 6f;
    [Tooltip("Sutra otomatis lepas saat teman sudah sedekat ini.")]
    [SerializeField] private float jarakSelesaiTarik = 1.5f;
    [SerializeField] private float waktuMaksTarik = 5f;

    private bool terpasang;
    private bool menarikTeman;
    private Transform temanTransform;
    private TargetSutra jangkar;
    private bool adaJangkar;
    private float waktuPasang;
    private float cooldownSelesai;
    private Coroutine alurMomentum;

    public override void OnUpdateSkill()
    {
        if (terpasang)
        {
            bool lepas = Masukan.DitekanGrapplingSilk ||
                         (lepasDenganLompat && Masukan.LompatDitekan);

            // Jangkar (Dragon Steel Skin) melepas kekokohannya / hilang -> Limau ikut lepas.
            if (adaJangkar && (jangkar == null || !jangkar.SedangKokoh))
                lepas = true;

            // Tarikan selesai: teman sudah dekat / hilang / kelamaan.
            if (menarikTeman &&
                (temanTransform == null ||
                 Vector3.Distance(Sutra.AsalSutra, temanTransform.position) <= jarakSelesaiTarik ||
                 Time.time - waktuPasang > waktuMaksTarik))
                lepas = true;

            if (lepas) Lepas();
            return;
        }

        if (!Masukan.DitekanGrapplingSilk || Time.time < cooldownSelesai) return;

        Vector2 arah = Masukan.HitungArahBidik(Sutra.AsalSutra);
        if (!Sutra.TryBidik(arah, out LimauSilkHandler.HasilBidik hasil))
        {
            cooldownSelesai = Time.time + cooldownTembak;
            Debug.Log("[Limau] Sutra meleset: tidak ada titik gantung/teman di arah bidik.", this);
            return;
        }

        Pasang(hasil);
    }

    public override void OnFixedUpdateSkill()
    {
        if (terpasang && !menarikTeman)
            Sutra.UpdateAyunan(Masukan.Horizontal, Masukan.Vertikal);
    }

    public override void OnKeluar()
    {
        if (terpasang) Lepas();

        // Mati / ganti level: kembalikan kontrol SEKARANG, tanpa menunggu jeda momentum.
        if (alurMomentum != null) StopCoroutine(alurMomentum);
        alurMomentum = null;
        Sutra.SetKontrolFisika(false);
    }

    private void Pasang(LimauSilkHandler.HasilBidik hasil)
    {
        terpasang = true;
        waktuPasang = Time.time;
        menarikTeman = hasil.teman != null;
        temanTransform = menarikTeman ? hasil.teman.TitikTambat : null;
        jangkar = hasil.jangkar;
        adaJangkar = jangkar != null;

        if (alurMomentum != null)
        {
            StopCoroutine(alurMomentum);
            alurMomentum = null;
        }

        if (!menarikTeman)
            Sutra.PasangAyunan(hasil.titik);

        Jaringan.KirimSutraTerpasang(
            hasil.titik,
            menarikTeman ? hasil.teman.photonView : null,
            menarikTeman ? kecepatanTarik : 0f);
    }

    private void Lepas()
    {
        bool adaAyunan = Sutra.Mengayun;

        terpasang = false;
        menarikTeman = false;
        temanTransform = null;
        jangkar = null;
        adaJangkar = false;
        cooldownSelesai = Time.time + cooldownTembak;

        Sutra.LepasAyunan();
        Jaringan.KirimSutraLepas();

        if (adaAyunan)
            alurMomentum = StartCoroutine(KembalikanKontrol());
    }

    private IEnumerator KembalikanKontrol()
    {
        yield return new WaitForSeconds(jedaMomentum);
        Sutra.SetKontrolFisika(false);
        alurMomentum = null;
    }
}