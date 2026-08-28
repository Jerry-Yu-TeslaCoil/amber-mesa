using UnityEngine;
using UnityEngine.Tilemaps;

public class SimpleBuildingPlacer : MonoBehaviour
{
    [Header("放置设置")]
    [SerializeField] private Tilemap referenceTilemap;
    [SerializeField] private Tilemap previewTilemap;
    [SerializeField] private TileBase previewTile;
    [SerializeField] private GameObject buildingPrefab;
    [SerializeField] private Vector3Int buildingSize = new Vector3Int(3, 3, 1);

    [Header("地形占位")]
    [Tooltip("建筑占位基底要画到的地形 Tilemap（与悬崖同一层，统一作为不可通过区域）。")]
    [SerializeField] private Tilemap terrainTilemap;
    [Tooltip("建筑占位基底 tile（放置后画到地形 Tilemap 上，表示该区域不可通过）。")]
    [SerializeField] private TileBase footprintTile;

    [Header("放置判定")]
    [Tooltip("不可放置时的预览 tile（红色）。判定范围内存在非触发器碰撞体时显示。")]
    [SerializeField] private TileBase invalidPreviewTile;
    [Tooltip("放置被拒绝时播放的禁止音效（可后续补充）。")]
    [SerializeField] private AudioClip denySound;

    [Header("鼠标")]
    [SerializeField] private Camera mainCamera;

    private Vector3Int currentGridPosition;
    private bool isPlacing = false;

    [Header("建造音效")]
    [SerializeField] private AudioClip buildSound;
    [Range(0f, 1f)]
    [SerializeField] private float buildSoundVolume = 1f;

    private AudioSource buildAudioSource;

    void Start()
    {
        Debug.Log("BuildingPlacer 已启动");

        if (mainCamera == null)
            mainCamera = Camera.main;

        if (previewTilemap != null)
            previewTilemap.ClearAllTiles();

        // 检查关键引用
        if (referenceTilemap == null)
            Debug.LogError("referenceTilemap 未设置");
        if (previewTilemap == null)
            Debug.LogError("previewTilemap 未设置");
        if (previewTile == null)
            Debug.LogError("previewTile 未设置");
        if (buildingPrefab == null)
            Debug.LogError("buildingPrefab 未设置");
        if (terrainTilemap == null)
            Debug.LogError("terrainTilemap 未设置");
        if (footprintTile == null)
            Debug.LogError("footprintTile 未设置");

        // 初始化建造音效的 AudioSource（2D 音效，无空间衰减）
        buildAudioSource = GetComponent<AudioSource>();
        if (buildAudioSource == null)
        {
            buildAudioSource = gameObject.AddComponent<AudioSource>();
        }
        buildAudioSource.playOnAwake = false;
        buildAudioSource.spatialBlend = 0f;
        buildAudioSource.volume = buildSoundVolume;
    }

