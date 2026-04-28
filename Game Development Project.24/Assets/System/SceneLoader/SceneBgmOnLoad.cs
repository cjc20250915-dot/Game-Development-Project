using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在任意场景中的物体上，加载该场景后自动切换 BGM。
/// 全局只需一个 <see cref="bgmManager"/>（首个场景 + DontDestroyOnLoad）；不要在每个场景都放 bgmManager（会被销毁且无独立播放）。
/// </summary>
public class SceneBgmOnLoad : MonoBehaviour
{
    [Header("曲目")]
    [SerializeField] private AudioClip bgm;
    [Tooltip("若勾选，进入本场景后会应用下方参数并播放 bgm")]
    [SerializeField] private bool playWhenSceneLoads = true;

    [Header("Audio（与 AudioSource 一致，进场景时应用到全局 BGM）")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 1f;
    [SerializeField] private bool loop = true;
    [SerializeField] private float pitch = 1f;
    [Range(0f, 1f)]
    [Tooltip("0 = 纯 2D，1 = 纯 3D")]
    [SerializeField] private float spatialBlend = 0f;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void RegisterSceneLoaded()
    {
        SceneManager.sceneLoaded -= OnAnySceneLoaded;
        SceneManager.sceneLoaded += OnAnySceneLoaded;
    }

    /// <summary>
    /// 不依赖每个实例 OnEnable 订阅，避免连续切场景时漏订阅或 scene 比较不一致导致第三场景不切换。
    /// </summary>
    private static void OnAnySceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!scene.IsValid() || !scene.isLoaded) return;

        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            SceneBgmOnLoad[] loaders = roots[i].GetComponentsInChildren<SceneBgmOnLoad>(true);
            for (int j = 0; j < loaders.Length; j++)
            {
                SceneBgmOnLoad loader = loaders[j];
                if (loader == null || !loader.isActiveAndEnabled) continue;
                loader.TryApplyBgm();
            }
        }
    }

    private void Start()
    {
        // 动态实例化到已加载场景时不会再触发 sceneLoaded，用 Start 补一次
        TryApplyBgm();
    }

    private void TryApplyBgm()
    {
        if (!playWhenSceneLoads || bgm == null) return;
        ApplyBgm();
    }

    /// <summary>可在编辑器按钮或其它逻辑里手动调用</summary>
    public void ApplyBgm()
    {
        if (bgm == null) return;
        if (bgmManager.Instance == null)
        {
            Debug.LogWarning($"[SceneBgmOnLoad] {SceneManager.GetActiveScene().name}: 未找到 bgmManager，无法播放 BGM。请在首个加载的场景放置带 bgmManager 的物体。");
            return;
        }

        AudioSource src = bgmManager.Instance.GetComponent<AudioSource>();
        if (src == null)
        {
            Debug.LogWarning($"[SceneBgmOnLoad] {SceneManager.GetActiveScene().name}: bgmManager 上未找到 AudioSource。");
            return;
        }

        src.volume = volume;
        src.loop = loop;
        src.pitch = pitch;
        src.spatialBlend = spatialBlend;

        bgmManager.Instance.PlayBGM(bgm);
    }
}
