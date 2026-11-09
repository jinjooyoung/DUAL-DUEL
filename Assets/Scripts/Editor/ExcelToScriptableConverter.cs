#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEngine;
using ExcelDataReader;
using System.Linq;

public enum ConversionType
{
    Card,
    Character,
    Monster,
    Reward
}

public class ExcelToScriptableConverter : EditorWindow
{
    private const string excelFilePath = "Assets/Data/Excel/DUAL_DUEL_DataTable.xlsx";
    private string outputFolder = "Assets/Data/Generated/Cards";
    private bool createDatabase = true;

    private ConversionType conversionType = ConversionType.Card;
    private ConversionType prevConversionType;

    [MenuItem("Tools/Excel to Scriptable Objects")]
    public static void ShowWindow()
    {
        GetWindow<ExcelToScriptableConverter>("Excel to Scriptable Objects");
    }

    private void OnGUI()
    {
        GUILayout.Label("Excel to Scriptable Object Converter", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        conversionType = (ConversionType)EditorGUILayout.EnumPopup("Conversion Type : ", conversionType);

        if (conversionType != prevConversionType)
        {
            outputFolder = $"Assets/Data/Generated/{conversionType}s";
            prevConversionType = conversionType;
        }

        outputFolder = EditorGUILayout.TextField("Output Folder : ", outputFolder);
        createDatabase = EditorGUILayout.Toggle("Create Database Asset", createDatabase);

        EditorGUILayout.Space();

        if (GUILayout.Button("Convert to Scriptable Objects"))
        {
            ExecuteConversion();
        }
    }

    // 변환 분기점: 달라지는 부분(데이터 파싱 및 SO 값 채우기)만 정의하여 공통 엔진 호출
    private void ExecuteConversion()
    {
        switch (conversionType)
        {
            case ConversionType.Card:
                ConvertSheet<CardSO, CardDatabaseSO>("Card", "CardDatabase", (row) =>
                {
                    if (row["cardId"] == DBNull.Value) return (null, null);

                    CardData data = ReadCardData(row);
                    if (data.cardId < 0) return (null, null);

                    CardSO cardSO = CreateInstance<CardSO>();

                    // 1. 데이터 값 세팅 (타입/등급/강화 단계별 수치/조건/보상 파싱은 CardSO에서 일괄 처리)
                    cardSO.SetupFromData(data);

                    // 2. 리소스 아트워크 (Assets/Resources 기준 상대경로, 확장자 없이)
                    // AssetDatabase.LoadAssetAtPath는 "Assets/...png" 전체 경로가 필요해서 "Cards/Public/..." 형태로는 null이 됨
                    cardSO.artwork = LoadResourceSprite($"Cards/Card_{data.cardId}");

                    cardSO.bgSprite = LoadResourceSprite("Cards/Public/Card_BG");
                    cardSO.typeIconSprite = LoadResourceSprite($"Cards/Public/{cardSO.cardType}");
                    cardSO.gimmikIconSprite = LoadResourceSprite("Cards/Public/GimmikIcon_Test");
                    cardSO.FilledStarSprite = LoadResourceSprite("Cards/Public/FilledStar");
                    cardSO.EmptyStartSprite = LoadResourceSprite("Cards/Public/EmptyStar");

                    string assetName = $"Card_{data.cardId:D4}";
                    return (cardSO, assetName);
                });
                break;

            case ConversionType.Character:
                ConvertSheet<CharacterSO, CharacterDatabaseSO>("Character", "CharacterDatabase", (row) =>
                {
                    if (row["characterID"] == DBNull.Value) return (null, null);

                    CharacterData data = ReadCharacterData(row);
                    if (data.characterID < 0) return (null, null);

                    CharacterSO charSO = CreateInstance<CharacterSO>();
                    charSO.characterID = data.characterID;
                    charSO.characterName = data.characterName;
                    charSO.descriptionText = data.descriptionText;
                    charSO.playStyleText = data.playStyleText;
                    charSO.signatureCardID = data.signatureCardID;
                    charSO.maxHp = data.maxHp;
                    charSO.startingLife = data.startingLife;

                    // "1001,1002,1003" 문자열을 List<int>로 파싱
                    charSO.startDeckID = ParseIdList(data.startDeckID);

                    string assetName = $"Character_{data.characterID:D4}";
                    return (charSO, assetName);
                });
                break;

            case ConversionType.Monster:
                ConvertSheet<MonsterSO, MonsterDatabaseSO>("Monster", "MonsterDatabase", (row) =>
                {
                    if (row["monsterID"] == DBNull.Value) return (null, null);

                    MonsterData data = ReadMonsterData(row);
                    if (data.monsterID < 0) return (null, null);

                    MonsterSO monSO = CreateInstance<MonsterSO>();

                    // 타입/등장 스테이지/기믹 파싱은 MonsterSO에서 일괄 처리
                    monSO.SetupFromData(data);

                    string assetName = $"Monster_{data.monsterID:D4}";
                    return (monSO, assetName);
                },
                // DB items를 등장 스테이지 오름차순으로 저장 (타입별 리스트도 이 순서를 그대로 따라감)
                MonsterDatabaseSO.CompareByAppearStage);
                break;

            case ConversionType.Reward:
                EditorUtility.DisplayDialog("Not Implemented", $"{conversionType} is not implemented yet.", "OK");
                break;
        }
    }

    /// <summary>
    /// [모든 데이터 타입 공통 변환 엔진]
    /// 엑셀 열기 -> 시트 탐색 -> 폴더 체크 -> SO 루프 생성 -> DB 생성 -> 저장/새로고침 -> 이 공통 로직을 하나의 함수로 처리
    /// </summary>
    private void ConvertSheet<TScriptable, TDatabase>(
        string sheetName,
        string dbFileName,
        Func<DataRow, (TScriptable so, string assetName)> parseRowFunc,
        Comparison<TScriptable> sortComparison = null)
        where TScriptable : ScriptableObject
        where TDatabase : ScriptableObject, IInitializableDatabase
    {
        if (!Directory.Exists(outputFolder)) Directory.CreateDirectory(outputFolder);

        try
        {
            string fullPath = Path.GetFullPath(excelFilePath);

            using (var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration { UseHeaderRow = true }
                });

                DataTable targetTable = null;
                foreach (DataTable table in result.Tables)
                {
                    if (table.TableName == sheetName)
                    {
                        targetTable = table;
                        break;
                    }
                }

                if (targetTable == null)
                {
                    EditorUtility.DisplayDialog("Error", $"Could not find '{sheetName}' sheet in the Excel file.", "OK");
                    return;
                }

                List<TScriptable> createdList = new List<TScriptable>();

                // 1. 각 행을 돌며 SO 생성
                foreach (DataRow row in targetTable.Rows)
                {
                    var (so, assetName) = parseRowFunc(row);
                    if (so == null) continue;

                    string assetPath = $"{outputFolder}/{assetName}.asset";
                    TScriptable existing = AssetDatabase.LoadAssetAtPath<TScriptable>(assetPath);

                    if (existing != null)
                    {
                        // 기존 에셋 유지 + 내용만 교체 -> GUID가 그대로라 프리팹/씬/다른 SO의 참조가 끊기지 않음
                        EditorUtility.CopySerialized(so, existing);
                        existing.name = assetName;
                        DestroyImmediate(so);
                        so = existing;
                    }
                    else
                    {
                        AssetDatabase.CreateAsset(so, assetPath);
                        so.name = assetName;
                    }

                    createdList.Add(so);
                    EditorUtility.SetDirty(so);
                }

                // 1-1. 정렬 기준이 있으면 DB에 넣기 전에 정렬
                if (sortComparison != null) createdList.Sort(sortComparison);

                // 2. 데이터베이스 SO 생성 및 할당 (items에 공통 주입)
                if (createDatabase && createdList.Count > 0)
                {
                    // 데이터베이스도 기존 에셋이 있으면 재사용 (GameManager 등에서 참조 중인 GUID 유지)
                    string databasePath = $"{outputFolder}/{dbFileName}.asset";
                    TDatabase database = AssetDatabase.LoadAssetAtPath<TDatabase>(databasePath);
                    bool isNewDatabase = database == null;
                    if (isNewDatabase) database = CreateInstance<TDatabase>();

                    // 앞서 통일한 BaseDatabaseSO의 items 필드를 찾아 동적 할당
                    var itemsField = typeof(TDatabase).GetField("items");
                    if (itemsField != null)
                    {
                        itemsField.SetValue(database, createdList);
                    }

                    if (isNewDatabase) AssetDatabase.CreateAsset(database, databasePath);
                    EditorUtility.SetDirty(database);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Success", $"Created {createdList.Count} {sheetName} SOs & Database!", "OK");
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog("Error", $"Failed: {e.Message}", "OK");
            Debug.LogError($"Excel conversion error: {e}");
        }
    }

    // --- 각 시트별 순수 데이터 파싱 헬퍼 함수들 ---

    private CardData ReadCardData(DataRow row)
    {
        // 열은 헤더 이름으로 읽으므로 엑셀 열 순서가 바뀌어도 상관없음
        // maxUpgradeCount가 0~1인 카드는 upgrade 열을 비워둘 수 있으므로 빈 셀은 기본값 처리
        return new CardData
        {
            cardId = GetCellInt(row, "cardId"),
            cardType = GetCellString(row, "cardType"),
            rank = GetCellInt(row, "rank", 1),
            nameKey = GetCellString(row, "nameKey"),
            typeDescKey = GetCellString(row, "typeDescKey"),
            gimmickDescKey = GetCellString(row, "gimmickDescKey"),

            baseValue = GetCellInt(row, "baseValue"),
            upgrade_1 = GetCellFloat(row, "upgrade_1"),
            upgrade_2 = GetCellFloat(row, "upgrade_2"),
            upgradeTarget = GetCellInt(row, "upgradeTarget", CardSO.UPGRADE_TARGET_VALUE),
            maxUpgradeCount = GetCellInt(row, "maxUpgradeCount", CardSO.MAX_UPGRADE_LIMIT),

            isExhaust = row["isExhaust"] != DBNull.Value && Convert.ToBoolean(row["isExhaust"]),
            conditionCategory = GetCellString(row, "conditionCategory", "None"),
            conditionParam = GetCellString(row, "conditionParam"),
            rewardCategory = GetCellString(row, "rewardCategory", "None"),
            rewardParam = GetCellFloat(row, "rewardParam")
        };
    }

    private CharacterData ReadCharacterData(DataRow row)
    {
        return new CharacterData
        {
            characterID = Convert.ToInt32(row["characterID"]),
            characterName = row["characterName"].ToString(),
            descriptionText = row["descriptionText"].ToString(),
            playStyleText = row["playStyleText"].ToString(),
            startDeckID = row["startDeckID"].ToString(),
            signatureCardID = Convert.ToInt32(row["signatureCardID"]),
            maxHp = Convert.ToInt32(row["maxHp"]),
            startingLife = Convert.ToInt32(row["startingLife"])
        };
    }

    private MonsterData ReadMonsterData(DataRow row)
    {
        return new MonsterData
        {
            monsterID = GetCellInt(row, "monsterID"),
            monsterName = GetCellString(row, "monsterName"),
            monsterType = GetCellString(row, "monsterType", "Normal"),   // "Elite " 같은 뒤 공백은 GetCellString에서 Trim
            appearStartStage = GetCellInt(row, "appearStartStage", 101),
            baseAttack = GetCellInt(row, "baseAttack"),
            maxHp = GetCellInt(row, "maxHp"),

            gimmickType1 = GetCellString(row, "gimmickType1", "None"),
            gimmickParam1 = GetCellInt(row, "gimmickParam1"),
            gimmickType2 = GetCellString(row, "gimmickType2", "None"),
            gimmickParam2 = GetCellInt(row, "gimmickParam2")
        };
    }

    // ;로 구분된 ID 문자열을 List<int>로 안전하게 변환하는 공통 함수
    private List<int> ParseIdList(string rawString)
    {
        if (string.IsNullOrWhiteSpace(rawString)) return new List<int>();

        return rawString.Split(';')
            .Select(id => id.Trim())
            .Where(id => int.TryParse(id, out _))
            .Select(int.Parse)
            .ToList();
    }

    private string GetCellString(DataRow row, string columnName, string defaultValue = "")
    {
        if (row[columnName] == DBNull.Value) return defaultValue;

        string value = row[columnName].ToString().Trim();
        return string.IsNullOrEmpty(value) ? defaultValue : value;
    }

    private int GetCellInt(DataRow row, string columnName, int defaultValue = 0)
    {
        if (row[columnName] == DBNull.Value) return defaultValue;
        return Convert.ToInt32(row[columnName]);
    }

    private float GetCellFloat(DataRow row, string columnName, float defaultValue = 0f)
    {
        if (row[columnName] == DBNull.Value) return defaultValue;
        return Convert.ToSingle(row[columnName]);
    }

    private Sprite LoadResourceSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite == null) Debug.LogWarning($"[ExcelConverter] 스프라이트를 찾을 수 없습니다: Resources/{resourcePath}");
        return sprite;
    }
}
#endif