using Photon.Pun;
using UnityEngine;

/// <summary>
/// LAYER 1 - INPUT.
/// Membaca tombol skill, arah gerak, lompat, dan arah bidik sutra. Tidak menyentuh fisika/jaringan.
/// Dipanggil oleh LimauSkillController tiap Update.
/// </summary>
public class LimauSkillInput : MonoBehaviour
{
    [Header("Tombol skill")]
    [SerializeField] private KeyCode tombolInflate = KeyCode.E;
    [SerializeField] private KeyCode tombolBioluminesensi = KeyCode.Q;
    [SerializeField] private KeyCode tombolGrapplingSilk = KeyCode.C;

    [Header("Aim Grappling Silk")]
    [SerializeField] private bool bidikDenganMouse = true;
    [SerializeField, Range(0.1f, 3f)] private float sensitivitasAimKeyboard = 1f;
    [SerializeField] private bool tampilkanPanelAim = true;

    public bool DitekanInflate { get; private set; }
    public bool DitekanBioluminesensi { get; private set; }
    public bool DitekanGrapplingSilk { get; private set; }
    public bool DitahanInflate { get; private set; }
    public bool DilepasInflate { get; private set; }
    public bool SedangMembidikDenganMouse { get; private set; }
    public bool LompatDitekan { get; private set; }
    public float Horizontal { get; private set; }
    public float Vertikal { get; private set; }

    private bool panelAimTerbuka;
    private GUIStyle gayaPanel;
    private GUIStyle gayaJudul;
    private float arahHadapTerakhir = 1f;

    public void Sampel()
    {
        bool boleh = !PlayerController.InputDikunci && !PlayerController.PermainanSelesai;

        bool inflateSebelumnya = DitahanInflate;
        DitahanInflate = boleh && Input.GetKey(tombolInflate);
        DitekanInflate = boleh && Input.GetKeyDown(tombolInflate);
        DilepasInflate = inflateSebelumnya && !DitahanInflate;
        DitekanBioluminesensi = boleh && Input.GetKeyDown(tombolBioluminesensi);
        DitekanGrapplingSilk = boleh && Input.GetKeyDown(tombolGrapplingSilk);
        SedangMembidikDenganMouse = boleh && bidikDenganMouse && Input.GetMouseButton(1);
        LompatDitekan = boleh && Input.GetButtonDown("Jump");

        float rawH = boleh ? Input.GetAxisRaw("Horizontal") : 0f;
        float rawV = boleh ? Input.GetAxisRaw("Vertical") : 0f;
        Horizontal = Mathf.Abs(rawH) > 0.3f ? Mathf.Sign(rawH) : 0f;
        Vertikal = Mathf.Abs(rawV) > 0.3f ? Mathf.Sign(rawV) : 0f;

        if (Horizontal != 0f) arahHadapTerakhir = Horizontal;
    }

    /// <summary>
    /// Arah bidik pada bidang XY. Mouse dipakai hanya saat klik kanan ditahan.
    /// Jika tidak, arah berasal dari WASD/panah.
    /// </summary>
    public Vector2 HitungArahBidik(Vector3 asal)
    {
        Camera kamera = Camera.main;
        if (SedangMembidikDenganMouse && kamera != null)
        {
            Ray sinar = kamera.ScreenPointToRay(Input.mousePosition);
            Plane bidang = new Plane(Vector3.forward, new Vector3(0f, 0f, asal.z));

            if (bidang.Raycast(sinar, out float jarak))
            {
                Vector3 titik = sinar.GetPoint(jarak);
                Vector2 arah = new Vector2(titik.x - asal.x, titik.y - asal.y);
                if (arah.sqrMagnitude > 0.01f) return arah.normalized;
            }
        }

        Vector2 tombol = new Vector2(Horizontal, Vertikal) * sensitivitasAimKeyboard;
        if (tombol == Vector2.zero) tombol = new Vector2(arahHadapTerakhir, 1f);
        return tombol.normalized;
    }

    private void Update()
    {
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView == null || !photonView.IsMine)
            return;

        if (Input.GetKeyDown(KeyCode.F1))
            panelAimTerbuka = !panelAimTerbuka;
    }

    private void OnGUI()
    {
        PhotonView photonView = GetComponent<PhotonView>();
        if (photonView == null || !photonView.IsMine ||
            !tampilkanPanelAim || !panelAimTerbuka || PlayerController.InputDikunci)
            return;

        gayaPanel = gayaPanel ?? new GUIStyle(GUI.skin.box);
        gayaJudul = gayaJudul ?? new GUIStyle(GUI.skin.label)
        {
            fontSize = 18,
            fontStyle = FontStyle.Bold
        };

        const float lebar = 300f;
        const float tinggi = 190f;
        Rect panel = new Rect(Screen.width - lebar - 20f, 20f, lebar, tinggi);
        GUI.Box(panel, "", gayaPanel);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 15f, lebar - 40f, 25f),
            "Pengaturan Aim Limau", gayaJudul);
        GUI.Label(new Rect(panel.x + 20f, panel.y + 48f, lebar - 40f, 22f),
            "C = Grappling Silk | Tahan klik kanan untuk aim mouse");

        bool mouseSebelumnya = bidikDenganMouse;
        bidikDenganMouse = GUI.Toggle(
            new Rect(panel.x + 20f, panel.y + 78f, lebar - 40f, 22f),
            bidikDenganMouse,
            "Aktifkan aim mouse");

        GUI.Label(new Rect(panel.x + 20f, panel.y + 108f, lebar - 40f, 22f),
            $"Sensitivitas keyboard: {sensitivitasAimKeyboard:0.0}");
        sensitivitasAimKeyboard = GUI.HorizontalSlider(
            new Rect(panel.x + 20f, panel.y + 135f, lebar - 40f, 18f),
            sensitivitasAimKeyboard, 0.1f, 3f);

        if (mouseSebelumnya != bidikDenganMouse)
            PlayerPrefs.SetInt("LimauAimMouse", bidikDenganMouse ? 1 : 0);
        PlayerPrefs.SetFloat("LimauAimSensitivity", sensitivitasAimKeyboard);
    }

    private void Awake()
    {
        bidikDenganMouse = PlayerPrefs.GetInt("LimauAimMouse", bidikDenganMouse ? 1 : 0) == 1;
        sensitivitasAimKeyboard = PlayerPrefs.GetFloat("LimauAimSensitivity", sensitivitasAimKeyboard);
    }
}
