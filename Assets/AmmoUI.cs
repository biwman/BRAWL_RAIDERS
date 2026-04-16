using Photon.Pun;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(PlayerShooting))]
public class AmmoUI : MonoBehaviourPun
{
    const string AmmoRootName = "AmmoCounter";
    const string AmmoLabelName = "AmmoLabel";
    const string AmmoValueName = "AmmoValue";

    PlayerShooting shooting;
    GameObject rootObject;
    Image backgroundImage;
    TextMeshProUGUI labelText;
    TextMeshProUGUI valueText;

    void Start()
    {
        shooting = GetComponent<PlayerShooting>();

        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        CreateCounter();
        RefreshCounter();
    }

    void Update()
    {
        RefreshCounter();
    }

    void OnDestroy()
    {
        if (rootObject != null)
        {
            Destroy(rootObject);
        }
    }

    void CreateCounter()
    {
        GameObject existing = GameObject.Find(AmmoRootName);
        if (existing != null)
        {
            Destroy(existing);
        }

        GameObject hpBar = GameObject.Find("HP_Bar");
        if (hpBar == null || hpBar.transform.parent == null)
            return;

        rootObject = new GameObject(AmmoRootName, typeof(RectTransform), typeof(Image));
        rootObject.transform.SetParent(hpBar.transform.parent, false);

        RectTransform rect = rootObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -188f);
        rect.sizeDelta = new Vector2(560f, 44f);

        backgroundImage = rootObject.GetComponent<Image>();
        backgroundImage.color = new Color(0.07f, 0.1f, 0.14f, 0.92f);
        backgroundImage.type = Image.Type.Sliced;

        labelText = CreateText(AmmoLabelName, new Vector2(16f, 0f), TextAlignmentOptions.Left);
        labelText.text = "AMMO";

        valueText = CreateText(AmmoValueName, new Vector2(-16f, 0f), TextAlignmentOptions.Right);
    }

    void RefreshCounter()
    {
        if (shooting == null || valueText == null || backgroundImage == null)
            return;

        if (shooting.IsReloading)
        {
            float secondsLeft = Mathf.Max(0f, shooting.reloadDuration * (1f - shooting.ReloadProgress));
            valueText.text = "RELOADING " + secondsLeft.ToString("0.0") + "s";
            valueText.color = new Color(1f, 0.78f, 0.28f, 1f);
            backgroundImage.color = new Color(0.16f, 0.12f, 0.07f, 0.94f);
        }
        else
        {
            valueText.text = shooting.CurrentAmmo + " / " + shooting.MaxAmmo;
            valueText.color = shooting.CurrentAmmo <= 3
                ? new Color(1f, 0.45f, 0.35f, 1f)
                : Color.white;
            backgroundImage.color = new Color(0.07f, 0.1f, 0.14f, 0.92f);
        }
    }

    TextMeshProUGUI CreateText(string objectName, Vector2 anchoredPosition, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(rootObject.transform, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = anchoredPosition;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.fontSize = 21f;
        text.fontStyle = FontStyles.Bold;
        text.color = Color.white;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.margin = new Vector4(16f, 6f, 16f, 6f);

        TMP_Text referenceText = FindAnyObjectByType<TMP_Text>();
        if (referenceText != null)
        {
            text.font = referenceText.font;
            text.fontSharedMaterial = referenceText.fontSharedMaterial;
        }

        return text;
    }
}
