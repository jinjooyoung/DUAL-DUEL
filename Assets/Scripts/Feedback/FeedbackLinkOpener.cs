using UnityEngine;
using UnityEngine.UI;

public class FeedbackLinkOpener : MonoBehaviour
{
    [Header("구글 폼 URL")]
    [SerializeField] private string googleFormUrl = "https://forms.gle/GdAiaB9MrGDcWCZbA";

    [Header("연결할 버튼")]
    [SerializeField] private Button feedbackButton;

    private void Start()
    {
        // 인스펙터에서 버튼을 직접 할당한 경우 자동으로 클릭 이벤트 연결
        if (feedbackButton != null)
        {
            feedbackButton.onClick.AddListener(OpenFeedbackForm);
        }
    }

    // 버튼의 OnClick() 이벤트에 직접 연결해서 사용 가능
    public void OpenFeedbackForm()
    {
        if (string.IsNullOrEmpty(googleFormUrl))
        {
            Debug.LogWarning("구글 폼 URL이 비어 있습니다.");
            return;
        }

        Application.OpenURL(googleFormUrl);
    }
}