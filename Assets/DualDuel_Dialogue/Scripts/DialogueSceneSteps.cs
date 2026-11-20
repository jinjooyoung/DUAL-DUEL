using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 하나의 대사 순서. 씬에 두고 인스펙터에서 단계를 순서대로 나열하세요. (한 씬에 여러 개 둬도 됨)
/// 단계마다
///   Wait Action: 비우면 바로(첫 단계면 씬 시작 시), 적으면 Notify(그 이름) 가 올 때 재생
///   Start Id: 처음 볼 때 재생할 묶음
///   이미 봤으면: Play Alt When Seen 이 꺼져 있으면 건너뜀 (기다리지도 않음), 켜져 있으면 Seen Start Id 를 재생
///
/// 씬 시작 직후(Start Delay 동안) 온 알림은 기억했다가, 첫 단계들을 시작할 때 기다리던 행동이면 바로 재생
/// (예: BattleManager.Start 에서 바로 Notify 해도 놓치지 않음). 첫 단계들을 시작한 뒤에는 기억을 지움.
///
/// 예) 스테이지 선택 씬:  (없음, 0)  ->  (StageClicked, 5)
/// 예) 전투 씬 보스용:    (BossBattleStart, 900, 이미 봤으면 950)   <- 보스 코드에서 Notify("BossBattleStart")
/// </summary>
public class DialogueSceneSteps : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Tooltip("비우면 이전 단계가 끝나자마자 (첫 단계면 씬 시작 시) 재생. 적으면 Notify(이 이름) 가 올 때 재생")]
        public string waitAction;
        [Tooltip("처음 볼 때 재생할 묶음의 첫 대사 id")]
        public int startId;
        [Tooltip("이미 본 묶음일 때: 끄면 이 단계를 건너뜀, 켜면 Seen Start Id 를 재생 (예: 보스 재도전 대사)")]
        public bool playAltWhenSeen;
        [Tooltip("이미 봤을 때 재생할 묶음의 첫 대사 id (Play Alt When Seen 이 켜져 있을 때만)")]
        public int seenStartId;
    }

    [SerializeField] private List<Step> steps = new List<Step>();
    [Tooltip("씬 시작 후 첫 대사까지 기다리는 시간 (씬 전환 글리치가 끝난 뒤 뜨도록)")]
    [SerializeField] private float startDelay = 0.6f;

    private int index;
    private bool waiting;
    private int waitingBlock = -1;
    private bool started;                                               // Start Delay 가 끝나 단계를 시작했는지
    private readonly List<string> earlyActions = new List<string>();    // 시작 전에 온 알림

    private void OnEnable() => DialogueManager.Notified += OnNotified;
    private void OnDisable() => DialogueManager.Notified -= OnNotified;

    private IEnumerator Start()
    {
        if (DialogueManager.Instance == null) { Debug.LogWarning("[Dialogue] DialogueManager 가 없습니다. 프리팹을 첫 씬(테스트하려면 이 씬에도)에 두세요."); yield break; }
        if (steps.Count == 0) { Debug.LogWarning($"[Dialogue] {name}: Steps 가 비어 있습니다."); yield break; }
        yield return new WaitForSecondsRealtime(startDelay);
        started = true;
        RunStep();
        earlyActions.Clear();   // 시작 직후 단계들이 쓰고 남은 알림은 버림 (이후엔 '미리 한 행동은 기억 안 함' 규칙)
    }

    // 현재 단계: 재생할 묶음을 정하고 (이미 봤으면 대체 묶음 또는 건너뜀), 기다릴 행동이 있으면 대기
    private void RunStep()
    {
        while (index < steps.Count)
        {
            Step step = steps[index];
            bool seen = DialogueProgress.HasSeen(step.startId);
            int block = !seen ? step.startId : (step.playAltWhenSeen ? step.seenStartId : -1);
            if (block < 0)
            {
                Debug.Log($"[Dialogue] {gameObject.scene.name} 단계 {index}: 묶음 {step.startId} 은 이미 봐서 건너뜀");
                index++;
                continue;
            }

            waitingBlock = block;
            waiting = !string.IsNullOrEmpty(step.waitAction);

            // 시작 전에 이미 온 알림이면 기다리지 않고 바로 재생
            if (waiting && earlyActions.Remove(step.waitAction))
            {
                Debug.Log($"[Dialogue] {gameObject.scene.name} 단계 {index}: '{step.waitAction}' 는 씬 시작 직후 이미 알림이 와서 바로 재생");
                waiting = false;
            }

            if (waiting) Debug.Log($"[Dialogue] {gameObject.scene.name} 단계 {index}: '{step.waitAction}' 를 기다리는 중 (묶음 {block})");
            else PlayStep();
            return;
        }
    }

    // 기다리던 행동이 오면 재생
    private void OnNotified(string action)
    {
        // 아직 Start Delay 중이면 기억해 둠 (첫 단계들을 시작할 때 확인)
        if (!started)
        {
            earlyActions.Add(action);
            return;
        }
        if (!waiting || index >= steps.Count || action != steps[index].waitAction) return;
        waiting = false;
        PlayStep();
    }

    // 묶음 재생 -> 끝나면 다음 단계
    private void PlayStep()
    {
        DialogueManager.Instance.Play(waitingBlock, () => { index++; RunStep(); });
    }
}
