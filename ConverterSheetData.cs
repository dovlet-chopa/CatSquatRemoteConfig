using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ConverterSheetData : MonoBehaviour
{
    public static ConverterSheetData Instance { get; private set; }

    [Header("Level Manager Sheets Range")]
    [SerializeField] string difficultyMode;
    [Space][SerializeField] List<string> levelsSheetRange;

    [Header("Enemy / Cat Stats Sheets Range")]
    [SerializeField] string enemyStats;
    [SerializeField] string catStats;
    // GitHub

    private int _initializationCount = 0;
    private int _completedCount = 0;
    private List<List<List<object>>> _levelSheetData = new List<List<List<object>>>();

    void Awake()
    {
        Instance = this;
        _initializationCount = levelsSheetRange.Count + 3;
    }

    public void OnInitializationComplete()
    {
        _completedCount++;
        if (_completedCount == _initializationCount)
        {
            EventsManager.FireOnInitializeData();
        }
    }

    #region LevelManager

    public void InitializeLevelDifficultyMode(Action<List<DifficultyModes>> OnSuccess)
    {
        GoogleSheetRemoteConfig.Instance.LoadSheetData(difficultyMode,
            result =>
            {
                OnSuccess?.Invoke(ConvertDifficultyModes(result));
            });
    }

    public void InitializeLevelManager(Action<List<LevelInfo>> OnSuccess)
    {
        int pendingLoads = levelsSheetRange.Count; // Track the number of sheets
        _levelSheetData = new List<List<List<object>>>(); // Initialize the data container

        foreach (var range in levelsSheetRange)
        {
            GoogleSheetRemoteConfig.Instance.LoadSheetData(range,
                result =>
                {
                    _levelSheetData.Add(result);
                    pendingLoads--; // Decrement counter when a sheet load completes

                    if (pendingLoads == 0) // Check if all sheets are loaded
                    {
                        OnSuccess?.Invoke(ConvertToLevelInfo(_levelSheetData)); // Process data
                    }
                });
        }
    }

    private List<LevelInfo> ConvertToLevelInfo(List<List<List<object>>> levelSheetData)
    {
        var levelInfos = new List<LevelInfo>();

        foreach (var sheetData in levelSheetData)
        {
            int levelIndex = sheetData.FindIndex(row => row.Contains("Level Properties"));
            int waveIndex = sheetData.FindIndex(row => row.Contains("Wave"));
            int partIndex = sheetData.FindIndex(row => row.Contains("Part"));
            int enemyTypeIndex = sheetData.FindIndex(row => row.Contains("Enemy Types"));
            int propertiesIndex = sheetData.FindIndex(row => row.Contains("Wave Properties"));

            var levelInfo = new LevelInfo
            {
                BaseHealth = GetIntFromRow(sheetData[levelIndex + 1], 1),
                LevelDificulty = GetFloatFromRow(sheetData[levelIndex + 2], 1),
                WaveInfos = new List<WaveInfo>()
            };

            if (waveIndex == -1 || partIndex == -1 || propertiesIndex == -1 || levelIndex == -1) continue;

            int waveCount = sheetData[waveIndex].Count - 1;
            WaveInfo currentWaveInfo = null;

            for (int i = 1; i <= waveCount; i++)
            {
                bool isWaveEmpty = sheetData[waveIndex][i] == null || sheetData[waveIndex][i].ToString() == "";
                bool isPartEmpty = sheetData[partIndex][i] == null || sheetData[partIndex][i].ToString() == "";

                if (isWaveEmpty)
                {
                    // Skip entirely if both Wave and Part are empty
                    if (isPartEmpty) continue;
                }
                else
                {
                    // Create a new WaveInfo since Wave has a value
                    currentWaveInfo = new WaveInfo
                    {
                        WaveScalingFactor = GetFloatFromRow(sheetData[propertiesIndex + 1], i),
                        WaveGoldAmount = GetFloatFromRow(sheetData[propertiesIndex + 2], i),
                        SpawnInfos = new List<EnemySpawnInfo>()
                    };
                    levelInfo.WaveInfos.Add(currentWaveInfo);
                }

                // Create SpawnInfo regardless of Wave existence if Part has a value
                var spawnInfo = new EnemySpawnInfo
                {
                    SpawnRate = GetFloatFromRow(sheetData[propertiesIndex + 3], i),
                    MinEnemyAmount = GetIntFromRow(sheetData[propertiesIndex + 4], i),
                    EnemyInfos = new List<EnemyTypeAndAmountInfo>()
                };

                for (int j = enemyTypeIndex; j < propertiesIndex - 2; j++)
                {
                    if (sheetData[j].Count > i)
                    {
                        string enemyName = sheetData[j][0].ToString();
                        int amount = GetIntFromRow(sheetData[j], i);

                        if (Enum.TryParse(enemyName, out EnemyType enemyType) && amount > 0)
                        {
                            var enemyInfo = new EnemyTypeAndAmountInfo
                            {
                                Type = enemyType,
                                Amount = amount
                            };
                            spawnInfo.EnemyInfos.Add(enemyInfo);
                        }
                    }
                }

                // Add spawnInfo to the current wave if it exists, else skip adding
                currentWaveInfo?.SpawnInfos.Add(spawnInfo);
            }

            levelInfos.Add(levelInfo);
        }

        return levelInfos;
    }

    private float GetFloatFromRow(List<object> row, int index)
    {
        return index < row.Count && float.TryParse(row[index]?.ToString().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float value) ? value : 0f;
    }

    private int GetIntFromRow(List<object> row, int index)
    {
        return index < row.Count && int.TryParse(row[index]?.ToString().Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) ? value : 0;
    }

    private List<DifficultyModes> ConvertDifficultyModes(List<List<object>> sheetData)
    {
        List<DifficultyModes> difficulties = new List<DifficultyModes>();

        // Extract each column for difficulty types
        List<object> typesRow = sheetData[1];  // "Difficulty Type", "Easy", "", "Norm", "", "Hard", "", "VeryHard"
        List<object> amountsRow = sheetData[2];
        List<object> speedsRow = sheetData[3];
        List<object> healthsRow = sheetData[4];
        List<object> rangesRow = sheetData[5];

        // Iterate over each column that represents a difficulty level (skipping the first index)
        for (int i = 1; i < typesRow.Count; i += 2)
        {
            // Only process if there's a valid type in this column
            if (typesRow[i] != null && typesRow[i].ToString() != "")
            {
                DifficultyModes mode = new DifficultyModes();

                // Assign enum type based on column name
                if (Enum.TryParse(typesRow[i].ToString(), out LevelDifficultType difficultyType))
                {
                    mode.Type = difficultyType;
                }

                // Parse and assign the other attributes, using CultureInfo.InvariantCulture
                mode.Amount = GetFloatFromRow(amountsRow, i);
                mode.Speed = GetFloatFromRow(speedsRow, i);
                mode.Health = GetFloatFromRow(healthsRow, i);

                // Parse range as Vector2
                string[] rangeParts = rangesRow[i]?.ToString().Trim().Split('-');
                if (rangeParts.Length == 2 &&
                    float.TryParse(rangeParts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float rangeMin) &&
                    float.TryParse(rangeParts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float rangeMax))
                {
                    mode.Range = new Vector2(rangeMin, rangeMax);
                }

                // Add to the list
                difficulties.Add(mode);
            }
        }

        return difficulties;
    }

    #endregion

    #region Enemy/Cat Stats

    public void InitializeEnemyStats(List<EnemyStatsInfo> enemiesList, Action OnSuccess)
    {
        GoogleSheetRemoteConfig.Instance.LoadSheetData(enemyStats,
            result =>
            {
                // Convert the result to get the latest stats
                List<EnemyStatsInfo> updatedStats = ConvertEnemyStats(result);

                foreach (var updatedEnemy in updatedStats)
                {
                    // Assuming 'Type' is the unique identifier
                    var existingEnemy = enemiesList.FirstOrDefault(e => e.Type == updatedEnemy.Type);

                    if (existingEnemy != null)
                    {
                        // Update properties as needed
                        existingEnemy.Health = updatedEnemy.Health;
                        existingEnemy.Damage = updatedEnemy.Damage;
                        existingEnemy.MoveSpeed = updatedEnemy.MoveSpeed;
                    }
                }

                OnSuccess?.Invoke();
            });
    }

    public void InitializeCatStats(Action<Dictionary<ItemType, CatStats>> OnSuccess)
    {
        GoogleSheetRemoteConfig.Instance.LoadSheetData(catStats,
            result =>
            {
                OnSuccess?.Invoke(ConvertCatStats(result));
            });
    }

    private List<EnemyStatsInfo> ConvertEnemyStats(List<List<object>> sheetData)
    {
        List<EnemyStatsInfo> enemies = new List<EnemyStatsInfo>();

        // Iterate over each row, skipping the header row
        for (int i = 1; i < sheetData.Count; i++)
        {
            List<object> row = sheetData[i];
            EnemyStatsInfo enemy = new EnemyStatsInfo();

            // Parse the enemy type
            if (Enum.TryParse(row[0].ToString(), out EnemyType enemyType))
            {
                enemy.Type = enemyType;
            }
            else
            {
                continue;
            }

            // Parse health, move speed, and damage, providing defaults for missing values
            enemy.Health = GetFloatFromRow(row, 1);
            enemy.MoveSpeed = GetFloatFromRow(row, 2);
            enemy.Damage = GetFloatFromRow(row, 3);

            // Add the enemy instance to the list
            enemies.Add(enemy);
        }

        return enemies;
    }

    private Dictionary<ItemType, CatStats> ConvertCatStats(List<List<object>> sheetData)
    {
        Dictionary<ItemType, CatStats> catStatsDictionary = new Dictionary<ItemType, CatStats>();
        ItemType currentCatType = ItemType.None;

        // Start from index 2 to skip the first two header rows
        for (int i = 2; i < sheetData.Count; i++)
        {
            List<object> row = sheetData[i];

            // Skip empty rows
            if (row.Count == 0 || (row[0] == null && row[1] == null && row[2] == null && row[3] == null))
            {
                continue;
            }

            // Check if row has a new Cat Type; if not, use the last Cat Type
            if (row[0] != null && Enum.TryParse(row[0].ToString(), out ItemType catType))
            {
                currentCatType = catType;

                // Initialize a new entry in the dictionary if it doesn't exist
                if (!catStatsDictionary.ContainsKey(currentCatType))
                {
                    catStatsDictionary[currentCatType] = new CatStats
                    {
                        ultDmgMult = GetFloatFromRow(row, 4),
                    };

                    // Parse range as Vector2
                    string[] coolDownParts = row[5]?.ToString().Trim().Split('-');
                    if (coolDownParts.Length == 2 &&
                        float.TryParse(coolDownParts[0].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float min) &&
                        float.TryParse(coolDownParts[1].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float max))
                    {
                        catStatsDictionary[currentCatType].ultCoolDwn = new Vector2(min, max);
                    }
                }
            }

            // Ensure a valid Cat Type is set
            if (currentCatType == ItemType.None)
                continue;

            CatStats catStats = catStatsDictionary[currentCatType];

            // Parse and add data to the respective lists
            if (float.TryParse(row[1]?.ToString().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float damage))
                catStats.Damage.Add(damage);

            if (float.TryParse(row[2]?.ToString().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float attackSpeed))
                catStats.AttackSpeed.Add(attackSpeed);

            if (float.TryParse(row[3]?.ToString().Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out float range))
                catStats.Range.Add(range);
        }

        return catStatsDictionary;
    }

    #endregion
}
