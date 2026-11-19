using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 씬 하나의 튜토리얼 순서. 씬에 하나 두고 인스펙터에서 단계를 나열하세요.
/// 단계마다: '기다릴 행동' 이 비어 있으면 바로, 아니면 그 행동이 Notify 될 때 '시작 대사' 부터 재생.
/// 대사 묶음이 끝나면(nextId -1) 다음 단계로 넘어간다.
///
/// 예) 스테이지 씬
///   0: 기다릴 행동 (없음)       시작 대사 0   -> 설명 ... "스테이지를 클릭해볼래?"
///   1: 기다릴 행동 StageClicked 시작 대사 5   -> "패널에 정보가 나와. 입장 버튼을 눌러봐!"
///   (입장 버튼을 누르면 씬이 바뀌고, 전투 씬의 TutorialSceneSteps 가 이어받음)
/// </summary>
public class TutorialSceneSteps : MonoBehaviour
{
    [Serializable]
    public class Step
    {
        [Tooltip("비우면 이전 단계가 끝나자마자 (첫 단계면 씬 시작 시) 재생. 적으면 Notify(이 이름) 가 올 때 재생")]
        public string waitAction;
        [Tooltip("재생할 첫 대사 id")]
        public int startId;
    }

    [SerializeField] private List<Step> steps = new List<Step>();
    [Tooltip("이 씬의 마지막 단계가 끝나면 튜토리얼 완료 처리 (PlayerPrefs 1 저장). 튜토리얼의 마지막 씬에서만 체크")]
    [SerializeField] private bool endTutorialAfterLastStep;
    [Tooltip("씬 시작 후 첫 대사까지 기다리는 시간 (씬 전환 글리치가 끝난 뒤 뜨도록)")]
    [SerializeField] private float startDelay = 0.6f;

    private int index;
    private bool waiting;

    private void OnEnable() => TutorialManager.Notified += OnNotified;
    private void OnDisable() => TutorialManager.Notified -= OnNotified;

    private IEnumerator Start()
    {
        TutorialManager tm = TutorialManager.Instance;
        if (tm == null) { Debug.LogWarning("[Tutorial] TutorialManager 가 없습니다. 프리팹을 메인 씬(테스트하려면 이 씬에도)에 두세요."); yield break; }
        if (!tm.IsRunning) yield break;   // 튜토리얼 진행 중이 아니면 아무것도 안 함

        yield return new WaitForSecondsRealtime(startDelay);
        RunStep();
    }

    // 현재 단계 실행: 기다릴 행동이 없으면 바로 재생, 있으면 대기 (스킵됐으면 중단)
    private void RunStep()
    {
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsRunning) return;
        if (index >= steps.Count)
        {
            if (endTutorialAfterLastStep && TutorialManager.Instance != null) TutorialManager.Instance.EndTutorial();
            return;
        }
        waiting = !string.IsNullOrEmpty(steps[index].waitAction);
        if (!waiting) PlayStep();
    }

    // 기다리던 행동이 오면 재생
    private void OnNotified(string action)
    {
        if (!waiting || index >= steps.Count || action != steps[index].waitAction) return;
        if (TutorialManager.Instance == null || !TutorialManager.Instance.IsRunning) return;
        waiting = false;
        PlayStep();
    }

    // 대사 재생 -> 끝나면 다음 단계
    private void PlayStep()
    {
        TutorialManager.Instance.Play(steps[index].startId, () => { index++; RunStep(); });
    }
}
