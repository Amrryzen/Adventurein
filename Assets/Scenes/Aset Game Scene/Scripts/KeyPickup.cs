using Photon.Pun;
using UnityEngine;

public class KeyPickup : MonoBehaviourPun
{
    private bool sudahDiambil;
    private PlayerController playerDekat;
    [SerializeField] private float jarakAmbil = 2f;

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || !player.photonView.IsMine)
            return;

        playerDekat = player;
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player != null && player == playerDekat)
            playerDekat = null;
    }

    private void Update()
    {
        if (sudahDiambil || playerDekat == null)
        {
            if (sudahDiambil)
                return;

            foreach (PlayerController pemain in FindObjectsByType<PlayerController>(FindObjectsSortMode.None))
            {
                if (pemain == null || !pemain.photonView.IsMine)
                    continue;

                float jarakX = Mathf.Abs(pemain.transform.position.x - transform.position.x);
                float jarakY = Mathf.Abs(pemain.transform.position.y - transform.position.y);
                if (jarakX <= jarakAmbil && jarakY <= jarakAmbil)
                {
                    playerDekat = pemain;
                    break;
                }
            }

            if (playerDekat == null)
                return;
        }

        if (!Input.GetKeyDown(KeyCode.F))
            return;

        PlayerController player = playerDekat;
        PlayerKeyState state =
            player.GetComponent<PlayerKeyState>();

        if (state == null || state.PunyaKunci || sudahDiambil)
            return;

        DragonDebug.Log($"Ambil kunci: Tekan SPACE oleh {DragonDebug.Label(player.photonView)}");

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayKeyPickup();

        photonView.RPC(
            nameof(RPC_AmbilKunci),
            RpcTarget.AllBuffered,
            player.photonView.ViewID
        );
    }

    [PunRPC]
    private void RPC_AmbilKunci(int playerViewID, PhotonMessageInfo info)
    {
        if (info.Sender == null)
            return;

        sudahDiambil = true;
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView == null) return;
        if (playerView.OwnerActorNr != info.Sender.ActorNumber)
            return;

        PlayerKeyState state = playerView.GetComponent<PlayerKeyState>();
        if (state == null)
        {
            Debug.LogError("PlayerKeyState tidak ditemukan pada pemain yang mengambil kunci.", playerView);
            return;
        }

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

        Transform anchor = state.KeyAnchor;
        if (anchor == null)
            anchor = CariTransformBernama(playerView.transform, "KeyAnchor");

        if (anchor == null)
        {
            Debug.LogError("KeyAnchor tidak ditemukan pada prefab pemain.", playerView);
            return;
        }

        transform.SetParent(anchor);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
    }

    private static Transform CariTransformBernama(Transform induk, string nama)
    {
        foreach (Transform child in induk.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == nama)
                return child;
        }

        return null;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private GUIStyle gayaPrompt;

    private void OnGUI()
    {
        if (sudahDiambil || playerDekat == null)
            return;

        if (gayaPrompt == null)
        {
            gayaPrompt = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter
            };
        }

        GUI.Box(new Rect(Screen.width / 2f - 140f, Screen.height - 100f, 280f, 40f), "");
        GUI.Label(
            new Rect(Screen.width / 2f - 130f, Screen.height - 92f, 260f, 28f),
            "<b>Tekan F untuk mengambil kunci</b>",
            gayaPrompt
        );
    }
#endif
}
