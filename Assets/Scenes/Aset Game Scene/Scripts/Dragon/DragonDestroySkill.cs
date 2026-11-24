using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// LEVEL 2 - DESTROY (Q), sekarang dengan CHARGE.
///  - Tekan Q (tap): pukulan biasa ke objek 'Breakable' terdekat (seperti sebelumnya).
///  - TAHAN Q: mengisi tenaga (visual di semua client lewat status 'Destroy aktif'). Lepas = menghantam.
///  - Isi PENUH = HANTAMAN PENUH: gelombang kejut radius besar yang memecahkan SEMUA Breakable di sekitar
///    (termasuk batu 'keras') dan memingsankan musuh ber-IBisaDistun. Tidak butuh target di depan.
/// Cooldown dikenakan setelah menghantam; Hantaman Penuh punya cooldown lebih panjang.
/// </summary>
public class DragonDestroySkill : DragonSkillBase
{
    public override DragonSkillType Tipe => DragonSkillType.Destroy;

    [Header("Pukulan")]
    [SerializeField] private string tagBreakable = "Breakable";
    [SerializeField] private float jangkauan = 1.6f;
    [SerializeField] private float cooldown = 0.8f;
    [Tooltip("Jeda antara mulai animasi dan objek benar-benar hancur.")]
    [SerializeField] private float waktuTundaHit = 0.15f;

    [Header("Charge (tahan Q)")]
    [Tooltip("Detik menahan Q sampai tenaga penuh.")]
    [SerializeField] private float waktuIsiPenuh = 0.9f;

    [Header("Hantaman Penuh")]
    [SerializeField] private float radiusGelombang = 3.2f;
    [SerializeField] private float durasiDistun = 3f;
    [SerializeField] private float cooldownPenuh = 1.8f;

    private readonly Collider[] buffer = new Collider[32];
    private readonly HashSet<BreakableObject> batuTerkena = new();
    private readonly HashSet<IBisaDistun> musuhTerkena = new();

    private float cooldownSelesai;
    private float cooldownTotal = 1f;
    private bool mengisi;
    private float mulaiIsi;
    private Coroutine alurPukul;

    public bool Mengisi => mengisi;
    public float RadiusGelombang => radiusGelombang;

    /// <summary>0..1 selama mengisi (1 = penuh).</summary>
    public float IsiNormal =>
        mengisi && waktuIsiPenuh > 0f ? Mathf.Clamp01((Time.time - mulaiIsi) / waktuIsiPenuh) : 0f;

    /// <summary>1 = baru dipakai, 0 = siap. Berguna untuk UI cooldown.</summary>
    public float CooldownNormal =>
        cooldownTotal <= 0f ? 0f : Mathf.Clamp01((cooldownSelesai - Time.time) / cooldownTotal);

    public override void OnUpdateSkill()
    {
        if (mengisi)
        {
            // Input terkunci (pause/menang) membatalkan charge, bukan menghantam.
            if (PlayerController.InputDikunci || PlayerController.PermainanSelesai)
                BatalkanIsi();
            else if (!Masukan.DitahanSmash)
                Lepaskan();

            return;
        }

        if (!Masukan.DitekanSmash) return;

        if (Time.time < cooldownSelesai)
        {
            DragonDebug.Log($"Destroy ditolak: cooldown {(cooldownSelesai - Time.time):0.00} detik");
            return;
        }

        mengisi = true;
        mulaiIsi = Time.time;
        Jaringan.KirimSkillAktif(Tipe, true); // status 'mengisi' untuk visual di semua client
    }

    private void Lepaskan()
    {
        float tenaga = IsiNormal;
        bool penuh = tenaga >= 0.99f;

        mengisi = false;
        Jaringan.KirimSkillAktif(Tipe, false);

        BreakableObject target = null;
        bool adaTarget = Fisik.TryCariTerdekat(tagBreakable, jangkauan, out Collider kol);
        if (adaTarget)
        {
            target = kol.GetComponentInParent<BreakableObject>();
            if (target == null)
            {
                Debug.LogWarning($"'{kol.name}' bertag {tagBreakable} tetapi tidak punya komponen BreakableObject.", kol);
                adaTarget = false;
            }
        }

        if (!penuh && !adaTarget)
        {
            DragonDebug.Log("Destroy ditolak: tidak ada objek 'Breakable' dalam jangkauan");
            return;
        }

        Vector3 titik = adaTarget
            ? kol.bounds.ClosestPoint(Fisik.Pusat)
            : Fisik.Pusat + Vector3.right * (Masukan.Horizontal * 0.8f);

        // Batu keras + charge belum penuh: kena tapi tidak pecah (umpan balik, cooldown singkat).
        if (!penuh && !target.BisaDihancurkan(tenaga))
        {
            AturCooldown(0.4f);
            DragonDebug.Log($"'{kol.name}' terlalu keras: tahan Q sampai penuh untuk Hantaman Penuh");
            Jaringan.KirimPukulan(titik, 0f);
            return;
        }

        AturCooldown(penuh ? cooldownPenuh : cooldown);
        DragonDebug.Log(penuh
            ? "Destroy: HANTAMAN PENUH"
            : $"Destroy mengenai '{kol.name}' (tenaga {tenaga:0.00})");

        Jaringan.KirimPukulan(titik, tenaga);
        alurPukul = StartCoroutine(AlurPukul(target, tenaga, penuh));
    }

    private IEnumerator AlurPukul(BreakableObject target, float tenaga, bool penuh)
    {
        yield return new WaitForSeconds(waktuTundaHit);

        if (penuh) GelombangPenuh();
        else if (target != null) target.RequestHancurkan(tenaga);

        alurPukul = null;
    }

    private void GelombangPenuh()
    {
        batuTerkena.Clear();
        musuhTerkena.Clear();

        Vector3 pusat = Fisik.Pusat;
        int jumlah = Fisik.CariDalamRadius(radiusGelombang, buffer);

        for (int i = 0; i < jumlah; i++)
        {
            Collider c = buffer[i];
            if (c.transform.IsChildOf(transform)) continue;

            BreakableObject batu = c.GetComponentInParent<BreakableObject>();
            if (batu != null && batuTerkena.Add(batu))
                batu.RequestHancurkan(1f);

            IBisaDistun musuh = c.GetComponentInParent<IBisaDistun>();
            if (musuh != null && musuhTerkena.Add(musuh))
                musuh.Distun(durasiDistun, pusat);
        }
    }

    private void AturCooldown(float durasi)
    {
        cooldownTotal = durasi;
        cooldownSelesai = Time.time + durasi;
    }

    private void BatalkanIsi()
    {
        if (!mengisi) return;

        mengisi = false;
        Jaringan.KirimSkillAktif(Tipe, false);
    }

    public override void OnKeluar()
    {
        if (alurPukul != null) StopCoroutine(alurPukul);
        alurPukul = null;
        BatalkanIsi();
    }
}