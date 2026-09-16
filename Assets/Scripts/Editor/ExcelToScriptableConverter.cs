#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using UnityEditor;
using UnityEngine;
using ExcelDataReader;

public enum ConversionType
{
    Card,
    Character,
    Reward,
    Localization
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
                // CardSO 생성 로직만 넘김
                ConvertSheet<CardSO, CardDatabaseSO>("Card", "CardDatabase", (row) =>
                {
                    if (row["cardId"] == DBNull.Value) return (null, null);

                    CardData data = ReadCardData(row);
                    if (data.cardId < 0) return (null, null);

                    CardSO cardSO = CreateInstance<CardSO>();
                    cardSO.cardId = data.cardId;

                    if (Enum.TryParse(data.cardType, true, out CardType type)) cardSO.cardType = type;
                    if (Enum.IsDefined(typeof(RankType), data.rank)) cardSO.rank = (RankType)data.rank;

                    cardSO.nameKey = data.nameKey;
                    cardSO.descKey = data.descKey;
                    cardSO.values = new List<int> { data.baseValue, data.upgrade_1, data.upgrade_2, data.upgrade_3, data.upgrade_4, data.upgrade_5 };

                    string artworkPath = $"Assets/Resources/Cards/Card_{data.cardId}.png";
                    cardSO.artwork = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);

                    string assetName = $"Card_{data.cardId:D4}";
                    return (cardSO, assetName);
                });
                break;

            case ConversionType.Localization:
                // LocalizationSO 생성 로직만 넘김
                ConvertSheet<LocalizationSO, LocalizationDatabaseSO>("Localization", "LocalizationDatabase", (row) =>
                {
                    if (row["nameKey"] == DBNull.Value) return (null, null);

                    LocalizationData data = ReadLocalizationData(row);
                    if (string.IsNullOrEmpty(data.key)) return (null, null);

                    LocalizationSO locSO = CreateInstance<LocalizationSO>();
                    locSO.key = data.key;
                    locSO.ko = data.ko;
                    locSO.en = data.en;
                    locSO.jp = data.jp;

                    string assetName = $"Localization_{data.key}";
                    return (locSO, assetName);
                });
                break;

            case ConversionType.Character:
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
        Func<DataRow, (TScriptable so, string assetName)> parseRowFunc)
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
                    AssetDatabase.CreateAsset(so, assetPath);
                    so.name = assetName;

                    createdList.Add(so);
                    EditorUtility.SetDirty(so);
                }

                // 2. 데이터베이스 SO 생성 및 할당 (items에 공통 주입)
                if (createDatabase && createdList.Count > 0)
                {
                    TDatabase database = CreateInstance<TDatabase>();

                    // 앞서 통일한 BaseDatabaseSO의 items 필드를 찾아 동적 할당
                    var itemsField = typeof(TDatabase).GetField("items");
                    if (itemsField != null)
                    {
                        itemsField.SetValue(database, createdList);
                    }

                    string databasePath = $"{outputFolder}/{dbFileName}.asset";
                    AssetDatabase.CreateAsset(database, databasePath);
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
        return new CardData
        {
            cardId = Convert.ToInt32(row["cardId"]),
            cardType = row["cardType"].ToString(),
            rank = Convert.ToInt32(row["rank"]),
            nameKey = row["nameKey"].ToString(),
            descKey = row["descKey"].ToString(),
            baseValue = Convert.ToInt32(row["baseValue"]),
            upgrade_1 = Convert.ToInt32(row["upgrade_1"]),
            upgrade_2 = Convert.ToInt32(row["upgrade_2"]),
            upgrade_3 = Convert.ToInt32(row["upgrade_3"]),
            upgrade_4 = Convert.ToInt32(row["upgrade_4"]),
            upgrade_5 = Convert.ToInt32(row["upgrade_5"])
        };
    }

    private LocalizationData ReadLocalizationData(DataRow row)
    {
        return new LocalizationData
        {
            key = row["nameKey"].ToString(),
            ko = GetCellString(row, "KO"),
            en = GetCellString(row, "EN"),
            jp = GetCellString(row, "JP")
        };
    }

    private string GetCellString(DataRow row, string columnName)
    {
        return (row[columnName] == DBNull.Value) ? string.Empty : row[columnName].ToString();
    }
}
#endif