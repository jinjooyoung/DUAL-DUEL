using UnityEngine;

public class DataManager : MonoBehaviour
{
    public static DataManager Instance { get; private set; }

    [Header("데이터베이스 SO")]
    [SerializeField] private CardDatabaseSO cardDatabase;
    [SerializeField] private LocalizationDatabaseSO localizationDatabase;
    // 추후에 엑셀에 데이터 더 생기면 추가

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);  // 데이터는 모든 씬에서 계속 써야 하므로 유지
            InitDatabases();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void InitDatabases()
    {
        IInitializableDatabase[] allDatabases = new IInitializableDatabase[]
        {
            cardDatabase,
            localizationDatabase
            // 새 DB 변수 이름만 여기에 쉼표로 추가
        };

        foreach (var db in allDatabases)
        {
            db?.Initialize();
        }
    }

    /// <summary>
    /// 카드 데이터 받아오기
    /// </summary>
    /// <param name="cardId">카드 int ID</param>
    /// <returns>찾은 카드SO</returns>
    public CardSO GetCard(int cardId)
    {
        return cardDatabase != null ? cardDatabase.GetCardById(cardId) : null;
    }
}
