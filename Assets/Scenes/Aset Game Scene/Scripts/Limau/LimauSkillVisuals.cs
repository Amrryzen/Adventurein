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
    [SerializeField] private ParticleSystem vfxSilk;
    [SerializeField] private Color warnaSilk = new Color(0.35f, 0.9f, 1f, 1f);
    [SerializeField, Min(0f)] private float jumlahPartikelSilk = 18f;

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

        if (vfxSilk == null)
            vfxSilk = BuatVfxSilk();

        if (vfxSilk != null)
        {
            vfxSilk.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            vfxSilk.transform.position = transform.position;
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
        AturPartikelSilk(true);
    }

    private void SaatSutraLepas()
    {
        sutraAktif = false;
        targetIkut = null;

        if (garis != null) garis.enabled = false;
        AturPartikelSilk(false);
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

        if (vfxSilk != null)
            vfxSilk.transform.position = ujung;
    }

    private void AturPartikelSilk(bool aktif)
    {
        if (vfxSilk == null) return;

        if (aktif)
        {
            vfxSilk.Play();
            var emission = vfxSilk.emission;
            emission.enabled = true;
        }
        else
        {
            var emission = vfxSilk.emission;
            emission.enabled = false;
            vfxSilk.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private ParticleSystem BuatVfxSilk()
    {
        GameObject objek = new GameObject("SilkParticles");
        objek.transform.SetParent(transform, true);

        ParticleSystem partikel = objek.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = partikel.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.35f;
        main.startSize = 0.08f;
        main.startColor = warnaSilk;
        main.maxParticles = 64;

        ParticleSystem.EmissionModule emission = partikel.emission;
        emission.enabled = false;
        emission.rateOverTime = jumlahPartikelSilk;

        ParticleSystem.ShapeModule shape = partikel.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystemRenderer renderer = partikel.GetComponent<ParticleSystemRenderer>();
        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null) shader = Shader.Find("Sprites/Default");
        if (shader != null) renderer.material = new Material(shader);

        return partikel;
    }
}
