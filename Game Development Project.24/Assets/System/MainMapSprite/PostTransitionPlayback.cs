using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// 回主图后播放排队视频：视频画在独立 Overlay Canvas（sortingOrder 较低），
/// 请把 CanvasTranslation/转场的 Canvas 设为更高的 Sort Order 以压在上面。
/// 使用 RenderTexture，不占用 Camera Near/Far，避免盖住转场 UI。
/// </summary>
public class PostTransitionPlayback : MonoBehaviour
{
    [SerializeField] string mapSceneName = "01_MainMap";

    [Tooltip("视频层 Canvas 的 Sort Order，须小于 CanvasTranslation（例如转场 300，这里填 100）")]
    [SerializeField] int videoOverlayCanvasSortOrder = 100;

    [Tooltip("播片时可选：渐隐主界面 CanvasGroup（与视频渐显同时进行）")]
    [SerializeField] CanvasGroup mapWorldCanvasGroup;

    [Tooltip("VideoPlayer.Prepare 超时（秒）")]
    [SerializeField] float prepareTimeoutSeconds = 12f;

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        if (IsConfiguredMapSceneActive())
            StartCoroutine(CoPlayReturnVideoIfAny());
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    bool IsConfiguredMapSceneActive()
    {
        return SceneManager.GetActiveScene().name == mapSceneName;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name != mapSceneName) return;
        StartCoroutine(CoPlayReturnVideoIfAny());
    }

    IEnumerator CoPlayReturnVideoIfAny()
    {
        yield return null;

        if (GameRunManager.Instance == null) yield break;
        if (!GameRunManager.Instance.TryConsumePendingMapPresentation(out var pres)) yield break;

        VideoClip clip = pres.presentationVideo;
        if (clip == null)
        {
            Debug.LogWarning("[PostTransitionPlayback] 排队展示无 VideoClip，跳过。");
            yield break;
        }

        float fadeIn = Mathf.Max(0f, pres.fadeInDuration);
        float fadeOut = Mathf.Max(0f, pres.fadeOutDuration);

        Debug.Log($"[PostTransitionPlayback] 回图视频（Overlay 下层）：{clip.name}，fadeIn={fadeIn}s fadeOut={fadeOut}s");

        var root = new GameObject("ReturnToMap_VideoOverlay");
        DontDestroyOnLoad(root);

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = videoOverlayCanvasSortOrder;
        canvas.overrideSorting = true;

        var scaler = root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        root.AddComponent<GraphicRaycaster>();

        var videoCg = root.AddComponent<CanvasGroup>();
        videoCg.alpha = 0f;
        videoCg.blocksRaycasts = false;
        videoCg.interactable = false;

        var rawGo = new GameObject("VideoRaw");
        rawGo.transform.SetParent(root.transform, false);
        var raw = rawGo.AddComponent<RawImage>();
        raw.raycastTarget = false;
        raw.color = Color.white;
        var rtf = raw.rectTransform;
        rtf.anchorMin = Vector2.zero;
        rtf.anchorMax = Vector2.one;
        rtf.offsetMin = Vector2.zero;
        rtf.offsetMax = Vector2.zero;

        int w = Mathf.Max(16, Screen.width);
        int h = Mathf.Max(16, Screen.height);
        var rt = new RenderTexture(w, h, 0, RenderTextureFormat.ARGB32);
        rt.name = "ReturnToMap_VideoRT";
        rt.Create();
        raw.texture = rt;

        var host = new GameObject("ReturnToMap_VideoPlayer");
        host.transform.SetParent(root.transform, false);
        var vp = host.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.clip = clip;
        vp.isLooping = false;
        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetTexture = rt;
        vp.timeReference = VideoTimeReference.InternalTime;

        var audio = host.AddComponent<AudioSource>();
        vp.audioOutputMode = VideoAudioOutputMode.AudioSource;
        if (clip.audioTrackCount > 0)
        {
            vp.EnableAudioTrack(0, true);
            vp.SetTargetAudioSource(0, audio);
        }

        float mapStartAlpha = 1f;
        if (mapWorldCanvasGroup != null)
            mapStartAlpha = mapWorldCanvasGroup.alpha;

        vp.Prepare();
        float elapsed = 0f;
        while (!vp.isPrepared && elapsed < prepareTimeoutSeconds)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        if (!vp.isPrepared)
        {
            Debug.LogError($"[PostTransitionPlayback] Prepare 超时，clip={clip.name}");
            CleanupVideoOverlay(vp, rt, root);
            yield break;
        }

        vp.Play();

        if (fadeIn > 0f)
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeIn);
                videoCg.alpha = k;
                if (mapWorldCanvasGroup != null)
                {
                    mapWorldCanvasGroup.alpha = Mathf.Lerp(mapStartAlpha, 0f, k);
                    mapWorldCanvasGroup.blocksRaycasts = false;
                    mapWorldCanvasGroup.interactable = false;
                }

                yield return null;
            }
        }

        videoCg.alpha = 1f;
        if (mapWorldCanvasGroup != null)
        {
            mapWorldCanvasGroup.alpha = 0f;
            mapWorldCanvasGroup.blocksRaycasts = false;
            mapWorldCanvasGroup.interactable = false;
        }

        yield return null;
        yield return new WaitUntil(() => !vp.isPlaying);

        vp.Stop();

        if (fadeOut > 0f)
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / fadeOut);
                videoCg.alpha = 1f - k;
                if (mapWorldCanvasGroup != null)
                    mapWorldCanvasGroup.alpha = Mathf.Lerp(0f, mapStartAlpha, k);
                yield return null;
            }
        }

        videoCg.alpha = 0f;
        if (mapWorldCanvasGroup != null)
        {
            mapWorldCanvasGroup.alpha = mapStartAlpha;
            mapWorldCanvasGroup.blocksRaycasts = true;
            mapWorldCanvasGroup.interactable = true;
        }

        CleanupVideoOverlay(vp, rt, root);
    }

    static void CleanupVideoOverlay(VideoPlayer vp, RenderTexture rt, GameObject root)
    {
        if (vp != null)
            vp.targetTexture = null;
        if (rt != null)
        {
            rt.Release();
            Destroy(rt);
        }

        if (root != null)
            Destroy(root);
    }
}
