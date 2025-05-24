using System.Collections.Generic;
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
    [SerializeField] private float loseSightTime = 3f;

    [Header("视野设置")]
    [SerializeField] private float fieldOfViewAngle = 110f;
    [SerializeField] private float closeRangeDetection = 2f;
    [SerializeField] private LayerMask obstacleMask;

    [Header("听觉设置")]
    [SerializeField] private float walkSoundDetectionRange = 4f;
    [SerializeField] private float runSoundDetectionRange = 12f;
    [SerializeField] private float loudSoundDetectionRange = 20f;
    [SerializeField] private float soundInvestigationTime = 5f;
    [SerializeField] private float soundCheckInterval = 0.3f; // 新增：声音检测间隔

    [Header("脚步声设置")]
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float patrolStepInterval = 0.5f;
    [SerializeField] private float chaseStepInterval = 0.3f;
    [SerializeField] private float patrolStepVolume = 0.4f;
    [SerializeField] private float chaseStepVolume = 0.7f;
    [SerializeField] private float footstepPitchRandomness = 0.1f;

    private AudioSource footstepSource;
    private float stepCycle;
    private float nextStep;

    private float soundCheckTimer; // 新增：声音检测计时器
    private NavMeshAgent navAgent;
    private Transform player;
    private FirstPersonController playerController; // 新增：获取玩家控制器
    private int currentPatrolIndex = 0;
    private float waitCounter;
    private bool isWaiting = false;
    private bool isChasing = false;
    private bool isInvestigating = false; // 新增：是否正在调查
    private Vector3 lastKnownPlayerPosition;
    private Vector3 soundPosition; // 新增：最后听到的声音位置
    private float lostSightTimer;
    private float investigationTimer; // 新增：调查计时器
    private float lastSoundTime; // 新增：最后听到声音的时间
    public bool IsInvestigating => isInvestigating;
    public bool IsChasing => isChasing;
    public Vector3 SoundPosition => soundPosition; // 添加这行到EnemyAIPatrol类中
    private List<int> remainingPatrolIndices = new List<int>();

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

        // 新增：获取玩家控制器
        playerController = player.GetComponent<FirstPersonController>();
        if (playerController == null)
        {
            Debug.LogError("玩家对象上没有找到FirstPersonController组件！");
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

        // 初始化脚步声源
        footstepSource = GetComponent<AudioSource>();
        if (footstepSource == null)
        {
            footstepSource = gameObject.AddComponent<AudioSource>();
        }
        footstepSource.spatialBlend = 1f; // 3D音效
        footstepSource = gameObject.AddComponent<AudioSource>();
        footstepSource.spatialBlend = 1f;
        footstepSource.playOnAwake = false;

        // 初始化巡逻点索引列表
        InitializePatrolIndices();
    }

    private void Update()
    {
        if (player == null) return;

        // 更新声音检测计时器
        soundCheckTimer += Time.deltaTime;
        if (soundCheckTimer >= soundCheckInterval)
        {
            soundCheckTimer = 0f;
            // 无论什么状态都检测声音
            CheckForSounds();
        }

        bool canSeePlayer = CanSeePlayer();

        if (canSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            lostSightTimer = 0f;

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
        else if (isInvestigating)
        {
            InvestigateSound();
        }
        else if (patrolPoints != null && patrolPoints.Length > 0)
        {
            Patrol();
        }

        // 更新脚步声
        if (navAgent.velocity.magnitude > 0.1f && navAgent.isOnNavMesh)
        {
            ProgressStepCycle();
        }
    }

    // 新增方法：检测玩家发出的声音
    private void CheckForSounds()
    {
        if (playerController == null) return;

         bool isPlayerMoving = playerController.IsMoving;
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        float soundRange = 0f;

        if (isPlayerMoving)
        {
            if (playerController.IsRunning && !playerController.IsCrouching)
            {
                soundRange = runSoundDetectionRange;
            }
            else if (!playerController.IsCrouching)
            {
                soundRange = walkSoundDetectionRange;
            }
        }

        if (soundRange > 0 && distanceToPlayer <= soundRange)
        {
            // 总是更新到最新的声音位置
            UpdateSoundPosition(player.position);
        }
    }


    // 新增方法：开始调查声音
    private void StartInvestigation()
    {
        isInvestigating = true;
        investigationTimer = 0f; // 重置计时器
        navAgent.speed = patrolSpeed * 1.2f;
        navAgent.SetDestination(soundPosition);
        Debug.Log($"<color=cyan>开始/更新调查声音来源...</color>");
    }

    // 新增方法：调查声音
    private void InvestigateSound()
    {
        // 如果还没到达声音位置，继续移动
        if (navAgent.remainingDistance > navAgent.stoppingDistance)
        {
            navAgent.SetDestination(soundPosition);
            return;
        }
        
        // 到达后才开始计时
        investigationTimer += Time.deltaTime;
        
        // 检查调查时间是否结束
        if (investigationTimer >= soundInvestigationTime)
        {
            isInvestigating = false;
            investigationTimer = 0f;
            
            // 返回最近的巡逻点
            if (patrolPoints != null && patrolPoints.Length > 0)
            {
                currentPatrolIndex = FindNearestPatrolPoint();
                navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                Debug.Log($"<color=cyan>调查结束，返回巡逻点: {patrolPoints[currentPatrolIndex].name}</color>");
            }
        }
    }

    // 新增方法：触发大声响(可由其他脚本调用)
    public void TriggerLoudSound(Vector3 position)
    {
        float distance = Vector3.Distance(transform.position, position);
        if (distance <= loudSoundDetectionRange)
        {
            // 大声响总是会覆盖其他声音
            UpdateSoundPosition(position, true);
        }
    }

    private void UpdateSoundPosition(Vector3 newPosition, bool isLoudSound = false)
    {
        // 如果是大声响或者当前没有在调查/追逐，或者新声音更近
        if (isLoudSound || !isInvestigating || !isChasing || 
            Vector3.Distance(transform.position, newPosition) < 
            Vector3.Distance(transform.position, soundPosition))
        {
            soundPosition = newPosition;
            lastSoundTime = Time.time;
            
            if (!isChasing)
            {
                StartInvestigation();
            }
            Debug.Log($"<color=cyan>更新声音位置: {newPosition}</color>");
        }
    }
    private bool CanSeePlayer()
    {
        if (player == null) return false;

        Vector3 directionToPlayer = player.position - transform.position;
        float distanceToPlayer = directionToPlayer.magnitude;

        // 1. 检查是否在近身检测范围内（无视视野角度，但检查障碍物）
        if (distanceToPlayer <= closeRangeDetection)
        {
            if (!Physics.Raycast(transform.position + Vector3.up * 0.5f,
                               directionToPlayer.normalized,
                               distanceToPlayer,
                               obstacleMask))
            {
                Debug.Log("<color=orange>玩家进入近身范围，且未被阻挡，强制检测！</color>");
                return true;
            }
        }

        // 2. 检查是否在常规检测范围内
        if (distanceToPlayer > detectionRange) return false;

        // 3. 检查是否在视野角度内
        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);
        if (angleToPlayer > fieldOfViewAngle / 2f) return false;

        // 4. 检查视线是否被阻挡
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
        isInvestigating = false; // 停止调查状态
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
        
        if (patrolPoints != null && patrolPoints.Length > 0)
        {
            currentPatrolIndex = FindNearestPatrolPoint();
            navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
            Debug.Log($"<color=yellow>玩家超出追击范围，返回最近的巡逻点: {patrolPoints[currentPatrolIndex].name}</color>");
        }
    }

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
                // 修改为随机选择下一个巡逻点
                currentPatrolIndex = GetNextRandomPatrolIndex();
                navAgent.SetDestination(patrolPoints[currentPatrolIndex].position);
                Debug.Log($"随机前往下一个巡逻点: {patrolPoints[currentPatrolIndex].name}");
            }
        }
    }
    private void InitializePatrolIndices()
    {
        remainingPatrolIndices.Clear();
        for (int i = 0; i < patrolPoints.Length; i++)
        {
            remainingPatrolIndices.Add(i);
        }
        // 移除当前巡逻点
        remainingPatrolIndices.Remove(currentPatrolIndex);
    }

    private int GetNextRandomPatrolIndex()
    {
        if (remainingPatrolIndices.Count == 0)
        {
            InitializePatrolIndices();
        }

        int randomIndex = Random.Range(0, remainingPatrolIndices.Count);
        int nextIndex = remainingPatrolIndices[randomIndex];
        remainingPatrolIndices.RemoveAt(randomIndex);
        
        return nextIndex;
    }
    private void ProgressStepCycle()
    {
        float speed = isChasing ? chaseSpeed : patrolSpeed;
        stepCycle += speed * Time.deltaTime;

        float interval = isChasing ? chaseStepInterval : patrolStepInterval;
        
        if (stepCycle > nextStep)
        {
            nextStep = stepCycle + interval;
            PlayFootstep();
        }
    }

    private void PlayFootstep()
    {
        if (footstepSounds == null || footstepSounds.Length == 0) return;

        int index = Random.Range(0, footstepSounds.Length);
        footstepSource.pitch = Random.Range(1f - footstepPitchRandomness, 1f + footstepPitchRandomness);
        footstepSource.volume = isChasing ? chaseStepVolume : patrolStepVolume;
        footstepSource.PlayOneShot(footstepSounds[index]);
    }


    private void OnDrawGizmosSelected()
    {
        // 绘制检测范围
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // 绘制近身检测范围
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, closeRangeDetection);

        // 绘制追击范围
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, chaseRange);

        // 绘制声音检测范围的线框
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, walkSoundDetectionRange);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, runSoundDetectionRange);

        Gizmos.color = new Color(1, 0.5f, 0); // 橙色
        Gizmos.DrawWireSphere(transform.position, loudSoundDetectionRange);

        // 绘制视野锥形
        Gizmos.color = Color.blue;
        Vector3 leftBoundary = Quaternion.Euler(0, -fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;
        Vector3 rightBoundary = Quaternion.Euler(0, fieldOfViewAngle / 2, 0) * transform.forward * detectionRange;
        Gizmos.DrawLine(transform.position, transform.position + leftBoundary);
        Gizmos.DrawLine(transform.position, transform.position + rightBoundary);
        Gizmos.DrawLine(transform.position + rightBoundary, transform.position + leftBoundary);
    }
}
