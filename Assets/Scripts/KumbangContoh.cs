
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(PhotonView), typeof(Collider))]
public class KumbangContoh : MonoBehaviourPun, IBisaDistun
{
    private enum AIState
    {
        Patrol,
        Chase,
        Escape,
        Stunned
    }

    [Header("Waypoint Patrol")]
    [SerializeField] private Transform titikA;
    [SerializeField] private Transform titikB;
    [SerializeField] private float kecepatanPatroli = 1.5f;
    [SerializeField] private float jarakToleransi = 0.1f;

    [Header("Player Detection")]
    [SerializeField] private float radiusDeteksi = 6f;
    [SerializeField] private float radiusBerhentiChase = 9f;
    [SerializeField] private float kecepatanChase = 2.5f;
    [SerializeField] private LayerMask layerPlayer = ~0;

    [Header("Escape")]
    [SerializeField] private float kecepatanKabur = 3.5f;
    [SerializeField] private float marginKabur = 1.5f;

    [Header("Stun")]
    [SerializeField] private float kecepatanRotasi = 10f;

    private AIState state = AIState.Patrol;

    private Vector3 posisiAsal;
    private Transform targetWaypoint;
    private PlayerController targetPlayer;

    private float distunSampai;

    private void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void Start()
    {
        posisiAsal = transform.position;

        // Inisialisasi waypoint.
        if (titikA == null || titikB == null)
        {
            Debug.LogWarning(
                "Waypoint Kumbang belum diatur. Patrol menggunakan jarak default.",
                this
            );
        }

        targetWaypoint = titikB;
    }

    private void Update()
    {
        // STUNNED memiliki prioritas tertinggi.
        if (Time.time < distunSampai)
        {
            UbahState(AIState.Stunned);
        }
        else
        {
            // Prioritas kedua: mengejar player.
            PlayerController player = CariPlayer();

            if (player != null)
            {
                targetPlayer = player;
                UbahState(AIState.Chase);
            }
            else
            {
                targetPlayer = null;

                // Prioritas ketiga: kabur dari LimauAura.
                if (LimauAura.TryCariPengusir(
                    transform.position,
                    out LimauAura aura))
                {
                    UbahState(AIState.Escape);
                }
                else
                {
                    UbahState(AIState.Patrol);
                }
            }
        }

        // Jalankan perilaku sesuai state.
        switch (state)
        {
            case AIState.Patrol:
                Patroli();
                break;

            case AIState.Chase:
                KejarPlayer();
                break;

            case AIState.Escape:
                Kabur();
                break;

            case AIState.Stunned:
                break;
        }

        // Animasi rotasi ketika terkena stun.
        Quaternion rotasiTarget = state == AIState.Stunned
            ? Quaternion.Euler(0f, 0f, 180f)
            : Quaternion.identity;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rotasiTarget,
            kecepatanRotasi * Time.deltaTime
        );
    }

    private void UbahState(AIState stateBaru)
    {
        if (state == stateBaru)
            return;

        state = stateBaru;
    }

    private PlayerController CariPlayer()
    {
        Collider[] objek = Physics.OverlapSphere(
            transform.position,
            radiusDeteksi,
            layerPlayer,
            QueryTriggerInteraction.Ignore
        );

        PlayerController pemainTerdekat = null;
        float jarakTerdekat = float.MaxValue;

        foreach (Collider col in objek)
        {
            PlayerController p =
                col.GetComponentInParent<PlayerController>();

            if (p == null || !p.photonView.IsMine)
                continue;

            float jarak = Vector3.Distance(
                transform.position,
                p.transform.position
            );

            if (jarak < jarakTerdekat)
            {
                jarakTerdekat = jarak;
                pemainTerdekat = p;
            }
        }

        return pemainTerdekat;
    }

    private void Patroli()
    {
        if (titikA == null || titikB == null)
            return;

        if (targetWaypoint == null)
            targetWaypoint = titikA;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetWaypoint.position,
            kecepatanPatroli * Time.deltaTime
        );

        if (Vector3.Distance(
            transform.position,
            targetWaypoint.position) <= jarakToleransi)
        {
            targetWaypoint =
                targetWaypoint == titikA ? titikB : titikA;
        }
    }

    private void KejarPlayer()
    {
        if (targetPlayer == null)
            return;

        Vector3 target = targetPlayer.transform.position;

        // Kumbang hanya bergerak secara horizontal.
        target.y = transform.position.y;
        target.z = transform.position.z;

        float jarak = Vector3.Distance(
            transform.position,
            targetPlayer.transform.position
        );

        // Berhenti ketika sudah cukup dekat.
        if (jarak <= 0.8f)
            return;

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            kecepatanChase * Time.deltaTime
        );
    }

    private void Kabur()
    {
        // Cari kembali sumber aura.
        if (!LimauAura.TryCariPengusir(
            transform.position,
            out LimauAura aura))
            return;

        float arahMenjauh = Mathf.Sign(
            transform.position.x - aura.transform.position.x
        );

        if (arahMenjauh == 0f)
            arahMenjauh = 1f;

        Vector3 posisiBaru = transform.position +
            Vector3.right *
            (arahMenjauh * kecepatanKabur * Time.deltaTime);

        float batas = 3f + marginKabur;

        posisiBaru.x = Mathf.Clamp(
            posisiBaru.x,
            posisiAsal.x - batas,
            posisiAsal.x + batas
        );

        transform.position = posisiBaru;
    }

    private void OnTriggerStay(Collider other)
    {
        if (Time.time < distunSampai)
            return;

        PlayerController p =
            other.GetComponentInParent<PlayerController>();

        if (p != null && p.photonView.IsMine)
        {
            p.Mati();
        }
    }

    public void Distun(float durasi, Vector3 dariTitik)
    {
        if (photonView.IsMine)
        {
            photonView.RPC(
                nameof(RPC_Distun),
                RpcTarget.All,
                durasi
            );
        }
    }

    [PunRPC]
    private void RPC_Distun(float durasi)
    {
        distunSampai = Time.time + durasi;
        state = AIState.Stunned;
    }

    private void OnDrawGizmosSelected()
    {
        // Radius deteksi player.
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radiusDeteksi);

        // Waypoint.
        if (titikA != null && titikB != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(titikA.position, 0.15f);
            Gizmos.DrawSphere(titikB.position, 0.15f);
            Gizmos.DrawLine(titikA.position, titikB.position);
        }
    }
}
