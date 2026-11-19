# DUAL DUEL - 다이얼로그 시스템

튜토리얼 개념(최초 실행, PlayerPrefs, 스킵 버튼)은 없고, "이 대사 묶음을 본 적 있는가" 하나로 동작합니다.

## 파일
| 파일 | 역할 |
|---|---|
| Scripts/DialogueData.cs | 엑셀 한 행(7열) + 대사 테이블(DialogueTableSO) |
| Scripts/DialogueProgress.cs | 본 대사 묶음 영구 기록 (dialogue.sav, 게임 진행 저장과 별개) |
| Scripts/DialogueManager.cs | 대사창 프리팹: 타이핑, 입력, 스탠딩 캐시, action 실행 |
| Scripts/DialogueSceneSteps.cs | 씬마다: (기다릴 행동, 시작 묶음, 이미 봤으면 대체 묶음) 순서 |
| Editor/DialogueEditorTools.cs | CSV 가져오기 / 프리팹 만들기 / 저장 데이터 삭제 |
| ../Modified/StageManager.cs | 튜토리얼 호출 제거, DialogueManager.Notify 로 교체, 진입 직전 층 상태 기억 (저장 연결 그대로) |
| ../Modified/SaveManager.cs | SaveInStage 추가 (전투 중 저장, 들어온 노드는 다시 들어갈 수 있게) |

## 저장 두 가지
| | 게임 진행 | 다이얼로그 기록 |
|---|---|---|
| 스크립트 | SaveManager | DialogueProgress |
| 파일 | run.sav | dialogue.sav |
| 수명 | 죽거나 클리어하면 삭제 | 영구 (툴 또는 ResetAll 로만 삭제) |
| 내용 | 덱, 체력, 골드, 맵, 가중치 | 본 묶음의 첫 대사 id 목록 |

본 기록은 묶음이 "시작되는 순간" 저장됩니다. 보는 도중 강제 종료해도 "봤음" 으로 남습니다.

## 적용 순서
1. 기존 튜토리얼 정리
   - 씬에 놓인 TutorialManager 오브젝트와 Assets/Prefabs/Tutorial 프리팹 삭제
   - 씬의 TutorialSceneSteps 컴포넌트 삭제 (단계 값은 메모해 두기)
   - Assets/DualDuel_Tutorial 폴더 삭제
2. DualDuel_Dialogue 폴더를 Assets 아래에 넣고, Modified/StageManager.cs, Modified/SaveManager.cs 로 기존 파일 교체
3. 컴파일 에러가 남으면 TutorialManager 를 쓰던 코드를 DialogueManager 로 바꾸기
   - TutorialManager.Notify(...)          -> DialogueManager.Notify(...)
   - TutorialManager.IsBlockingInput      -> DialogueManager.IsBlockingInput
   - StartTutorialIfFirstRun / EndTutorial / SkipTutorial / IsRunning 호출은 삭제 (더 이상 없음)
4. 예시 엑셀 Dialogue 시트(7열) -> "CSV UTF-8" 저장 -> Tools > DUAL DUEL > 대사 CSV 가져오기
5. Tools > DUAL DUEL > 다이얼로그 매니저 프리팹 만들기 -> 메인 씬에 배치 (단독 테스트할 씬에도 두면 편함)
6. 씬마다 빈 오브젝트 + DialogueSceneSteps, 단계 입력 (아래 표)
7. 테스트 전 Tools > DUAL DUEL > 저장 데이터 > 다이얼로그 기록 삭제

## 씬 단계 (예시 엑셀 기준)
스테이지 선택 씬
| Wait Action | Start Id | Play Alt When Seen | Seen Start Id |
|---|---|---|---|
| (비움) | 0 | 끔 | - |
| StageClicked | 5 | 끔 | - |

전투 씬, 첫 전투 안내 (이미 봤으면 모두 건너뜀)
| Wait Action | Start Id | Play Alt When Seen | Seen Start Id |
|---|---|---|---|
| (비움) | 10 | 끔 | - |
| CardPlaced | 20 | 끔 | - |
| TurnEndClicked | 30 | 끔 | - |

전투 씬, 보스용 (같은 씬에 DialogueSceneSteps 를 하나 더)
| Wait Action | Start Id | Play Alt When Seen | Seen Start Id |
|---|---|---|---|
| BossBattleStart | 900 | 켬 | 950 |

## 게임 코드에 넣을 것
```csharp
DialogueManager.Notify("StageClicked");     // 스테이지 노드 클릭 (Modified/StageManager.cs 에 이미 들어 있음)
DialogueManager.Notify("CardPlaced");       // 카드를 슬롯에 놓은 곳
DialogueManager.Notify("TurnEndClicked");   // Turn End 처리부
DialogueManager.Notify("BossBattleStart");  // 보스 전투가 시작되는 곳 (보스일 때만)
if (DialogueManager.IsBlockingInput) return; // StageNodeUI.OnMouseDown, CardDisplay.OnMouseDown 맨 위
```

## action 열
- Glitch: 화면 글리치 (SceneTransition.Pulse, 기본 제공)
- 그 외: DialogueManager.LineAction 이벤트로 게임 코드에 전달

```csharp
// 보스 쪽 스크립트 예시
void OnEnable()  => DialogueManager.LineAction += OnLineAction;
void OnDisable() => DialogueManager.LineAction -= OnLineAction;

void OnLineAction(string action, DialogueData line)
{
    if (action != "StealCard") return;
    // 1. GameManager.deck 에서 카드 제거
    // 2. 저장 (다음 대사 "게임은 저장되었어요" 가 뜨기 전에 끝나야 대사가 거짓말이 아님)
}
```

## 보스 이스터에그 흐름
1. 보스 첫 진입 -> Notify("BossBattleStart") -> 900 묶음 시작 (이 순간 dialogue.sav 에 900 기록)
2. 904 대사에서 StealCard -> 카드 제거 + 저장 -> "게임은 저장되었어요"
3. 플레이어가 강제 종료 -> 이어하기 -> 보스 재진입 -> 900 은 이미 봤으므로 950 묶음
   "어머, 돌아오셨네요. 강제 종료라니. 귀여운 시도였어요. 이미 없어진 카드는 돌아오지 않아요."
4. dialogue.sav 는 죽어도 남으므로, 다음 런에서 보스를 만나도 950 이 나옴
   (다음 런에서는 첫 만남 대사를 다시 보여주고 싶다면 보스용 단계를 900 이 아닌 런별 조건으로 나누기)

## 보스 카드 강탈 저장 (SaveManager.SaveInStage)
전투 중에는 SaveManager.Save() 대신 SaveManager.SaveInStage() 를 쓰세요.
- 덱 / 체력 / 골드 / 가중치: 지금 값 (빼앗긴 카드는 사라진 상태)
- 들어온 보스 노드가 있는 층: 들어가기 직전 상태 (이어하기 후 보스에 다시 들어갈 수 있음)

```csharp
void OnLineAction(string action, DialogueData line)
{
    if (action != "StealCard") return;
    // 1. GameManager.Instance.deck 에서 카드 제거
    // 2. SaveManager.SaveInStage();   (다음 대사 "게임은 저장되었어요" 보다 먼저)
}
```
다이얼로그 기록(dialogue.sav)은 영구라서, 한 번 강제 종료한 플레이어는 다음 런에서도 보스에게 950 묶음(재도전 대사)을 듣게 됩니다.
