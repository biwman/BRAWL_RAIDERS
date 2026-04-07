using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class TreasureCollector : MonoBehaviour
{
    public float collectTime = 3f;
    public Button collectButton;
    public TMP_Text scoreText;
    private Treasure currentTreasure;
    private bool isCollecting = false;

    public int totalScore = 0;

    public PlayerMovement movement;
    public PlayerShooting shooting;

    void Start()
    {
        scoreText.text = "Score: 0";
        
    }
    void OnTriggerEnter2D(Collider2D other)
    {
        Treasure t = other.GetComponent<Treasure>();
        if (t != null)
        {
            Debug.Log("Wszedłem w skarb!");

            currentTreasure = t;
            t.Highlight();
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        Treasure t = other.GetComponent<Treasure>();
        if (t != null)
        {
            t.Unhighlight(); 
            currentTreasure = null;
        }
    }

 
    void OnTriggerStay2D(Collider2D other)
    {
        if (other.GetComponent<Treasure>() != null)
        {
            Debug.Log("STOJĘ na skarbie");
        }
    }
    public void StartCollect()
    {
        Debug.Log("Kliknalem collect");
        if (currentTreasure != null && !isCollecting)
        {
            StartCoroutine(CollectRoutine());
        }
    }
    public void StartHolding()
    {
        Debug.Log("START HOLD");

        if (currentTreasure != null && !isCollecting)
        {
            StartCoroutine(CollectRoutine());
        }
    }

    public void StopHolding()
    {
        Debug.Log("STOP HOLD");

        if (isCollecting)
        {
            StopAllCoroutines();

            isCollecting = false;

            movement.enabled = true;
            shooting.enabled = true;
        }
    }
    IEnumerator CollectRoutine()
    {
        isCollecting = true;

        movement.enabled = false;
        shooting.enabled = false;

        float timer = 0f;

        while (timer < collectTime)
        {
            timer += Time.deltaTime;
            yield return null;
        }

        totalScore += currentTreasure.value;
        scoreText.text = "Score: " + totalScore;

        Destroy(currentTreasure.gameObject);

        isCollecting = false;

        movement.enabled = true;
        shooting.enabled = true;
    }
}