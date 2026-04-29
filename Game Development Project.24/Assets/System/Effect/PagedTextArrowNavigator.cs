using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 在同一 TMP_Text 位置分页显示多条文案；左右箭头切换上一页/下一页。
/// 在第一页时左箭头不可点（需在 Button 上用 Color Tint，并把 Disabled Color 调成灰色），最后一页同理禁用右箭头。
/// </summary>
public class PagedTextArrowNavigator : MonoBehaviour
{
    [Header("Display")]
    [SerializeField] private TMP_Text displayText;

    [SerializeField]
    [Tooltip("按顺序：第 0 条为初始显示的「a」，依次为 b、c…")]
    private string[] pages = new string[3];

    [Header("Navigation")]
    [SerializeField] private Button prevButton;
    [SerializeField] private Button nextButton;

    [Header("Audio (optional)")]
    [SerializeField] private AudioClip navigateSFX;

    [SerializeField]
    private AudioSource audioSource;

    private int index;

    private void Awake()
    {
        EnsureAudioSource();

        if (prevButton != null)
            prevButton.onClick.AddListener(GoPrev);

        if (nextButton != null)
            nextButton.onClick.AddListener(GoNext);
    }

    private void OnDestroy()
    {
        if (prevButton != null)
            prevButton.onClick.RemoveListener(GoPrev);

        if (nextButton != null)
            nextButton.onClick.RemoveListener(GoNext);
    }

    private void Start()
    {
        index = 0;
        RefreshView();
    }

    /// <summary>右箭头：下一页。</summary>
    public void GoNext()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (index >= pages.Length - 1)
            return;

        index++;
        PlayNavigateSFX();
        RefreshView();
    }

    /// <summary>左箭头：上一页。</summary>
    public void GoPrev()
    {
        if (pages == null || pages.Length == 0)
            return;

        if (index <= 0)
            return;

        index--;
        PlayNavigateSFX();
        RefreshView();
    }

    /// <summary>运行时跳到某一页（0 为第一页）。</summary>
    public void SetPage(int pageIndex)
    {
        if (pages == null || pages.Length == 0)
            return;

        index = Mathf.Clamp(pageIndex, 0, pages.Length - 1);
        RefreshView();
    }

    private void RefreshView()
    {
        if (displayText != null && pages != null && index >= 0 && index < pages.Length)
            displayText.text = pages[index];

        bool hasPrev = pages != null && pages.Length > 0 && index > 0;
        bool hasNext = pages != null && pages.Length > 0 && index < pages.Length - 1;

        if (prevButton != null)
            prevButton.interactable = hasPrev;

        if (nextButton != null)
            nextButton.interactable = hasNext;
    }

    private void EnsureAudioSource()
    {
        if (audioSource != null)
            return;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = GetComponentInChildren<AudioSource>(true);
    }

    private void PlayNavigateSFX()
    {
        if (navigateSFX == null)
            return;

        if (audioSource != null)
        {
            audioSource.PlayOneShot(navigateSFX);
            return;
        }

        if (Camera.main != null)
            AudioSource.PlayClipAtPoint(navigateSFX, Camera.main.transform.position);
    }
}
