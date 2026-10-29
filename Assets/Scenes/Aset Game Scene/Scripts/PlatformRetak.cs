using System.Collections;
using System.Collections.Generic;
using Photon.Pun;
using UnityEngine;

[RequireComponent(typeof(Collider), typeof(PhotonView))]
public class PlatformRetak : MonoBehaviourPun
{
    [SerializeField] private float waktuHancur = 3f;
    [SerializeField] private float waktuMuncul = 2f;
    [SerializeField] private float getar = 0.03f;
    [SerializeField] private GameObject modelPecahan;

    private Collider kol;
    private Renderer[] visualUtuh;
    private readonly List<Pecahan> pecahan = new();
    private Vector3 posAwal;
    private bool berjalan;

    private sealed class Pecahan
    {
        public Transform transform;
        public Vector3 posisiLokal;
        public Quaternion rotasiLokal;
        public Vector3 skalaLokal;
        public Rigidbody rigidbody;
        public Collider collider;
    }

    private void Awake()
    {
        kol = GetComponent<Collider>();
        visualUtuh = GetComponents<Renderer>();
        posAwal = transform.localPosition;

        if (modelPecahan == null)
            return;

        modelPecahan.SetActive(false);
        SiapkanPecahan();
    }

    private void OnCollisionEnter(Collision c)
    {
        if (berjalan) return;

        PlayerController p = c.collider.GetComponentInParent<PlayerController>();
        if (p == null || !p.photonView.IsMine) return;
        if (c.collider.bounds.min.y < kol.bounds.max.y - 0.2f) return;

        photonView.RPC(nameof(RPC_Mulai), RpcTarget.All);
    }

    [PunRPC]
    private void RPC_Mulai()
    {
        if (!berjalan) StartCoroutine(Alur());
    }

    private IEnumerator Alur()
    {
        berjalan = true;

        float t = 0f;
        while (t < waktuHancur)
        {
            t += Time.deltaTime;
            transform.localPosition = posAwal + Random.insideUnitSphere * getar;
            yield return null;
        }

        transform.localPosition = posAwal;
        AturPlatformUtuh(false);
        AktifkanPecahan();

        yield return new WaitForSeconds(waktuMuncul);

        PulihkanPecahan();
        AturPlatformUtuh(true);
        berjalan = false;
    }

    private void SiapkanPecahan()
    {
        MeshFilter[] meshFilters = modelPecahan.GetComponentsInChildren<MeshFilter>(true);
        foreach (MeshFilter meshFilter in meshFilters)
        {
            if (meshFilter.sharedMesh == null)
                continue;

            MeshCollider meshCollider = meshFilter.GetComponent<MeshCollider>();
            if (meshCollider == null)
                meshCollider = meshFilter.gameObject.AddComponent<MeshCollider>();

            meshCollider.sharedMesh = meshFilter.sharedMesh;
            meshCollider.convex = true;
            meshCollider.enabled = false;

            Rigidbody body = meshFilter.GetComponent<Rigidbody>();
            if (body == null)
                body = meshFilter.gameObject.AddComponent<Rigidbody>();

            body.isKinematic = true;
            body.useGravity = false;

            pecahan.Add(new Pecahan
            {
                transform = meshFilter.transform,
                posisiLokal = meshFilter.transform.localPosition,
                rotasiLokal = meshFilter.transform.localRotation,
                skalaLokal = meshFilter.transform.localScale,
                rigidbody = body,
                collider = meshCollider
            });
        }

        if (pecahan.Count == 0)
        {
            Debug.LogError(
                $"Model pecahan '{modelPecahan.name}' tidak memiliki MeshFilter untuk dibuat sebagai pecahan fisika.",
                this
            );
        }
        else if (pecahan.Count == 1)
        {
            Debug.LogWarning(
                $"Model pecahan '{modelPecahan.name}' hanya memiliki satu mesh. Pastikan setiap pecahan Blender diekspor sebagai objek terpisah.",
                this
            );
        }
    }

    private void AktifkanPecahan()
    {
        if (modelPecahan == null)
            return;

        modelPecahan.SetActive(true);
        foreach (Renderer renderer in visualUtuh)
            renderer.enabled = false;

        foreach (Pecahan bagian in pecahan)
        {
            bagian.rigidbody.isKinematic = false;
            bagian.rigidbody.useGravity = true;
            bagian.collider.enabled = true;
        }
    }

    private void PulihkanPecahan()
    {
        if (modelPecahan == null)
            return;

        foreach (Pecahan bagian in pecahan)
        {
            bagian.rigidbody.isKinematic = true;
            bagian.rigidbody.useGravity = false;
            bagian.rigidbody.linearVelocity = Vector3.zero;
            bagian.rigidbody.angularVelocity = Vector3.zero;
            bagian.collider.enabled = false;
            bagian.transform.localPosition = bagian.posisiLokal;
            bagian.transform.localRotation = bagian.rotasiLokal;
            bagian.transform.localScale = bagian.skalaLokal;
        }

        modelPecahan.SetActive(false);
    }

    private void AturPlatformUtuh(bool aktif)
    {
        kol.enabled = aktif;
        foreach (Renderer renderer in visualUtuh)
            renderer.enabled = aktif;
    }
}