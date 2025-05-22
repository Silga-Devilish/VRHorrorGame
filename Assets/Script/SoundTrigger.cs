using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SoundTrigger : MonoBehaviour
{
    [Header("声音设置")]
    [SerializeField] private float soundRadius = 20f;
    [SerializeField] private AudioClip triggerSound;
    [SerializeField] private float soundVolume = 0.8f;
    [SerializeField] private bool onlyTriggerOnce = true;

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
        if (hasTriggered && onlyTriggerOnce) return;
        
        // 检查是否是玩家
        if (other.CompareTag("Player"))
        {
            FirstPersonController playerController = other.GetComponent<FirstPersonController>();
            if (playerController != null && !playerController.IsCrouching)
            {
                TriggerSound();
            }
        }
    }

    private void TriggerSound()
    {
        // 播放音效
        if (triggerSound != null)
        {
            audioSource.Play();
        }

        // 通知所有敌人
        EnemyAIPatrol[] enemies = FindObjectsOfType<EnemyAIPatrol>();
        foreach (EnemyAIPatrol enemy in enemies)
        {
            if (Vector3.Distance(enemy.transform.position, transform.position) <= soundRadius)
            {
                enemy.TriggerLoudSound(transform.position);
            }
        }

        hasTriggered = true;
        Debug.Log($"<color=orange>触发大声响！位置: {transform.position}</color>");
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制声音触发范围
        Gizmos.color = new Color(1, 0.5f, 0, 0.2f); // 半透明橙色
        Gizmos.DrawSphere(transform.position, soundRadius);
        Gizmos.color = new Color(1, 0.5f, 0); // 橙色
        Gizmos.DrawWireSphere(transform.position, soundRadius);
    }
}
