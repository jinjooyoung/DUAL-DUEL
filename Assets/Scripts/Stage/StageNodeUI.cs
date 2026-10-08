using UnityEngine;

[RequireComponent(typeof(Collider))]
public class StageNodeUI : MonoBehaviour
{
    public StageNode nodeData;

    [Header("스프라이트 렌더러 (자식 오브젝트들)")]
    [SerializeField] private SpriteRenderer selectedRenderer; // Order in Layer 0
    [SerializeField] private SpriteRenderer bgRenderer;       // Order in Layer 1
    [SerializeField] private SpriteRenderer iconRenderer;     // Order in Layer 2

    public void Setup(StageNode node)
    {
        this.nodeData = node;

        // 리소스 동적 로드 (Resources/Stage/ 폴더 기준)
        if (bgRenderer != null)
        {
            bgRenderer.sprite = Resources.Load<Sprite>("Stage/StageBG");
        }

        if (selectedRenderer != null)
        {
            selectedRenderer.sprite = Resources.Load<Sprite>("Stage/Selected");
            selectedRenderer.gameObject.SetActive(false);
        }

        if (iconRenderer != null)
        {
            iconRenderer.sprite = Resources.Load<Sprite>($"Stage/{node.stageType}");
        }

        RefreshVisual();
    }

    /// <summary>
    /// 갈 수 있는 노드는 흰색(본래 색), 갈 수 없는 노드는 검은색으로 표시
    /// </summary>
    public void RefreshVisual()
    {
        if (bgRenderer != null)
        {
            if (nodeData.canGo && !nodeData.isVisited)
            {
                bgRenderer.color = Color.white;
            }
            else
            {
                bgRenderer.color = Color.black;
            }
        }
    }

    /// <summary>
    /// 노드 선택 시 Selected 오브젝트 활성화/비활성화
    /// </summary>
    public void SetSelected(bool isSelected)
    {
        if (selectedRenderer != null)
        {
            selectedRenderer.gameObject.SetActive(isSelected);
        }
    }

    private void OnMouseDown()
    {
        if (StageManager.Instance != null && nodeData != null)
        {
            StageManager.Instance.SelectNode(this);
        }
    }
}