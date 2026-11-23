using UnityEngine;

/// <summary>
/// LAYER 2b - PHYSICS (JOINT & BIDIK SUTRA).
/// Raycast bidik, SpringJoint ayunan, dan kontrol gaya ayun. Hanya dipanggil oleh owner.
/// Catatan penting: joint Unity TIDAK bisa disinkronkan antar client. Joint hanya hidup di client
/// Limau; pemain lain melihat posisi Limau lewat sinkronisasi transform + garis sutra lewat RPC.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerController))]
public class LimauSilkHandler : MonoBehaviour
{
    public struct HasilBidik
    {
        public Vector3 titik;       // titik tambat di dunia
        public TargetSutra teman;   // null = titik gantung statis, non-null = teman yang ditarik
    }

    [Header("Bidik")]
    [SerializeField] private string tagTitikGantung = "GrapplePoint";
    [SerializeField] private float jangkauanMaks = 10f;
    [SerializeField] private float radiusBidik = 0.25f;
    [SerializeField] private LayerMask lapisBidik = ~0;
    [Tooltip("Posisi lokal asal sutra (kira-kira tangan/dada).")]
    [SerializeField] private Vector3 offsetTangan = new Vector3(0f, 0.5f, 0f);

    [Header("SpringJoint")]
    [SerializeField] private float spring = 6f;
    [SerializeField] private float damper = 7f;
    [SerializeField] private float massScale = 4.5f;
    [Tooltip("maxDistance = jarak tembak x faktor ini (lebih kecil = sutra menegang).")]
    [Range(0.5f, 1f)] [SerializeField] private float faktorPanjang = 0.85f;
    [SerializeField] private float jarakMinimum = 1.5f;

    [Header("Ayunan")]
    [Tooltip("Gaya horizontal dari tombol kiri/kanan saat berayun.")]
    [SerializeField] private float gayaAyun = 18f;
    [Tooltip("Kecepatan menggulung/mengulur sutra (tombol atas/bawah).")]
    [SerializeField] private float kecepatanGulung = 3f;

    private Rigidbody rb;
    private PlayerController pemain;
    private SpringJoint joint;
    private readonly RaycastHit[] hits = new RaycastHit[16];

    public Vector3 AsalSutra => transform.TransformPoint(offsetTangan);
    public bool Mengayun => joint != null;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        pemain = GetComponent<PlayerController>();
    }

    /// <summary>Cari titik gantung atau teman di arah bidik. Geometri lain di depannya memblokir sutra.</summary>
    public bool TryBidik(Vector2 arah, out HasilBidik hasil)
    {
        hasil = default;

        Vector3 asal = AsalSutra;
        Vector3 dir = new Vector3(arah.x, arah.y, 0f);
        if (dir.sqrMagnitude < 0.0001f) return false;
        dir.Normalize();

        int jumlah = Physics.SphereCastNonAlloc(
            asal, radiusBidik, dir, hits, jangkauanMaks, lapisBidik, QueryTriggerInteraction.Collide);

        float terdekat = float.MaxValue;
        int terbaik = -1;

        for (int i = 0; i < jumlah; i++)
        {
            Collider c = hits[i].collider;
            if (c.transform.IsChildOf(transform)) continue;

            bool sasaran = PunyaTitikGantung(c) || c.GetComponentInParent<TargetSutra>() != null;
            if (c.isTrigger && !sasaran) continue; // trigger biasa tidak memblokir sutra

            if (hits[i].distance < terdekat)
            {
                terdekat = hits[i].distance;
                terbaik = i;
            }
        }

        if (terbaik < 0) return false;

        Collider kena = hits[terbaik].collider;

        TargetSutra teman = kena.GetComponentInParent<TargetSutra>();
        if (teman != null)
        {
            hasil.teman = teman;
            hasil.titik = teman.TitikTambat.position;
            return true;
        }

        if (!PunyaTitikGantung(kena)) return false; // terhalang geometri biasa

        Vector3 titik = hits[terbaik].distance > 0f ? hits[terbaik].point : kena.bounds.ClosestPoint(asal);
        titik.z = transform.position.z; // side-scroller: jaga di bidang yang sama
        hasil.titik = titik;
        return true;
    }

    private bool PunyaTitikGantung(Collider c)
    {
        if (c.CompareTag(tagTitikGantung)) return true;
        return c.attachedRigidbody != null && c.attachedRigidbody.CompareTag(tagTitikGantung);
    }

    /// <summary>Pasang SpringJoint ke titik dunia dan ambil alih kontrol fisika dari PlayerController.</summary>
    public void PasangAyunan(Vector3 titik)
    {
        LepasAyunan();
        SetKontrolFisika(true);

        joint = gameObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = titik; // tanpa connectedBody = koordinat dunia
        joint.anchor = offsetTangan;

        float jarak = Vector3.Distance(AsalSutra, titik);
        joint.maxDistance = Mathf.Clamp(jarak * faktorPanjang, jarakMinimum, jangkauanMaks);
        joint.minDistance = 0f;
        joint.spring = spring;
        joint.damper = damper;
        joint.massScale = massScale;

        rb.WakeUp();
    }

    public void LepasAyunan()
    {
        if (joint == null) return;
        Destroy(joint);
        joint = null;
    }

    /// <summary>Panggil dari FixedUpdate saat berayun: gaya kiri/kanan + gulung/ulur sutra.</summary>
    public void UpdateAyunan(float horizontal, float vertikal)
    {
        if (joint == null) return;

        if (horizontal != 0f)
            rb.AddForce(Vector3.right * (horizontal * gayaAyun), ForceMode.Force);

        if (vertikal != 0f)
        {
            joint.maxDistance = Mathf.Clamp(
                joint.maxDistance - vertikal * kecepatanGulung * Time.fixedDeltaTime,
                jarakMinimum,
                jangkauanMaks);
        }
    }

    /// <summary>true = PlayerController berhenti menimpa velocity (butuh patch KontrolFisikaDilepas).</summary>
    public void SetKontrolFisika(bool dilepas)
    {
        pemain.KontrolFisikaDilepas = dilepas;
    }
}
