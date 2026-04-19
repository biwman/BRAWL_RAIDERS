using System.IO;
using Photon.Pun;
using UnityEngine;
using UnityEngine.Rendering;

public class PirateBattleshipEventManager : MonoBehaviour
{
    const float CheckInterval = 0.1f;
    const float ShipSpeed = 8.2f;
    const float FireInterval = 0.15f;
    const float FireRange = 22f;
    const float PushRadius = 2.6f;
    const float PushImpulse = 1.9f;
    const float SpawnMargin = 8f;

    static PirateBattleshipEventManager instance;
    static Sprite cachedSprite;

    GameObject activeShip;
    SpriteRenderer shipRenderer;
    TrailRenderer[] trailRenderers;
    Vector2 activeStart;
    Vector2 activeEnd;
    double eventStartTime;
    double eventEndTime;
    float nextCheckTime;
    float nextFireTime;
    bool hasSchedule;
    bool wasGameStarted;

    public static void EnsureExists()
    {
        if (instance != null)
            return;

        GameObject root = new GameObject("PirateBattleshipEventManager");
        instance = root.AddComponent<PirateBattleshipEventManager>();
    }

    void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    void Update()
    {
        if (Time.unscaledTime < nextCheckTime)
            return;

        nextCheckTime = Time.unscaledTime + CheckInterval;

        if (!PhotonNetwork.InRoom || !IsRoundStarted() || !RoomSettings.IsPirateBattleshipEventEnabled())
        {
            ResetEventState();
            return;
        }

        if (!hasSchedule || !wasGameStarted)
        {
            ScheduleCurrentRoundEvent();
        }

        double currentTime = PhotonNetwork.Time;
        bool active = hasSchedule && currentTime >= eventStartTime && currentTime <= eventEndTime;
        if (!active)
        {
            DestroyShipVisual();
            return;
        }

        EnsureShipVisual();
        UpdateShipVisual(currentTime);

        if (PhotonNetwork.IsMasterClient)
        {
            PushNearbyObjects();
            TryFireAtTarget();
        }
    }

    void ScheduleCurrentRoundEvent()
    {
        if (PhotonNetwork.CurrentRoom == null ||
            !PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("startTime", out object startObj) ||
            startObj is not double roundStartTime)
        {
            return;
        }

        Vector2 mapSize = RoomSettings.GetMapDimensions();
        float travelDistance = Mathf.Sqrt(mapSize.x * mapSize.x + mapSize.y * mapSize.y) + SpawnMargin * 2f;
        float travelDuration = travelDistance / ShipSpeed;

        int hash = (PhotonNetwork.CurrentRoom.Name + "_" + roundStartTime.ToString("F3")).GetHashCode();
        double roundDuration = RoomSettings.GetRoundDuration();
        int configuredSecond = RoomSettings.GetPirateBattleshipEventSecond();
        double configuredTriggerTime = Mathf.Clamp(configuredSecond, 15, Mathf.Max(15, Mathf.RoundToInt((float)roundDuration) - 5));

        eventStartTime = roundStartTime + configuredTriggerTime;
        eventEndTime = eventStartTime + travelDuration;
        activeStart = GetCornerPosition(mapSize, hash, true);
        activeEnd = GetCornerPosition(mapSize, hash, false);
        hasSchedule = true;
        wasGameStarted = true;
        nextFireTime = 0f;
    }

    Vector2 GetCornerPosition(Vector2 mapSize, int hash, bool start)
    {
        bool leftToRight = (hash & 1) == 0;
        bool topToBottom = (hash & 2) == 0;

        if (!start)
        {
            leftToRight = !leftToRight;
            topToBottom = !topToBottom;
        }

        float x = leftToRight ? -mapSize.x * 0.5f - SpawnMargin : mapSize.x * 0.5f + SpawnMargin;
        float y = topToBottom ? mapSize.y * 0.5f + SpawnMargin : -mapSize.y * 0.5f - SpawnMargin;
        return new Vector2(x, y);
    }

    void EnsureShipVisual()
    {
        if (activeShip != null)
            return;

        activeShip = new GameObject("PirateBattleship");
        shipRenderer = activeShip.AddComponent<SpriteRenderer>();
        shipRenderer.sprite = LoadPirateSprite();
        shipRenderer.color = Color.white;
        shipRenderer.sortingOrder = 3;
        FitRendererToTargetSize(shipRenderer, 4.8f);

        trailRenderers = new TrailRenderer[2];
        for (int i = 0; i < 2; i++)
        {
            GameObject trailObject = new GameObject("Trail" + i);
            trailObject.transform.SetParent(activeShip.transform, false);
            trailObject.transform.localPosition = new Vector3(i == 0 ? -0.7f : 0.7f, 1.1f, 0f);

            TrailRenderer trail = trailObject.AddComponent<TrailRenderer>();
            trail.material = new Material(Shader.Find("Sprites/Default"));
            trail.time = 0.72f;
            trail.minVertexDistance = 0.03f;
            trail.widthMultiplier = 0.18f;
            trail.alignment = LineAlignment.View;
            trail.textureMode = LineTextureMode.Stretch;
            trail.numCapVertices = 10;
            trail.numCornerVertices = 6;
            trail.shadowCastingMode = ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sortingOrder = 1;
            trail.colorGradient = BuildTrailGradient();
            trail.widthCurve = new AnimationCurve(
                new Keyframe(0f, 1f),
                new Keyframe(0.25f, 0.82f),
                new Keyframe(0.7f, 0.24f),
                new Keyframe(1f, 0f));

            trailRenderers[i] = trail;
        }
    }

