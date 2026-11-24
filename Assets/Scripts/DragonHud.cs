using Photon.Pun;
using UnityEngine;

/// <summary>
/// HUD minimalis sesuai GDD ("hanya ikon kecil untuk kemampuan: push, destroy, shield").
/// Tiga ikon di bawah layar, hanya untuk Dragon milik pemain lokal:
///  E PUSH  = menyala saat mendorong.
///  Q SMASH = isi naik saat charge (kuning berdenyut = siap Hantaman Penuh), lalu cooldown mengisi lagi.
///  C BAJA  = bar stamina; bingkai putih = jendela parry terbuka.
/// Digambar dengan OnGUI tanpa aset.
/// </summary>
public class DragonHud : MonoBehaviour
{
    [SerializeField] private bool tampil = true;
    [SerializeField] private float ukuran = 56f;
    [SerializeField] private float jarak = 10f;
    [SerializeField] private float margin = 24f;

    private PhotonView view;
    private DragonSkillNetwork jaringan;
    private DragonDestroySkill smash;
    private DragonSteelSkinSkill baja;
    private GUIStyle gayaTombol;
    private GUIStyle gayaNama;

    private void Awake()
    {
        view = GetComponent<PhotonView>();
        jaringan = GetComponent<DragonSkillNetwork>();
        smash = GetComponent<DragonDestroySkill>();
        baja = GetComponent<DragonSteelSkinSkill>();
    }

    private void OnGUI()
    {
        if (!tampil || view == null || !view.IsMine || PlayerController.PermainanSelesai) return;

        SiapkanGaya();

        float total = ukuran * 3f + jarak * 2f;
        float x = (Screen.width - total) * 0.5f;
        float y = Screen.height - ukuran - margin;

        // PUSH
        bool push = jaringan != null && jaringan.IsAktif(DragonSkillType.Push);
        Slot(new Rect(x, y, ukuran, ukuran), "E", "PUSH", new Color(0.2f, 0.8f, 1f), push ? 1f : 0f, push, false);

        // SMASH
        float isiSmash = 0f;
        Color warnaSmash = new Color(1f, 0.55f, 0.15f);
        bool mengisi = smash != null && smash.Mengisi;
        if (smash != null)
        {
            if (mengisi)
            {
                isiSmash = smash.IsiNormal;
                if (isiSmash >= 0.99f)
                    warnaSmash = Color.Lerp(warnaSmash, Color.yellow, Mathf.PingPong(Time.time * 6f, 1f));
            }
            else
            {
                isiSmash = 1f - smash.CooldownNormal;
                warnaSmash.a = 0.55f;
            }
        }
        Slot(new Rect(x + ukuran + jarak, y, ukuran, ukuran), "Q", "SMASH", warnaSmash, isiSmash, mengisi, false);

        // BAJA
        bool aktif = baja != null && baja.Aktif;
        float stamina = baja != null ? baja.StaminaNormal : 0f;
        Color warnaBaja = stamina < 0.25f ? new Color(1f, 0.35f, 0.3f) : new Color(0.75f, 0.78f, 0.85f);
        Slot(new Rect(x + (ukuran + jarak) * 2f, y, ukuran, ukuran), "C", "BAJA", warnaBaja, stamina, aktif,
             baja != null && baja.JendelaParryAktif);
    }

    private void Slot(Rect r, string tombol, string nama, Color warna, float isi, bool aktif, bool bingkaiPutih)
    {
        Kotak(r, new Color(0f, 0f, 0f, 0.55f));

        float tinggiIsi = r.height * Mathf.Clamp01(isi);
        Kotak(new Rect(r.x, r.yMax - tinggiIsi, r.width, tinggiIsi), warna);

        if (aktif || bingkaiPutih)
            Bingkai(r, bingkaiPutih ? Color.white : warna, 3f);

        GUI.Label(new Rect(r.x, r.y + 2f, r.width, 22f), tombol, gayaTombol);
        GUI.Label(new Rect(r.x, r.yMax - 16f, r.width, 16f), nama, gayaNama);
    }

    private static void Kotak(Rect r, Color warna)
    {
        Color lama = GUI.color;
        GUI.color = warna;
        GUI.DrawTexture(r, Texture2D.whiteTexture);
        GUI.color = lama;
    }

    private static void Bingkai(Rect r, Color warna, float tebal)
    {
        Kotak(new Rect(r.x, r.y, r.width, tebal), warna);
        Kotak(new Rect(r.x, r.yMax - tebal, r.width, tebal), warna);
        Kotak(new Rect(r.x, r.y, tebal, r.height), warna);
        Kotak(new Rect(r.xMax - tebal, r.y, tebal, r.height), warna);
    }

    private void SiapkanGaya()
    {
        if (gayaTombol != null) return;

        gayaTombol = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold
        };
        gayaTombol.normal.textColor = Color.white;

        gayaNama = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.LowerCenter,
            fontSize = 10,
            fontStyle = FontStyle.Bold
        };
        gayaNama.normal.textColor = new Color(1f, 1f, 1f, 0.85f);
    }
}
