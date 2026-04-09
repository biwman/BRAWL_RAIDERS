using UnityEngine;
using Photon.Pun;

public class Bullet : MonoBehaviourPun
{
    public float lifetime = 2f;
    public int damage = 10;

    void Start()
    {
        Debug.Log("BULLET SPAWN");
        Destroy(gameObject, lifetime);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        PlayerHealth hp = collision.gameObject.GetComponent<PlayerHealth>();

        if (hp != null && hp.photonView != photonView)
        {
            hp.photonView.RPC("TakeDamage", RpcTarget.All, damage);
        }

        if (photonView.IsMine)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }
}
