using Photon.Pun;
using UnityEngine;

/// <summary>
/// GDD Level 3: "rintangan angin kencang yang datang berkala, mewajibkan pemain bersembunyi atau
/// berlindung di balik Ulat Naga".
///
/// - Jadwal dihitung dari jam jaringan (UtilJaringan.Jam): semua client melihat gelombang angin yang sama
///   TANPA RPC. Ada fase peringatan (daun mulai beterbangan) sebelum angin kencang.
/// - Tiap client hanya mendorong PEMAIN LOKALnya (fisikanya dijalankan pemilik).
/// - Terlindung bila di antara pemain dan arah datangnya angin ada: Dragon yang Steel Skin-nya aktif
///   (status tersinkron lewat DragonSkillNetwork) atau benda padat dunia.
/// Butuh patch PlayerController: properti DorongLuarX (lihat PATCH_PlayerController.txt).
/// Pasang di GameObject dengan Collider trigger yang menutupi area berangin.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AnginBerkala : MonoBehaviour
{
    [Header("Jadwal (sinkron lewat jam jaringan)")]
    [SerializeField, Min(0.5f)] private float jedaAntarAngin = 5f;
    [SerializeField, Min(0.5f)] private float durasiAngin = 3.5f;
    [SerializeField, Min(0f)] private float waktuPeringatan = 1.5f;
    [SerializeField] private float offsetFase = 0f;

    [Header("Dorongan")]
    [Tooltip("+1 = angin bertiup ke kanan, -1 = ke kiri.")]
    [SerializeField] private float arahX = 1f;
    [SerializeField] private float kecepatanAngin = 7f;
    [Tooltip("Newton. Dipakai saat kontrol pemain dilepas (mis. sedang berayun dengan sutra).")]
    [SerializeField] private float gayaSaatBerayun = 25f;
    [SerializeField] private float waktuNaik = 0.5f;

    [Header("Perlindungan")]
    [SerializeField] private float jarakLindung = 3f;
    [Tooltip("Layer yang dianggap penghalang angin. Benda dekoratif ber-collider di layer ini ikut melindungi.")]
    [SerializeField] private LayerMask lapisPenghalang = ~0;

    private PlayerController pemain;
    private CapsuleCollider kolPemain;
    private Rigidbody rbPemain;
    private bool sedangMendorong;
    private ParticleSystem partikel;
    private readonly RaycastHit[] hits = new RaycastHit[8];

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Awake()
    {
        Bounds b = GetComponent<Collider>().bounds;
        float kecepatan = Mathf.Max(0.5f, kecepatanAngin);

        partikel = FabrikEfek.BuatPartikel("Angin Daun", transform, new Color(0.7f, 0.9f, 0.5f, 0.85f), 0.16f, 1f, 0f, 0f, 0f);
        partikel.transform.position = new Vector3(b.center.x - Mathf.Sign(arahX) * b.extents.x, b.center.y, b.center.z);

        ParticleSystem.MainModule utama = partikel.main;
        utama.startLifetime = Mathf.Max(1f, b.size.x / kecepatan);

        ParticleSystem.ShapeModule bentuk = partikel.shape;
        bentuk.shapeType = ParticleSystemShapeType.Box;
        bentuk.scale = new Vector3(0.1f, Mathf.Max(0.5f, b.size.y), 0.1f);

        ParticleSystem.VelocityOverLifetimeModule kecepatanPartikel = partikel.velocityOverLifetime;
        kecepatanPartikel.enabled = true;
        kecepatanPartikel.space = ParticleSystemSimulationSpace.World;
        kecepatanPartikel.x = new ParticleSystem.MinMaxCurve(Mathf.Sign(arahX) * kecepatan * 0.9f, Mathf.Sign(arahX) * kecepatan * 1.2f);
        kecepatanPartikel.y = new ParticleSystem.MinMaxCurve(-0.3f, 0.3f);
        kecepatanPartikel.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    // ---------------- Jadwal ----------------

    private float Fase()
    {
        float siklus = jedaAntarAngin + durasiAngin;
        return (float)((UtilJaringan.Jam + offsetFase) % siklus);
    }

    private float Kekuatan(float fase)
    {
        float sejak = fase - jedaAntarAngin;
        if (sejak < 0f) return 0f;

        float naik = waktuNaik > 0f ? Mathf.Clamp01(sejak / waktuNaik) : 1f;
        float turun = Mathf.Clamp01((durasiAngin - sejak) / 0.4f);
        return Mathf.Min(naik, turun);
    }

    private void Update()
    {
        float fase = Fase();
        bool berhembus = fase >= jedaAntarAngin;
        bool peringatan = !berhembus && fase >= jedaAntarAngin - waktuPeringatan;

        FabrikEfek.AturLaju(partikel, berhembus ? 36f : (peringatan ? 6f : 0f));

        if (pemain != null && berhembus)
            CameraGetar.Getar(0.035f, 0.12f); // getaran halus saat angin menerpa pemain lokal
    }

    // ---------------- Fisika ----------------

    private void FixedUpdate()
    {
        bool mendorong = false;

        if (pemain != null && kolPemain != null && kolPemain.enabled)
        {
            float kekuatan = Kekuatan(Fase());
            if (kekuatan > 0f && !Terlindung())
            {
                if (pemain.KontrolFisikaDilepas)
                {
                    rbPemain.AddForce(new Vector3(Mathf.Sign(arahX) * gayaSaatBerayun * kekuatan, 0f, 0f), ForceMode.Force);
                }
                else
                {
                    pemain.DorongLuarX = Mathf.Sign(arahX) * kecepatanAngin * kekuatan;
                    mendorong = true;
                }
            }
        }

        if (!mendorong && sedangMendorong)
            HentikanDorongan();

        sedangMendorong = mendorong;
    }

    private bool Terlindung()
    {
        Vector3 asal = kolPemain.bounds.center;
        Vector3 keSumber = new Vector3(-Mathf.Sign(arahX), 0f, 0f);

        int jumlah = Physics.RaycastNonAlloc(asal, keSumber, hits, jarakLindung, lapisPenghalang, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < jumlah; i++)
        {
            Collider c = hits[i].collider;
            if (c.transform.IsChildOf(pemain.transform)) continue;

            DragonSkillNetwork dragon = c.GetComponentInParent<DragonSkillNetwork>();
            if (dragon != null)
            {
                // Dragon hanya melindungi saat Steel Skin aktif; kalau tidak, dia 'tembus pandang' bagi angin.
                if (dragon.IsAktif(DragonSkillType.SteelSkin)) return true;
                continue;
            }

            if (c.GetComponentInParent<PlayerController>() != null) continue; // pemain lain tidak melindungi

            return true; // benda padat dunia
        }

        return false;
    }

    private void HentikanDorongan()
    {
        if (pemain != null) pemain.DorongLuarX = 0f;
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null || !p.photonView.IsMine) return;

        pemain = p;
        kolPemain = p.GetComponent<CapsuleCollider>();
        rbPemain = p.GetComponent<Rigidbody>();
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController p = other.GetComponentInParent<PlayerController>();
        if (p == null || p != pemain) return;

        HentikanDorongan();
        sedangMendorong = false;
        pemain = null;
    }

    private void OnDisable()
    {
        HentikanDorongan();
        sedangMendorong = false;
        pemain = null;
    }

    private void OnDrawGizmos()
    {
        Collider c = GetComponent<Collider>();
        if (c == null) return;

        Gizmos.color = new Color(0.5f, 0.9f, 1f, 0.9f);
        Vector3 pusat = c.bounds.center;
        Vector3 ujung = pusat + Vector3.right * (Mathf.Sign(arahX) * Mathf.Min(c.bounds.extents.x, 3f));
        Gizmos.DrawLine(pusat, ujung);
        Gizmos.DrawSphere(ujung, 0.2f);
    }
}
