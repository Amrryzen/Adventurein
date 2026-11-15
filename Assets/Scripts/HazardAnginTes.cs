using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class HazardAnginTes : MonoBehaviour
{
    [SerializeField] private float gayaAngin = 8f;

    private readonly HashSet<PlayerController> pemainDalamZona = new HashSet<PlayerController>();

    private void OnTriggerEnter(Collider other)
    {
        PlayerController pemain = other.GetComponentInParent<PlayerController>();
        if (pemain != null)
        {
            pemainDalamZona.Add(pemain);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        PlayerController pemain = other.GetComponentInParent<PlayerController>();
        if (pemain != null)
        {
            pemainDalamZona.Remove(pemain);
        }
    }

    private void FixedUpdate()
    {
        foreach (PlayerController pemain in pemainDalamZona)
        {
            if (pemain == null || !pemain.photonView.IsMine)
            {
                continue;
            }

            DragonSkillNetwork jaringan = pemain.GetComponent<DragonSkillNetwork>();
            if (jaringan != null && jaringan.IsAktif(DragonSkillType.SteelSkin))
            {
                continue;
            }

            Rigidbody badan = pemain.GetComponent<Rigidbody>();
            if (badan != null)
            {
                badan.AddForce(Vector3.right * gayaAngin, ForceMode.Acceleration);
            }
        }
    }
}
