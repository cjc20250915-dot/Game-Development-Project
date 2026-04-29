using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// End 场景感谢界面与全屏视频 RawImage；由 <see cref="EndSceneVideoController"/> 调用。
/// </summary>
public class EndThankYouPanel : MonoBehaviour
{
    [SerializeField] GameObject thankYouRoot;
    [SerializeField] RawImage videoRawImage;
    [SerializeField] RenderTexture videoRenderTexture;

    void Awake()
    {
        if (thankYouRoot != null)
            thankYouRoot.SetActive(false);
    }

    public void ConfigureVideoOutput(VideoPlayer vp)
    {
        if (vp == null || videoRawImage == null || videoRenderTexture == null)
            return;

        vp.renderMode = VideoRenderMode.RenderTexture;
        vp.targetCamera = null;
        vp.targetTexture = videoRenderTexture;
        videoRawImage.texture = videoRenderTexture;
    }

    public void ShowThankYou()
    {
        if (thankYouRoot != null)
            thankYouRoot.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void OnReturnToMainMenu()
    {
        if (GameRunManager.Instance != null)
            GameRunManager.Instance.AbortUnfinishedBattleIfNeeded();

        SceneManager.LoadScene("00_MainMenu");
    }
}
