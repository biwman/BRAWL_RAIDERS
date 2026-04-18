using UnityEngine;
using Photon.Pun;

public class Treasure : MonoBehaviourPun
{
    public const float CollectRange = 2.2f;
    public int value;

    private SpriteRenderer sr;
    private Color originalColor;

    // blokada zbierania
    public bool isBeingCollected = false;

    void Start()
    {
        value = Random.Range(1, 11);

        sr = GetComponent<SpriteRenderer>();

        if (sr != null)
        {
            sr.color = Color.white;
            originalColor = Color.white;
        }

        BoxCollider2D bodyCollider = GetComponent<BoxCollider2D>();
        if (bodyCollider == null)
        {
            bodyCollider = gameObject.AddComponent<BoxCollider2D>();
        }

        bodyCollider.isTrigger = false;

        MovingSpaceObject movingObject = GetComponent<MovingSpaceObject>();
        if (movingObject == null)
        {
            movingObject = gameObject.AddComponent<MovingSpaceObject>();
        }

        string stableId = photonView != null
            ? "treasure_" + photonView.ViewID
            : "treasure_" + gameObject.name;
        movingObject.Configure(stableId, MovingSpaceObject.SpaceObjectType.Treasure);
    }

    public void Highlight()
    {
        if (sr != null)
            sr.color = Color.green;
    }

    public void Unhighlight()
    {
        if (sr != null)
            sr.color = originalColor;
    }
}
