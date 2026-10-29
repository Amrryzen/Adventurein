using Photon.Pun;
using UnityEngine;

public class KeyPickup : MonoBehaviourPun
{
    private bool sudahDiambil;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || !player.photonView.IsMine)
            return;

        PlayerKeyState state =
            player.GetComponent<PlayerKeyState>();

        if (state == null || state.PunyaKunci || sudahDiambil)
            return;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayKeyPickup();

        photonView.RPC(
            nameof(RPC_AmbilKunci),
            RpcTarget.AllBuffered,
            player.photonView.ViewID
        );
    }

    [PunRPC]
    private void RPC_AmbilKunci(int playerViewID)
    {
        sudahDiambil = true;
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView == null) return;

        PlayerKeyState state = playerView.GetComponent<PlayerKeyState>();
        if (state == null) return;

        state.SetPunyaKunci(true);

        Collider keyCollider = GetComponent<Collider>();
        if (keyCollider != null) keyCollider.enabled = false;

        Rigidbody keyRb = GetComponent<Rigidbody>();
        if (keyRb != null) keyRb.isKinematic = true;

        // --- EFEK SAAT DIAMBIL ---
        
        // 1. Matikan skrip animasi agar berhenti berputar
        KeyAnimation keyAnim = GetComponent<KeyAnimation>();
        if (keyAnim != null) keyAnim.enabled = false;

        // 2. Matikan komponen cahaya
        Light cahaya = GetComponentInChildren<Light>();
        if (cahaya != null) cahaya.enabled = false;

        // 3. Kecilkan ukuran kunci menjadi 40% dari ukuran aslinya
        transform.localScale = transform.localScale * 0.4f;

        // -------------------------

        transform.SetParent(state.KeyAnchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }
}
