using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 런 저장 / 이어하기. JsonUtility + AES 암호화 (.NET 기본 기능만 사용, 외부 패키지 없음).
///
/// 저장: SaveManager.Save()          스테이지 선택 화면에 들어올 때마다 StageManager 가 호출
/// 전투 중 저장: SaveManager.SaveInStage()  보스 카드 강탈처럼 되돌릴 수 없게 할 때 (들어온 노드는 다시 들어갈 수 있게 저장)
/// 이어하기: SaveManager.ContinueGame()  메인 화면 이어하기 버튼 (SaveMenuButtons.OnClickContinue)
/// 삭제: SaveManager.DeleteSave()     게임 오버 / 런 클리어 때 호출
///
/// 불러오기 순서: 덱 -> 체력 -> 골드 -> 가중치 2개 -> 스테이지 챕터 -> 스테이지 플로어
/// (StageManager 가 아직 없으면 챕터/플로어는 들고 있다가, 스테이지 씬에서 StageManager 가 맵을 그리기 직전에 넣음)
///
/// 강제 종료 대비: 임시 파일에 다 쓴 뒤 한 번에 교체하고, 직전 저장은 .bak 으로 남겨 본 파일이 깨지면 그걸 읽음.
/// </summary>
public static class SaveManager
{
    public const int Version = 1;
    private const string FileName = "run.sav";

    // 암호화 키 (클라이언트에 들어 있으므로 '쉽게 열어보거나 고치지 못하게' 하는 용도)
    private const string Secret = "DUAL_DUEL::run-save::v1";
    private static byte[] key;

    private static SaveData pending;           // 불러왔지만 아직 적용하지 못한 데이터
    private static bool pendingGlobals;        // 덱/체력/골드/가중치 미적용
    private static bool pendingStage;          // 챕터/플로어 미적용

    /// <summary>
    /// 저장 파일 경로
    /// </summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// 이어할 저장이 있는지 (이어하기 버튼 활성화에 사용)
    /// </summary>
    public static bool HasSave => File.Exists(SavePath) || File.Exists(SavePath + ".bak");

    // ------------------------------------------------------------------ 저장

    /// <summary>
    /// 지금 런 상태를 저장합니다. 성공하면 true
    /// </summary>
    public static bool Save()
    {
        return Write(false);
    }

    /// <summary>
    /// 스테이지(전투 등) 안에서 저장합니다. 덱/체력/골드/가중치는 지금 값, 들어온 노드가 있는 층은 '들어가기 직전' 상태로 저장해서
    /// 이어하기 후 같은 노드에 다시 들어갈 수 있습니다. (보스의 카드 강탈 직후 호출: 카드는 사라진 채로 보스에 재도전)
    /// 스테이지 밖이면 일반 저장과 같습니다. 성공하면 true
    /// </summary>
    public static bool SaveInStage()
    {
        return Write(true);
    }

    // 모으기 -> 암호화 -> 원자적 쓰기
    private static bool Write(bool beforeEntry)
    {
        if (pendingGlobals || pendingStage)
        {
            Debug.LogWarning("[Save] 불러온 데이터가 아직 다 적용되지 않아 저장을 건너뜁니다.");
            return false;
        }

        SaveData data = Capture(beforeEntry);
        if (data == null) return false;

        try
        {
            string json = JsonUtility.ToJson(data);
            WriteAtomic(Encrypt(json));
            Debug.Log($"<color=green>[Save] {(beforeEntry ? "스테이지 안에서 " : "")}저장 완료 ({data.deck.Count}장, HP {data.currentHP}, 골드 {data.currentGold}, {data.currentChapter}챕터 {data.floors.Count}층)</color>");
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[Save] 저장 실패: {e.Message}");
            return false;
        }
    }

