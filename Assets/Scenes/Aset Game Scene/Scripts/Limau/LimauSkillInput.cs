using UnityEngine;

/// <summary>
/// LAYER 1 - INPUT.
/// Membaca E, arah gerak, lompat, dan arah bidik sutra. Tidak menyentuh fisika/jaringan.
/// Dipanggil oleh LimauSkillController tiap Update.
/// </summary>
public class LimauSkillInput : MonoBehaviour
{
    [SerializeField] private KeyCode tombolSkill = KeyCode.E;

    [Tooltip("true = sutra mengarah ke kursor mouse. false = arah tombol panah/WASD (default 45 derajat ke atas).")]
    [SerializeField] private bool bidikDenganMouse = true;

    public bool Ditekan { get; private set; }
    public bool Ditahan { get; private set; }
    public bool Dilepas { get; private set; }
    public bool LompatDitekan { get; private set; }
    public float Horizontal { get; private set; }
    public float Vertikal { get; private set; }

    private float arahHadapTerakhir = 1f;

    public void Sampel()
    {
        // Menghormati sistem yang sudah ada: menu pause & panel menang.
        bool boleh = !PlayerController.InputDikunci && !PlayerController.PermainanSelesai;
        bool sebelumnya = Ditahan;

        Ditahan = boleh && Input.GetKey(tombolSkill);
        Ditekan = boleh && Input.GetKeyDown(tombolSkill);
        Dilepas = sebelumnya && !Ditahan;
        LompatDitekan = boleh && Input.GetButtonDown("Jump");

        float rawH = boleh ? Input.GetAxisRaw("Horizontal") : 0f;
        float rawV = boleh ? Input.GetAxisRaw("Vertical") : 0f;
        Horizontal = Mathf.Abs(rawH) > 0.3f ? Mathf.Sign(rawH) : 0f;
        Vertikal = Mathf.Abs(rawV) > 0.3f ? Mathf.Sign(rawV) : 0f;

        if (Horizontal != 0f) arahHadapTerakhir = Horizontal;
    }

    /// <summary>
    /// Arah bidik (bidang XY, panjang 1). Dihitung hanya saat dibutuhkan (saat menembak),
    /// bukan tiap frame.
    /// </summary>
    public Vector2 HitungArahBidik(Vector3 asal)
    {
        Camera kamera = Camera.main;
        if (bidikDenganMouse && kamera != null)
        {
            // Proyeksikan kursor ke bidang permainan (Z sama dengan Limau).
            Ray sinar = kamera.ScreenPointToRay(Input.mousePosition);
            Plane bidang = new Plane(Vector3.forward, new Vector3(0f, 0f, asal.z));

            if (bidang.Raycast(sinar, out float jarak))
            {
                Vector3 titik = sinar.GetPoint(jarak);
                Vector2 arah = new Vector2(titik.x - asal.x, titik.y - asal.y);
                if (arah.sqrMagnitude > 0.01f) return arah.normalized;
            }
        }

        // Cadangan: tombol arah. Tanpa tombol -> menghadap terakhir + 45 derajat ke atas.
        Vector2 tombol = new Vector2(Horizontal, Vertikal);
        if (tombol == Vector2.zero) tombol = new Vector2(arahHadapTerakhir, 1f);
        return tombol.normalized;
    }
}
