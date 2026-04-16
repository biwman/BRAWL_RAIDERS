using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ButtonClickSoundHook : MonoBehaviour
{
    Button button;

    void Awake()
    {
        button = GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
        }
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
        }
    }

    void HandleClick()
    {
        string objectName = gameObject.name.ToLowerInvariant();
        string label = string.Empty;

        TMPro.TMP_Text text = GetComponentInChildren<TMPro.TMP_Text>(true);
        if (text != null)
        {
            label = text.text.ToLowerInvariant();
        }

        if (objectName.Contains("collect") || label.Contains("use"))
            return;

        AudioManager.Instance.PlayClick();
    }
}
