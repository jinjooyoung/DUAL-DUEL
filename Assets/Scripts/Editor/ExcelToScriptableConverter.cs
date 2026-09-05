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
    private const string excelFilePath = "Assets/Data/Excel/GameData.xlsx";     // 엑셀 파일 하나에 시트로 관리할 예정

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
            switch (conversionType)
            {
                case ConversionType.Card:
                    outputFolder = "Assets/Data/Generated/Cards";
                    break;

                case ConversionType.Character:
                    outputFolder = "Assets/Data/Generated/Characters";
                    break;

                case ConversionType.Reward:
                    outputFolder = "Assets/Data/Generated/Rewards";
                    break;

                case ConversionType.Localization:
                    outputFolder = "Assets/Data/Generated/Localization";
                    break;
            }

            prevConversionType = conversionType;
        }

        outputFolder = EditorGUILayout.TextField("Output Folder : ", outputFolder);
        createDatabase = EditorGUILayout.Toggle("Create Database Asset", createDatabase);

        EditorGUILayout.Space();

        if (GUILayout.Button("Convert to Scriptable Objects"))
        {
            if (string.IsNullOrEmpty(excelFilePath))
            {
                EditorUtility.DisplayDialog("Error", "Please select an Excel file first.", "OK");
                return;
            }

            switch (conversionType)
            {
                case ConversionType.Card:
                    ConvertExcelToCardSO();
                    break;

                    // 추후 구현. 일단 카드만
                case ConversionType.Character:
                    EditorUtility.DisplayDialog("Not Implemented", "Character conversion is not implemented yet.", "OK");
                    break;

                case ConversionType.Reward:
                    EditorUtility.DisplayDialog("Not Implemented", "Reward conversion is not implemented yet.", "OK");
                    break;

                case ConversionType.Localization:
                    EditorUtility.DisplayDialog("Not Implemented", "Localization conversion is not implemented yet.", "OK");
                    break;
            }
        }
    }

    private void ConvertExcelToCardSO()
    {
        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        try
        {
            string fullPath = Path.GetFullPath(excelFilePath);

            using (var stream = File.Open(fullPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            using (var reader = ExcelReaderFactory.CreateReader(stream))
            {
                DataSet result = reader.AsDataSet(new ExcelDataSetConfiguration
                {
                    ConfigureDataTable = (_) => new ExcelDataTableConfiguration
                    {
                        UseHeaderRow = true     // 엑셀의 첫 번째 행을 칼럼 이름으로 사용
                    }
                });

                DataTable cardTable = null;

                string sheetName = conversionType.ToString();

                // 읽은 엑셀 시트(테이블) 중에 시트 이름이 conversionType 타입인 시트를 찾아, cardTable에 할당
                foreach (DataTable table in result.Tables)
                {
                    if (table.TableName == sheetName)
                    {
                        cardTable = table;
                        break;
                    }
                }

                if (cardTable == null)
                {
                    EditorUtility.DisplayDialog("Error", "Could not find 'Card' sheet in the Excel file.", "OK");
                    return;
                }

                List<CardSO> createdCards = new List<CardSO>();

                foreach (DataRow row in cardTable.Rows)
                {
                    if (row["cardId"] == DBNull.Value)
                    {
                        continue;
                    }

                    CardData cardData = ReadCardData(row);

                    if (cardData.cardId <= 0)
                    {
                        Debug.LogWarning("Invalid Card ID. Skipping row.");
                        continue;
                    }

                    CardSO cardSO = ScriptableObject.CreateInstance<CardSO>();

                    cardSO.cardId = cardData.cardId;

                    if (Enum.TryParse(cardData.ownerType, true, out OwnerType ownerType))
                    {
                        cardSO.ownerType = ownerType;
                    }
                    else
                    {
                        Debug.LogError($"Card ID {cardData.cardId}: Invalid OwnerType '{cardData.ownerType}'");
                        DestroyImmediate(cardSO);
                        continue;
                    }

                    if (Enum.TryParse(cardData.cardType, true, out CardType cardType))
                    {
                        cardSO.cardType = cardType;
                    }
                    else
                    {
                        Debug.LogError($"Card ID {cardData.cardId}: Invalid CardType '{cardData.cardType}'");
                        DestroyImmediate(cardSO);
                        continue;
                    }

                    cardSO.nameKey = cardData.nameKey;
                    cardSO.descKey = cardData.descKey;

                    cardSO.values.Clear();
                    cardSO.values.Add(cardData.baseValue);
                    cardSO.values.Add(cardData.upgrade_1);
                    cardSO.values.Add(cardData.upgrade_2);
                    cardSO.values.Add(cardData.upgrade_3);
                    cardSO.values.Add(cardData.upgrade_4);
                    cardSO.values.Add(cardData.upgrade_5);

                    string artworkPath = $"Assets/Resources/Card_{cardData.cardId}.png";
                    cardSO.artwork = AssetDatabase.LoadAssetAtPath<Sprite>(artworkPath);

                    if (cardSO.artwork == null)
                    {
                        Debug.LogWarning($"Card ID {cardData.cardId}: Artwork not found at {artworkPath}");
                    }

                    string assetName = $"Card_{cardData.cardId:D4}.asset";
                    string assetPath = $"{outputFolder}/{assetName}";

                    AssetDatabase.CreateAsset(cardSO, assetPath);

                    cardSO.name = $"Card_{cardData.cardId:D4}";

                    createdCards.Add(cardSO);
                    EditorUtility.SetDirty(cardSO);
                }

                if (createDatabase && createdCards.Count > 0)
                {
                    CardDatabaseSO database = ScriptableObject.CreateInstance<CardDatabaseSO>();
                    database.cards = createdCards;

                    string databasePath = $"{outputFolder}/CardDatabase.asset";
                    AssetDatabase.CreateAsset(database, databasePath);

                    EditorUtility.SetDirty(database);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog(
                    "Success",
                    $"Created {createdCards.Count} Card SOs!",
                    "OK"
                );
            }
        }
        catch (Exception e)
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Failed to convert Excel: {e.Message}",
                "OK"
            );

            Debug.LogError($"Excel conversion error: {e}");
        }
    }

    private CardData ReadCardData(DataRow row)
    {
        CardData data = new CardData();

        data.cardId = Convert.ToInt32(row["cardId"]);
        data.ownerType = row["ownerType"].ToString();
        data.cardType = row["cardType"].ToString();
        data.nameKey = row["nameKey"].ToString();
        data.descKey = row["descKey"].ToString();
        data.baseValue = Convert.ToInt32(row["baseValue"]);
        data.upgrade_1 = Convert.ToInt32(row["upgrade_1"]);
        data.upgrade_2 = Convert.ToInt32(row["upgrade_2"]);
        data.upgrade_3 = Convert.ToInt32(row["upgrade_3"]);
        data.upgrade_4 = Convert.ToInt32(row["upgrade_4"]);
        data.upgrade_5 = Convert.ToInt32(row["upgrade_5"]);

        return data;
    }
}

#endif