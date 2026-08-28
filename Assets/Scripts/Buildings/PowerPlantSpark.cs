using System.Collections;
using UnityEngine;

/// <summary>
/// 发电厂电火花闪烁动画。
/// 房子（发电厂）在运行过程中会周期性地「电火花闪一下」：
///   - 无灯状态：每隔一段时间播放一次 <see cref="sparkFrames"/> 动画帧，播完恢复原样。
///   - 亮灯状态：同样周期性播放；优先使用 <see cref="litSparkFrames"/>，
///     若未配置（亮灯版帧后续再补），则回退复用无灯帧 <see cref="sparkFrames"/>。
///
/// 本脚本与亮灯逻辑（HouseLightInteraction）解耦：仅负责「播放电火花动画」，
/// 通过 <see cref="HouseLightInteraction.IsLit"/> 判断当前亮灯状态来选帧。
/// </summary>
[RequireComponent(typeof(SpriteRenderer))]
public class PowerPlantSpark : MonoBehaviour
{
    [Header("无灯状态 - 电火花动画帧（按播放顺序）")]
    [Tooltip("房子无灯时的电火花动画帧，按顺序排列（例如 7 帧）。")]
    [SerializeField] private Sprite[] sparkFrames;

    [Header("亮灯状态 - 电火花动画帧（预留）")]
    [Tooltip("房子亮灯时的电火花动画帧。未配置时，亮灯状态会回退复用上面的无灯帧。")]
    [SerializeField] private Sprite[] litSparkFrames;

    [Header("动画节奏")]
    [Tooltip("每帧持续时长（秒）。默认 0.08 秒，约 12.5 帧/秒。")]
    [SerializeField] private float frameDuration = 0.08f;

    [Header("闪烁间隔")]
    [Tooltip("是否使用随机间隔（电火花更自然）。")]
    [SerializeField] private bool useRandomInterval = true;

    [Tooltip("固定间隔（秒），仅当 useRandomInterval 关闭时生效。")]
    [SerializeField] private float sparkInterval = 3f;

    [Tooltip("随机间隔最小值（秒）。")]
    [SerializeField] private float minInterval = 2f;

    [Tooltip("随机间隔最大值（秒）。")]
    [SerializeField] private float maxInterval = 5f;

    private SpriteRenderer spriteRenderer;
    private HouseLightInteraction lightInteraction;
    private Coroutine sparkRoutine;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        lightInteraction = GetComponent<HouseLightInteraction>();
    }

    private void OnEnable()
    {
        // 未配置动画帧时静默不启动，避免空协程报错。
        if (HasFrames(sparkFrames) || HasFrames(litSparkFrames))
        {
            sparkRoutine = StartCoroutine(SparkLoop());
        }
        else
        {
            Debug.LogWarning($"[PowerPlantSpark] {name} 未配置任何电火花动画帧，脚本不播放动画。", this);
        }
    }

    private void OnDisable()
    {
        if (sparkRoutine != null)
        {
            StopCoroutine(sparkRoutine);
            sparkRoutine = null;
        }
    }

    /// <summary>主循环：等待一个间隔 -> 播放一次电火花 -> 往复。</summary>
    private IEnumerator SparkLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(GetNextInterval());
            yield return PlaySparkOnce();
        }
    }

    /// <summary>播放一次完整的电火花动画，结束后恢复播放前的精灵。</summary>
    private IEnumerator PlaySparkOnce()
    {
        if (spriteRenderer == null) yield break;

        Sprite[] frames = ResolveFrames();
        if (!HasFrames(frames)) yield break;

        // 记录播放前的精灵，播完恢复（normal 或 lit 都能正确还原）。
        Sprite restoreSprite = spriteRenderer.sprite;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] == null) continue;
            spriteRenderer.sprite = frames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        spriteRenderer.sprite = restoreSprite;
    }

    /// <summary>根据当前亮灯状态选择要播放的动画帧集。</summary>
    private Sprite[] ResolveFrames()
    {
        bool isLit = lightInteraction != null && lightInteraction.IsLit;

        // 亮灯状态优先用亮灯帧；未配置则回退无灯帧。
        if (isLit && HasFrames(litSparkFrames))
        {
            return litSparkFrames;
        }

        return sparkFrames;
    }

    /// <summary>计算下一次闪烁的等待时长。</summary>
    private float GetNextInterval()
    {
        if (useRandomInterval)
        {
            return Random.Range(minInterval, maxInterval);
        }
        return sparkInterval;
    }

    /// <summary>判断数组是否包含有效（非空）帧。</summary>
    private static bool HasFrames(Sprite[] frames)
    {
        if (frames == null || frames.Length == 0) return false;
        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null) return true;
        }
        return false;
    }
}
