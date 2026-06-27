using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private Vector2 normalSpawnIntervalRange = new Vector2(1.1f, 1.7f);
    [SerializeField] private float fastForwardSpawnRateMultiplier = 2.2f;
    [SerializeField] private float moveDuration = 6.0f;
    [SerializeField] private float spawnX = 650f;
    [SerializeField] private float despawnX = -650f;
    [SerializeField] private float topLaneY = 270f;
    [SerializeField] private float bottomLaneY = -90f;
    [SerializeField] private float laneRandomYRange = 60f;
    [SerializeField] private float bombSpawnRate = 0.10f;
    [SerializeField] private float minAllowedSpawnInterval = 0.22f;
    [SerializeField] private int initialPoolSize = 28;
    [SerializeField] private bool debugSpawnSelectionLog = false;

    private GameManager gameManager;
    private RectTransform itemLayer;
    private ItemController itemTemplate;
    private ComponentPool<ItemController> itemPool;
    private ItemData bombData;
    private float topLaneSpawnTimer;
    private float bottomLaneSpawnTimer;
    private float nextNoCandidateLogTime;
    private float activeSpawnX;
    private float activeDespawnX;
    private ItemFlowDirection flowDirection = ItemFlowDirection.RightToLeft;

    public void Configure(GameManager manager, RectTransform layer, ItemController template)
    {
        gameManager = manager;
        itemLayer = layer;
        itemTemplate = template;
        bombData = ItemData.CreateBomb(ItemDatabase.LoadSpriteOrNull("Item_Bomb"));
        itemPool = new ComponentPool<ItemController>(
            CreatePooledItem,
            initialPoolSize,
            item => item.gameObject.SetActive(true),
            item =>
            {
                item.ResetForPool();
                item.gameObject.SetActive(false);
            });
        RefreshFlowDirection();
        topLaneSpawnTimer = 0.15f;
        bottomLaneSpawnTimer = 0.55f;
        Debug.Log($"[ItemSpawner] Item pool initialized. InitialSize={initialPoolSize}, FlowDirection={FlowDirectionSettings.GetDisplayName(flowDirection)}");
    }

    public void BeginCountdownSpawn()
    {
        topLaneSpawnTimer = 0f;
        bottomLaneSpawnTimer = 0.35f;
    }

    public void Tick(float deltaTime)
    {
        if (gameManager == null || itemLayer == null || itemTemplate == null || gameManager.IsTimeUp)
        {
            return;
        }

        topLaneSpawnTimer -= deltaTime;
        bottomLaneSpawnTimer -= deltaTime;

        if (topLaneSpawnTimer <= 0f)
        {
            SpawnItem(GetRandomLaneY(topLaneY));
            topLaneSpawnTimer = GetNextSpawnInterval();
        }

        if (bottomLaneSpawnTimer <= 0f)
        {
            SpawnItem(GetRandomLaneY(bottomLaneY));
            bottomLaneSpawnTimer = GetNextSpawnInterval();
        }
    }

    private void SpawnItem(float laneY)
    {
        var spawnBomb = Random.value < bombSpawnRate;
        var pool = spawnBomb ? null : gameManager.GetCurrentSpawnPool();
        if (!spawnBomb && (pool == null || pool.Count == 0))
        {
            if (Time.realtimeSinceStartup >= nextNoCandidateLogTime)
            {
                Debug.LogWarning($"[ItemSpawner] No spawn candidates for stage: {StageManager.GetStageForLevel(gameManager.CurrentSuctionLevel)}, Lv={gameManager.CurrentSuctionLevel}");
                nextNoCandidateLogTime = Time.realtimeSinceStartup + 2f;
            }
            return;
        }

        var data = spawnBomb ? bombData : ChooseWeightedItem(pool);
        if (debugSpawnSelectionLog)
        {
            Debug.Log($"[ItemSpawner] Selected item: {data.Id} / {data.DisplayName} / {(data.IsBomb ? "Bomb" : "Lv" + data.RequiredLevel)} / {data.SpriteName} / {(data.Sprite != null ? "sprite found" : "placeholder used")}");
        }
        var item = itemPool.Get();
        var duration = Mathf.Max(2.2f, moveDuration - gameManager.CurrentSuctionLevel * 0.18f);
        item.Initialize(data, gameManager.CurrentSuctionLevel, new Vector2(activeSpawnX, laneY), activeDespawnX, duration, gameManager.HandleItemMissed);
        gameManager.RegisterItem(item);
    }

    private void RefreshFlowDirection()
    {
        flowDirection = FlowDirectionSettings.Load();
        var leftX = Mathf.Min(spawnX, despawnX);
        var rightX = Mathf.Max(spawnX, despawnX);

        if (flowDirection == ItemFlowDirection.LeftToRight)
        {
            activeSpawnX = leftX;
            activeDespawnX = rightX;
            return;
        }

        activeSpawnX = rightX;
        activeDespawnX = leftX;
    }

    public void ReleaseItem(ItemController item)
    {
        itemPool?.Release(item);
    }

    private ItemController CreatePooledItem()
    {
        var item = Instantiate(itemTemplate, itemLayer);
        item.gameObject.SetActive(false);
        return item;
    }

    private float GetNextSpawnInterval()
    {
        var range = normalSpawnIntervalRange;
        var stageMultiplier = GameManager.GetStageSpawnIntervalMultiplier(gameManager.CurrentStage);
        var fastMultiplier = gameManager.IsFastForwardActive ? fastForwardSpawnRateMultiplier : 1f;
        var interval = Random.Range(range.x, range.y) / stageMultiplier / fastMultiplier;
        return Mathf.Max(minAllowedSpawnInterval, interval);
    }

    private float GetRandomLaneY(float centerY)
    {
        return Random.Range(centerY - laneRandomYRange, centerY + laneRandomYRange);
    }

    private static ItemData ChooseWeightedItem(IReadOnlyList<ItemData> pool)
    {
        var totalWeight = 0;
        for (var i = 0; i < pool.Count; i++)
        {
            totalWeight += Mathf.Max(0, pool[i].SpawnWeight);
        }

        if (totalWeight <= 0)
        {
            return pool[Random.Range(0, pool.Count)];
        }

        var roll = Random.Range(0, totalWeight);
        for (var i = 0; i < pool.Count; i++)
        {
            roll -= Mathf.Max(0, pool[i].SpawnWeight);
            if (roll < 0)
            {
                return pool[i];
            }
        }

        return pool[pool.Count - 1];
    }
}
