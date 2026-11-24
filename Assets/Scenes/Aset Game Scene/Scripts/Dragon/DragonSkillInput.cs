using UnityEngine;

/// <summary>
/// LAYER 1 - INPUT.
/// Hanya membaca input dan menyediakannya sebagai properti. Tidak menyentuh fisika/jaringan.
/// Dipanggil oleh DragonSkillController setiap Update (urutan eksekusi terjamin).
/// </summary>
public class DragonSkillInput : MonoBehaviour
{
    [SerializeField] private KeyCode tombolSkill = KeyCode.E;
    [SerializeField] private KeyCode tombolSmash = KeyCode.Q;
    [SerializeField] private KeyCode tombolSteelSkin = KeyCode.C;

    public bool Ditekan { get; private set; }   // frame saat tombol baru ditekan
    public bool Ditahan { get; private set; }   // selama tombol ditahan
    public bool Dilepas { get; private set; }   // frame saat tombol dilepas (atau input terkunci)
    public bool DitekanSmash { get; private set; }
    public bool DitahanSmash { get; private set; }   // selama Q ditahan (charge Hantaman)
    public bool DilepasSmash { get; private set; }   // frame saat Q dilepas
    public bool DitekanSteelSkin { get; private set; }
    public bool DitahanSteelSkin { get; private set; }
    public bool DilepasSteelSkin { get; private set; }
    public float Horizontal { get; private set; } // -1, 0, atau 1

    public void Sampel()
    {
        // Menghormati sistem yang sudah ada: menu pause & panel menang.
        bool boleh = !PlayerController.InputDikunci && !PlayerController.PermainanSelesai;
        bool sebelumnya = Ditahan;

        Ditahan = boleh && Input.GetKey(tombolSkill);
        Ditekan = boleh && Input.GetKeyDown(tombolSkill);
        Dilepas = sebelumnya && !Ditahan;
        bool smashSebelumnya = DitahanSmash;
        DitahanSmash = boleh && Input.GetKey(tombolSmash);
        DitekanSmash = boleh && Input.GetKeyDown(tombolSmash);
        DilepasSmash = smashSebelumnya && !DitahanSmash;
        bool bajaSebelumnya = DitahanSteelSkin;
        DitahanSteelSkin = boleh && Input.GetKey(tombolSteelSkin);
        DitekanSteelSkin = boleh && Input.GetKeyDown(tombolSteelSkin);
        DilepasSteelSkin = bajaSebelumnya && !DitahanSteelSkin;

        float raw = boleh ? Input.GetAxisRaw("Horizontal") : 0f;
        Horizontal = Mathf.Abs(raw) > 0.3f ? Mathf.Sign(raw) : 0f;
    }
}