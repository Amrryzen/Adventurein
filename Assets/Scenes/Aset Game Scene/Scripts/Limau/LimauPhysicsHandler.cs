using UnityEngine;

/// <summary>
/// LAYER 2a - PHYSICS (BADAN).
/// Mengatur bentuk fisik Limau saat Inflate: skala model + ukuran CapsuleCollider.
/// Berjalan di SEMUA client (event-driven dari LimauSkillNetwork) karena collider
/// Limau di client Dragon harus ikut membesar agar Dragon bisa memantul di atasnya.
/// Sengaja TIDAK menskalakan root agar tidak bentrok dengan sinkronisasi transform & fisika.
/// Asumsi: CapsuleCollider.direction = Y dan skala root = 1.
/// </summary>
[RequireComponent(typeof(CapsuleCollider))]
public class LimauPhysicsHandler : MonoBehaviour
{
    [Header("Inflate")]
    [Tooltip("Child visual yang diskalakan (BUKAN root).")]
    [SerializeField] private Transform model;
    [SerializeField] private float faktorMaks = 2f;
    [Tooltip("Satuan faktor per detik.")]
    [SerializeField] private float kecepatanMengembang = 3f;
    [Tooltip("Dianggap 'sudah mengembang' (siap memantulkan) setelah mencapai fraksi ini.")]
    [Range(0.5f, 1f)] [SerializeField] private float ambangSiap = 0.9f;
    [Tooltip("Penghalang yang membatalkan Inflate bila ruang tidak muat. Pemain otomatis diabaikan.")]
    [SerializeField] private LayerMask lapisHalangan = ~0;

    private CapsuleCollider kapsul;
    private LimauSkillNetwork jaringan;
    private readonly Collider[] buffer = new Collider[16];

    private float radiusAsal;
    private float tinggiAsal;
    private Vector3 centerAsal;
    private Vector3 skalaModelAsal = Vector3.one;

    private float faktor = 1f;
    private float faktorTarget = 1f;

    /// <summary>PlayerController menonaktifkan collider saat mati -> collider nonaktif = mati.</summary>
    public bool Mati => !kapsul.enabled;

    public bool SudahMengembang =>
        faktorTarget > 1f && faktor >= 1f + (faktorMaks - 1f) * ambangSiap;

    private void Awake()
    {
        kapsul = GetComponent<CapsuleCollider>();
        jaringan = GetComponent<LimauSkillNetwork>();

        radiusAsal = kapsul.radius;
        tinggiAsal = kapsul.height;
        centerAsal = kapsul.center;

        if (model != null) skalaModelAsal = model.localScale;
        else Debug.LogWarning("LimauPhysicsHandler: 'model' belum diisi, hanya collider yang membesar.", this);
    }

    private void OnEnable()
    {
        if (jaringan != null) jaringan.SkillAktifBerubah += SaatSkillAktif;
    }

    private void OnDisable()
    {
        if (jaringan != null) jaringan.SkillAktifBerubah -= SaatSkillAktif;
    }

    private void SaatSkillAktif(LimauSkillType tipe, bool aktif)
    {
        if (tipe != LimauSkillType.Inflate) return;
        faktorTarget = aktif ? faktorMaks : 1f;
    }

    private void Update()
    {
        if (Mathf.Approximately(faktor, faktorTarget)) return;

        faktor = Mathf.MoveTowards(faktor, faktorTarget, kecepatanMengembang * Time.deltaTime);
        TerapkanBentuk();
    }

    private void TerapkanBentuk()
    {
        if (model != null) model.localScale = skalaModelAsal * faktor;

        float tinggiBaru = tinggiAsal * faktor;
        Vector3 center = centerAsal;
        center.y += (tinggiBaru - tinggiAsal) * 0.5f; // jaga posisi kaki tetap di tempat

        kapsul.radius = radiusAsal * faktor;
        kapsul.height = tinggiBaru;
        kapsul.center = center;
    }

    /// <summary>
    /// Cek apakah ukuran maksimum muat tanpa menembus dinding/langit-langit.
    /// Dipanggil owner sebelum mengembang.
    /// </summary>
    public bool BisaMengembang()
    {
        float r = radiusAsal * faktorMaks;
        float tinggi = tinggiAsal * faktorMaks;

        Vector3 center = centerAsal;
        center.y += (tinggi - tinggiAsal) * 0.5f;

        float setengah = Mathf.Max(0f, tinggi * 0.5f - r);
        Vector3 p1 = transform.TransformPoint(center + Vector3.up * setengah);
        Vector3 p2 = transform.TransformPoint(center - Vector3.up * setengah);
        float skala = Mathf.Max(transform.lossyScale.x, transform.lossyScale.z);

        int jumlah = Physics.OverlapCapsuleNonAlloc(
            p1, p2, r * skala * 0.95f, buffer, lapisHalangan, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < jumlah; i++)
        {
            Collider c = buffer[i];
            if (c.transform.IsChildOf(transform)) continue;
            if (c.GetComponentInParent<PlayerController>() != null) continue; // pemain boleh terdorong

            return false;
        }

        return true;
    }
}
