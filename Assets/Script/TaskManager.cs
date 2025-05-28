using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class TaskManager : MonoBehaviour
{
    [Header("任务UI设置")]
    [SerializeField] private TextMeshProUGUI taskListText;
    [SerializeField] private Color incompleteColor = Color.white;
    [SerializeField] private Color completeColor = Color.green;

    [Header("时间设置")]
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private float realTimeToGameTimeRatio = 1f; // 1现实秒=6游戏分钟
    [SerializeField] private int targetGameHour = 6; // 目标时间6点

    [Header("场景设置")]
    [SerializeField] private string winSceneName = "WinScene"; // 胜利场景名称

    [Header("游戏提示设置")]
    [SerializeField] private TextMeshProUGUI mainHintText; // 主提示（左下角）
    [SerializeField] private TextMeshProUGUI tvWarningText; // 电视警告（右下角）
    [SerializeField] private float tvWarningBlinkSpeed = 0.5f; // 闪烁速度

    [Header("敌人设置")]
    [SerializeField] private GameObject enemyA; // 阶段1敌人
    [SerializeField] private GameObject enemyB; // 阶段2敌人
    [SerializeField] private GameObject enemyC; // 阶段3敌人
    [SerializeField] private float enemyTransitionDelay = 1f; // 敌人切换延迟

    private GameObject currentActiveEnemy;

    private Coroutine tvWarningCoroutine;
    private bool[] taskCompleted = new bool[3];
    private float gameTimeElapsed;
    private bool isTimerRunning;
    private FuseBox fuseBox;
    private WindUpBoxController tvController;

    private void Awake()
    {
        fuseBox = FindObjectOfType<FuseBox>();
        tvController = FindObjectOfType<WindUpBoxController>();
        timeText.text = "??:??";
        UpdateTaskText();
        UpdateHintText(); // 初始化提示文本
                          // 初始禁用所有敌人
        SetEnemyActive(enemyA, false);
        SetEnemyActive(enemyB, false);
        SetEnemyActive(enemyC, false);

        // 根据任务进度启用敌人
        UpdateEnemyState();
    }


    private void Update()
    {
        CheckTaskCompletion();
        
        if (isTimerRunning)
        {
            gameTimeElapsed += Time.deltaTime;
            Debug.Log($"游戏时间累计: {gameTimeElapsed}秒 | 倍率: {realTimeToGameTimeRatio}");
            
            UpdateTimeDisplay(gameTimeElapsed);
            
            // 计算总游戏分钟数（考虑时间倍率）
            float totalGameMinutes = gameTimeElapsed * realTimeToGameTimeRatio;
            
            if (totalGameMinutes >= 360f) // 360分钟=6小时
            {
                GameWin();
            }
        }
    }

    private void CheckTaskCompletion()
    {
        // 任务1: 找到保险丝（需要手动调用CompleteTask(0)）

        // 任务2: 修理电箱
        if (!taskCompleted[1] && fuseBox != null && fuseBox.IsRepaired)
        {
            CompleteTask(1);
        }

        // 任务3: 保持电视开启（开始计时）
        if (!taskCompleted[2] && tvController != null && tvController.IsPoweredOn)
        {
            CompleteTask(2);
            StartTimer();
        }
    }

    public void CompleteTask(int taskIndex)
    {
        if (taskIndex >= 0 && taskIndex < taskCompleted.Length)
        {
            taskCompleted[taskIndex] = true;
            UpdateTaskText();
            UpdateHintText();
            UpdateEnemyState(); // 更新敌人状态
            Debug.Log($"任务 {taskIndex + 1} 完成!");
        }
    }
    private void StartTimer()
    {
        isTimerRunning = true;
        gameTimeElapsed = 0f;
        Debug.Log("计时器启动，重置时间计数");
    }

    private void UpdateTaskText()
    {
        if (taskListText == null) return;

        string taskText = "如何存活下来？\n";
        taskText += GetTaskString(0, "步骤一：找到扳手");
        taskText += GetTaskString(1, "步骤二：修理电箱，修理完毕后电灯开关和电视将通电");
        taskText += GetTaskString(2, "步骤三：保持电视开启并存活到6点");

        taskListText.text = taskText;
    }

    private string GetTaskString(int taskIndex, string taskDescription)
    {
        Color color = taskCompleted[taskIndex] ? completeColor : incompleteColor;
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{taskDescription}</color>\n";
    }

    private void UpdateTimeDisplay(float timeInSeconds)
    {
        if (timeText == null) return;
        
        if (!taskCompleted[2])
        {
            timeText.text = "??:??";
            return;
        }
        
        // 使用realTimeToGameTimeRatio计算游戏时间
        float totalGameMinutes = timeInSeconds * realTimeToGameTimeRatio;
        
        // 计算小时和分钟（只显示十位）
        int gameHours = Mathf.FloorToInt(totalGameMinutes / 60f);
        int gameMinutes = Mathf.FloorToInt((totalGameMinutes % 60f) / 10f) * 10;
        
        // 确保不超过5:50
        gameHours = Mathf.Min(gameHours, 5);
        gameMinutes = Mathf.Min(gameMinutes, 50);
        
        timeText.text = $"{gameHours}:{gameMinutes:00}";
    }


    private void GameWin()
    {
        // 停止所有提示
        if (tvWarningCoroutine != null)
        {
            StopCoroutine(tvWarningCoroutine);
            tvWarningCoroutine = null;
        }

        if (tvWarningText != null)
        {
            tvWarningText.gameObject.SetActive(false);
        }

        // 解锁鼠标并显示
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        isTimerRunning = false;
        Debug.Log("游戏胜利！存活到6点！");

        // 加载胜利场景
        if (!string.IsNullOrEmpty(winSceneName))
        {
            SceneManager.LoadScene(winSceneName);
        }
        else
        {
            // 如果没有设置场景，暂停游戏
            Time.timeScale = 0f;
        }
    }

    private void UpdateHintText()
    {
        if (mainHintText == null) return;

        if (!taskCompleted[0]) // 步骤一未完成
        {
            mainHintText.text = "- 右键开关手电 Shift跑步 Ctrl蹲行 -";
        }
        else if (!taskCompleted[1]) // 步骤一完成，步骤二未完成
        {
            mainHintText.text = "- 小心：注视一些东西以防止其不适当地移动 -";
        }
        else if (taskCompleted[2]) // 步骤三完成
        {
            mainHintText.text = "- 小心：它生气了，行走、跑步以及一些声音可能会吸引它的注意，躲进房间以苟活 -";

            // 启动电视警告闪烁
            if (tvWarningCoroutine == null && tvWarningText != null)
            {
                tvWarningCoroutine = StartCoroutine(BlinkTvWarning());
            }
        }
    }
    private IEnumerator BlinkTvWarning()
    {
        tvWarningText.gameObject.SetActive(true);
        string warningMsg = "- 请不要让电视关掉，不要不要不要不要 -";

        while (true)
        {
            tvWarningText.text = warningMsg;
            yield return new WaitForSeconds(tvWarningBlinkSpeed);
            tvWarningText.text = "";
            yield return new WaitForSeconds(tvWarningBlinkSpeed);
        }
    }
    
    private void UpdateEnemyState()
    {
        GameObject enemyToEnable = null;
        
        if (!taskCompleted[0]) // 阶段1
        {
            enemyToEnable = enemyA;
        }
        else if (!taskCompleted[1]) // 阶段2
        {
            enemyToEnable = enemyB;
        }
        else if (taskCompleted[2]) // 阶段3
        {
            enemyToEnable = enemyC;
        }

        StartCoroutine(SwitchEnemyCoroutine(enemyToEnable));
    }

    private IEnumerator SwitchEnemyCoroutine(GameObject newEnemy)
    {
        // 禁用当前敌人
        if (currentActiveEnemy != null)
        {
            SetEnemyActive(currentActiveEnemy, false);
            yield return new WaitForSeconds(enemyTransitionDelay);
        }

        // 启用新敌人
        if (newEnemy != null)
        {
            SetEnemyActive(newEnemy, true);
            currentActiveEnemy = newEnemy;
        }
    }

    private void SetEnemyActive(GameObject enemy, bool active)
    {
        if (enemy != null)
        {
            enemy.SetActive(active);
            
            // 确保敌人组件也被正确启用/禁用
            var enemyAI = enemy.GetComponent<EnemyAIPatrol>();
            if (enemyAI != null)
            {
                enemyAI.enabled = active;
            }
            
            var collider = enemy.GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = active;
            }
        }
    }
}
