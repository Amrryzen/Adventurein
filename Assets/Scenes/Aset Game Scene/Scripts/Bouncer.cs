using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Bouncer : MonoBehaviour
{
    [SerializeField] private float pantulanDasar = 6f;
    [SerializeField] private float pantulanLompat = 12f;

    private Collider kol;

    private void Awake()
    {
        kol = GetComponent<Collider>();
    }

    private void OnCollisionEnter(Collision c)
    {
        PlayerController p = c.collider.GetComponentInParent<PlayerController>();
        if (p == null || !p.photonView.IsMine) return;
        if (c.collider.bounds.min.y < kol.bounds.max.y - 0.2f) return;

        if (GameAudio.Instance != null)
            GameAudio.Instance.PlayBouncer();

        p.Pantul(p.TahanLompat ? pantulanLompat : pantulanDasar);
    }
}