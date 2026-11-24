using System.Collections;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// "JUICE" Dragon: squash &amp; stretch (GDD: animasi 'squash and stretch yang kental'), getar kamera,
/// hit-stop singkat, debu/serpihan/percikan, dan slot SFX. Murni reaksi atas event DragonSkillNetwork,
/// jadi berjalan di SEMUA client dan tidak mengubah logika gameplay.
/// Semua referensi opsional. Hapus komponen ini untuk mematikan semuanya.
/// </summary>
public class DragonJuice : MonoBehaviour
{
    [Header("Squash & Stretch")]
    [Tooltip("Root visual yang diskalakan. Kosong = dicari otomatis (lebih baik isi manual dengan root model).")]
    [SerializeField] private Transform model;
    [SerializeField] private float kekakuan = 240f;
    [SerializeField] private float redaman = 13f;

    [Header("Getar kamera & hit-stop")]
    [SerializeField] private bool getarKamera = true;
    [SerializeField] private bool hitStop = true;
    [SerializeField] private float jarakGetarMaks = 14f;

    [Header("Audio (opsional, kosong = senyap)")]
    [SerializeField] private AudioClip sfxDorong;       // loop saat mendorong
    [SerializeField] private AudioClip sfxMengisi;
    [SerializeField] private AudioClip sfxPukul;        // renyah (crunchy)
    [SerializeField] private AudioClip sfxPukulPenuh;
    [SerializeField] private AudioClip sfxBajaAktif;
    [SerializeField] private AudioClip sfxBajaLepas;
    [SerializeField] private AudioClip sfxParry;

    private DragonSkillNetwork jaringan;
    private DragonDestroySkill destroySkill;
    private PhotonView view;
    private CapsuleCollider kapsul;

    private Vector3 skalaAsal = Vector3.one;
    private Vector3 offset;
    private Vector3 kecepatanOffset;
    private Vector3 targetOffset;

    private bool pushAktif;
    private bool bajaAktif;
    private bool mengisi;

    private AudioSource sumberEfek;
    private AudioSource sumberLoop;
    private ParticleSystem debu;
    private ParticleSystem serpihan;
    private ParticleSystem percikan;
    private bool hitStopBerjalan;

    private void Awake()
    {
        jaringan = GetComponent<DragonSkillNetwork>();
        destroySkill = GetComponent<DragonDestroySkill>();
        view = GetComponent<PhotonView>();
        kapsul = GetComponent<CapsuleCollider>();

        if (model == null) model = CariModel();
        if (model != null) skalaAsal = model.localScale;
        else Debug.LogWarning("DragonJuice: 'model' tidak ditemukan, squash & stretch dilewati.", this);

        sumberEfek = BuatSumber(false);
        sumberLoop = BuatSumber(true);

        debu = FabrikEfek.BuatPartikel("Juice Debu", transform, new Color(0.78f, 0.68f, 0.5f, 0.85f), 0.24f, 0.55f, 0.2f, 1f, -0.02f);
        serpihan = FabrikEfek.BuatPartikel("Juice Serpihan", transform, new Color(0.55f, 0.42f, 0.3f, 1f), 0.2f, 0.8f, 2.5f, 6f, 1.6f);
        percikan = FabrikEfek.BuatPartikel("Juice Percikan", transform, new Color(1f, 0.9f, 0.5f, 1f), 0.12f, 0.35f, 3f, 8f, 0.4f);
    }

    private void Start()
    {
        if (getarKamera && !CameraGetar.Ada && Camera.main != null)
            Camera.main.gameObject.AddComponent<CameraGetar>();
    }

    private void OnEnable()
    {
        if (jaringan == null) return;
        jaringan.SkillAktifBerubah += SaatSkillAktif;
        jaringan.PukulanBerkekuatan += SaatPukulan;
        jaringan.ParryDipicu += SaatParry;
    }

