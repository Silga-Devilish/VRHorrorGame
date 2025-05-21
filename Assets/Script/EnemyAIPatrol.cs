using UnityEngine;
using UnityEngine.AI;

public class EnemyAIPatrol : MonoBehaviour
{
    [Header("巡逻设置")]
    [SerializeField] private Transform[] patrolPoints;
    [SerializeField] private float patrolSpeed = 2f;
    [SerializeField] private float waitTimeAtPoint = 2f;
    [SerializeField] private float detectionRange = 10f;
    [SerializeField] private float chaseRange = 15f;
    [SerializeField] private float chaseSpeed = 4f;
    [SerializeField] private float loseSightTime = 3f; // 新增：丢失视野后多久脱战

    [Header("视野设置")]
    [SerializeField] private float fieldOfViewAngle = 110f;
    [SerializeField] private LayerMask obstacleMask;

    private NavMeshAgent navAgent;
    private Transform player;
    private int currentPatrolIndex = 0;
    private float waitCounter;
    private bool isWaiting = false;
    private bool isChasing = false;
    private Vector3 lastKnownPlayerPosition;
    private float lostSightTimer; // 新增：丢失视野计时器
    

    private void Awake()
    {
        navAgent = GetComponent<NavMeshAgent>();
        if (navAgent == null)
        {
            navAgent = gameObject.AddComponent<NavMeshAgent>();
            Debug.LogWarning("自动添加了NavMeshAgent组件");
        }

        player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (player == null)
        {
            Debug.LogError("场景中未找到带有'Player'标签的对象！");
            enabled = false;
            return;
        }

        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            navAgent.speed = patrolSpeed;
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            Debug.Log($"敌人开始巡逻，首个目标点: {patrolPoints[currentPatrolIndex].name}");
        }
        else
        {
            Debug.LogWarning("未分配巡逻点！");
        }
    }

    private void Update()
    {
        if (player == null) return;

        bool canSeePlayer = CanSeePlayer();
        
        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            lostSightTimer = 0f; // 重置丢失视野计时器
            
            if (!isChasing)
            {
                StartChasing();
            }
            ChasePlayer();
        }
        else if (isChasing)
        {
            // 如果看不到玩家但还在追逐范围内，继续前往最后已知位置
            if (Vector3.Distance(transform.position, lastKnownPlayerPosition) > navAgent.stoppingDistance)
            {
                navAgent.SetDestination(lastKnownPlayerPosition);
            }
            
            // 检查是否超出追逐范围或丢失视野时间过长
            float distanceToPlayer = Vector3.Distance(transform.position, player.position);
            lostSightTimer += Time.deltaTime;
            
            if (distanceToPlayer > chaseRange || lostSightTimer >= loseSightTime)
            {
                StopChasing();
            }
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol();
        }
    }

    private bool CanSeePlayer()
    {
        if (player == null) return false;
        
        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;
        
        // 1. 检查是否在检测范围内
        if (distanceToPlayer > detectionRange) return false;
        
        // 2. 检查是否在视野角度内
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angleToPlayer > fieldOfViewAngle / 2f) return false;
        
        // 3. 检查视线是否被阻挡
        if (Physics.Raycast(transform.position + Vector3.up * 0.5f,
                          directionToPlayer.normalized, 
                          distanceToPlayer, 
                          obstacleMask))
        {
            return false;
        }
        
        return true;
    }

    private void StartChasing()
    {
        isChasing = true;
        navAgent.speed = chaseSpeed;
        Debug.Log($"<color=red>发现玩家！开始追逐！</color>");
    }

    private void ChasePlayer()
    {
        if (Vector3.Distance(transform.position, player.position) > chaseRange)
        {
            StopChasing();
            return;
        }
        
        navAgent.SetDestination(player.position);
    }

    private void StopChasing()
    {
        isChasing = false;
        navAgent.speed = patrolSpeed;
        lostSightTimer = 0f;
        
        // 寻找最近的巡逻点
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolIndex = FindNearestPatrolPoint();
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            Debug.Log($"<color=yellow>玩家超出追击范围，返回最近的巡逻点: {patrolPoints[currentPatrolIndex].name}</color>");
        }
    }

    // 新增方法：寻找最近的巡逻点
    private int FindNearestPatrolPoint()
    {
        int nearestIndex = 0;
        float minDistance = float.MaxValue;
        
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestIndex = i;
            }
        }
        
        return nearestIndex;
    }

    private void Patrol()
    {
        if (patrolPoints.Length == 0) return;

        if (navAgent.remainingDistance <= navAgent.stoppingDistance && !isWaiting)
        {
            isWaiting = true;
            waitCounter = 0f;
            Debug.Log($"到达巡逻点: {patrolPoints[currentPatrolIndex].name}，等待{waitTimeAtPoint}秒");
        }

        if (isWaiting)
        {
            waitCounter += Time.deltaTime;
            if (waitCounter >= waitTimeAtPoint)
            {
                isWaiting = false;
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                Debug.Log($"前往下一个巡逻点: {patrolPoints[currentPatrolIndex].name}");
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 绘制追击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 绘制视野锥形
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position + rightBoundary, transform.position + leftBoundary);
    }
}
