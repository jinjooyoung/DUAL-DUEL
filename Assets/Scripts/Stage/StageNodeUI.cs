using UnityEngine;
using UnityEngine.UI;

public class StageNodeUI : MonoBehaviour
{
    public StageNode nodeData;

    [SerializeField] private Button button;
    [SerializeField] private Image iconImage;
    [SerializeField] private GameObject highlightEffect;

    /// <summary>
    /// 전달받은 노드 데이터를 바인딩하고 비주얼 상태(진입 가능/방문 여부)를 갱신합니다.
    /// </summary>
    public void Setup(StageNode node)
    {
        this.nodeData = node;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClickNode);

        RefreshVisual();
    }

    public void RefreshVisual()
    {
        // 기획서 2-2-4: 갈 수 있든 없든 모든 버튼은 Interactable true 유지
        button.interactable = true;

        if (highlightEffect != null)
        {
            highlightEffect.SetActive(nodeData.canGo && !nodeData.isVisited);
        }

        // 방문 완료 노드는 어둡게 처리
        iconImage.color = nodeData.isVisited ? Color.gray : (nodeData.canGo ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f));
    }

    private void OnClickNode()
    {
        StageManager.Instance.TryMoveToNode(nodeData);
    }
}