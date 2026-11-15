using Photon.Pun;
using UnityEngine;

/// <summary>
/// Dipasang pada objek bertag "Pushable" (kotak/batu dorong).
/// - Pemilik (owner) PhotonView menjalankan fisika; client lain membuat Rigidbody kinematic
///   dan mengikuti data dari PhotonRigidbodyView.
/// - Dragon memanggil AmbilKendali() sebelum mendorong (butuh Ownership = Takeover).
/// Dipolling tiap FixedUpdate agar tetap benar saat owner pindah / master berganti.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(PhotonView))]
public class PushableObject : MonoBehaviourPun
{
    [Tooltip("Side-scroller: kunci rotasi agar kotak tidak terguling.")]
    [SerializeField] private bool kunciRotasi = true;

    private Rigidbody rb;
    private bool permintaanKendaliAktif;

    public Rigidbody Body => rb;
    public bool AdaKendali => photonView.IsMine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        RigidbodyConstraints c = RigidbodyConstraints.FreezePositionZ;
        if (kunciRotasi) c |= RigidbodyConstraints.FreezeRotation;
        rb.constraints = c;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void FixedUpdate()
    {
        bool harusKinematic = !photonView.IsMine;
        if (rb.isKinematic != harusKinematic)
            rb.isKinematic = harusKinematic;
    }

    public void AmbilKendali()
    {
        if (permintaanKendaliAktif) return;

        // Kirim sekali per usaha dorong agar dua pemain tidak saling merebut kendali.
        permintaanKendaliAktif = true;
        if (!photonView.IsMine) photonView.RequestOwnership();
    }

    public void LepasKendali()
    {
        permintaanKendaliAktif = false;
    }
}
