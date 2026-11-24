using UnityEngine;

/// <summary>
/// LAYER 2 - PHYSICS.
/// Semua operasi fisika Dragon: deteksi objek, dorongan, dan pengunciran badan (Steel Skin).
/// Tidak tahu apa pun soal input atau Photon.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class DragonPhysicsHandler : MonoBehaviour
{
    [Header("Deteksi")]
    [Tooltip("Layer yang boleh dideteksi skill (Pushable/Breakable).")]
    [SerializeField] private LayerMask lapisDeteksi = ~0;
    [Tooltip("Layer tanah untuk cek 'menyentuh tanah' (JANGAN sertakan layer Player).")]
    [SerializeField] private LayerMask lapisTanah = ~0;

    private Rigidbody rb;
    private CapsuleCollider kapsul;
    private readonly Collider[] buffer = new Collider[16];

    private RigidbodyConstraints constraintAsal;
    private float massaAsal;
    private bool terkunci;

    public Vector3 Pusat => kapsul.bounds.center;

    /// <summary>
    /// PlayerController menonaktifkan collider saat mati (RPC_TampilkanPemain),
    /// jadi collider nonaktif = Dragon sedang mati/menunggu respawn.
    /// </summary>
    public bool Mati => !kapsul.enabled;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        kapsul = GetComponent<CapsuleCollider>();
    }

    /// <summary>
    /// Cari collider terdekat bertag tertentu dalam jangkauan.
    /// Tag boleh ada di collider-nya langsung atau di Rigidbody induknya.
    /// </summary>
    public bool TryCariTerdekat(string tag, float jangkauan, out Collider hasil)
    {
        hasil = null;
        Bounds b = kapsul.bounds;

        int jumlah = Physics.OverlapSphereNonAlloc(
            b.center, jangkauan + b.extents.x, buffer, lapisDeteksi, QueryTriggerInteraction.Ignore);

        float terdekat = float.MaxValue;
        for (int i = 0; i < jumlah; i++)
        {
            Collider c = buffer[i];
            if (c.transform.IsChildOf(transform)) continue;
            if (!PunyaTag(c, tag)) continue;

            // Harus sejajar secara vertikal (hindari batu di atas kepala / di bawah kaki).
            if (c.bounds.max.y < b.min.y + 0.05f || c.bounds.min.y > b.max.y - 0.05f) continue;

            float jarak = (c.bounds.ClosestPoint(b.center) - b.center).sqrMagnitude;
            if (jarak < terdekat)
            {
                terdekat = jarak;
                hasil = c;
            }
        }

        return hasil != null;
    }

    /// <summary>Semua collider (non-trigger) dalam radius dari pusat Dragon. Dipakai Hantaman Penuh.</summary>
    public int CariDalamRadius(float radius, Collider[] hasil)
    {
        return Physics.OverlapSphereNonAlloc(
            kapsul.bounds.center, radius, hasil, lapisDeteksi, QueryTriggerInteraction.Ignore);
    }

    private static bool PunyaTag(Collider c, string tag)
    {
        if (c.CompareTag(tag)) return true;
        return c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(tag);
    }

    /// <summary>
    /// Dorong Rigidbody target searah sumbu X. Gaya berhenti ditambah jika sudah mencapai kecepatan maksimum.
    /// Panggil dari FixedUpdate, dan hanya jika client ini pemilik target.
    /// </summary>
    public void DorongTarget(Rigidbody target, float arahX, float gaya, float kecepatanMaks)
    {
        float vx = target.linearVelocity.x;
        bool searah = Mathf.Sign(vx) == arahX;
        if (searah && Mathf.Abs(vx) >= kecepatanMaks) return;

        target.AddForce(new Vector3(arahX * gaya, 0f, 0f), ForceMode.Force);
    }

    /// <summary>
    /// Steel Skin: bekukan semua sumbu + naikkan massa drastis.
    /// Sengaja TIDAK memakai isKinematic karena PlayerController menulis linearVelocity
    /// tiap FixedUpdate (kinematic akan memicu warning), sedangkan constraint FreezeAll
    /// membuat velocity tersebut diabaikan oleh PhysX.
    /// </summary>
    public void KunciBadan(bool kunci, float pengaliMassa)
    {
        if (kunci == terkunci) return;
        terkunci = kunci;

        if (kunci)
        {
            constraintAsal = rb.constraints;
            massaAsal = rb.mass;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.constraints = RigidbodyConstraints.FreezeAll;
            rb.mass = massaAsal * pengaliMassa;
        }
        else
        {
            rb.constraints = constraintAsal;
            rb.mass = massaAsal;
        }
    }

    public bool MenyentuhTanah()
    {
        Bounds b = kapsul.bounds;
        Vector3 asal = new Vector3(b.center.x, b.min.y + 0.1f, b.center.z);
        return Physics.Raycast(asal, Vector3.down, 0.25f, lapisTanah, QueryTriggerInteraction.Ignore);
    }
}