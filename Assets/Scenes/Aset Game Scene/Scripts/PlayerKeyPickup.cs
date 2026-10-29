using Photon.Pun;
using UnityEngine;

public class PlayerKeyPickup : MonoBehaviourPun
{
    [Header("Key Settings")]
    [SerializeField] private Transform keyAnchor;

    private bool punyaKunci;

    public bool PunyaKunci => punyaKunci;

    private void OnTriggerEnter(Collider other)
    {
        if (!photonView.IsMine) return;

        PlayerKeyPickup key = other.GetComponentInParent<PlayerKeyPickup>();

        if (key == null || key == this) return;
        if (!key.photonView.IsMine && key.photonView.Owner != null) return;
    }

    public void AmbilKunci(PhotonView keyView)
    {
        if (!photonView.IsMine || punyaKunci) return;

        photonView.RPC(
            nameof(RPC_SetKunci),
            RpcTarget.AllBuffered,
            keyView.ViewID
        );
    }

    [PunRPC]
    private void RPC_SetKunci(int keyViewID)
    {
        punyaKunci = true;

        PhotonView keyView = PhotonView.Find(keyViewID);
        if (keyView == null || keyAnchor == null) return;

        Transform keyTransform = keyView.transform;

        Collider keyCollider =
            keyTransform.GetComponent<Collider>();

        if (keyCollider != null)
            keyCollider.enabled = false;

        Rigidbody keyRb =
            keyTransform.GetComponent<Rigidbody>();

        if (keyRb != null)
            keyRb.isKinematic = true;

        keyTransform.SetParent(keyAnchor);
        keyTransform.localPosition = Vector3.zero;
        keyTransform.localRotation = Quaternion.identity;
    }
}
