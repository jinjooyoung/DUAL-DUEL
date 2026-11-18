# DUAL DUEL - 카드 상점

## 설치
1. `DualDuel_Shop` 폴더를 `.meta` 파일과 함께 `Assets/` 아래에 넣습니다.
2. 상점 씬을 열고 **Tools > DUAL DUEL > 상점 씬 구성** 을 실행한 뒤 씬을 저장합니다.
   - 배경, 카드 8장(프로젝트의 UI_ViewerCard 프리팹), 가격, 제목, 보유 골드, 나가기, 덱 보기가 배치되고 연결까지 끝납니다.
   - 여러 번 실행해도 같은 결과입니다 (기존 상점 구성은 지우고 다시 만듦).
3. CRT 분위기가 필요하면 Tools > DUAL DUEL > 배틀씬 CRT 연출 세팅 도 실행합니다.
4. 다른 씬에서 시작해 상점으로 들어와 확인합니다 (GameManager, RewardManager, StageManager 가 넘어와 있어야 함).

## 흐름
- 입장: 상점 추첨기가 없으면 Initialize -> 8번 반복 (EvaluateRank -> RewardManager.GetRandomCardByRank -> i번째 UIViewerCard 에 Setup). 연출 없이 바로 보임.
- 구매: 카드 클릭 -> CardInstance 를 GameManager.deck 에 추가, currentGold 차감, 가격 표시 제거, 골드 롤링, 남은 카드의 Interactable = (가격 <= 현재 골드). 살 수 없는 카드는 카드와 가격이 함께 어두워짐.
- 나가기: StageManager.Instance.ReturnToStageSelect()
- 덱 보기: 씬에 DeckViewerPopup 이 있으면 보유 덱을 열어줌 (없으면 구매 연출의 도착 지점으로만 쓰임).

## 구매 연출 (ShopPurchaseFX)
| 시간 | 내용 |
|---|---|
| 누름 | 0.1초에 원래 크기 x0.7 |
| 뗌 ~ 0.1초 | x0.9 로 튕김 (원본보다 작게) |
| 0.1 ~ 1.1초 | 두 바퀴(720도) 돌며 x0.1 로 |
| 0.8초 (회전 0.7초) | 스파클 + 원형 빛이 카드 높이만큼 번쩍, 0.3초 동안 카드와 함께 사그라듦 |
| 0.8 ~ 1.1초 | 빛줄기가 카드 위로 살짝 떴다가 포물선으로 덱 보기 버튼까지, 도착하면 작게 반짝 |

시간과 크기는 ShopScene 오브젝트의 ShopPurchaseFX 인스펙터에서 조정합니다.

## 상점 추첨기
- RewardManager 와 같은 가중치 테이블(chapter1/2/3Weights)을 쓰지만 누적은 따로 합니다.
- 상점 씬 오브젝트는 씬을 나가면 사라지므로 추첨기를 static 으로 두어 상점 방문 사이에도 누적이 유지됩니다.
- 챕터가 바뀔 때: 상점 씬에서 `FindFirstObjectByType<ShopManager>().SetChapter(n)` 처럼 호출하거나,
  RewardManager.SetChapter 를 부르는 곳에서 함께 처리하세요 (상점 입장 시 현재 챕터로 맞추는 방식 권장).
- 새 게임을 시작할 때 `ShopManager.ResetShopRoll()` 을 호출하면 상점 누적이 지워집니다.

## 리소스 (모두 PPU 100, Point, 압축 없음)
- Shop_BG (1780 x 1000): 빈 카드 슬롯 8개와 가격표 자리가 그려진 배경
- Shop_GoldPanel, Shop_Button_Bright, Shop_Button_Dark (9-슬라이스), Shop_Coin
- FX_Shop_Sparkle (변이 움푹 팬 네 꼭짓점 별), FX_Shop_Glow (원형 빛)
- pixelfx_source: 위 리소스의 원본 코드 (유니티에는 넣지 않아도 됨)
