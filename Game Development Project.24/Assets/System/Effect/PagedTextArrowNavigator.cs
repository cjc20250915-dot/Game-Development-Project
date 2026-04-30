using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 分页切换：每一页对应一个 UI 根物体（下面可任意嵌套复杂布局）。
/// 同一时间只激活当前页，其余页 SetActive(false)。
/// 第一页禁用左箭头，最后一页禁用右箭头（Button 建议用 Color Tint + 灰色 Disabled Color）。
/// </summary>
public class PagedTextArrowNavigator : MonoBehaviour
{
    [Header("Pages")]
    [SerializeField]
    [Tooltip("按顺序：第 0 页默认显示；每页拖一个面板根物体（同级叠放、坐标对齐更方便）")]
    private GameObject[] pageRoots;

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

        ApplyPageVisibilityImmediate();
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
        if (!HasAnyPage())
            return;

        if (index >= pageRoots.Length - 1)
            return;

        index++;
        PlayNavigateSFX();
        RefreshView();
    }

    /// <summary>左箭头：上一页。</summary>
    public void GoPrev()
    {
        if (!HasAnyPage())
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
        if (!HasAnyPage())
            return;

        index = Mathf.Clamp(pageIndex, 0, pageRoots.Length - 1);
        RefreshView();
    }

    /// <summary>当前页索引（只读）。</summary>
    public int CurrentPageIndex => index;

    private bool HasAnyPage()
    {
        return pageRoots != null && pageRoots.Length > 0;
    }

    private void RefreshView()
    {
        ApplyPageVisibilityImmediate();

        bool hasPrev = HasAnyPage() && index > 0;
        bool hasNext = HasAnyPage() && index < pageRoots.Length - 1;

        if (prevButton != null)
            prevButton.interactable = hasPrev;

        if (nextButton != null)
            nextButton.interactable = hasNext;
    }

    /// <summary>
    /// 根据 index 只激活一页；null 槽位会被跳过显示（始终保持 inactive）。
    /// </summary>
    private void ApplyPageVisibilityImmediate()
    {
        if (pageRoots == null)
            return;

        for (int i = 0; i < pageRoots.Length; i++)
        {
            GameObject root = pageRoots[i];
            if (root == null)
                continue;

            root.SetActive(i == index);
        }
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
