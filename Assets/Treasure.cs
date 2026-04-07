using UnityEngine;

public class Treasure : MonoBehaviour
{
    public int value;

    private SpriteRenderer sr;
    private Color originalColor;

    void Start()
    {
        value = Random.Range(1, 11);

        sr = GetComponent<SpriteRenderer>();
        originalColor = sr.color;
    }

    public void Highlight()
    {
        sr.color = Color.yellow; // możesz zmienić kolor
    }

    public void Unhighlight()
    {
        sr.color = originalColor;
    }
}