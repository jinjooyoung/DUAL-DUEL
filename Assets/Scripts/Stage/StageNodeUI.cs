using UnityEngine;

/// <summary>
/// 3D 월드 공간의 스테이지 노드 뷰어 컴포넌트
/// </summary>
[RequireComponent(typeof(Collider))] // 3D 콜라이더 필수 (2D 스프라이트면 Collider2D)
public class StageNodeUI : MonoBehaviour
{
    public StageNode nodeData;

    [Header("렌더러 및 이펙트")]
    [SerializeField] private SpriteRenderer iconRenderer; // 또는 MeshRenderer
    [SerializeField] private GameObject highlightEffect;

    public void Setup(StageNode node)
    {
        this.nodeData = node;
        RefreshVisual();
    }

    public void RefreshVisual()
    {
        if (highlightEffect != null)
        {
            highlightEffect.SetActive(nodeData.canGo && !nodeData.isVisited);
        }

        if (iconRenderer != null)
        {
            // 방문 완료 노드는 어둡게, 진입 가능 노드는 밝게
            iconRenderer.color = nodeData.isVisited
                ? Color.gray
                : (nodeData.canGo ? Color.white : new Color(0.7f, 0.7f, 0.7f, 0.5f));
        }
    }

    /// <summary>
    /// 마우스로 3D 노드를 클릭했을 때 실행
    /// </summary>
    private void OnMouseDown()
    {
        if (StageManager.Instance != null && nodeData != null)
        {
            StageManager.Instance.TryMoveToNode(nodeData);
        }
    }
}