    private void OnDisable()
    {
        if (jaringan != null)
        {
            jaringan.SkillAktifBerubah -= SaatSkillAktif;
            jaringan.PukulanBerkekuatan -= SaatPukulan;
            jaringan.ParryDipicu -= SaatParry;
        }

        if (hitStopBerjalan)
        {
            Time.timeScale = 1f;
            hitStopBerjalan = false;
        }
    }

    // ---------------- Reaksi event ----------------

    private void SaatSkillAktif(DragonSkillType tipe, bool aktif)
    {
        switch (tipe)
        {
            case DragonSkillType.Push:
                pushAktif = aktif;
                Dorong(aktif ? new Vector3(0.15f, -0.12f, 0f) : new Vector3(-0.1f, 0.12f, 0f));
                FabrikEfek.AturLaju(debu, aktif ? 14f : 0f);

                if (aktif && sfxDorong != null)
                {
                    sumberLoop.clip = sfxDorong;
                    sumberLoop.Play();
                }
                else if (!aktif && sumberLoop.isPlaying)
                {
                    sumberLoop.Stop();
                }
                break;

            case DragonSkillType.Destroy: // dipakai sebagai status "sedang mengisi (charge)"
                mengisi = aktif;
                if (aktif) Putar(sfxMengisi);
                break;

            case DragonSkillType.SteelSkin:
                bajaAktif = aktif;
                Dorong(aktif ? new Vector3(0.3f, -0.35f, 0.3f) : new Vector3(-0.15f, 0.25f, -0.15f));
                Putar(aktif ? sfxBajaAktif : sfxBajaLepas);

                if (aktif)
                {
                    FabrikEfek.Pancar(percikan, Pusat(), 10);
                    GetarDekat(Pusat(), 0.08f, 0.12f);
                }
                break;
        }

        targetOffset = HitungTarget();
    }

    private void SaatPukulan(Vector3 titik, float tenaga)
    {
        bool penuh = tenaga >= 0.99f;

        Dorong(new Vector3(0.35f, -0.45f, 0.35f) * (0.6f + 0.8f * tenaga));
        FabrikEfek.Pancar(serpihan, titik, penuh ? 30 : 12);
        Putar(penuh ? sfxPukulPenuh : sfxPukul);
        GetarDekat(titik, penuh ? 0.35f : 0.14f, penuh ? 0.35f : 0.18f);

        if (penuh)
        {
            float radius = destroySkill != null ? destroySkill.RadiusGelombang : 3f;
            StartCoroutine(EfekGelombang(Pusat(), radius));
        }

        MulaiHitStop(penuh ? 0.09f : 0.05f);
    }

    private void SaatParry(Vector3 titik)
    {
        Dorong(new Vector3(0.5f, -0.6f, 0.5f));
        FabrikEfek.Pancar(percikan, Pusat(), 22);
        Putar(sfxParry);
        GetarDekat(titik, 0.3f, 0.3f);
        MulaiHitStop(0.1f);
    }

    // ---------------- Squash & stretch ----------------

    private Vector3 HitungTarget()
    {
        Vector3 target = Vector3.zero;
        if (pushAktif) target += new Vector3(0.10f, -0.08f, 0f);
        if (bajaAktif) target += new Vector3(0.12f, -0.16f, 0.12f);
        if (mengisi) target += new Vector3(0.14f, -0.20f, 0.14f);
        return target;
    }

    /// <summary>Beri 'dorongan' pegas; 'puncak' ~ besar deformasi maksimum yang diinginkan.</summary>
    private void Dorong(Vector3 puncak)
    {
        kecepatanOffset += puncak * (Mathf.Sqrt(kekakuan) * 1.2f);
    }