    // 매니저들에서 값 모으기 (beforeEntry: 들어온 층의 노드는 진입 직전 상태로)
    private static SaveData Capture(bool beforeEntry)
    {
        GameManager gm = GameManager.Instance;
        StageManager sm = StageManager.Instance;
        if (gm == null || sm == null || RewardManager.Instance == null)
        {
            Debug.LogWarning("[Save] GameManager / StageManager / RewardManager 중 없는 것이 있어 저장하지 않습니다.");
            return null;
        }

        var data = new SaveData
        {
            currentHP = gm.currentHP,
            currentGold = gm.currentGold,
            rewardWeights = RewardManager.Instance.ExportRankWeights(),
            shopWeights = ShopManager.ExportRankWeights(),
            currentChapter = sm.currentChapter,
        };

        // 덱: CardSO 아이디 + 강화 단계
        foreach (CardInstance card in gm.deck)
        {
            if (card == null || card.baseData == null) continue;
            data.deck.Add(new CardSaveData { cardId = card.baseData.cardId, upgradeLevel = card.upgradeLevel });
        }

        // 플로어: 층 -> 노드 -> 연결된 노드 id
        if (sm.floors != null)
        {
            foreach (List<StageNode> floor in sm.floors)
            {
                var fs = new FloorSaveData();
                foreach (StageNode node in floor)
                {
                    // 들어온 층의 노드만 진입 직전 상태로 바꾸고, 나머지는 지금 값 그대로
                    bool canGo = node.canGo, isVisited = node.isVisited;
                    if (beforeEntry && sm.TryGetEntryState(node.id, out bool entryCanGo, out bool entryVisited))
                    {
                        canGo = entryCanGo;
                        isVisited = entryVisited;
                    }

                    fs.nodes.Add(new NodeSaveData
                    {
                        id = node.id,
                        floor = node.floor,
                        xIndex = node.xIndex,
                        stageType = (int)node.stageType,
                        canGo = canGo,
                        isVisited = isVisited,
                        prevIds = Ids(node.prevStages),
                        nextIds = Ids(node.nextStages),
                    });
                }
                data.floors.Add(fs);
            }
        }
        return data;
    }

    // 연결된 노드들 -> id 배열
    private static int[] Ids(List<StageNode> nodes)
    {
        if (nodes == null) return new int[0];
        var ids = new int[nodes.Count];
        for (int i = 0; i < nodes.Count; i++) ids[i] = nodes[i].id;
        return ids;
    }

    // ------------------------------------------------------------------ 이어하기

    /// <summary>
    /// 이어하기: 저장 파일을 읽어 적용하고 스테이지 선택 씬으로 이동합니다. 저장이 없거나 깨졌으면 false
    /// </summary>
    public static bool ContinueGame()
    {
        if (!Load()) return false;
        SceneTransition.Load("StageSelectScene");
        return true;
    }

    /// <summary>
    /// 저장 파일을 읽어 적용합니다 (씬 이동 없음). 지금 없는 매니저의 몫은 나중에 ApplyPending 에서 적용
    /// </summary>
    public static bool Load()
    {
        SaveData data = ReadFile(SavePath) ?? ReadFile(SavePath + ".bak");
        if (data == null)
        {
            Debug.LogWarning("[Save] 불러올 저장이 없거나 읽을 수 없습니다.");
            return false;
        }
        pending = data;
        pendingGlobals = true;
        pendingStage = true;
        ApplyPending();
        return true;
    }

    /// <summary>
    /// 불러온 데이터 중 아직 적용하지 못한 부분을 적용합니다.
    /// StageManager 가 맵을 그리기 직전(InitializeStageSelectScene)에 호출하므로 직접 부를 일은 거의 없음
    /// </summary>
    public static void ApplyPending()
    {
        if (pending == null) return;

        // 1~4. 덱 -> 체력 -> 골드 -> 가중치 2개 (GameManager, RewardManager 가 있어야 함)
        if (pendingGlobals && GameManager.Instance != null && RewardManager.Instance != null && DataManager.Instance != null)
        {
            ApplyDeck(pending.deck);
            GameManager.Instance.currentHP = pending.currentHP;
            GameManager.Instance.currentGold = pending.currentGold;
            RewardManager.Instance.ImportRankWeights(pending.currentChapter, pending.rewardWeights);
            ShopManager.ImportRankWeights(pending.currentChapter, pending.shopWeights);
            pendingGlobals = false;
        }

        // 5~6. 스테이지 챕터 -> 플로어 (StageManager 가 있어야 함)
        if (pendingStage && !pendingGlobals && StageManager.Instance != null)
        {
            StageManager.Instance.SetRunData(pending.currentChapter, BuildFloors(pending.floors));
            pendingStage = false;
        }

        if (!pendingGlobals && !pendingStage)
        {
            Debug.Log($"<color=green>[Save] 불러오기 완료 ({pending.deck.Count}장, HP {pending.currentHP}, 골드 {pending.currentGold}, {pending.currentChapter}챕터 {pending.floors.Count}층)</color>");
            pending = null;
        }
    }

    // 덱을 비우고, 저장된 카드 아이디로 CardSO 를 찾아 CardInstance 를 만들어 다시 채움
    private static void ApplyDeck(List<CardSaveData> saved)
    {
        List<CardInstance> deck = GameManager.Instance.deck;
        deck.Clear();
        if (DataManager.Instance == null) { Debug.LogError("[Save] DataManager 가 없어 카드를 찾을 수 없습니다."); return; }
        foreach (CardSaveData c in saved)
        {
            CardSO so = DataManager.Instance.GetCard(c.cardId);
            if (so == null)
            {
                Debug.LogWarning($"[Save] 카드 아이디 {c.cardId} 를 찾을 수 없어 덱에서 뺍니다.");
                continue;
            }
            CardInstance card = new CardInstance(so, 0);
            card.upgradeLevel = Mathf.Clamp(c.upgradeLevel, 0, so.maxUpgradeCount);
            deck.Add(card);
        }
    }

