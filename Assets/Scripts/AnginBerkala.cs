
using System;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class AnginBerkala : MonoBehaviour
{
    [Serializable]
    private class PengaturanJadwal
    {
        [Min(0.5f)] public float interval = 5f;
        [Min(0.5f)] public float durasi = 3.5f;
        [Min(0f)] public float peringatan = 1.5f;
        public float offset = 0f;
    }

    [Serializable]
    private class PengaturanKekuatan
    {
        [Tooltip("Arah angin: 1 = kanan, -1 = kiri.")]
        [Range(-1, 1)] public float arah = 1f;

        [Min(0f)] public float kecepatan = 7f;
        [Min(0f)] public float doronganSaatBerayun = 25f;
        [Min(0f)] public float waktuTransisi = 0.5f;
    }

    [Serializable]
    private class PengaturanPerlindungan
    {
        [Min(0f)] public float jarakDeteksi = 12f;
        public LayerMask layerPenghalang = ~0;
    }

    [Serializable]
    private class PengaturanVisual
    {
        [Min(0f)] public float lebar = 12f;
        [Min(0f)] public float tinggi = 5f;
    }

    [Header("1. Jadwal Angin")]
    [SerializeField] private PengaturanJadwal jadwal = new PengaturanJadwal();

    [Header("2. Kekuatan Angin")]
    [SerializeField] private PengaturanKekuatan kekuatan = new PengaturanKekuatan();

    [Header("3. Perlindungan")]
    [SerializeField] private PengaturanPerlindungan perlindungan = new PengaturanPerlindungan();

    [Header("4. Visual Angin")]
    [SerializeField] private PengaturanVisual visual = new PengaturanVisual();

    private PlayerController pemain;
    private CapsuleCollider kolPemain;
    private Rigidbody rbPemain;
    private Collider zona;

    private bool sedangMendorong;
    private bool terlindung;

    private ParticleSystem partikel;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    private float Arah => kekuatan.arah >= 0f ? 1f : -1f;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        zona = GetComponent<Collider>();

        float kecepatan = Mathf.Max(0.5f, kekuatan.kecepatan);

        partikel = FabrikEfek.BuatPartikel(
            "Angin Daun",
            transform,
            new Color(0.7f, 0.9f, 0.5f, 0.85f),
            0.16f, 1f, 0f, 0f, 0f
        );

        ParticleSystem.MainModule utama = partikel.main;
        utama.startLifetime = Mathf.Max(0.5f, visual.lebar / kecepatan);

        ParticleSystem.ShapeModule bentuk = partikel.shape;
        bentuk.shapeType = ParticleSystemShapeType.Box;
        bentuk.scale = new Vector3(
            0.1f,
            Mathf.Max(0.5f, visual.tinggi),
            0.1f
        );

        ParticleSystem.VelocityOverLifetimeModule vel =
            partikel.velocityOverLifetime;

        vel.enabled = true;
        vel.space = ParticleSystemSimulationSpace.World;

        vel.x = new ParticleSystem.MinMaxCurve(
            Arah * kecepatan * 0.9f,
            Arah * kecepatan * 1.2f
        );

        vel.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        vel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    private float Fase()
    {
        float siklus = jadwal.interval + jadwal.durasi;

        return Mathf.Repeat(
            (float)UtilJaringan.Jam + jadwal.offset,
            siklus
        );
    }

    private float KekuatanAngin(float fase)
    {
        float sejakMulai = fase - jadwal.interval;

        if (sejakMulai < 0f)
            return 0f;

        float naik = kekuatan.waktuTransisi > 0f
            ? Mathf.Clamp01(sejakMulai / kekuatan.waktuTransisi)
            : 1f;

        float turun = Mathf.Clamp01(
            (jadwal.durasi - sejakMulai) / 0.4f
        );

        return Mathf.Min(naik, turun);
    }

    private void Update()
    {
        if (pemain == null)
        {
            FabrikEfek.AturLaju(partikel, 0f);
            return;
        }

        float fase = Fase();

        bool berhembus = fase >= jadwal.interval;

        bool peringatan =
            !berhembus &&
            fase >= jadwal.interval - jadwal.peringatan;

        Vector3 posisiPemain = pemain.transform.position;
        Bounds batasZona = zona.bounds;

        float x = Mathf.Clamp(
            posisiPemain.x - Arah * visual.lebar * 0.5f,
            batasZona.min.x,
            batasZona.max.x
        );

        partikel.transform.position = new Vector3(
            x,
            posisiPemain.y,
            posisiPemain.z
        );

        float laju = berhembus ? 36f : (peringatan ? 6f : 0f);

        FabrikEfek.AturLaju(
            partikel,
            terlindung ? 0f : laju
        );

        if (berhembus && !terlindung)
        {
            CameraGetar.Getar(0.035f, 0.12f);
        }
    }

    private void FixedUpdate()
    {
        bool mendorong = false;
        terlindung = false;

        if (pemain != null &&
            kolPemain != null &&
            kolPemain.enabled)
        {
            float kekuatanSaatIni = KekuatanAngin(Fase());

            if (kekuatanSaatIni > 0f)
            {
                terlindung = Terlindung();

                if (!terlindung)
                {
                    if (pemain.KontrolFisikaDilepas)
                    {
                        if (rbPemain != null)
                        {
                            rbPemain.AddForce(
                                new Vector3(
                                    Arah *
                                    kekuatan.doronganSaatBerayun *
                                    kekuatanSaatIni,
                                    0f,
                                    0f
                                ),
                                ForceMode.Force
                            );
                        }
                    }
                    else
                    {
                        pemain.DorongLuarX =
                            Arah *
                            kekuatan.kecepatan *
                            kekuatanSaatIni;

                        mendorong = true;
                    }
                }
            }
        }

        if (!mendorong && sedangMendorong)
        {
            HentikanDorongan();
        }

        sedangMendorong = mendorong;
    }

    private bool Terlindung()
    {
        Bounds bp = kolPemain.bounds;
        Bounds bz = zona.bounds;

        float tepiHulu = Arah > 0f
            ? bz.min.x
            : bz.max.x;

        float jarak = Mathf.Min(
            perlindungan.jarakDeteksi,
            Mathf.Abs(bp.center.x - tepiHulu)
        );

        if (jarak < 0.05f)
            return false;

        Vector3 setengah = new Vector3(
            0.05f,
            bp.extents.y * 0.8f,
            bp.extents.z * 0.8f
        );

        Vector3 keHulu = new Vector3(-Arah, 0f, 0f);

        int jumlah = Physics.BoxCastNonAlloc(
            bp.center,
            setengah,
            keHulu,
            hits,
            Quaternion.identity,
            jarak,
            perlindungan.layerPenghalang,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < jumlah; i++)
        {
            RaycastHit h = hits[i];
            Collider c = h.collider;

            if (c.transform.IsChildOf(pemain.transform))
                continue;

            if (h.normal.y > 0.7f)
                continue;

            DragonSkillNetwork dragon =
                c.GetComponentInParent<DragonSkillNetwork>();

            if (dragon != null)
            {
                if (dragon.IsAktif(DragonSkillType.SteelSkin))
                    return true;

                continue;
            }

            if (c.GetComponentInParent<PlayerController>() != null)
                continue;

            return true;
        }

        return false;
    }

    private void HentikanDorongan()
    {
        if (pemain != null)
            pemain.DorongLuarX = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController p =
            other.GetComponentInParent<PlayerController>();

        if (p == null || !p.photonView.IsMine)
            return;

        pemain = p;
        kolPemain = p.GetComponent<CapsuleCollider>();
        rbPemain = p.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController p =
            other.GetComponentInParent<PlayerController>();

        if (p == null || p != pemain)
            return;

        HentikanDorongan();

        sedangMendorong = false;
        terlindung = false;

        pemain = null;
        kolPemain = null;
        rbPemain = null;
    }

    private void OnDisable()
    {
        HentikanDorongan();

        sedangMendorong = false;
        terlindung = false;

        pemain = null;
        kolPemain = null;
        rbPemain = null;
    }

    private void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();

        if (c == null)
            return;

        Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.9f);

        Vector3 pusat = c.bounds.center;

        Vector3 ujung = pusat +
            Vector3.right *
            (Mathf.Sign(kekuatan.arah) * Mathf.Min(c.bounds.extents.x, 3f));

        Gizmos.DrawLine(pusat, ujung);
        Gizmos.DrawSphere(ujung, 0.2f);
    }
}
