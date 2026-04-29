using System.Collections;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// End 场景：按胜负选片 → RenderTexture 播放 → 结束后显示感谢界面。
/// </summary>
[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public class EndSceneVideoController : MonoBehaviour
{
    [SerializeField] VideoPlayer videoPlayer;

    [Header("三类结局视频（Inspector 拖入 VideoClip）")]
    [SerializeField] VideoClip videoWhenWinCountEquals10;
    [SerializeField] VideoClip videoWhenWinGreaterLoseButWinLessThan10;
    [SerializeField] VideoClip videoWhenLoseGreaterWin;

    EndThankYouPanel _thankYouUi;

    void Awake()
    {
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        _thankYouUi = FindFirstObjectByType<EndThankYouPanel>();
        if (_thankYouUi == null)
            Debug.LogWarning("[EndSceneVideoController] 未找到 EndThankYouPanel（场景应包含 EndThankYouCanvas 预制体）。", this);
    }

    void Start()
    {
        VideoClip clip = SelectClip();
        if (clip == null)
        {
            Debug.LogWarning("[EndSceneVideoController] 当前分支未配置 VideoClip，直接显示感谢界面。", this);
            _thankYouUi?.ShowThankYou();
            return;
        }

        StartCoroutine(PlayAndThenThankYou(clip));
    }

    VideoClip SelectClip()
    {
        int w = 0;
        int l = 0;

        if (GameRunManager.Instance != null)
        {
            w = GameRunManager.Instance.winCount;
            l = GameRunManager.Instance.loseCount;
        }
        else
            Debug.LogWarning("[EndSceneVideoController] GameRunManager.Instance 为空，胜负按 0:0 处理（建议从带有 GameRunManager 的流程进入 End）。", this);

        if (w == 10)
            return videoWhenWinCountEquals10;

        if (l > w)
            return videoWhenLoseGreaterWin;

        if (w > l && w < 10)
            return videoWhenWinGreaterLoseButWinLessThan10;

        if (w > l)
            return videoWhenWinCountEquals10;

        return videoWhenWinGreaterLoseButWinLessThan10;
    }

    IEnumerator PlayAndThenThankYou(VideoClip clip)
    {
        var audio = GetComponent<AudioSource>();

        ApplyStableVideoSettings();
        _thankYouUi?.ConfigureVideoOutput(videoPlayer);

        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = false;
        videoPlayer.clip = clip;

        if (clip.audioTrackCount > 0)
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
            videoPlayer.EnableAudioTrack(0, true);
            videoPlayer.SetTargetAudioSource(0, audio);
        }
        else
        {
            videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        }

        bool finished = false;
        void OnLoopPoint(VideoPlayer vp) => finished = true;
        void OnError(VideoPlayer vp, string message)
        {
            Debug.LogError($"[EndSceneVideoController] 视频错误：{message}", this);
            finished = true;
        }

        videoPlayer.loopPointReached += OnLoopPoint;
        videoPlayer.errorReceived += OnError;

        try
        {
            videoPlayer.Prepare();

            const float prepareTimeout = 45f;
            float wait = 0f;
            while (!videoPlayer.isPrepared && wait < prepareTimeout)
            {
                wait += Time.unscaledDeltaTime;
                yield return null;
            }

            if (!videoPlayer.isPrepared)
            {
                Debug.LogError($"[EndSceneVideoController] 视频 Prepare 超时：{clip.name}", this);
            }
            else
            {
                videoPlayer.Play();

                float playStart = Time.unscaledTime;
                while (!videoPlayer.isPlaying && Time.unscaledTime - playStart < 5f)
                    yield return null;

                yield return new WaitUntil(() => finished);
            }
        }
        finally
        {
            videoPlayer.loopPointReached -= OnLoopPoint;
            videoPlayer.errorReceived -= OnError;
            if (videoPlayer.isPlaying)
                videoPlayer.Stop();
        }

        _thankYouUi?.ShowThankYou();
    }

    void ApplyStableVideoSettings()
    {
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = false;
        videoPlayer.timeReference = VideoTimeReference.InternalTime;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
    }
}
