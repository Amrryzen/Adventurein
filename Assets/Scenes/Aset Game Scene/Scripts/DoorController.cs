
using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorController : MonoBehaviourPun
{
    [Header("Door")]
    [SerializeField] private Transform modelPintu;
    [SerializeField] private float tinggiBuka = 5f;
    [SerializeField] private float kecepatanBuka = 2f;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI teksPrompt;
    [Header("Progresi Tes")]
    [SerializeField] private int levelSesudahBuka = 0;
    [SerializeField] private string namaSceneSesudahBuka = "";
    [SerializeField] private float jedaPindahScene = 1f;

    private bool playerDiArea;
    private bool pintuTerbuka;
    private PlayerKeyState playerSaatIni;
    private Vector3 posisiAwal;
    private bool pindahSceneSudahDiminta;

    private void Start()
    {
        if (modelPintu != null)
            posisiAwal = modelPintu.localPosition;

        if (teksPrompt != null)
            teksPrompt.gameObject.SetActive(false);

    }

    private void Update()
    {
        if (pintuTerbuka && modelPintu != null)
        {
            Vector3 arahBuka = modelPintu.parent != null
                ? modelPintu.parent.InverseTransformVector(Vector3.up * tinggiBuka)
                : Vector3.up * tinggiBuka;
            Vector3 posisiTujuan = posisiAwal + arahBuka;
            modelPintu.localPosition = Vector3.MoveTowards(
                modelPintu.localPosition,
                posisiTujuan,
                kecepatanBuka * Time.deltaTime
            );
        }

        if (!playerDiArea || pintuTerbuka) return;

        if (Input.GetKeyDown(KeyCode.F))
        {
            if (playerSaatIni == null) return;

            if (!playerSaatIni.PunyaKunci)
            {
                if (teksPrompt != null)
                    teksPrompt.text = "Kamu membutuhkan kunci!";
                return;
            }

            if (teksPrompt != null)
                teksPrompt.text = "Membuka pintu...";

            photonView.RPC(
                nameof(RPC_MintaBukaPintu),
                RpcTarget.MasterClient,
                playerSaatIni.GetComponent<PhotonView>().ViewID
            );
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || !player.photonView.IsMine)
            return;

        playerDiArea = true;
        playerSaatIni = player.GetComponent<PlayerKeyState>();

        if (teksPrompt != null)
        {
            teksPrompt.gameObject.SetActive(!pintuTerbuka);
            teksPrompt.text = playerSaatIni != null &&
                              playerSaatIni.PunyaKunci
                ? "Tekan F untuk membuka pintu"
                : "Kamu membutuhkan kunci!";
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController player =
            other.GetComponentInParent<PlayerController>();

        if (player == null || !player.photonView.IsMine)
            return;

        playerDiArea = false;
        playerSaatIni = null;

        if (teksPrompt != null)
            teksPrompt.gameObject.SetActive(false);
    }

    [PunRPC]
    private void RPC_MintaBukaPintu(int playerViewID, PhotonMessageInfo info)
    {
        if (!PhotonNetwork.IsMasterClient || pintuTerbuka) return;

        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView == null || playerView.OwnerActorNr != info.Sender.ActorNumber) return;

        PlayerKeyState state = playerView.GetComponent<PlayerKeyState>();
        if (state == null || !state.PunyaKunci) return;

        // Kirimkan ID player yang membuka pintu ke semua client
        photonView.RPC(nameof(RPC_BukaPintu), RpcTarget.AllBuffered, playerViewID);
    }

    [PunRPC]
    // Tambahkan parameter playerViewID di sini
    private void RPC_BukaPintu(int playerViewID) 
    {
        if (pintuTerbuka) return;
        pintuTerbuka = true;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayGateOpen();

        if (teksPrompt != null) teksPrompt.gameObject.SetActive(false);
        
        if (PanelMenang.Instance != null)
            PanelMenang.Instance.Tampil();
        else
            Debug.LogError("PanelMenang belum dipasang di scene.");

        // Hapus kunci dari tangan/anchor player
        PhotonView playerView = PhotonView.Find(playerViewID);
        if (playerView != null)
        {
            PlayerKeyState state = playerView.GetComponent<PlayerKeyState>();
            if (state != null)
            {
                state.SetPunyaKunci(false); // Reset status punya kunci
                
                // Hancurkan objek kunci (child) yang menempel di keyAnchor
                foreach (Transform child in state.KeyAnchor)
                {
                    Destroy(child.gameObject);
                }
            }
        }

                if (levelSesudahBuka > 0)
                {
                    BiomeProgression progresi = BiomeProgression.Instance;
                    if (progresi != null)
                        progresi.MintaGantiLevel(levelSesudahBuka);
                }

                if (!string.IsNullOrEmpty(namaSceneSesudahBuka))
                    StartCoroutine(PindahSceneTes());
    }

    private System.Collections.IEnumerator PindahSceneTes()
    {
        if (pindahSceneSudahDiminta)
            yield break;

        pindahSceneSudahDiminta = true;
        yield return new WaitForSeconds(jedaPindahScene);

        if (PhotonNetwork.InRoom)
        {
            if (PhotonNetwork.IsMasterClient)
                PhotonNetwork.LoadLevel(namaSceneSesudahBuka);
        }
        else
        {
            SceneManager.LoadScene(namaSceneSesudahBuka);
        }
    }
}
