using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

/// <summary>
/// 본 대사 묶음 기록 (영구). 게임 진행 저장(run.sav)과 별개 파일이라 죽거나 런을 클리어해도 남는다.
/// 묶음이 '시작되는 순간' 바로 저장하므로, 보는 도중 강제 종료해도 '봤음' 으로 남는다.
/// 지우는 방법은 에디터 메뉴(Tools > DUAL DUEL > 다이얼로그 기록 삭제) 또는 ResetAll() 뿐.
/// </summary>
public static class DialogueProgress
{
    private const string FileName = "dialogue.sav";
    private const string Secret = "DUAL_DUEL::dialogue::v1";

    [Serializable]
    private class Data { public List<int> seen = new List<int>(); }

    private static HashSet<int> seen;
    private static byte[] key;

    /// <summary>
    /// 저장 파일 경로
    /// </summary>
    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>
    /// 이 대사 묶음(시작 id)을 본 적 있는지
    /// </summary>
    public static bool HasSeen(int blockStartId)
    {
        EnsureLoaded();
        return seen.Contains(blockStartId);
    }

    /// <summary>
    /// 본 것으로 기록하고 즉시 저장 (DialogueManager.Play 가 묶음을 시작할 때 호출)
    /// </summary>
    public static void MarkSeen(int blockStartId)
    {
        EnsureLoaded();
        if (!seen.Add(blockStartId)) return;
        Write();
    }

    /// <summary>
    /// 본 묶음 목록 (읽기 전용)
    /// </summary>
    public static IReadOnlyCollection<int> SeenBlocks
    {
        get { EnsureLoaded(); return seen; }
    }

    /// <summary>
    /// 기록 전부 삭제 (에디터 메뉴, 설정 화면의 '스토리 초기화' 등)
    /// </summary>
    public static void ResetAll()
    {
        seen = new HashSet<int>();
        foreach (string p in new[] { SavePath, SavePath + ".bak", SavePath + ".tmp" })
            if (File.Exists(p)) File.Delete(p);
        Debug.Log("[Dialogue] 본 대사 기록을 모두 삭제했습니다.");
    }

    // ------------------------------------------------------------------ 파일

    // 처음 쓸 때 한 번 읽음 (본 파일이 깨졌으면 .bak)
    private static void EnsureLoaded()
    {
        if (seen != null) return;
        seen = new HashSet<int>();
        Data data = Read(SavePath) ?? Read(SavePath + ".bak");
        if (data != null) foreach (int id in data.seen) seen.Add(id);
    }

    // 임시 파일에 다 쓴 뒤 교체 (강제 종료돼도 기존 기록이 남음)
    private static void Write()
    {
        try
        {
            var data = new Data { seen = new List<int>(seen) };
            byte[] bytes = Encrypt(JsonUtility.ToJson(data));
            string path = SavePath, tmp = path + ".tmp", bak = path + ".bak";
            File.WriteAllBytes(tmp, bytes);
            if (!File.Exists(path)) { File.Move(tmp, path); return; }
            try { File.Replace(tmp, path, bak); }
            catch (PlatformNotSupportedException)
            {
                File.Copy(path, bak, true);
                File.Delete(path);
                File.Move(tmp, path);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[Dialogue] 본 대사 기록 저장 실패: {e.Message}");
        }
    }

    // 읽기 + 복호화 (실패하면 null)
    private static Data Read(string path)
    {
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<Data>(Decrypt(File.ReadAllBytes(path))); }
        catch (Exception e)
        {
            Debug.LogWarning($"[Dialogue] '{Path.GetFileName(path)}' 를 읽을 수 없습니다: {e.Message}");
            return null;
        }
    }

    // AES-256, [IV 16바이트][암호문]
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
                byte[] result = new byte[16 + cipher.Length];
                Buffer.BlockCopy(aes.IV, 0, result, 0, 16);
                Buffer.BlockCopy(cipher, 0, result, 16, cipher.Length);
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
                return Encoding.UTF8.GetString(dec.TransformFinalBlock(data, 16, data.Length - 16));
        }
    }

    // 문자열 -> 32바이트 키
    private static byte[] Key()
    {
        if (key != null) return key;
        using (SHA256 sha = SHA256.Create()) key = sha.ComputeHash(Encoding.UTF8.GetBytes(Secret));
        return key;
    }

    // 에디터에서 Domain Reload 를 끈 채 다시 플레이해도 파일 기준으로 다시 읽도록
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetCache() => seen = null;
}
