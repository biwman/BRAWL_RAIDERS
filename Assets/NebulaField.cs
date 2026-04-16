using System.IO;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(CircleCollider2D))]
public class NebulaField : MonoBehaviour
{
    const float TargetVisualSize = 3.36f;
    const float ColliderRadiusFactor = 0.4f;
    const float PlayerDeepHideFactor = 0.6f;

    static Sprite cachedNebulaSprite;

    SpriteRenderer spriteRenderer;
    CircleCollider2D triggerCollider;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        triggerCollider = GetComponent<CircleCollider2D>();
        ConfigureVisual();
        ConfigureCollider();
    }

    void ConfigureVisual()
    {
        if (spriteRenderer == null)
            return;

        if (cachedNebulaSprite == null)
        {
            cachedNebulaSprite = LoadSprite();
        }

        if (cachedNebulaSprite == null)
            return;

        spriteRenderer.sprite = cachedNebulaSprite;
        spriteRenderer.color = new Color(0.72f, 0.9f, 1f, 0.72f);
        ApplySortingLayer();

        float maxDimension = Mathf.Max(cachedNebulaSprite.bounds.size.x, cachedNebulaSprite.bounds.size.y);
        if (maxDimension > 0f)
        {
            float scale = TargetVisualSize / maxDimension;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    void ApplySortingLayer()
    {
        SpriteRenderer reference = FindReferenceRenderer("Obstacle");
        if (reference == null)
            reference = FindReferenceRenderer("Ground");
        if (reference == null)
            reference = FindReferenceRenderer("Player");

        if (reference != null)
        {
            spriteRenderer.sortingLayerID = reference.sortingLayerID;
            spriteRenderer.sortingOrder = reference.sortingOrder + 1;
        }
        else
        {
            spriteRenderer.sortingOrder = 10;
        }
    }

    SpriteRenderer FindReferenceRenderer(string objectName)
    {
        GameObject referenceObject = GameObject.Find(objectName);
        if (referenceObject == null)
            return null;

        return referenceObject.GetComponent<SpriteRenderer>();
    }

    void ConfigureCollider()
    {
        if (triggerCollider == null || spriteRenderer == null || spriteRenderer.sprite == null)
            return;

        triggerCollider.isTrigger = true;
        float radius = Mathf.Max(spriteRenderer.bounds.size.x, spriteRenderer.bounds.size.y) * ColliderRadiusFactor;
        Vector3 lossyScale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        if (maxScale <= 0.0001f)
            maxScale = 1f;

        triggerCollider.radius = radius / maxScale;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HideInNebulaTarget target = other.GetComponentInParent<HideInNebulaTarget>();
        if (target != null)
        {
            target.UpdateNebulaState(GetInstanceID(), ShouldHideTarget(target));
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        HideInNebulaTarget target = other.GetComponentInParent<HideInNebulaTarget>();
        if (target != null)
        {
            target.UpdateNebulaState(GetInstanceID(), ShouldHideTarget(target));
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        HideInNebulaTarget target = other.GetComponentInParent<HideInNebulaTarget>();
        if (target != null)
        {
            target.RemoveNebula(GetInstanceID());
        }
    }

    bool ShouldHideTarget(HideInNebulaTarget target)
    {
        if (target.GetComponent<PlayerHealth>() == null)
            return true;

        Bounds targetBounds = GetTargetBounds(target);
        float nebulaRadius = GetWorldNebulaRadius();
        float targetRadius = Mathf.Max(targetBounds.extents.x, targetBounds.extents.y);
        float allowedDistance = nebulaRadius - (targetRadius * PlayerDeepHideFactor);

        if (allowedDistance <= 0f)
            return false;

        float distance = Vector2.Distance(transform.position, targetBounds.center);
        return distance <= allowedDistance;
    }

    Bounds GetTargetBounds(HideInNebulaTarget target)
    {
        SpriteRenderer[] targetRenderers = target.GetComponentsInChildren<SpriteRenderer>(true);
        if (targetRenderers.Length == 0)
        {
            return new Bounds(target.transform.position, Vector3.one);
        }

        Bounds combined = targetRenderers[0].bounds;
        for (int i = 1; i < targetRenderers.Length; i++)
        {
            combined.Encapsulate(targetRenderers[i].bounds);
        }

        return combined;
    }

    float GetWorldNebulaRadius()
    {
        if (triggerCollider == null)
            return 0f;

        Vector3 lossyScale = transform.lossyScale;
        float maxScale = Mathf.Max(Mathf.Abs(lossyScale.x), Mathf.Abs(lossyScale.y));
        if (maxScale <= 0.0001f)
            maxScale = 1f;

        return triggerCollider.radius * maxScale;
    }

    static Sprite LoadSprite()
    {
        string filePath = Path.Combine(Application.dataPath, "nebula_frayed.png");
        if (!File.Exists(filePath))
            return null;

        byte[] bytes = File.ReadAllBytes(filePath);
        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(bytes, false);
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        float pixelsPerUnit = Mathf.Max(texture.width, texture.height);
        return Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);
    }
}