    private void Update()
    {
        if (model == null) return;

        float dt = Mathf.Min(Time.deltaTime, 0.033f);

        Vector3 target = targetOffset;
        if (mengisi) // gemetar saat mengisi tenaga
            target += new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * 0.02f;

        Vector3 akselerasi = -kekakuan * (offset - target) - redaman * kecepatanOffset;
        kecepatanOffset += akselerasi * dt;
        offset += kecepatanOffset * dt;
        offset = new Vector3(
            Mathf.Clamp(offset.x, -0.6f, 0.9f),
            Mathf.Clamp(offset.y, -0.6f, 0.9f),
            Mathf.Clamp(offset.z, -0.6f, 0.9f));

        model.localScale = Vector3.Scale(skalaAsal, Vector3.one + offset);
    }

    private void LateUpdate()
    {
        // Debu mengikuti kaki selama mendorong.
        if (pushAktif && debu != null && kapsul != null)
        {
            Bounds b = kapsul.bounds;
            debu.transform.position = new Vector3(b.center.x, b.min.y + 0.05f, b.center.z);
        }
    }

    // ---------------- Getar & hit-stop ----------------

    private void GetarDekat(Vector3 titik, float kekuatan, float durasi)
    {
        if (!getarKamera) return;

        Camera kamera = Camera.main;
        float faktor = 1f;
        if (kamera != null)
        {
            Vector2 selisih = new Vector2(titik.x - kamera.transform.position.x, titik.y - kamera.transform.position.y);
            faktor = Mathf.Clamp01(1f - selisih.magnitude / jarakGetarMaks);
        }

        CameraGetar.Getar(kekuatan * faktor, durasi);
    }

    private void MulaiHitStop(float durasi)
    {
        // Hanya di client pemilik Dragon agar tidak ada 4 client yang saling membekukan waktu.
        if (!hitStop || hitStopBerjalan || view == null || !view.IsMine) return;
        StartCoroutine(AlurHitStop(durasi));
    }

    private IEnumerator AlurHitStop(float durasi)
    {
        hitStopBerjalan = true;
        float asal = Time.timeScale;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(durasi);
        Time.timeScale = asal;
        hitStopBerjalan = false;
    }

    // ---------------- Efek ----------------

    private IEnumerator EfekGelombang(Vector3 pusat, float radius)
    {
        GameObject cakram = FabrikEfek.BuatPrimitif(PrimitiveType.Sphere, "Juice Gelombang", new Color(1f, 0.6f, 0.2f, 0.55f), null);
        Renderer renderer = cakram.GetComponent<Renderer>();
        Material material = renderer != null ? renderer.sharedMaterial : null; // salinan milik cakram ini
        cakram.transform.position = pusat;

        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float n = Mathf.Clamp01(t / 0.35f);
            float diameter = Mathf.Lerp(0.4f, radius * 2f, 1f - (1f - n) * (1f - n));
            cakram.transform.localScale = new Vector3(diameter, diameter, 0.08f);

            if (material != null)
            {
                Color warna = material.color;
                warna.a = Mathf.Lerp(0.55f, 0f, n);
                material.color = warna;
            }

            yield return null;
        }

        if (material != null) Destroy(material);
        Destroy(cakram);
    }

    private Vector3 Pusat()
    {
        return kapsul != null ? kapsul.bounds.center : transform.position;
    }

    private void Putar(AudioClip clip)
    {
        if (clip != null && sumberEfek != null) sumberEfek.PlayOneShot(clip);
    }

    private AudioSource BuatSumber(bool loop)
    {
        AudioSource sumber = gameObject.AddComponent<AudioSource>();
        sumber.playOnAwake = false;
        sumber.loop = loop;
        sumber.spatialBlend = 0.6f;
        sumber.rolloffMode = AudioRolloffMode.Linear;
        sumber.minDistance = 4f;
        sumber.maxDistance = 30f;
        return sumber;
    }

    private Transform CariModel()
    {
        foreach (Renderer r in GetComponentsInChildren<Renderer>(true))
        {
            if (r is ParticleSystemRenderer || r is LineRenderer || r is TrailRenderer) continue;

            string nama = r.name;
            if (nama.StartsWith("Debug") || nama.StartsWith("VFX") || nama.StartsWith("Juice")) continue;

            return r.transform;
        }

        return null;
    }
}
