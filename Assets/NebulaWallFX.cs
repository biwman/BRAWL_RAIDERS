using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NebulaWallFX : MonoBehaviour
{
    class NebulaLayer
    {
        public Transform transform;
        public SpriteRenderer renderer;
        public Vector3 baseScale;
        public float driftSpeed;
        public float driftAmount;
        public float pulseSpeed;
        public float pulseOffset;
        public Color colorA;
        public Color colorB;
    }

    static NebulaWallBootstrap bootstrap;
    static readonly Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();

    readonly List<NebulaLayer> layers = new List<NebulaLayer>();

    SpriteRenderer baseRenderer;
    bool isHorizontal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void EnsureBootstrap()
    {
        if (bootstrap != null)
            return;

        GameObject root = new GameObject("NebulaWallBootstrap");
        DontDestroyOnLoad(root);
        bootstrap = root.AddComponent<NebulaWallBootstrap>();
    }

    void Awake()
    {
        baseRenderer = GetComponent<SpriteRenderer>();
        BoxCollider2D collider2D = GetComponent<BoxCollider2D>();
        isHorizontal = collider2D == null || collider2D.bounds.size.x >= collider2D.bounds.size.y;
    }

    void Start()
    {
        BuildFx();
    }

    void Update()
    {
        float time = Time.time;

        foreach (NebulaLayer layer in layers)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(time * layer.pulseSpeed + layer.pulseOffset);
            layer.renderer.color = Color.Lerp(layer.colorA, layer.colorB, pulse);

            Vector3 drift = isHorizontal
                ? new Vector3(0f, Mathf.Sin(time * layer.driftSpeed + layer.pulseOffset) * layer.driftAmount, 0f)
                : new Vector3(Mathf.Sin(time * layer.driftSpeed + layer.pulseOffset) * layer.driftAmount, 0f, 0f);

            layer.transform.localPosition = drift;

            float scalePulse = 1f + 0.025f * Mathf.Sin(time * (layer.pulseSpeed * 0.85f) + layer.pulseOffset * 1.3f);
            layer.transform.localScale = layer.baseScale * scalePulse;
        }
    }

    void BuildFx()
    {
        if (baseRenderer == null || layers.Count > 0)
            return;

        baseRenderer.color = new Color(0.03f, 0.05f, 0.09f, 0.98f);
        baseRenderer.sortingOrder = 0;

        CreateLayer(
            "NebulaCore",
            GetNebulaSprite("core", new Color(0.2f, 0.1f, 0.4f, 0.8f), new Color(0.05f, 0.45f, 0.6f, 0.55f), 0.95f),
            isHorizontal ? new Vector3(1.02f, 1.55f, 1f) : new Vector3(1.55f, 1.02f, 1f),
            0.32f,
            0.18f,
            0.45f,
            new Color(0.45f, 0.2f, 0.75f, 0.58f),
            new Color(0.12f, 0.65f, 0.9f, 0.48f),
            0.3f);

        CreateLayer(
            "NebulaMist",
            GetNebulaSprite("mist", new Color(0.58f, 0.2f, 0.72f, 0.42f), new Color(0.15f, 0.8f, 0.95f, 0.34f), 0.8f),
            isHorizontal ? new Vector3(0.98f, 1.18f, 1f) : new Vector3(1.18f, 0.98f, 1f),
            0.25f,
            0.12f,
            0.62f,
            new Color(0.82f, 0.28f, 0.68f, 0.24f),
            new Color(0.2f, 0.55f, 0.92f, 0.3f),
            1.4f);

        CreateLayer(
            "NebulaGlow",
            GetNebulaSprite("glow", new Color(0.2f, 0.26f, 0.65f, 0.3f), new Color(0.75f, 0.28f, 0.9f, 0.18f), 0.72f),
            isHorizontal ? new Vector3(1.08f, 0.82f, 1f) : new Vector3(0.82f, 1.08f, 1f),
            0.2f,
            0.08f,
            0.54f,
            new Color(0.16f, 0.34f, 0.75f, 0.2f),
            new Color(0.86f, 0.36f, 0.78f, 0.16f),
            2.1f);
    }

    void CreateLayer(string name, Sprite sprite, Vector3 scale, float driftSpeed, float driftAmount, float pulseSpeed, Color colorA, Color colorB, float pulseOffset)
    {
        GameObject layerObject = new GameObject(name);
        layerObject.transform.SetParent(transform, false);

        SpriteRenderer renderer = layerObject.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = baseRenderer.sortingLayerID;
        renderer.sortingOrder = baseRenderer.sortingOrder + 1;
        renderer.drawMode = SpriteDrawMode.Simple;
        renderer.color = colorA;

        layerObject.transform.localScale = scale;

        layers.Add(new NebulaLayer
        {
            transform = layerObject.transform,
            renderer = renderer,
            baseScale = scale,
            driftSpeed = driftSpeed,
            driftAmount = driftAmount,
            pulseSpeed = pulseSpeed,
            pulseOffset = pulseOffset,
            colorA = colorA,
            colorB = colorB
        });
    }

    static Sprite GetNebulaSprite(string key, Color colorA, Color colorB, float intensity)
    {
        if (spriteCache.TryGetValue(key, out Sprite cached))
            return cached;

        const int size = 128;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = x / (float)size;
                float ny = y / (float)size;

                float noiseA = Mathf.PerlinNoise(nx * 2.2f, ny * 3.6f);
                float noiseB = Mathf.PerlinNoise((nx + 7.2f) * 4.4f, (ny + 2.9f) * 1.8f);
                float noiseC = Mathf.PerlinNoise((nx + 13.1f) * 6.2f, (ny + 5.3f) * 6.2f);
                float combined = noiseA * 0.5f + noiseB * 0.32f + noiseC * 0.18f;

                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                float edgeFade = Mathf.Clamp01(1f - Mathf.Pow(dist, 1.45f));
                float alpha = Mathf.Pow(Mathf.Clamp01(combined * edgeFade), 1.8f) * intensity;

                Color color = Color.Lerp(colorA, colorB, Mathf.Clamp01(noiseB * 0.85f));
                color.a = alpha;
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply();

        Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        spriteCache[key] = sprite;
        return sprite;
    }
}

public class NebulaWallBootstrap : MonoBehaviour
{
    readonly string[] wallNames = { "WallTop", "WallBottom", "WallLeft", "WallRight" };

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        AttachToWalls();
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        AttachToWalls();
    }

    void AttachToWalls()
    {
        foreach (string wallName in wallNames)
        {
            GameObject wall = GameObject.Find(wallName);
            if (wall == null)
                continue;

            if (wall.GetComponent<NebulaWallFX>() == null)
            {
                wall.AddComponent<NebulaWallFX>();
            }
        }
    }
}