    Gradient BuildTrailGradient()
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.96f, 0.98f, 1f), 0f),
                new GradientColorKey(new Color(0.56f, 0.88f, 1f), 0.14f),
                new GradientColorKey(new Color(0.14f, 0.58f, 1f), 0.44f),
                new GradientColorKey(new Color(0.05f, 0.18f, 0.8f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.92f, 0f),
                new GradientAlphaKey(0.62f, 0.2f),
                new GradientAlphaKey(0.26f, 0.62f),
                new GradientAlphaKey(0f, 1f)
            });
        return gradient;
    }

    void UpdateShipVisual(double currentTime)
    {
        if (activeShip == null)
            return;

        float duration = Mathf.Max(0.01f, (float)(eventEndTime - eventStartTime));
        float progress = Mathf.Clamp01((float)((currentTime - eventStartTime) / duration));
        Vector2 position = Vector2.Lerp(activeStart, activeEnd, progress);
        Vector2 direction = (activeEnd - activeStart).normalized;

        activeShip.transform.position = new Vector3(position.x, position.y, 0f);
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;
        activeShip.transform.rotation = Quaternion.Euler(0f, 0f, angle);
    }

    void PushNearbyObjects()
    {
        if (activeShip == null)
            return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(activeShip.transform.position, PushRadius);
        Vector2 pushDirection = (activeEnd - activeStart).normalized;
        for (int i = 0; i < hits.Length; i++)
        {
            MovingSpaceObject movingObject = hits[i].GetComponentInParent<MovingSpaceObject>();
            if (movingObject != null)
            {
                movingObject.ApplyImpulse(pushDirection * PushImpulse);
            }
        }
    }

    void TryFireAtTarget()
    {
        if (activeShip == null || Time.time < nextFireTime)
            return;

        Transform target = FindTarget();
        if (target == null)
            return;

        nextFireTime = Time.time + FireInterval;
        Vector2 direction = ((Vector2)target.position - (Vector2)activeShip.transform.position).normalized;
        Vector3 spawnPos = activeShip.transform.position + activeShip.transform.up * -1.35f;

        GameObject bullet = PhotonNetwork.Instantiate("Bullet", spawnPos, Quaternion.identity);
        Bullet bulletComponent = bullet != null ? bullet.GetComponent<Bullet>() : null;
        if (bulletComponent != null)
            bulletComponent.ownerViewID = -777;

        Rigidbody2D bulletBody = bullet != null ? bullet.GetComponent<Rigidbody2D>() : null;
        if (bulletBody != null)
            bulletBody.linearVelocity = direction * 10f;
    }

    Transform FindTarget()
    {
        PlayerHealth[] targets = FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);
        Transform bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < targets.Length; i++)
        {
            PlayerHealth target = targets[i];
            if (target == null || target.IsWreck)
                continue;

            HideInNebulaTarget nebula = target.GetComponent<HideInNebulaTarget>();
            if (nebula != null && nebula.IsHiddenForOthers)
                continue;

            float distance = Vector2.Distance(activeShip.transform.position, target.transform.position);
            if (distance > FireRange || distance >= bestDistance)
                continue;

            bestDistance = distance;
            bestTarget = target.transform;
        }

        return bestTarget;
    }

    void ResetEventState()
    {
        hasSchedule = false;
        wasGameStarted = false;
        DestroyShipVisual();
    }

    void DestroyShipVisual()
    {
        if (activeShip != null)
            Destroy(activeShip);

        activeShip = null;
        shipRenderer = null;
        trailRenderers = null;
    }

    bool IsRoundStarted()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        return PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
               value is bool started &&
               started;
    }

    Sprite LoadPirateSprite()
    {
        if (cachedSprite != null)
            return cachedSprite;

        string filePath = Path.Combine(Application.dataPath, "piracki_pancernik.png");
        if (!File.Exists(filePath))
            return null;

        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        cachedSprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
        return cachedSprite;
    }

    void FitRendererToTargetSize(SpriteRenderer renderer, float targetSize)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        Bounds spriteBounds = renderer.sprite.bounds;
        float largestDimension = Mathf.Max(spriteBounds.size.x, spriteBounds.size.y);
        if (largestDimension <= 0.0001f)
            return;

        float scale = targetSize / largestDimension;
        renderer.transform.localScale = new Vector3(scale, scale, 1f);
    }
}
