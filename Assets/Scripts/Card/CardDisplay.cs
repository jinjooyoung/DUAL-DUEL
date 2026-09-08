using TMPro;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class CardDisplay : MonoBehaviour
{
    [Header("카드 데이터(SO)")]
    public CardSO cardSO;

    [Header("이미지")]
    public SpriteRenderer background;
    public SpriteRenderer cardResource;
    public SpriteRenderer ownerBorder;
    public SpriteRenderer typeIcon;

    [Header("텍스트")]
    public TextMeshPro nameText;
    public TextMeshPro typeText;
    public TextMeshPro descriptionText;

    private bool isDragging = false;
    private Vector3 originalPosition;

    [Header("레이어 마스크")]
    public LayerMask playerLayer;
    public LayerMask enemyLayer;

    void Start()
    {
        playerLayer = LayerMask.GetMask("Player");
        enemyLayer = LayerMask.GetMask("Enemy");

        //SetupCard(cardData);
    }

    // 카드 데이터 설정
    public void SetupCard(CardSO data)
    {
        cardSO = data;

        // 3D 텍스트 업데이트
        if (nameText != null) nameText.text = data.nameKey;                         // 추후 로컬라이징 추가 시 키 대신 로컬라이징 데이터베이스에서 찾아, 적용하는 방식으로 수정
        if (typeText != null) typeText.text = data.cardType.ToString();
        if (descriptionText != null) descriptionText.text = data.descKey;

        // 카드 리소스
        // 기본 배경
        if (background != null)
        {
            string artworkPath = $"Assets/Resources/Cards/Public/Card_BG.png";
            background.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);
        }

        // 카드 리소스
        if (cardResource != null && cardSO.artwork != null)
        {
            cardResource.sprite = cardSO.artwork;
        }

        // 카드 테두리
        if (ownerBorder != null)
        {
            string artworkPath = $"Assets/Resources/Cards/Public/ownerBorder_{cardSO.ownerType}.png";
            ownerBorder.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);
        }

        // 카드 타입 아이콘
        if (typeIcon != null)
        {
            string artworkPath = $"Assets/Resources/Cards/Public/{cardSO.cardType}.png";
            ownerBorder.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);
        }

        // 로컬라이징 후 설명 텍스트의 {value}를 cardSO.values로 Replace해주는 코드 여기 작성해야함
    }

    private void OnMouseDown()
    {
        // 드래그 시작 시 원래 위치 저장
        originalPosition = transform.position;
        isDragging = true;
    }

    private void OnMouseDrag()
    {
        if (isDragging)
        {
            // 마우스 위치로 카드 이동
            Vector3 mousePos = Input.mousePosition;
            mousePos.z = Camera.main.WorldToScreenPoint(transform.position).z;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            transform.position = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        }
    }

    private void OnMouseUp()
    {
        isDragging = false;

        // 레이캐스트로 타겟 감지
        RaycastHit hit;
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 카드 사용 판정 지역 변수
        bool cardUsed = false;

        // 적 슬롯 위에 드롭했는지 검사
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, enemyLayer))
        {
            if (cardSO.ownerType == OwnerType.Player)       // 이 카드가 플레이어 카드라면
            {
                // 놓을 수 없으니 위치 되돌리고 리턴
                // 나중에 스무스 하게 되돌아가는 연출 함수 작성해서 여기에서 호출해줌
                transform.position = originalPosition;
                return;
            }

            // 슬롯에 배치 리스트 추가, 위치 저장
        }
        else if (Physics.Raycast(ray, out hit, Mathf.Infinity, playerLayer))
        {
            if (cardSO.ownerType == OwnerType.Enemy)       // 이 카드가 적 카드라면
            {
                // 놓을 수 없으니 위치 되돌리고 리턴
                // 나중에 스무스 하게 되돌아가는 연출 함수 작성해서 여기에서 호출해줌
                transform.position = originalPosition;
                return;
            }

            // 슬롯에 배치 리스트 추가, 위치 저장
        }

        if (!cardUsed)      // 카드를 사용하지 않았다면 원래 위치로 되돌리기
        {
            transform.position = originalPosition;
            return;
        }
    }
}
