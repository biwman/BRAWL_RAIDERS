using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPun
{
    public float lifetime = 2f;
    public int damage = 10;

    void Start()
    {
        if (photonView.IsMine)
        {
            Destroy(gameObject, lifetime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // 🔥 tylko właściciel pocisku robi logikę
        if (!photonView.IsMine) return;

        PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();

        if (hp != null && hp.photonView != photonView)
        {
            // 🔥 damage liczy tylko MasterClient
            hp.photonView.RPC("TakeDamage", RpcTarget.MasterClient, damage);
        }

        // 🔥 niszczenie tylko raz (networkowo)
        PhotonNetwork.Destroy(gameObject);
    }
}
