using System;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// 코드에서 로컬라이제이션 테이블 문자열을 바로 꺼내 쓰기 위한 공용 헬퍼 (툴팁 등 LocalizeStringEvent가 없는 곳에서 사용)
/// 테이블 문자열의 {0}, {1} ... 자리에 args가 순서대로 들어감
/// </summary>
public static class LocalizedTextUtil
{
    /// <summary>
    /// 테이블에서 key 문자열을 찾아 args로 채워 반환. 키가 비었거나 없으면 fallback(없으면 빈 문자열)을 같은 args로 채워 반환
    /// </summary>
    public static string Get(string tableName, string key, string fallback, params object[] args)
    {
        if (string.IsNullOrEmpty(key)) return Format(fallback, args);

        // 엔트리가 없으면 Localization이 "No translation found" 문구를 돌려주므로 미리 확인
        var table = LocalizationSettings.StringDatabase.GetTable(tableName);
        if (table == null || table.GetEntry(key) == null)
        {
            if (string.IsNullOrEmpty(fallback))
                Debug.LogWarning($"[LocalizedTextUtil] '{tableName}' 테이블에 '{key}' 키가 없습니다.");
            return Format(fallback, args);
        }

        try
        {
            // Smart 엔트리면 SmartFormat, 아니면 string.Format으로 args 적용
            return LocalizationSettings.StringDatabase.GetLocalizedString(tableName, key, args);
        }
        catch (FormatException e)
        {
            Debug.LogWarning($"[LocalizedTextUtil] '{key}' 포맷 오류: {e.Message}");
            return Format(fallback, args);
        }
    }

    private static string Format(string format, object[] args)
    {
        if (string.IsNullOrEmpty(format)) return string.Empty;
        if (args == null || args.Length == 0) return format;

        try { return string.Format(format, args); }
        catch (FormatException) { return format; }
    }
}
