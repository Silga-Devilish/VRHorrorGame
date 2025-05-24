using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    public static BackgroundMusicController Instance;
    
    private AudioSource audioSource;

    private void Awake()
    {
        // 单例模式确保音乐不会重复
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 场景切换时不销毁
            audioSource = GetComponent<AudioSource>();
            PlayMusic();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void PlayMusic()
    {
        if (!audioSource.isPlaying)
        {
            audioSource.Play();
        }
    }

    public void StopMusic()
    {
        audioSource.Stop();
    }

    public void ChangeMusic(AudioClip newClip)
    {
        audioSource.Stop();
        audioSource.clip = newClip;
        audioSource.Play();
    }
}
