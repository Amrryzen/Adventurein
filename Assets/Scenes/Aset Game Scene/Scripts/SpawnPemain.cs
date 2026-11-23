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
        string prefabKarakter = KarakterPemain.Ambil(PhotonNetwork.LocalPlayer, namaPrefab);

        if (!PhotonNetwork.LocalPlayer.CustomProperties.ContainsKey(KarakterPemain.Kunci))
        {
            // Default multiplayer: pemain pertama Dragon, pemain berikutnya Limau.
            // Peran tetap stabil menurut ActorNumber selama scene berpindah.
            prefabKarakter = PhotonNetwork.LocalPlayer.ActorNumber % 2 == 1
                ? "Dragon"
                : "Limau";
        }

        GameObject pemain = PhotonNetwork.Instantiate(
            prefabKarakter,
            spawn.position,
            spawn.rotation
        );

        PlayerController playerController =
            pemain.GetComponent<PlayerController>();

        if (playerController == null)
        {
            Debug.LogError(
                $"Prefab '{prefabKarakter}' tidak memiliki PlayerController.",
                pemain
            );
            return;
        }

        playerController.AturTitikRespawn(spawn.position, spawn.rotation);
    }
}