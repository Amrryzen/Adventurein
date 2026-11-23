using UnityEngine;

/// <summary>
/// LAYER VISUAL (LineRenderer + Light + VFX). Berjalan di SEMUA client dengan mendengarkan
/// event LimauSkillNetwork, jadi pemain lokal dan remote melihat benang & cahaya yang sama.
/// Tidak berisi logika gameplay. Semua referensi bersifat opsional (null-safe).
/// </summary>
public class LimauSkillVisuals : MonoBehaviour
{
    [SerializeField] private LimauSkillNetwork jaringan;
    [SerializeField] private LimauSilkHandler sutra;
    [SerializeField] private LimauAura aura;

    [Header("Sutra (LineRenderer)")]
    [SerializeField] private LineRenderer garis;
    [Tooltip("Kecepatan benang 'meluncur' keluar (unit/detik).")]
    [SerializeField] private float kecepatanTembak = 40f;

    [Header("Bioluminesensi")]
    [SerializeField] private Light cahaya;
    [SerializeField] private float intensitasMaks = 3f;
    [SerializeField] private float kecepatanFade = 6f;
    [SerializeField] private ParticleSystem vfxAura;

    private bool sutraAktif;
    private Vector3 titikTujuan;
    private Transform targetIkut;
    private float panjangTampil;
    private float intensitasTarget;

    private void Awake()
    {
        if (jaringan == null) jaringan = GetComponent<LimauSkillNetwork>();
        if (sutra == null) sutra = GetComponent<LimauSilkHandler>();
        if (aura == null) aura = GetComponent<LimauAura>();

        if (garis != null)
        {
            garis.useWorldSpace = true;
            garis.positionCount = 2;
            garis.enabled = false;

            // Cadangan agar benang tidak magenta bila material belum diisi.
            if (garis.sharedMaterial == null)
            {
                Shader shader = Shader.Find("Sprites/Default");
                if (shader != null) garis.material = new Material(shader);
            }
        }

        if (cahaya != null)
        {
            cahaya.intensity = 0f;
            cahaya.enabled = false;
            if (aura != null) cahaya.range = aura.Radius;
        }
    }

    private void OnEnable()
    {
        if (jaringan == null) return;
        jaringan.SkillAktifBerubah += SaatSkillAktif;
        jaringan.SutraTerpasang += SaatSutraTerpasang;
        jaringan.SutraLepas += SaatSutraLepas;
    }

    private void OnDisable()
    {
        if (jaringan == null) return;
        jaringan.SkillAktifBerubah -= SaatSkillAktif;
        jaringan.SutraTerpasang -= SaatSutraTerpasang;
        jaringan.SutraLepas -= SaatSutraLepas;
    }

    private void SaatSkillAktif(LimauSkillType tipe, bool aktif)
    {
        if (tipe != LimauSkillType.Bioluminesensi) return;

        intensitasTarget = aktif ? intensitasMaks : 0f;

        if (vfxAura != null)
        {
            if (aktif) vfxAura.Play();
            else vfxAura.Stop();
        }
    }

    private void SaatSutraTerpasang(Vector3 titik, Transform target)
    {
        sutraAktif = true;
        titikTujuan = titik;
        targetIkut = target;
        panjangTampil = 0f; // mulai animasi meluncur

        if (garis != null) garis.enabled = true;
    }

    private void SaatSutraLepas()
    {
        sutraAktif = false;
        targetIkut = null;

        if (garis != null) garis.enabled = false;
    }

    private void Update()
    {
        if (cahaya == null || Mathf.Approximately(cahaya.intensity, intensitasTarget)) return;

        cahaya.intensity = Mathf.MoveTowards(cahaya.intensity, intensitasTarget, kecepatanFade * Time.deltaTime);
        cahaya.enabled = cahaya.intensity > 0.01f;
    }

    // LateUpdate: ikut posisi Limau/teman terbaru setelah semua pergerakan frame ini.
    private void LateUpdate()
    {
        if (!sutraAktif || garis == null || sutra == null) return;

        Vector3 asal = sutra.AsalSutra;
        Vector3 tujuan = targetIkut != null ? targetIkut.position : titikTujuan;

        Vector3 selisih = tujuan - asal;
        float panjang = selisih.magnitude;
        panjangTampil = Mathf.MoveTowards(panjangTampil, panjang, kecepatanTembak * Time.deltaTime);

        Vector3 ujung = panjang > 0.001f ? asal + selisih / panjang * Mathf.Min(panjangTampil, panjang) : asal;

        garis.SetPosition(0, asal);
        garis.SetPosition(1, ujung);
    }
}
