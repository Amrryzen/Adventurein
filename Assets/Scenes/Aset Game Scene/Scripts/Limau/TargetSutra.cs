using Photon.Pun;
using UnityEngine;

/// <summary>
/// Pasang di prefab yang boleh DITARIK sutra Limau (mis. Dragon).
/// Fisika tarikan dijalankan di client PEMILIK karakter ini (karena di client lain Rigidbody-nya
/// kinematic), dipicu oleh LimauSkillNetwork lewat RPC dari Limau.
/// Butuh patch kecil PlayerController: properti KontrolFisikaDilepas.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(PlayerController))]
public class TargetSutra : MonoBehaviourPun
{
    [Tooltip("Titik tempat sutra menempel (mis. dada). Kosong = transform root.")]
    [SerializeField] private Transform titikTambat;
    [SerializeField] private float jarakBerhenti = 1.2f;
    [SerializeField] private float waktuMaks = 5f;
    [Tooltip("Arah tarik diarahkan sedikit di atas Limau agar teman terangkat melewati tepi platform.")]
    [SerializeField] private Vector3 offsetTujuan = new Vector3(0f, 0.5f, 0f);

    private Rigidbody rb;
    private CapsuleCollider kapsul;
    private PlayerController pemain;
    private DragonSkillNetwork dragon; // opsional: Steel Skin = terlalu berat untuk ditarik

    private Transform sumber;
    private float kecepatan;
    private float waktuMulai;
    private bool ditarik;

    public Transform TitikTambat => titikTambat != null ? titikTambat : transform;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        kapsul = GetComponent<CapsuleCollider>();
        pemain = GetComponent<PlayerController>();
        dragon = GetComponent<DragonSkillNetwork>();
    }

    /// <summary>Mulai ditarik menuju sumber (Limau). Hanya valid di client pemilik.</summary>
    public bool MulaiTarik(Transform sumberTarik, float kecepatanTarik)
    {
        if (!photonView.IsMine || ditarik) return false;

        if (dragon != null && dragon.IsAktif(DragonSkillType.SteelSkin))
            return false; // sedang Steel Skin: beku dan terlalu berat

        sumber = sumberTarik;
        kecepatan = kecepatanTarik;
        waktuMulai = Time.time;
        ditarik = true;

        pemain.KontrolFisikaDilepas = true;
        return true;
    }

    public void Berhenti()
    {
        if (!ditarik) return;

        ditarik = false;
        sumber = null;
        pemain.KontrolFisikaDilepas = false;
    }

    private void FixedUpdate()
    {
        if (!ditarik) return;

        // Selesai / aman: Limau hilang, kelamaan, atau karakter ini mati (collider dimatikan saat mati).
        if (sumber == null || !kapsul.enabled || Time.time - waktuMulai > waktuMaks)
        {
            Berhenti();
            return;
        }

        Vector3 arah = (sumber.position + offsetTujuan) - rb.position;
        arah.z = 0f;

        if (arah.magnitude <= jarakBerhenti)
        {
            Berhenti();
            return;
        }

        rb.linearVelocity = arah.normalized * kecepatan;
    }

    private void OnDestroy()
    {
        if (ditarik && pemain != null) pemain.KontrolFisikaDilepas = false;
    }
}
