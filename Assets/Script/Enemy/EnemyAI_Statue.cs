using UnityEngine;
using UnityEngine.AI;

public class EnemyAI_Statue : MonoBehaviour
{
    [Header("移动设置")]
    [SerializeField] private float normalSpeed = 1.5f;
    [SerializeField] private float stoppingDistance = 1f;
    
    [Header("视线检测")]
    [SerializeField] private float detectionAngle = 30f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private LayerMask obstacleMask;
    
    [Header("动画控制")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParam = "Speed"; // 控制动画速度的参数名
    
    private NavMeshAgent navAgent;
    private Transform player;
    private Transform playerCamera;

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        
        if (player != null)
        {
            playerCamera = player.GetComponentInChildren<Camera>()?.transform;
        }

        if (navAgent != null)
        {
            navAgent.speed = normalSpeed;
            navAgent.stoppingDistance = stoppingDistance;
        }

        if (player == null || playerCamera == null || animator == null)
        {
            Debug.LogError("缺少必要组件!");
            enabled = false;
        }
    }

    private void Update()
    {
        if (player == null || !navAgent.enabled) return;

        bool isPlayerLooking = IsPlayerLookingAtEnemy();
        
        // 直接控制动画速度
        float targetSpeed = isPlayerLooking ? 0f : 1f;
        animator.SetFloat(speedParam, targetSpeed);
        
        // 控制实际移动
        navAgent.isStopped = isPlayerLooking;
        if (!isPlayerLooking)
        {
            navAgent.SetDestination(player.position);
        }
    }

    private bool IsPlayerLookingAtEnemy()
    {
        Vector3 directionToEnemy = transform.position - playerCamera.position;
        float angle = Vector3.Angle(playerCamera.forward, directionToEnemy);
        float distance = directionToEnemy.magnitude;

        if (angle < detectionAngle && distance < detectionRange)
        {
            return !Physics.Raycast(playerCamera.position, 
                                 directionToEnemy.normalized, 
                                 distance, 
                                 obstacleMask);
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (playerCamera != null)
        {
            Gizmos.color = Color.red;
            Vector3 leftBoundary = Quaternion.Euler(0, -detectionAngle/2, 0) * playerCamera.forward * detectionRange;
            Vector3 rightBoundary = Quaternion.Euler(0, detectionAngle/2, 0) * playerCamera.forward * detectionRange;
            
            Gizmos.DrawLine(playerCamera.position, playerCamera.position + leftBoundary);
            Gizmos.DrawLine(playerCamera.position, playerCamera.position + rightBoundary);
        }
    }
}
