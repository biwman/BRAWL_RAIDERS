using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPun
{
    public float lifetime = 2f;
    public int damage = 10;
    public int ownerViewID;

    void Start()
    {
        if (photonView.IsMine)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;

        PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();

        if (hp != null && hp.photonView.ViewID != ownerViewID)
        {
            hp.photonView.RPC("TakeDamage", RpcTarget.All, damage, ownerViewID);
        }

        PhotonNetwork.Destroy(gameObject);
    }
}