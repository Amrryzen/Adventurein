using System;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

/// <summary>
/// HUB / STATE MACHINE. Memilih skill berdasarkan level bioma dan meneruskan Update/FixedUpdate
/// ke skill aktif. Hanya berjalan di client pemilik Dragon (photonView.IsMine);
/// pemain remote cukup menerima event visual dari DragonSkillNetwork.
/// </summary>
[RequireComponent(typeof(DragonSkillInput), typeof(DragonPhysicsHandler), typeof(DragonSkillNetwork))]
public class DragonSkillController : MonoBehaviourPun
{
    public event Action<DragonSkillType> SkillBerganti;
    public DragonSkillType SkillSaatIni { get; private set; } = DragonSkillType.None;

    /// <summary>Dipakai hazard (angin/burung) untuk melewati dorongan saat Steel Skin aktif.</summary>
    public bool KebalDorongan => jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin);

    private DragonSkillInput masukan;
    private DragonPhysicsHandler fisik;
    private DragonSkillNetwork jaringan;
    private readonly Dictionary<DragonSkillType, DragonSkillBase> daftarSkill = new();
    private bool sedangMati;

    private void Awake()
    {
        masukan = GetComponent<DragonSkillInput>();
        fisik = GetComponent<DragonPhysicsHandler>();
        jaringan = GetComponent<DragonSkillNetwork>();

        foreach (DragonSkillBase skill in GetComponents<DragonSkillBase>())
        {
            skill.Inisialisasi(masukan, fisik, jaringan);
            daftarSkill[skill.Tipe] = skill;
        }
    }

    private void Start()
    {
        // Pastikan eksekusi input hanya di pemilik karakter.
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        SkillSaatIni = DragonSkillType.None;
        SkillBerganti?.Invoke(DragonSkillType.Push);
        SkillBerganti?.Invoke(DragonSkillType.Destroy);
        SkillBerganti?.Invoke(DragonSkillType.SteelSkin);
    }

    private void OnDisable()
    {
        foreach (DragonSkillBase skill in daftarSkill.Values)
            skill.OnKeluar();
    }

    private void Update()
    {
        // Mati -> batalkan skill (mis. lepas Steel Skin) sekali, lalu diam sampai respawn.
        if (fisik.Mati)
        {
            if (!sedangMati)
            {
                sedangMati = true;
                foreach (DragonSkillBase skill in daftarSkill.Values)
                    skill.OnKeluar();
                DragonDebug.Log("Dragon mati: skill dibatalkan");
            }
            return;
        }

        if (sedangMati)
        {
            sedangMati = false;
            DragonDebug.Log("Dragon respawn: skill aktif lagi");
        }

        masukan.Sampel();
        foreach (DragonSkillBase skill in daftarSkill.Values)
            skill.OnUpdateSkill();
    }

    private void FixedUpdate()
    {
        if (fisik.Mati) return;
        foreach (DragonSkillBase skill in daftarSkill.Values)
            skill.OnFixedUpdateSkill();
    }
}
