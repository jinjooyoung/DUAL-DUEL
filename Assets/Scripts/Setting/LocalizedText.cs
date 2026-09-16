using TMPro;
using UnityEngine;

public class LocalizedText : MonoBehaviour
{
    [SerializeField]
    private string key;

    [SerializeField]
    private TMP_Text text;

    /// <summary>
    /// 언어 변경 후에 호출하는 텍스트 새로고침 함수
    /// </summary>
    public void Refresh()
    {
        text.text = LocalizationManager.Instance.GetText(key);
    }

    private void Start()
    {
        var temp = gameObject.GetComponent<TMP_Text>();

        if (temp == null)
        {
            Debug.LogWarning($"{name}에 text 컴포넌트가 존재하지 않습니다.");
            return;
        }

        text = temp;

        Refresh();

        // SettingsManager 싱글턴 선언이 Awake라서 안전하게 Start에서 호출
        SettingsManager.Instance.OnLanguageChanged.AddListener(Refresh);
    }

    private void OnDestroy()
    {
        SettingsManager.Instance.OnLanguageChanged.RemoveListener(Refresh);
    }
}
