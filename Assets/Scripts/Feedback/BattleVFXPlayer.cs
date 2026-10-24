using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// vfxPrefabs 리스트의 인덱스와 순서를 맞춰주세요. (0: 피격, 1: 버프, 2: 방어)
/// </summary>
public enum VFXType
{
    Hit = 0,
    Buff = 1,
    Defense = 2
}

/// <summary>
/// 타겟 트랜스폼 위치에서 VFX 프리팹을 재생하는 전투 전용 VFX 플레이어.
/// 사용 예: BattleVFXPlayer.Instance.Play(monsterTransform, VFXType.Hit);
/// </summary>
public class BattleVFXPlayer : MonoBehaviour
{
    public static BattleVFXPlayer Instance { get; private set; }

    [Header("VFX 프리팹 리스트 (0: 피격, 1: 버프, 2: 방어)")]
    [SerializeField] private List<GameObject> vfxPrefabs = new List<GameObject>();

    [Header("재생 옵션")]
    [Tooltip("타겟 위치 기준 오프셋 (예: 캐릭터 몸통 높이로 올리기)")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;
    [Tooltip("체크하면 VFX가 타겟을 따라다님")]
    [SerializeField] private bool followTarget = false;
    [Tooltip("루프 파티클 안전장치: 이 시간이 지나면 방출을 멈추고 회수")]
    [SerializeField] private float maxLifetime = 3f;

    [Header("2D 정렬 (스프라이트에 가려질 때 사용)")]
    [SerializeField] private bool overrideSorting = false;
    [SerializeField] private string sortingLayerName = "Default";
    [Tooltip("프리팹 내부 렌더러들의 기존 Order에 이 값을 더함")]
    [SerializeField] private int sortingOrderOffset = 100;

    // 프리팹 인덱스별 대기 풀
    private readonly Dictionary<int, Queue<GameObject>> pools = new Dictionary<int, Queue<GameObject>>();
    // 인스턴스별 파티클 시스템 캐시
    private readonly Dictionary<GameObject, ParticleSystem[]> particleCache = new Dictionary<GameObject, ParticleSystem[]>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// 타겟 위치에서 지정한 타입의 VFX를 재생합니다.
    /// </summary>
    /// <param name="target">VFX를 보여줄 대상</param>
    /// <param name="type">재생할 VFX 종류</param>
    public GameObject Play(Transform target, VFXType type)
    {
        return Play(target, (int)type);
    }

    /// <summary>
    /// 타겟 위치에서 리스트의 index번째 VFX를 재생합니다.
    /// </summary>
    public GameObject Play(Transform target, int index)
    {
        if (target == null)
        {
            Debug.LogWarning("[BattleVFXPlayer] 타겟이 null이라 VFX를 재생할 수 없습니다.");
            return null;
        }

        if (index < 0 || index >= vfxPrefabs.Count || vfxPrefabs[index] == null)
        {
            Debug.LogWarning($"[BattleVFXPlayer] {index}번 VFX 프리팹이 등록되지 않았습니다.");
            return null;
        }

        GameObject vfx = GetFromPool(index);
        Transform t = vfx.transform;

        // 위치/회전 세팅 (회전은 프리팹 원본 회전 유지)
        t.SetParent(followTarget ? target : transform, true);
        t.SetPositionAndRotation(target.position + spawnOffset, vfxPrefabs[index].transform.rotation);

        // 재생
        vfx.SetActive(true);
        ParticleSystem[] systems = particleCache[vfx];
        foreach (var ps in systems)
        {
            ps.Clear(false);
            ps.Play(false);
        }

        StartCoroutine(Co_ReturnWhenFinished(vfx, index, systems));
        return vfx;
    }

    private GameObject GetFromPool(int index)
    {
        if (!pools.TryGetValue(index, out var queue))
        {
            queue = new Queue<GameObject>();
            pools.Add(index, queue);
        }

        // 따라가던 타겟과 함께 파괴된 오브젝트는 건너뜀
        while (queue.Count > 0)
        {
            GameObject pooled = queue.Dequeue();
            if (pooled != null) return pooled;
        }

        return CreateInstance(index);
    }

    private GameObject CreateInstance(int index)
    {
        GameObject vfx = Instantiate(vfxPrefabs[index], transform);
        vfx.SetActive(false);

        ParticleSystem[] systems = vfx.GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in systems)
        {
            var main = ps.main;
            main.playOnAwake = false;                         // 재생은 스크립트가 제어
            main.stopAction = ParticleSystemStopAction.None;  // 에셋의 Destroy/Disable 설정 무력화
        }

        if (overrideSorting)
        {
            foreach (var r in vfx.GetComponentsInChildren<ParticleSystemRenderer>(true))
            {
                r.sortingLayerName = sortingLayerName;
                r.sortingOrder += sortingOrderOffset;
            }
        }

        particleCache[vfx] = systems;
        return vfx;
    }

    private IEnumerator Co_ReturnWhenFinished(GameObject vfx, int index, ParticleSystem[] systems)
    {
        float elapsed = 0f;
        bool stopped = false;

        yield return null; // 재생 직후 IsAlive가 false일 수 있어 한 프레임 대기

        while (vfx != null)
        {
            elapsed += Time.deltaTime;

            // 루프 파티클 등 오래 남는 경우 방출 정지
            if (!stopped && elapsed >= maxLifetime)
            {
                foreach (var ps in systems)
                    if (ps != null) ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
                stopped = true;
            }

            bool alive = false;
            foreach (var ps in systems)
            {
                if (ps != null && ps.IsAlive(false)) { alive = true; break; }
            }

            // 모든 파티클 종료 or 강제 종료 시간 초과
            if (!alive || elapsed >= maxLifetime + 2f) break;

            yield return null;
        }

        ReturnToPool(vfx, index);
    }

    private void ReturnToPool(GameObject vfx, int index)
    {
        if (vfx == null)
        {
            particleCache.Remove(vfx);
            return;
        }

        vfx.SetActive(false);
        vfx.transform.SetParent(transform, false);
        pools[index].Enqueue(vfx);
    }
}