    // 분해한 것의 역순: 노드를 모두 만든 뒤 id 로 이전/다음 연결
    private static List<List<StageNode>> BuildFloors(List<FloorSaveData> saved)
    {
        var floors = new List<List<StageNode>>();
        var byId = new Dictionary<int, StageNode>();

        foreach (FloorSaveData fs in saved)
        {
            var floor = new List<StageNode>();
            foreach (NodeSaveData n in fs.nodes)
            {
                var node = new StageNode(n.id, n.floor, n.xIndex)
                {
                    stageType = (StageType)n.stageType,
                    canGo = n.canGo,
                    isVisited = n.isVisited,
                };
                floor.Add(node);
                byId[n.id] = node;
            }
            floors.Add(floor);
        }

        foreach (FloorSaveData fs in saved)
            foreach (NodeSaveData n in fs.nodes)
            {
                StageNode node = byId[n.id];
                Link(node.prevStages, n.prevIds, byId);
                Link(node.nextStages, n.nextIds, byId);
            }
        return floors;
    }

    // id 배열 -> 노드 연결
    private static void Link(List<StageNode> target, int[] ids, Dictionary<int, StageNode> byId)
    {
        target.Clear();
        if (ids == null) return;
        foreach (int id in ids)
            if (byId.TryGetValue(id, out StageNode other)) target.Add(other);
    }

    // ------------------------------------------------------------------ 삭제

    /// <summary>
    /// 저장 삭제 (게임 오버, 런 클리어 때 호출)
    /// </summary>
    public static void DeleteSave()
    {
        foreach (string p in new[] { SavePath, SavePath + ".bak", SavePath + ".tmp" })
            if (File.Exists(p)) File.Delete(p);
        pending = null;
        pendingGlobals = pendingStage = false;
        Debug.Log("[Save] 저장 삭제");
    }

    // ------------------------------------------------------------------ 파일 / 암호화

    // 임시 파일에 다 쓴 뒤 교체 (쓰는 도중 강제 종료돼도 기존 저장이 남음), 직전 저장은 .bak
    private static void WriteAtomic(byte[] bytes)
    {
        string path = SavePath, tmp = path + ".tmp", bak = path + ".bak";
        File.WriteAllBytes(tmp, bytes);
        if (!File.Exists(path)) { File.Move(tmp, path); return; }
        try
        {
            File.Replace(tmp, path, bak);
        }
        catch (PlatformNotSupportedException)
        {
            File.Copy(path, bak, true);
            File.Delete(path);
            File.Move(tmp, path);
        }
    }

    // 파일 읽기 + 복호화 + JSON (실패하면 null)
    private static SaveData ReadFile(string path)
    {
        if (!File.Exists(path)) return null;
        try
        {
            string json = Decrypt(File.ReadAllBytes(path));
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            if (data == null) return null;
            if (data.version != Version) Debug.LogWarning($"[Save] 저장 버전 {data.version} (현재 {Version})");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[Save] '{Path.GetFileName(path)}' 를 읽을 수 없습니다 (손상 또는 변조): {e.Message}");
            return null;
        }
    }

    // AES-256 (저장마다 새 IV): [IV 16바이트][암호문]
    private static byte[] Encrypt(string text)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key();
            aes.GenerateIV();
            using (ICryptoTransform enc = aes.CreateEncryptor())
            {
                byte[] plain = Encoding.UTF8.GetBytes(text);
                byte[] cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
                byte[] result = new byte[aes.IV.Length + cipher.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, aes.IV.Length);
                Buffer.BlockCopy(cipher, 0, result, aes.IV.Length, cipher.Length);
                return result;
            }
        }
    }

    // 복호화
    private static string Decrypt(byte[] data)
    {
        using (Aes aes = Aes.Create())
        {
            aes.Key = Key();
            byte[] iv = new byte[16];
            Buffer.BlockCopy(data, 0, iv, 0, 16);
            aes.IV = iv;
            using (ICryptoTransform dec = aes.CreateDecryptor())
            {
                byte[] plain = dec.TransformFinalBlock(data, 16, data.Length - 16);
                return Encoding.UTF8.GetString(plain);
            }
        }
    }

    // 문자열 -> 32바이트 키
    private static byte[] Key()
    {
        if (key != null) return key;
        using (SHA256 sha = SHA256.Create()) key = sha.ComputeHash(Encoding.UTF8.GetBytes(Secret));
        return key;
    }
}
