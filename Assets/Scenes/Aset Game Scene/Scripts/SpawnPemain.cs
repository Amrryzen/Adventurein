using Photon.Pun;
using UnityEngine;

public class SpawnPemain : MonoBehaviour
{
    public Transform[] titikSpawn;
    public string namaPrefab = "Player";

    private void Start()
    {
        int i = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % titikSpawn.Length;
        Transform spawn = titikSpawn[i];
        GameObject pemain = PhotonNetwork.Instantiate(
            namaPrefab,
            spawn.position,
            spawn.rotation
        );

        PlayerController playerController =
            pemain.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError(
                $"Prefab '{namaPrefab}' tidak memiliki PlayerController.",
                pemain
            );
            return;
        }

        playerController.AturTitikRespawn(spawn.position, spawn.rotation);
    }
}