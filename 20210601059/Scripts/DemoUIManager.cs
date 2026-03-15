using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Mehmet{
public class DemoUIManager : MonoBehaviour
{
    [Header("References")]
    public BSPDungeonGenerator dungeonGenerator;
    public GameObject playerPrefab;
    public GameObject gridObject;
    
    [Header("UI Elements")]
    public GameObject mainMenuPanel;
    public GameObject gameplayPanel;
    public TextMeshProUGUI statusText;
    public TextMeshProUGUI statsText;
    public Button restartButton;
    
    [Header("Level Templates")]
    public DungeonTemplate[] templates;
    
    private GameObject currentPlayer;
    private PlayerController currentPlayerController;
    private int currentLevelIndex = 0;
    private float startTime;
    private int totalDeaths = 0;
    private int totalCompletions = 0;
    private bool levelEnded = false;
    
    [System.Serializable]
    public class DungeonTemplate
    {
        public string name;
        public int seed;
        public int minRoomSize = 6;
        public int maxRoomSize = 15;
    }
    
    void Start()
    {
        if (gridObject == null)
        {
            gridObject = GameObject.Find("Grid");
        }
        
        ShowMainMenu();
    }
    
    void Update()
    {
        if (gameplayPanel.activeSelf && currentPlayer != null && !levelEnded)
        {
            UpdateGameplayStats();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (gameplayPanel.activeSelf)
            {
                OnBackToMenu();
            }
        }
    }
    
    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        gameplayPanel.SetActive(false);
        
        if (gridObject != null)
        {
            gridObject.SetActive(false);
        }
        
        Time.timeScale = 0f;
        
        if (statusText != null)
        {
            statusText.color = Color.white;
        }
        
        levelEnded = false;
    }
    
    void HideMainMenu()
    {
        mainMenuPanel.SetActive(false);
        gameplayPanel.SetActive(true);
        
        if (gridObject != null)
        {
            gridObject.SetActive(true);
        }
        
        Time.timeScale = 1f;
        levelEnded = false;
    }
    
    public void OnGenerateLevel1()
    {
        GenerateLevel(0);
    }
    
    public void OnGenerateLevel2()
    {
        GenerateLevel(1);
    }
    
    public void OnGenerateLevel3()
    {
        GenerateLevel(2);
    }
    
    public void OnGenerateLevel4()
    {
        GenerateLevel(3);
    }
    
    public void OnGenerateLevel5()
    {
        GenerateLevel(4);
    }
    
    void GenerateLevel(int templateIndex)
    {
        if (templates == null || templateIndex >= templates.Length)
        {
            Debug.LogError($"Template {templateIndex} not found!");
            return;
        }
        
        currentLevelIndex = templateIndex;
        DungeonTemplate template = templates[templateIndex];
        
        Debug.Log($"🎮 Generating Level: {template.name}");
        
        CleanupCurrentLevel();
        
        Random.InitState(template.seed != 0 ? template.seed : (int)System.DateTime.Now.Ticks);
        
        if (dungeonGenerator != null)
        {
            dungeonGenerator.minRoomWidth = template.minRoomSize;
            dungeonGenerator.maxRoomWidth = template.maxRoomSize;
            dungeonGenerator.minRoomHeight = template.minRoomSize;
            dungeonGenerator.maxRoomHeight = template.maxRoomSize;
            dungeonGenerator.GenerateDungeon();
        }
        
        SpawnPlayer();
        
        HideMainMenu();
        
        if (statusText != null)
        {
            statusText.text = $"Level: {template.name}";
            statusText.color = Color.white;
        }
        
        startTime = Time.time;
        
        Debug.Log($"✅ Level {template.name} generated successfully!");
    }
    
    void CleanupCurrentLevel()
    {
        if (currentPlayerController != null)
        {
            currentPlayerController.OnGoalReached = null;
            currentPlayerController.OnPlayerDeath = null;
        }
        
        if (currentPlayer != null)
        {
            Destroy(currentPlayer);
            currentPlayer = null;
            currentPlayerController = null;
        }
        
        EnemyBase[] enemies = FindObjectsOfType<EnemyBase>();
        foreach (EnemyBase enemy in enemies)
        {
            Destroy(enemy.gameObject);
        }
        
        levelEnded = false;
        
        Debug.Log("🧹 Previous level cleaned up");
    }
    
    void SpawnPlayer()
    {
        if (playerPrefab == null || dungeonGenerator == null)
        {
            Debug.LogError("Player prefab or dungeon generator not assigned!");
            return;
        }
        
        Vector2Int spawnPos = dungeonGenerator.spawnPosition;
        Vector3 worldPos = new Vector3(spawnPos.x, spawnPos.y, 0);
        
        currentPlayer = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        
        currentPlayerController = currentPlayer.GetComponent<PlayerController>();
        if (currentPlayerController != null)
        {
            currentPlayerController.Initialize(dungeonGenerator);
            
            currentPlayerController.OnGoalReached = HandleGoalReached;
            currentPlayerController.OnPlayerDeath = HandlePlayerDeath;
            
            Debug.Log("✅ Player events connected!");
        }
        
        PlayerShooting shooting = currentPlayer.GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            if (shooting.currentAmmo <= 0)
            {
                shooting.currentAmmo = shooting.maxAmmo > 0 ? shooting.maxAmmo : 30;
                shooting.maxAmmo = shooting.maxAmmo > 0 ? shooting.maxAmmo : 30;
            }
            Debug.Log($"✅ Player shooting initialized - Ammo: {shooting.currentAmmo}/{shooting.maxAmmo}");
        }
        else
        {
            Debug.LogWarning("⚠️ PlayerShooting component not found on player!");
        }
        
        Debug.Log($"✅ Player spawned at {spawnPos}");
    }
    
    void UpdateGameplayStats()
    {
        if (currentPlayer == null || currentPlayerController == null) return;
        
        float elapsedTime = Time.time - startTime;
        int minutes = (int)(elapsedTime / 60f);
        int seconds = (int)(elapsedTime % 60f);
        
        float distToGoal = currentPlayerController.GetDistanceToGoal();
        
        int aliveEnemies = CountAliveEnemies();
        int totalEnemies = FindObjectsOfType<EnemyBase>().Length;
        
        int currentAmmo = 0;
        int maxAmmo = 0;
        
        PlayerShooting shooting = currentPlayer.GetComponent<PlayerShooting>();
        if (shooting != null)
        {
            currentAmmo = shooting.currentAmmo;
            maxAmmo = shooting.maxAmmo;
        }
        
        if (statsText != null)
        {
            statsText.text = $"Time: {minutes:00}:{seconds:00}\n" +
                            $"Health: {currentPlayerController.health:F0}/{currentPlayerController.maxHealth}\n" +
                            $"Ammo: {currentAmmo}/{maxAmmo}\n" +
                            $"Enemies: {aliveEnemies}\n" +
                            $"Distance: {distToGoal:F1}m\n" +
                            $"Deaths: {totalDeaths}\n" +
                            $"Wins: {totalCompletions}";
        }
    }
    
    int CountAliveEnemies()
    {
        EnemyBase[] allEnemies = FindObjectsOfType<EnemyBase>();
        int count = 0;
        
        foreach (EnemyBase enemy in allEnemies)
        {
            if (enemy != null && enemy.isAlive)
            {
                count++;
            }
        }
        
        return count;
    }
    
    void HandleGoalReached()
    {
        if (levelEnded) return; 
        
        levelEnded = true;
        totalCompletions++;
        
        if (statusText != null)
        {
            statusText.text = "GOAL REACHED!";
            statusText.color = Color.green;
        }
        
        float completionTime = Time.time - startTime;
        Debug.Log($"✅ Goal reached! Time: {completionTime:F2}s");
        
        Invoke("ShowMainMenu", 2f);
    }
    
    void HandlePlayerDeath()
    {
        if (levelEnded) return; 
        
        levelEnded = true;
        totalDeaths++;
        
        if (statusText != null)
        {
            statusText.text = "PLAYER DIED";
            statusText.color = Color.red;
        }
        
        float deathTime = Time.time - startTime;
        Debug.Log($"💀 Player died at {deathTime:F2}s");
        
        Invoke("ShowMainMenu", 2f);
    }
    
    public void OnRestartLevel()
    {
        GenerateLevel(currentLevelIndex);
    }
    
    public void OnBackToMenu()
    {
        CleanupCurrentLevel();
        ShowMainMenu();
    }
    
    public void OnQuitGame()
    {
        Debug.Log("Quitting game...");
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
}