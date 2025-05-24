using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SoundTrigger : MonoBehaviour
{
    [Header("声音设置")]
    [SerializeField] private float soundRadius = 15f; // 声音传播范围
    [SerializeField] private AudioClip triggerSound; // 触发时的音效
    [SerializeField] private float soundVolume = 0.8f;
    [SerializeField] private bool onlyTriggerOnce = true; // 是否只触发一次

    [Header("视觉效果")]
    [SerializeField] private ParticleSystem impactParticles; // 触发时的粒子效果

    private AudioSource audioSource;
    private bool hasTriggered = false;

    private void Awake()
    {
        // 确保碰撞体是触发器
        GetComponent<Collider>().isTrigger = true;

        // 设置音频源
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f; // 3D音效
        audioSource.playOnAwake = false;
        audioSource.clip = triggerSound;
        audioSource.volume = soundVolume;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (onlyTriggerOnce && hasTriggered) return;

        // 检查是否是玩家且不是蹲伏状态
        if (other.CompareTag("Player"))
        {
            FirstPersonController playerController = other.GetComponent<FirstPersonController>();
            if (playerController != null && !playerController.IsCrouching)
            {
                TriggerSoundEffect();
            }
        }
    }

    private void TriggerSoundEffect()
    {
        hasTriggered = true;

        // 播放音效
        if (triggerSound != null)
        {
            audioSource.Play();
        }

        // 播放粒子效果
        if (impactParticles != null)
        {
            impactParticles.Play();
        }

        // 通知所有敌人
        EnemyAIPatrol[] allEnemies = FindObjectsOfType<EnemyAIPatrol>();
        foreach (EnemyAIPatrol enemy in allEnemies)
        {
            if (Vector3.Distance(transform.position, enemy.transform.position) <= soundRadius)
            {
                enemy.TriggerLoudSound(transform.position);
            }
        }

        Debug.Log($"<color=orange>触发大声响！位置: {transform.position}</color>");
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制声音范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }
}
