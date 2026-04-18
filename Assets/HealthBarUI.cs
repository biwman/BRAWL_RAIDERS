using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerHealth))]
public class HealthBarUI : MonoBehaviourPun
{
    const string HpBarName = "HP_Bar";
    const string LabelName = "HealthLabel";
    const string ValueName = "HealthValue";

    Slider hpBar;
    RectTransform hpRect;
    Image backgroundImage;
    Image fillImage;
    Image handleImage;
    TextMeshProUGUI labelText;
    TextMeshProUGUI valueText;
    bool isVisible = true;

    void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        InitializeBar();
        RefreshVisuals();
    }

    void Update()
    {
        UpdateVisibility();
        RefreshVisuals();
    }

    void InitializeBar()
    {
        GameObject hpBarObject = GameObject.Find(HpBarName);
        if (hpBarObject == null)
            return;

        hpBar = hpBarObject.GetComponent<Slider>();
        hpRect = hpBarObject.GetComponent<RectTransform>();

        if (hpBar == null || hpRect == null)
            return;

        hpRect.sizeDelta = new Vector2(560f, 44f);
        hpRect.anchoredPosition = new Vector2(0f, -76f);

        backgroundImage = FindImage(hpBar.transform, "Background");
        fillImage = hpBar.fillRect != null ? hpBar.fillRect.GetComponent<Image>() : null;
        handleImage = FindImage(hpBar.transform, "Handle");

        if (backgroundImage != null)
        {
            backgroundImage.color = new Color(0.09f, 0.11f, 0.14f, 0.95f);
        }

        if (fillImage != null)
        {
            fillImage.color = new Color(0.24f, 0.86f, 0.38f, 1f);
        }

        hpBar.transition = Selectable.Transition.None;
        hpBar.targetGraphic = backgroundImage;

        labelText = GetOrCreateText(LabelName, new Vector2(16f, 0f), TextAlignmentOptions.Left, "HEALTH");
        valueText = GetOrCreateText(ValueName, new Vector2(-16f, 0f), TextAlignmentOptions.Right, string.Empty);
    }

    void RefreshVisuals()
    {
        if (hpBar == null)
            return;

        float normalized = hpBar.maxValue > 0f ? hpBar.value / hpBar.maxValue : 0f;

        if (valueText != null)
        {
            valueText.text = Mathf.RoundToInt(hpBar.value) + " / " + Mathf.RoundToInt(hpBar.maxValue);
        }

        if (fillImage == null)
            return;

        Color low = new Color(0.89f, 0.2f, 0.24f, 1f);
        Color mid = new Color(0.95f, 0.75f, 0.2f, 1f);
        Color high = new Color(0.24f, 0.86f, 0.38f, 1f);

        if (normalized > 0.5f)
        {
            float t = Mathf.InverseLerp(0.5f, 1f, normalized);
            fillImage.color = Color.Lerp(mid, high, t);
        }
        else
        {
            float t = Mathf.InverseLerp(0f, 0.5f, normalized);
            fillImage.color = Color.Lerp(low, mid, t);
        }

        if (handleImage != null)
        {
            handleImage.color = normalized >= 0.999f
                ? new Color(0.96f, 0.38f, 0.4f, 1f)
                : new Color(0.6f, 0.64f, 0.7f, 1f);
        }
    }

    void UpdateVisibility()
    {
        if (hpBar == null)
            return;

        bool shouldBeVisible = IsGameplayHudVisible();
        if (isVisible == shouldBeVisible)
            return;

        isVisible = shouldBeVisible;
        hpBar.gameObject.SetActive(shouldBeVisible);
    }

    bool IsGameplayHudVisible()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return false;

        if (PhotonNetwork.CurrentRoom.CustomProperties.TryGetValue("gameStarted", out object value) &&
            value is bool started)
        {
            return started;
        }

        return false;
    }

    TextMeshProUGUI GetOrCreateText(string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment, string initialText)
    {
        Transform existing = hpBar.transform.Find(objectName);
        GameObject textObject;

        if (existing != null)
        {
            textObject = existing.gameObject;
        }
        else
        {
            textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
            textObject.transform.SetParent(hpBar.transform, false);
        }

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = 20f;
        text.color = Color.white;
        text.alignment = alignment;
        text.enableWordWrapping = false;

        TMP_Text referenceText = FindFirstObjectByType<TMP_Text>();
        if (referenceText != null)
        {
            text.font = referenceText.font;
            text.fontSharedMaterial = referenceText.fontSharedMaterial;
        }

        return text;
    }

    Image FindImage(Transform root, string objectName)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name == objectName)
            {
                return image;
            }
        }

        return null;
    }
}
