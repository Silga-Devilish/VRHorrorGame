using UnityEngine;
using TMPro;

public class FuseBox : MonoBehaviour
{
    [Header("设置")]
    [SerializeField] private LightSwitchController[] connectedSwitches;
    [SerializeField] private GameObject repairedVisual;
    
    [Header("UI提示")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string needFuseText = "需要保险丝 (长按E修理)";
    [SerializeField] private string repairedText = "已修复";
    
    [Header("修理音效")]
    [SerializeField] private AudioClip repairingSound;
    [SerializeField] private float repairingVolume = 0.7f;
    [SerializeField] private float repairingSoundDelay = 0.5f;
    
    [Header("怪物音效")] 
    [SerializeField] private AudioClip monsterRoarSound;
    [SerializeField] private float roarVolume = 1f;
    [SerializeField] private float roarDelayAfterRepair = 1f;
    
    public bool IsRepaired { get; private set; }

    private AudioSource audioSource;
    private bool hasPlayedRoar;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D音效
        }
    }

    private void Start()
    {
        UpdateVisuals();
        ShowPrompt(false);
        hasPlayedRoar = false;
    }

    public void Repair()
    {
        IsRepaired = true;
        
        // 播放修理完成音效
        if (audioSource != null)
        {
            audioSource.Stop(); // 停止修理中音效
            audioSource.PlayOneShot(repairingSound, repairingVolume);
            
            // 延迟播放怪物咆哮
            if (monsterRoarSound != null && !hasPlayedRoar)
            {
                Invoke("PlayMonsterRoar", roarDelayAfterRepair);
                hasPlayedRoar = true;
            }
        }

        foreach (var switchCtrl in connectedSwitches)
        {
            if (switchCtrl != null) switchCtrl.RepairFuse();
        }
        UpdateVisuals();
    }

    private void PlayMonsterRoar()
    {
        if (monsterRoarSound != null)
        {
            audioSource.PlayOneShot(monsterRoarSound, roarVolume);
        }
    }

    public void StartRepairingSound()
    {
        if (!IsRepaired && repairingSound != null)
        {
            Invoke("PlayRepairingSound", repairingSoundDelay);
        }
    }

    public void StopRepairingSound()
    {
        CancelInvoke("PlayRepairingSound");
        if (audioSource != null && audioSource.isPlaying && 
            audioSource.clip == repairingSound)
        {
            audioSource.Stop();
        }
    }

    private void PlayRepairingSound()
    {
        audioSource.loop = true;
        audioSource.clip = repairingSound;
        audioSource.volume = repairingVolume;
        audioSource.Play();
    }

    public void ShowPrompt(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
            if (show && promptText != null)
            {
                promptText.text = IsRepaired ? repairedText : needFuseText;
            }
        }
    }

    private void UpdateVisuals()
    {
        if (repairedVisual != null)
        {
            repairedVisual.SetActive(IsRepaired);
        }
    }
}
