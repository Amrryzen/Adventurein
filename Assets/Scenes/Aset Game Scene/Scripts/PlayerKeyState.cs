
using UnityEngine;

public class PlayerKeyState : MonoBehaviour
{
    [SerializeField] private Transform keyAnchor;

    public bool PunyaKunci { get; private set; }
    public Transform KeyAnchor => keyAnchor;

    public void SetPunyaKunci(bool status)
    {
        PunyaKunci = status;
    }
}
