using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using Photon.Pun;
using ExitGames.Client.Photon;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TreasureCollector : MonoBehaviourPun
{
    const float TreasureScanInterval = 0.08f;
    const float BeamWidth = 0.24f;
    const float BeamJitterAmplitude = 0.08f;
    const float BeamJitterFrequency = 18f;
    const float BeamZOffset = -0.35f;

    public Button collectButton;
    public TMP_Text scoreText;

    public PlayerMovement movement;
    public PlayerShooting shooting;

    Treasure currentTreasure;
    bool isCollecting = false;
    ExtractionZone currentExtraction;
    public float collectTime = 3f;
    public int totalScore = 0;
    AudioSource drillingAudioSource;
    LineRenderer collectionBeam;
    float nextTreasureScanTime;
    bool beamActive;

    void Start()
    {
        SetupDrillingAudio();
        SetupBeam();

        if (!photonView.IsMine)
            return;

        if (scoreText == null)
        {
            GameObject obj = GameObject.Find("ScoreText");
            if (obj != null)
            {
                scoreText = obj.GetComponent<TMP_Text>();
            }
        }

        if (scoreText != null)
        {
            scoreText.text = "Score: 0";
        }

        SyncScoreProperty();

        if (collectButton == null)
        {
            GameObject obj = GameObject.Find("CollectButton");
            if (obj != null)
            {
                collectButton = obj.GetComponent<Button>();
            }
        }

        if (collectButton != null)
        {
            EventTrigger trigger = collectButton.GetComponent<EventTrigger>();
            if (trigger == null)
                trigger = collectButton.gameObject.AddComponent<EventTrigger>();

            trigger.triggers = new List<EventTrigger.Entry>();

            EventTrigger.Entry down = new EventTrigger.Entry();
            down.eventID = EventTriggerType.PointerDown;
            down.callback.AddListener((data) => { StartHolding(); });
            trigger.triggers.Add(down);

            EventTrigger.Entry up = new EventTrigger.Entry();
            up.eventID = EventTriggerType.PointerUp;
            up.callback.AddListener((data) => { StopHolding(); });
            trigger.triggers.Add(up);
        }
    }

    void Update()
    {
        if (photonView.IsMine && Time.unscaledTime >= nextTreasureScanTime)
        {
            nextTreasureScanTime = Time.unscaledTime + TreasureScanInterval;
            RefreshClosestTreasure();
        }

        UpdateCollectionBeam();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!photonView.IsMine)
            return;

        ExtractionZone ez = other.GetComponent<ExtractionZone>();
        if (ez != null)
        {
            currentExtraction = ez;
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!photonView.IsMine)
            return;

        ExtractionZone ez = other.GetComponent<ExtractionZone>();
        if (ez != null && currentExtraction == ez)
        {
            currentExtraction = null;
        }
    }

    public void StartHolding()
    {
        if (!photonView.IsMine)
            return;

        if (currentExtraction != null)
        {
            PhotonView ezView = currentExtraction.GetComponent<PhotonView>();
            if (ezView != null)
            {
                photonView.RPC(nameof(RequestUseExtraction), RpcTarget.MasterClient, ezView.ViewID);
            }

            return;
        }

        RefreshClosestTreasure();

        if (currentTreasure != null && !isCollecting)
        {
            isCollecting = true;
            photonView.RPC(nameof(StartDrillingLoopSfx), RpcTarget.All);
            PhotonView treasureView = currentTreasure.GetComponent<PhotonView>();
            if (treasureView != null)
            {
                photonView.RPC(nameof(SetBeamTargetRpc), RpcTarget.All, treasureView.ViewID, true);
            }
            StartCoroutine(CollectRoutine(currentTreasure));
        }
    }

    public void StopHolding()
    {
        if (!photonView.IsMine)
            return;

        if (isCollecting)
        {
            isCollecting = false;
        }

        photonView.RPC(nameof(StopDrillingLoopSfx), RpcTarget.All);
        photonView.RPC(nameof(ClearBeamTargetRpc), RpcTarget.All);

        if (movement != null) movement.enabled = true;
        if (shooting != null) shooting.enabled = true;
    }

    IEnumerator CollectRoutine(Treasure treasureToCollect)
    {
        if (treasureToCollect == null)
        {
            isCollecting = false;
            StopLocalDrillingLoop();
            yield break;
        }

        if (treasureToCollect.isBeingCollected)
        {
            isCollecting = false;
            StopLocalDrillingLoop();
            yield break;
        }

        treasureToCollect.isBeingCollected = true;

        if (movement != null) movement.enabled = false;
        if (shooting != null) shooting.enabled = false;

        float timer = 0f;

        while (timer < collectTime)
        {
            if (!isCollecting || treasureToCollect == null || !IsTreasureInCollectRange(treasureToCollect))
            {
                if (treasureToCollect != null)
                    treasureToCollect.isBeingCollected = false;

                photonView.RPC(nameof(StopDrillingLoopSfx), RpcTarget.All);
                photonView.RPC(nameof(ClearBeamTargetRpc), RpcTarget.All);

                if (movement != null) movement.enabled = true;
                if (shooting != null) shooting.enabled = true;
                yield break;
            }

            currentTreasure = treasureToCollect;
            timer += Time.deltaTime;
            yield return null;
        }

        AddScore(treasureToCollect.value);

        PhotonView treasureView = treasureToCollect.GetComponent<PhotonView>();
        if (treasureView != null)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.Destroy(treasureView.gameObject);
            }
            else
            {
                photonView.RPC(nameof(RequestDestroyTreasure), RpcTarget.MasterClient, treasureView.ViewID);
            }
        }

        treasureToCollect.isBeingCollected = false;
        isCollecting = false;
        currentTreasure = null;
        photonView.RPC(nameof(StopDrillingLoopSfx), RpcTarget.All);
        photonView.RPC(nameof(ClearBeamTargetRpc), RpcTarget.All);

        if (movement != null) movement.enabled = true;
        if (shooting != null) shooting.enabled = true;
    }

    void RefreshClosestTreasure()
    {
        Treasure nextTreasure = FindClosestTreasureInRange();
        if (currentTreasure == nextTreasure)
            return;

        if (currentTreasure != null && !currentTreasure.isBeingCollected)
        {
            currentTreasure.Unhighlight();
        }

        currentTreasure = nextTreasure;

        if (currentTreasure != null)
        {
            currentTreasure.Highlight();
        }
    }

    Treasure FindClosestTreasureInRange()
    {
        Treasure[] treasures = FindObjectsByType<Treasure>(FindObjectsInactive.Exclude);
        Treasure bestTreasure = null;
        float bestDistance = float.MaxValue;
        Vector2 tipPosition = GetShipTipPosition();

        foreach (Treasure treasure in treasures)
        {
            if (treasure == null)
                continue;

            float distance = GetDistanceFromTipToTreasure(treasure, tipPosition);
            if (distance > Treasure.CollectRange)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestTreasure = treasure;
            }
        }

        return bestTreasure;
    }

    bool IsTreasureInCollectRange(Treasure treasure)
    {
        if (treasure == null)
            return false;

        return GetDistanceFromTipToTreasure(treasure, GetShipTipPosition()) <= Treasure.CollectRange;
    }

    float GetDistanceFromTipToTreasure(Treasure treasure, Vector2 tipPosition)
    {
        Collider2D collider = treasure.GetComponent<Collider2D>();
        if (collider != null)
        {
            Vector2 closestPoint = collider.ClosestPoint(tipPosition);
            return Vector2.Distance(tipPosition, closestPoint);
        }

        return Vector2.Distance(tipPosition, treasure.transform.position);
    }

    Vector2 GetShipTipPosition()
    {
        float forwardOffset = 0.55f;
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        if (renderer != null)
        {
            forwardOffset = Mathf.Max(0.4f, renderer.bounds.extents.y * 0.9f);
        }

        return (Vector2)transform.position + (Vector2)transform.up * forwardOffset;
    }

    void SetupBeam()
    {
        Transform existing = transform.Find("TreasureBeam");
        GameObject beamObject = existing != null ? existing.gameObject : new GameObject("TreasureBeam");
        beamObject.transform.SetParent(transform, false);

        collectionBeam = beamObject.GetComponent<LineRenderer>();
        if (collectionBeam == null)
        {
            collectionBeam = beamObject.AddComponent<LineRenderer>();
        }

        collectionBeam.useWorldSpace = true;
        collectionBeam.alignment = LineAlignment.View;
        collectionBeam.positionCount = 2;
        collectionBeam.widthMultiplier = BeamWidth;
        collectionBeam.startWidth = BeamWidth;
        collectionBeam.endWidth = BeamWidth * 0.7f;
        collectionBeam.numCapVertices = 6;
        collectionBeam.numCornerVertices = 4;
        collectionBeam.material = new Material(Shader.Find("Sprites/Default"));
        collectionBeam.startColor = new Color(0.7f, 1f, 0.82f, 1f);
        collectionBeam.endColor = new Color(0.28f, 1f, 0.52f, 0.78f);
        collectionBeam.textureMode = LineTextureMode.Stretch;

        SpriteRenderer referenceRenderer = GetComponent<SpriteRenderer>();
        if (referenceRenderer != null)
        {
            collectionBeam.sortingLayerID = referenceRenderer.sortingLayerID;
            collectionBeam.sortingOrder = referenceRenderer.sortingOrder + 20;
        }
        else
        {
            collectionBeam.sortingLayerName = "Default";
            collectionBeam.sortingOrder = 50;
        }

        collectionBeam.enabled = false;
    }

    void UpdateCollectionBeam()
    {
        if (collectionBeam == null)
            return;

        bool shouldShow = beamActive && currentTreasure != null;
        if (shouldShow && photonView.IsMine)
        {
            shouldShow = IsTreasureInCollectRange(currentTreasure);
        }

        collectionBeam.enabled = shouldShow;

        if (!shouldShow)
            return;

        Vector2 start = GetShipTipPosition();
        Vector2 end = GetTreasureBeamTarget(currentTreasure, start);
        Vector2 direction = (end - start).normalized;
        Vector2 perpendicular = new Vector2(-direction.y, direction.x);
        float jitter = Mathf.Sin(Time.time * BeamJitterFrequency) * BeamJitterAmplitude;

        Vector3 startPos = new Vector3(start.x + perpendicular.x * jitter * 0.5f, start.y + perpendicular.y * jitter * 0.5f, BeamZOffset);
        Vector3 endPos = new Vector3(end.x - perpendicular.x * jitter, end.y - perpendicular.y * jitter, BeamZOffset);

        collectionBeam.SetPosition(0, startPos);
        collectionBeam.SetPosition(1, endPos);
    }

    Vector2 GetTreasureBeamTarget(Treasure treasure, Vector2 start)
    {
        Collider2D collider = treasure.GetComponent<Collider2D>();
        if (collider != null)
        {
            return collider.ClosestPoint(start);
        }

        return treasure.transform.position;
    }

    void SetBeamEnabled(bool enabled)
    {
        beamActive = enabled;
        if (collectionBeam != null)
            collectionBeam.enabled = enabled;
    }

    void OnDestroy()
    {
        if (currentTreasure != null && !currentTreasure.isBeingCollected)
        {
            currentTreasure.Unhighlight();
        }

        StopLocalDrillingLoop();
    }

    [PunRPC]
    void RequestDestroyTreasure(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            PhotonNetwork.Destroy(pv.gameObject);
        }
    }

    public void AddScore(int amount)
    {
        totalScore += amount;

        if (scoreText != null)
        {
            scoreText.text = "Score: " + totalScore;
        }

        SyncScoreProperty();
    }

    [PunRPC]
    void RequestUseExtraction(int viewID)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        PhotonView pv = PhotonView.Find(viewID);
        if (pv != null)
        {
            ExtractionZone ez = pv.GetComponent<ExtractionZone>();
            if (ez != null)
            {
                ez.TryUse(photonView);
            }
        }
    }

    void SyncScoreProperty()
    {
        if (!photonView.IsMine || !PhotonNetwork.IsConnected)
            return;

        ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
        props[RoomSettings.ScoreKey] = totalScore;
        PhotonNetwork.LocalPlayer.SetCustomProperties(props);
    }

    [PunRPC]
    void StartDrillingLoopSfx()
    {
        if (drillingAudioSource == null)
        {
            SetupDrillingAudio();
        }

        if (drillingAudioSource == null || drillingAudioSource.clip == null)
            return;

        if (!drillingAudioSource.isPlaying)
            drillingAudioSource.Play();
    }

    [PunRPC]
    void StopDrillingLoopSfx()
    {
        StopLocalDrillingLoop();
    }

    void SetupDrillingAudio()
    {
        AudioClip clip = AudioManager.Instance.DrillingClip;
        if (clip == null)
            return;

        Transform existing = transform.Find("DrillingAudioSource");
        GameObject audioObject = existing != null ? existing.gameObject : new GameObject("DrillingAudioSource");
        audioObject.transform.SetParent(transform, false);

        drillingAudioSource = audioObject.GetComponent<AudioSource>();
        if (drillingAudioSource == null)
        {
            drillingAudioSource = audioObject.AddComponent<AudioSource>();
        }

        drillingAudioSource.clip = clip;
        drillingAudioSource.loop = true;
        drillingAudioSource.playOnAwake = false;
        drillingAudioSource.volume = 0.455f;
        AudioManager.Instance.ConfigureSpatialSource(drillingAudioSource, 0.455f);
    }

    void StopLocalDrillingLoop()
    {
        if (drillingAudioSource != null && drillingAudioSource.isPlaying)
            drillingAudioSource.Stop();
    }

    [PunRPC]
    void SetBeamTargetRpc(int treasureViewId, bool active)
    {
        PhotonView targetView = PhotonView.Find(treasureViewId);
        currentTreasure = targetView != null ? targetView.GetComponent<Treasure>() : null;
        SetBeamEnabled(active && currentTreasure != null);
    }

    [PunRPC]
    void ClearBeamTargetRpc()
    {
        SetBeamEnabled(false);
        currentTreasure = null;
    }
}
