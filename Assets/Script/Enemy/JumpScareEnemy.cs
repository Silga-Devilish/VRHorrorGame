using UnityEngine;
using UnityEngine.SceneManagement;

public class JumpScareEnemy : MonoBehaviour
{
    [Header("跳杀设置")]
    [SerializeField] private string gameOverScene = "GameOverScene";
    [SerializeField] private AudioClip jumpScareSound;
    [SerializeField] private float scareDelay = 0.5f;
    
    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (!hasTriggered && other.CompareTag("Player"))
        {
            hasTriggered = true;
            TriggerJumpScare();
        }
    }

    private void TriggerJumpScare()
    {
        // 播放跳杀音效
        if (jumpScareSound != null)
        {
            AudioSource.PlayClipAtPoint(jumpScareSound, Camera.main.transform.position);
        }
        
        // 禁用玩家控制
        FirstPersonController player = FindObjectOfType<FirstPersonController>();
        if (player != null)
        {
            player.enabled = false;
        }
        
        // 解锁鼠标并显示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        // 延迟后跳转场景
        Invoke(nameof(LoadGameOverScene), scareDelay);
    }

    private void LoadGameOverScene()
    {
        if (!string.IsNullOrEmpty(gameOverScene))
        {
            SceneManager.LoadScene(gameOverScene);
        }
        else
        {
            Debug.LogError("未设置游戏结束场景！");
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
