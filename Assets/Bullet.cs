using UnityEngine;
using Photon.Pun;
using System.Collections;

public class Bullet : MonoBehaviourPun
{
    public int damage = 10;
    public int ownerViewID;
    public float rangeMultiplier = 15f;
    public float fallbackPlayerLength = 1f;
    public float safetyLifetime = 10f;
    public float minimumWorldRadius = 0.12f;

    Vector2 spawnPosition;
    float maxTravelDistance;

    void Start()
    {
        spawnPosition = transform.position;
        maxTravelDistance = GetOwnerLength() * rangeMultiplier;

        if (GetComponent<HideInNebulaTarget>() == null)
        {
            gameObject.AddComponent<HideInNebulaTarget>();
        }

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        CircleCollider2D collider2D = GetComponent<CircleCollider2D>();
        if (collider2D != null)
        {
            float worldRadius = GetWorldRadius(collider2D);
            if (worldRadius < minimumWorldRadius)
            {
                SetWorldRadius(collider2D, minimumWorldRadius);
            }
        }

        if (maxTravelDistance <= 0f)
        {
            maxTravelDistance = fallbackPlayerLength * rangeMultiplier;
        }

        StartCoroutine(DestroyAfterSafetyLifetime());
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        if (Vector2.Distance(spawnPosition, transform.position) >= maxTravelDistance)
        {
            DestroyBullet();
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!photonView.IsMine) return;

        // ważne: szukamy w parent (na wypadek colliderów)
        PlayerHealth hp = collision.gameObject.GetComponentInParent<PlayerHealth>();

        if (hp != null && hp.photonView.ViewID != ownerViewID)
        {
            // tylko MASTER dostaje damage
            hp.photonView.RPC("TakeDamage", RpcTarget.MasterClient, damage, ownerViewID);
        }

        DestroyBullet();
    }

    IEnumerator DestroyAfterSafetyLifetime()
    {
        yield return new WaitForSeconds(safetyLifetime);

        if (this == null || gameObject == null)
            yield break;

        if (PhotonNetwork.IsConnected && photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void DestroyBullet()
    {
        if (PhotonNetwork.IsConnected && photonView != null)
        {
            if (photonView.IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    float GetOwnerLength()
    {
        PhotonView ownerView = PhotonView.Find(ownerViewID);
        if (ownerView == null)
            return fallbackPlayerLength;

        SpriteRenderer spriteRenderer = ownerView.GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            return Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y);
        }

        Collider2D collider2D = ownerView.GetComponentInChildren<Collider2D>();
        if (collider2D != null)
        {
            return Mathf.Max(collider2D.bounds.size.x, collider2D.bounds.size.y);
        }

        return fallbackPlayerLength;
    }

    float GetWorldRadius(CircleCollider2D collider2D)
    {
        Vector3 scale = collider2D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        return collider2D.radius * maxScale;
    }

    void SetWorldRadius(CircleCollider2D collider2D, float worldRadius)
    {
        Vector3 scale = collider2D.transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y));
        if (maxScale <= 0.0001f)
            maxScale = 1f;

        collider2D.radius = worldRadius / maxScale;
    }
}
