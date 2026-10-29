using System.Collections;
using Photon.Pun;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
public class PlayerController : MonoBehaviourPun, IPunObservable
{
    public static bool InputDikunci;
    public static bool PermainanSelesai;

    [Header("Gerak")]
    [SerializeField] private float kecepatan = 6f;
    [SerializeField] private float kecepatanLompat = 9f;
    [SerializeField] private LayerMask lapisTanah;

    [Header("Respawn")]
    [SerializeField] private float jedaRespawn;

    [Header("Visual")]
    [SerializeField] private Transform model;
    [SerializeField] private Animator animator;
    [SerializeField] private TextMeshProUGUI teksNama;

    [Header("Parameter Animator")]
    [SerializeField] private string paramSpeed = "Speed";
    [SerializeField] private string paramGrounded = "Grounded";

    private Rigidbody rb;
    private CapsuleCollider col;
    private float input;
    private bool jumpDitekan;
    private bool mati;
    private Vector3 titikRespawn;
    private Quaternion rotasiRespawn;
    private bool titikRespawnDiatur;

    private float animSpeed;
    private bool grounded;
    private float arahHadap = 1f;

    public bool TahanLompat => photonView.IsMine && Input.GetButton("Jump");

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
        col = GetComponent<CapsuleCollider>();
        rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
    }

    private void Start()
    {
        if (!titikRespawnDiatur)
        {
            titikRespawn = transform.position;
            rotasiRespawn = transform.rotation;
        }

        if (teksNama != null)
        {
            teksNama.text = photonView.Owner != null ? photonView.Owner.NickName : "Dummy";
            if (photonView.IsMine) teksNama.color = Color.green;
        }

        if (photonView.IsMine) CameraIkut.Daftar(transform);
        else rb.isKinematic = true;
    }

    private void OnDestroy()
    {
        CameraIkut.Hapus(transform);
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            bool boleh = !InputDikunci && !PermainanSelesai && !mati;

            float raw = boleh ? Input.GetAxisRaw("Horizontal") : 0f;
            input = Mathf.Abs(raw) > 0.3f ? Mathf.Sign(raw) : 0f;

            if (boleh && Input.GetButtonDown("Jump")) jumpDitekan = true;

            animSpeed = Mathf.Abs(input);
            if (input != 0f) arahHadap = input;

            if (GameAudio.Instance != null)
                GameAudio.Instance.SetWalking(input != 0f && grounded && !mati && !PermainanSelesai);
        }

        TerapkanVisual();
    }

    private void FixedUpdate()
    {
        if (!photonView.IsMine) return;

        if (mati)
        {
            rb.linearVelocity = Vector3.zero;
            return;
        }

        grounded = CekTanah();

        Vector3 v = rb.linearVelocity;
        v.x = input * kecepatan;

        if (jumpDitekan && grounded && v.y <= 0.1f)
        {
            v.y = kecepatanLompat;
            if (GameAudio.Instance != null)
                GameAudio.Instance.PlayJump();
        }
        jumpDitekan = false;

        rb.linearVelocity = v;
    }

    public void Mati()
    {
        if (!photonView.IsMine || mati) return;
        photonView.RPC(nameof(RPC_MintaMatiTim), RpcTarget.MasterClient);
    }

    [PunRPC]
    private void RPC_MintaMatiTim(PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient ||
            info.Sender == null ||
            info.Sender.ActorNumber != photonView.OwnerActorNr)
            return;

        photonView.RPC(nameof(RPC_MatikanSemuaPemain), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_MatikanSemuaPemain(PhotonMessageInfo info)
    {
        if (info.Sender == null ||
            PhotonNetwork.MasterClient == null ||
            info.Sender.ActorNumber != PhotonNetwork.MasterClient.ActorNumber)
            return;

        foreach (PlayerController pemain in FindObjectsByType<PlayerController>(
                     FindObjectsSortMode.None))
        {
            if (pemain.photonView.IsMine)
                pemain.MulaiMati();
        }
    }

    private void MulaiMati()
    {
        if (!photonView.IsMine || mati) return;
        StartCoroutine(AlurMati());
    }

    public void AturTitikRespawn(Vector3 posisi, Quaternion rotasi)
    {
        titikRespawn = posisi;
        rotasiRespawn = rotasi;
        titikRespawnDiatur = true;
    }

    public void Pantul(float kekuatan)
    {
        if (!photonView.IsMine || mati) return;
        Vector3 v = rb.linearVelocity;
        v.y = kekuatan;
        rb.linearVelocity = v;
    }

    private IEnumerator AlurMati()
    {
        mati = true;
        if (GameAudio.Instance != null)
        {
            GameAudio.Instance.SetWalking(false);
            GameAudio.Instance.PlayDeath();
        }

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        photonView.RPC(nameof(RPC_TampilkanPemain), RpcTarget.All, false);

        yield return new WaitForSeconds(jedaRespawn);

        rb.position = titikRespawn;
        rb.rotation = rotasiRespawn;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.useGravity = true;
        photonView.RPC(nameof(RPC_TampilkanPemain), RpcTarget.All, true);
        mati = false;
    }

    [PunRPC]
    private void RPC_TampilkanPemain(bool tampil)
    {
        if (model != null) model.gameObject.SetActive(tampil);
        col.enabled = tampil;
    }

    private bool CekTanah()
    {
        float r = Mathf.Min(col.bounds.extents.x, col.bounds.extents.z) * 0.9f;
        float jarak = col.bounds.extents.y - r + 0.05f;

        RaycastHit[] hits = Physics.SphereCastAll(
            col.bounds.center,
            r,
            Vector3.down,
            jarak,
            lapisTanah,
            QueryTriggerInteraction.Ignore
        );

        foreach (RaycastHit hit in hits)
        {
            if (hit.collider == col || hit.collider.transform.IsChildOf(transform))
                continue;

            if (hit.normal.y > 0.7f)
                return true;
        }

        return false;
    }

    private void TerapkanVisual()
    {
        if (model != null)
        {
            Quaternion target = Quaternion.Euler(0f, arahHadap > 0 ? 0f : 180f, 0f);
            model.localRotation = Quaternion.Slerp(model.localRotation, target, 15f * Time.deltaTime);
        }

        if (animator != null && animator.gameObject.activeInHierarchy)
        {
            animator.SetFloat(paramSpeed, animSpeed);
            animator.SetBool(paramGrounded, grounded);
        }

        if (teksNama != null && Camera.main != null)
            teksNama.transform.rotation = Camera.main.transform.rotation;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(animSpeed);
            stream.SendNext(grounded);
            stream.SendNext(arahHadap);
        }
        else
        {
            animSpeed = (float)stream.ReceiveNext();
            grounded = (bool)stream.ReceiveNext();
            arahHadap = (float)stream.ReceiveNext();
        }
    }
}