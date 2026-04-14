using UnityEngine;
using Photon.Pun;

public class Treasure : MonoBehaviourPun
{
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
