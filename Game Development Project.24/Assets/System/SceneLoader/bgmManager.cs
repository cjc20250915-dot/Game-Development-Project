using UnityEngine;

public class bgmManager : MonoBehaviour
{
    public static bgmManager Instance;

    private AudioSource audioSource;

    void Awake()
    {
        if (Instance != null)
        {
            ApplyThisObjectBgmToExistingInstance();
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
    }

    public void PlayBGM(AudioClip clip)
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || clip == null) return;
        if (audioSource.clip == clip && audioSource.isPlaying) return;

        audioSource.clip = clip;
        audioSource.Play();
    }

    public void StopBGM()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null) return;
        audioSource.Stop();
    }

    public bool PauseBGM()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || !audioSource.isPlaying) return false;

        audioSource.Pause();
        return true;
    }

    public void ResumeBGM()
    {
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();

        if (audioSource == null || audioSource.isPlaying) return;

        audioSource.UnPause();
    }

    private void ApplyThisObjectBgmToExistingInstance()
    {
        AudioSource duplicateSource = GetComponent<AudioSource>();
        if (duplicateSource == null || duplicateSource.clip == null) return;

        AudioSource existingSource = Instance.GetComponent<AudioSource>();
        if (existingSource == null) return;

        existingSource.volume = duplicateSource.volume;
        existingSource.loop = duplicateSource.loop;
        existingSource.pitch = duplicateSource.pitch;
        existingSource.spatialBlend = duplicateSource.spatialBlend;

        Instance.PlayBGM(duplicateSource.clip);
    }
}