    void Update()
    {
        // 按B键切换放置模式
        if (Input.GetKeyDown(KeyCode.B))
        {
            Debug.Log("B键被按下，当前isPlacing = " + isPlacing);
            if (isPlacing)
            {
                CancelPlacement();
            }
            else
            {
                StartPlacement();
            }
            return;
        }

        if (!isPlacing) return;

        // 鼠标位置转换
        Vector3 mouseWorldPos = mainCamera.ScreenToWorldPoint(Input.mousePosition);
        Vector3Int gridPos = referenceTilemap.WorldToCell(mouseWorldPos);

        if (gridPos != currentGridPosition)
        {
            currentGridPosition = gridPos;
            UpdatePreview(gridPos);
        }

        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("左键点击，尝试放置");
            PlaceBuilding();
        }

        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Log("右键或ESC，取消放置");
            CancelPlacement();
        }
    }

    // 鼠标指向的格子作为 3x3 建筑的中心，返回建筑占据区域的左下角格子
    private Vector3Int GetOriginFromCenter(Vector3Int center)
    {
        int originX = center.x - buildingSize.x / 2;
        int originY = center.y - buildingSize.y / 2;
        return new Vector3Int(originX, originY, 0);
    }

    private void UpdatePreview(Vector3Int centerPos)
    {
        previewTilemap.ClearAllTiles();

        if (previewTile == null) return;

        Vector3Int origin = GetOriginFromCenter(centerPos);

        // 逐格子判定：每个格子独立检测碰撞体，冲突的格子画红色，其余画绿色。
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                Vector3Int cell = origin + new Vector3Int(x, y, 0);

                TileBase tile = IsCellBlocked(cell) ? invalidPreviewTile : previewTile;
                if (tile == null)
                {
                    // 红色 tile 未配置时回退绿色，避免预览直接消失。
                    tile = previewTile;
                }

                previewTilemap.SetTile(cell, tile);
            }
        }
    }

    private void PlaceBuilding()
    {
        if (buildingPrefab == null)
        {
            Debug.LogWarning("未设置建筑预制件");
            return;
        }

        Vector3Int origin = GetOriginFromCenter(currentGridPosition);

        // 占位区域内任意格子存在非触发器碰撞体（悬崖 / 已建建筑等）时，拒绝放置并播放禁止音。
        if (IsAnyCellBlocked(origin))
        {
            PlayDenySound();
            return;
        }

        // 在地形 Tilemap 上绘制建筑占位基底，标记该区域不可通过。
        PaintFootprint(origin);

        Vector3 worldPos = referenceTilemap.CellToWorld(origin);
        Vector3 centerOffset = new Vector3(buildingSize.x * 0.5f, buildingSize.y * 0.5f, 0);
        worldPos += centerOffset;

        GameObject newBuilding = Instantiate(buildingPrefab, worldPos, Quaternion.identity);
        Debug.Log("建筑已放置，位置: " + worldPos);

        PlayBuildSound();

        previewTilemap.ClearAllTiles();
        isPlacing = false;
    }

    /// <summary>
    /// 在建筑占据区域（<see cref="buildingSize"/>）的地形 Tilemap 上绘制占位基底，
    /// 用于将该区域标记为不可通过。
    /// </summary>
    private void PaintFootprint(Vector3Int origin)
    {
        if (terrainTilemap == null || footprintTile == null) return;

        PaintRect(terrainTilemap, footprintTile, origin, buildingSize);
    }

    /// <summary>
    /// 判断建筑占据区域（以 <paramref name="origin"/> 为左下角、大小 <see cref="buildingSize"/>）
    /// 内是否存在任意一个被占用的格子。
    /// </summary>
    private bool IsAnyCellBlocked(Vector3Int origin)
    {
        for (int x = 0; x < buildingSize.x; x++)
        {
            for (int y = 0; y < buildingSize.y; y++)
            {
                if (IsCellBlocked(origin + new Vector3Int(x, y, 0)))
                {
                    return true;
                }
            }
        }
        return false;
    }

    /// <summary>
    /// 判断单个格子是否不可放置。
    /// 优先用地形 Tilemap 的 tile 数据判断（悬崖、建筑基底都在上面），
    /// 避免 tile 碰撞体因 sprite pivot 偏移导致的物理检测误差；
    /// 再用物理检测兜底非 tile 的碰撞体（如门洞等手动 Collider），并忽略触发器。
    /// </summary>
    private bool IsCellBlocked(Vector3Int cell)
    {
        // 1. tile 占用检测：悬崖、建筑基底都画在地形 Tilemap 上，直接查 tile 最准确。
        if (terrainTilemap != null && terrainTilemap.HasTile(cell))
        {
            return true;
        }

        // 2. 物理检测兜底：覆盖非 tile 的碰撞体（门洞等），忽略触发器。
        Vector2 center = (Vector2)referenceTilemap.GetCellCenterWorld(cell);
        Vector2 size = (Vector2)referenceTilemap.cellSize;

        Collider2D[] colliders = Physics2D.OverlapBoxAll(center, size, 0f);
        foreach (Collider2D collider in colliders)
        {
            if (!collider.isTrigger)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>播放放置被拒绝的禁止音效。</summary>
    private void PlayDenySound()
    {
        if (denySound == null || buildAudioSource == null) return;
        buildAudioSource.clip = denySound;
        buildAudioSource.Play();
    }

    /// <summary>
    /// 在指定 Tilemap 上，以 <paramref name="origin"/> 为左下角，绘制一个
    /// <paramref name="size"/> 大小的矩形区域，全部填充 <paramref name="tile"/>。
    /// </summary>
    private static void PaintRect(Tilemap tilemap, TileBase tile, Vector3Int origin, Vector3Int size)
    {
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                tilemap.SetTile(origin + new Vector3Int(x, y, 0), tile);
            }
        }
    }

    /// <summary>播放建造完成音效。</summary>
    private void PlayBuildSound()
    {
        if (buildSound == null || buildAudioSource == null) return;
        buildAudioSource.clip = buildSound;
        buildAudioSource.Play();
    }

    private void CancelPlacement()
    {
        previewTilemap.ClearAllTiles();
        isPlacing = false;
        Debug.Log("已取消放置模式");
    }

    public void StartPlacement()
    {
        if (buildingPrefab == null)
        {
            Debug.LogError("未设置建筑预制件");
            return;
        }
        isPlacing = true;
        currentGridPosition = Vector3Int.zero;
        Debug.Log("进入放置模式");
    }
}
