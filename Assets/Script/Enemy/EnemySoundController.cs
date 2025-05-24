using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class EnemySoundController : MonoBehaviour
{
        [Header("电磁音效设置")]
    [SerializeField] private AudioClip electromagneticLoop;
    [SerializeField] private float minElectroVolume = 0.1f;
    [SerializeField] private float maxElectroVolume = 0.5f;
    [SerializeField] private float minElectroPitch = 0.8f;
    [SerializeField] private float maxElectroPitch = 1.2f;
    [SerializeField] private float electroDetectionRange = 15f;

    [Header("追逐音效设置")] 
    [SerializeField] private AudioClip chaseLoop;
    [SerializeField] private float minChasePitch = 1.0f;
    [SerializeField] private float maxChasePitch = 1.5f;
    [SerializeField] private float chaseVolume = 0.6f;

    [Header("侦测提示音设置")]
    [SerializeField] private AudioClip[] detectionVoices;
    [SerializeField] private float detectionVoiceRange = 20f;
    [SerializeField] private float minVoiceInterval = 3f;
    [SerializeField] private float voiceVolume = 0.7f;
    [SerializeField] private float voiceSpatialBlend = 0.3f;

    private AudioSource electroSource;
    private AudioSource chaseSource;
    private AudioSource voiceSource;
    private Transform player;
    private EnemyAIPatrol enemyAI;
    private float lastVoiceTime;
    private Vector3 lastSoundPosition;

    private void Awake()
    {
        // 初始化所有音频源
        electroSource = gameObject.AddComponent<AudioSource>();
        chaseSource = gameObject.AddComponent<AudioSource>();
        voiceSource = gameObject.AddComponent<AudioSource>();

        // 电磁音效设置
        electroSource.clip = electromagneticLoop;
        electroSource.loop = true;
        electroSource.spatialBlend = 1f;
        electroSource.playOnAwake = true;
        electroSource.Play();

        // 追逐音效设置
        chaseSource.clip = chaseLoop;
        chaseSource.loop = true;
        chaseSource.spatialBlend = 1f;
        chaseSource.volume = 0f;
        chaseSource.playOnAwake = true;
        chaseSource.Play();

        // 语音提示设置
        voiceSource.spatialBlend = voiceSpatialBlend;
        voiceSource.volume = voiceVolume;
        voiceSource.maxDistance = detectionVoiceRange;
        
        // 获取父物体的EnemyAIPatrol组件
        enemyAI = GetComponentInParent<EnemyAIPatrol>();
        if (enemyAI == null)
        {
            Debug.LogError("在父物体上找不到EnemyAIPatrol组件！");
        }

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

     private void Update()
    {
        if (player == null || enemyAI == null) return;

        UpdateElectroSound();
        UpdateChaseSound();
        CheckDetectionVoice();
    }

    private void UpdateElectroSound()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float volume = Mathf.Lerp(maxElectroVolume, minElectroVolume, 
                                Mathf.Clamp01(distanceToPlayer / electroDetectionRange));
        float pitch = Mathf.Lerp(maxElectroPitch, minElectroPitch, 
                               Mathf.Clamp01(distanceToPlayer / electroDetectionRange));

        electroSource.volume = volume;
        electroSource.pitch = pitch;
    }

    private void UpdateChaseSound()
    {
        if (enemyAI == null) return;

        if (enemyAI.IsChasing)
        {
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            float chaseIntensity = Mathf.Clamp01(1 - (distanceToPlayer / electroDetectionRange));
            
            chaseSource.volume = chaseVolume * chaseIntensity;
            chaseSource.pitch = Mathf.Lerp(minChasePitch, maxChasePitch, chaseIntensity);
        }
        else
        {
            chaseSource.volume = 0f;
        }
    }

    private void CheckDetectionVoice()
    {
        if (enemyAI == null || !enemyAI.IsInvestigating) return;

        // 只有当声音位置更新时才播放提示音
        if (enemyAI.SoundPosition != lastSoundPosition && CanPlayVoice())
        {
            PlayDetectionVoice();
            lastSoundPosition = enemyAI.SoundPosition;
        }
    }

    private void PlayDetectionVoice()
    {
        if (detectionVoices == null || detectionVoices.Length == 0) return;

        float distance = Vector3.Distance(transform.position, player.position);
        if (distance > detectionVoiceRange) return;

        float distanceVolume = 1f - Mathf.Clamp01(distance / detectionVoiceRange);
        int index = Random.Range(0, detectionVoices.Length);
        
        voiceSource.PlayOneShot(detectionVoices[index], distanceVolume * voiceVolume);
        lastVoiceTime = Time.time;
    }

    private bool CanPlayVoice()
    {
        return Time.time - lastVoiceTime > minVoiceInterval;
    }
}
