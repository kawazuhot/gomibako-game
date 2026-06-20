using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] private float spawnInterval = 1.25f;
    [SerializeField] private float moveDuration = 6.2f;
    [SerializeField] private float spawnX = 650f;
    [SerializeField] private float despawnX = -650f;
    [SerializeField] private Vector2 spawnYRange = new Vector2(-100f, 160f);

    private GameManager gameManager;
    private RectTransform itemLayer;
    private ItemController itemTemplate;
    private float spawnTimer;

    public void Configure(GameManager manager, RectTransform layer, ItemController template)
    {
        gameManager = manager;
        itemLayer = layer;
        itemTemplate = template;
        spawnTimer = 0.15f;
    }

    public void Tick(float deltaTime)
    {
        if (gameManager == null || itemLayer == null || itemTemplate == null || gameManager.IsTimeUp)
        {
            return;
        }

        spawnTimer -= deltaTime;
        if (spawnTimer > 0f)
        {
            return;
        }

        SpawnItem();
        spawnTimer = spawnInterval;
    }

    private void SpawnItem()
    {
        var pool = gameManager.GetCurrentSpawnPool();
        if (pool == null || pool.Count == 0)
        {
            return;
        }

        var data = pool[Random.Range(0, pool.Count)];
        var item = Instantiate(itemTemplate, itemLayer);
        item.gameObject.SetActive(true);
        var y = Random.Range(spawnYRange.x, spawnYRange.y);
        var duration = Mathf.Max(2.2f, moveDuration - gameManager.CurrentSuctionLevel * 0.18f);
        item.Initialize(data, gameManager.CurrentSuctionLevel, new Vector2(spawnX, y), despawnX, duration, gameManager.HandleItemMissed);
        gameManager.RegisterItem(item);
    }
}
