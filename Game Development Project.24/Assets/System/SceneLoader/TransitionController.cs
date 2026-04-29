using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using System.Collections;

public class TransitionController : MonoBehaviour
{
    public Material transitionMat;
    public float transitionTime = 1f;
    public float minWaitTime = 1.5f;
    public float destroyDelayAfterLoad = 2f;

    [Header("Intro Video Before First Map")]
    public VideoClip introVideoBeforeFirstMap;
    public string introVideoTargetScene = "01_MainMap";
    public bool playIntroOnlyOnce = true;
    public Color introBackgroundColor = Color.black;
    public float introVideoPrepareTimeout = 10f;
    public int transitionCanvasSortingOrder = 32766;

    private bool isTransitioning = false;
    private bool introVideoPlayed = false;
    private Canvas transitionCanvas;

    void Awake()
    {
        // 强制恢复鼠标与时间状态，避免从暂停菜单切场景后鼠标失效
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        EnsureTransitionCanvasOnTop();
    }

    public void LoadSceneWithTransition(string sceneName)
    {
        if (!isTransitioning)
        {
            StartCoroutine(Transition(sceneName));
        }
    }

    IEnumerator Transition(string sceneName)
    {
        isTransitioning = true;

        // 禁用鼠标（转场开始）
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;

        yield return StartCoroutine(AnimateCircle(-0.2f, 2f));

        yield return new WaitForSecondsRealtime(minWaitTime);

        if (ShouldPlayIntroVideo(sceneName))
        {
            introVideoPlayed = true;
            yield return StartCoroutine(PlayIntroVideoFullscreen());
        }

        ShowClosedTransitionFrame();
        yield return new WaitForEndOfFrame();

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
        while (!op.isDone)
            yield return null;

        ShowClosedTransitionFrame();
        yield return new WaitForEndOfFrame();

        yield return StartCoroutine(AnimateCircle(2f, -0.2f));

        // 转场结束时必须恢复：否则目标场景若没有会重置 Cursor 的对象（例如 Indoor），鼠标会一直锁定/隐藏
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        isTransitioning = false;

        Destroy(gameObject, destroyDelayAfterLoad);
    }

    private bool ShouldPlayIntroVideo(string sceneName)
    {
        if (introVideoBeforeFirstMap == null) return false;
        if (playIntroOnlyOnce && introVideoPlayed) return false;
        return sceneName == introVideoTargetScene;
    }

    private IEnumerator PlayIntroVideoFullscreen()
    {
        bool pausedBgm = false;
        if (bgmManager.Instance != null)
            pausedBgm = bgmManager.Instance.PauseBGM();

        GameObject videoRoot = new GameObject("IntroVideoOverlay");
        DontDestroyOnLoad(videoRoot);

        Canvas canvas = videoRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;

        CanvasScaler scaler = videoRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        videoRoot.AddComponent<GraphicRaycaster>();

        GameObject backgroundObject = new GameObject("Background");
        backgroundObject.transform.SetParent(videoRoot.transform, false);
        Image background = backgroundObject.AddComponent<Image>();
        background.color = introBackgroundColor;
        StretchToParent(background.rectTransform);

        GameObject rawImageObject = new GameObject("Video");
        rawImageObject.transform.SetParent(videoRoot.transform, false);
        RawImage rawImage = rawImageObject.AddComponent<RawImage>();
        rawImage.color = Color.white;
        StretchToParent(rawImage.rectTransform);

        int width = Mathf.Max(16, Screen.width);
        int height = Mathf.Max(16, Screen.height);
        RenderTexture renderTexture = new RenderTexture(width, height, 0);
        renderTexture.Create();
        rawImage.texture = renderTexture;

        VideoPlayer videoPlayer = videoRoot.AddComponent<VideoPlayer>();
        AudioSource audioSource = videoRoot.AddComponent<AudioSource>();

        bool finished = false;
        bool failed = false;

        videoPlayer.playOnAwake = false;
        videoPlayer.clip = introVideoBeforeFirstMap;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = renderTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.isLooping = false;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.controlledAudioTrackCount = 1;
        videoPlayer.EnableAudioTrack(0, true);
        videoPlayer.SetTargetAudioSource(0, audioSource);
        videoPlayer.loopPointReached += _ => finished = true;
        videoPlayer.errorReceived += (_, message) =>
        {
            failed = true;
            Debug.LogWarning("[TransitionController] Intro video failed: " + message);
        };

        videoPlayer.Prepare();
        float prepareDeadline = Time.realtimeSinceStartup + Mathf.Max(0.1f, introVideoPrepareTimeout);
        while (!videoPlayer.isPrepared && !failed && Time.realtimeSinceStartup < prepareDeadline)
            yield return null;

        if (videoPlayer.isPrepared && !failed)
        {
            videoPlayer.Play();

            float playbackDeadline = Time.realtimeSinceStartup + Mathf.Max(1f, (float)videoPlayer.length + 1f);
            while (!finished && !failed && Time.realtimeSinceStartup < playbackDeadline)
                yield return null;
        }

        videoPlayer.Stop();
        rawImage.texture = null;
        renderTexture.Release();
        Destroy(renderTexture);
        videoRoot.SetActive(false);
        Destroy(videoRoot);

        if (pausedBgm && bgmManager.Instance != null)
            bgmManager.Instance.ResumeBGM();
    }

    private void EnsureTransitionCanvasOnTop()
    {
        if (transitionCanvas == null)
            transitionCanvas = GetComponent<Canvas>();

        if (transitionCanvas == null)
            return;

        transitionCanvas.enabled = true;
        transitionCanvas.overrideSorting = true;
        transitionCanvas.sortingOrder = transitionCanvasSortingOrder;
    }

    private void SetTransitionFullyClosed()
    {
        if (transitionMat != null)
            transitionMat.SetFloat("_CircleRate", 2f);
    }

    private void ShowClosedTransitionFrame()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        EnsureTransitionCanvasOnTop();
        SetTransitionFullyClosed();
        Canvas.ForceUpdateCanvases();
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    IEnumerator AnimateCircle(float from, float to)
    {
        EnsureTransitionCanvasOnTop();

        float t = 0f;

        while (t < transitionTime)
        {
            t += Time.unscaledDeltaTime;
            float value = Mathf.Lerp(from, to, t / transitionTime);
            transitionMat.SetFloat("_CircleRate", value);
            yield return null;
        }

        transitionMat.SetFloat("_CircleRate", to);
    }
}
