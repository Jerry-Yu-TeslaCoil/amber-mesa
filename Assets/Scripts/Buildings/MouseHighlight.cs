using UnityEngine;

public class HouseLightInteraction : MonoBehaviour
{
    [Header("外观效果")]
    [SerializeField] private Sprite normalSprite;
    [SerializeField] private Sprite litSprite;
    [SerializeField] private Color highlightColor = new Color(1f, 1f, 0.8f, 1f);
    [SerializeField] private float scaleMultiplier = 1.05f;

    [Header("音效（随机播放）")]
    [SerializeField] private AudioClip[] lightOnSounds;   // 点灯音效数组，拖入多个
    [SerializeField] private AudioClip[] lightOffSounds;  // 熄灯音效数组，拖入多个
    [Range(0f, 1f)]
    [SerializeField] private float soundVolume = 0.7f;

    private SpriteRenderer spriteRenderer;
    private AudioSource audioSource;
    private Vector3 originalScale;
    private Color originalColor;
    private bool isHighlighted = false;

    /// <summary>当前是否处于亮灯（高亮）状态，供其他脚本（如电火花动画）读取。</summary>
    public bool IsLit => isHighlighted;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            originalColor = spriteRenderer.color;
            if (normalSprite == null)
            {
                normalSprite = spriteRenderer.sprite;
            }
        }
        originalScale = transform.localScale;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.volume = soundVolume;
    }

    void OnMouseEnter()
    {
        if (isHighlighted) return;
        isHighlighted = true;

        if (spriteRenderer != null && litSprite != null)
        {
            spriteRenderer.sprite = litSprite;
        }

        transform.localScale = originalScale * scaleMultiplier;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = highlightColor;
        }

        PlayRandomSound(lightOnSounds);
    }

    void OnMouseExit()
    {
        if (!isHighlighted) return;
        isHighlighted = false;

        if (spriteRenderer != null && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
        }

        transform.localScale = originalScale;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = originalColor;
        }

        PlayRandomSound(lightOffSounds);
    }

    private void PlayRandomSound(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0) return;
        if (audioSource == null) return;

        // 过滤掉空的条目
        System.Collections.Generic.List<AudioClip> validClips = new System.Collections.Generic.List<AudioClip>();
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                validClips.Add(clip);
            }
        }

        if (validClips.Count == 0) return;

        int randomIndex = Random.Range(0, validClips.Count);
        AudioClip selectedClip = validClips[randomIndex];

        if (audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        audioSource.clip = selectedClip;
        audioSource.Play();
    }

    public void ToggleLight(bool turnOn)
    {
        if (spriteRenderer == null) return;

        if (turnOn && litSprite != null)
        {
            spriteRenderer.sprite = litSprite;
            PlayRandomSound(lightOnSounds);
        }
        else if (!turnOn && normalSprite != null)
        {
            spriteRenderer.sprite = normalSprite;
            PlayRandomSound(lightOffSounds);
        }
    }
}