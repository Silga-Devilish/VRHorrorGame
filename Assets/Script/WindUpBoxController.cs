using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class WindUpBoxController : MonoBehaviour
{
    [Header("发条设置")]
    [SerializeField] private float maxWindUpValue = 100f;
    [SerializeField] private float drainRate = 1f;
    [SerializeField] private float refillRate = 10f;
    [SerializeField] private float gameOverDelay = 5f;

    [Header("UI设置")]
    [SerializeField] private Slider windUpSlider;
    [SerializeField] private TextMeshProUGUI warningText;
    [SerializeField] private GameObject gameOverUI;
    [SerializeField] private string warningMessage = "电视即将关闭";
    [SerializeField] private Color warningColor = Color.yellow;

    [Header("交互提示")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string interactText = "按E 维持电视开启";
    [SerializeField] private float interactionDistance = 2f;

    [Header("灯光控制")]
    [SerializeField] private Light tvLight;
    [SerializeField] private float normalFlickerIntensity = 0.2f;
    [SerializeField] private float lowPowerFlickerIntensity = 0.5f;
    [SerializeField] private float normalFlickerSpeed = 5f;
    [SerializeField] private float lowPowerFlickerSpeed = 15f;
    [SerializeField] private float minLightIntensity = 0.3f;
    [SerializeField] private float maxLightIntensity = 1f;

    [Header("屏幕材质控制")]
    [SerializeField] private Renderer tvScreenRenderer;
    [SerializeField] private string emissionColorProperty = "_EmissionColor";
    [SerializeField] private string ifChangeProperty = "_IfChange";
    [SerializeField] private Color normalEmission = new Color(2f, 2f, 2f); // HDR值
    [SerializeField] private Color lowPowerEmission = new Color(0.6f, 0.6f, 0.6f); // HDR值
    [SerializeField] private float scareDuration = 0.5f;

    [Header("音效")]
    [SerializeField] private AudioClip windUpSound;
    [SerializeField] private AudioClip lowEnergySound;
    [SerializeField] private AudioClip powerDownSound;
    [SerializeField] private AudioClip ambientNoise;
    [SerializeField] private AudioClip scareSound; // 新增：跳吓音效
    [SerializeField] private float windUpVolume = 0.7f;
    [SerializeField] private float ambientVolume = 0.5f;

    [Header("声音触发")]
    [SerializeField] private float refillSoundThreshold = 10f;
    [SerializeField] private SoundTrigger soundTrigger;

    [Header("依赖")]
    [SerializeField] private FuseBox fuseBox;

    private float currentWindUpValue;
    private float lastRefillAmount;
    private bool isPoweredOn;
    private bool isBeingWound;
    private bool hasGivenWarning;
    private bool isGameOver;
    private float emptyTimer;
    private AudioSource audioSource;
    private AudioSource ambientSource;
    private Transform player;
    private Material tvScreenMaterial;
    private float flickerTimer;
    private bool isLowPowerMode;
    public bool IsPoweredOn => isPoweredOn; // 公开电源状态

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.spatialBlend = 1f;

        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.spatialBlend = 1f;
        ambientSource.loop = true;
        ambientSource.clip = ambientNoise;
        ambientSource.volume = 0f;

        player = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 获取屏幕材质实例
        if (tvScreenRenderer != null)
        {
            tvScreenMaterial = tvScreenRenderer.materials[1]; // 假设屏幕材质是第2个
        }

        currentWindUpValue = maxWindUpValue;
        UpdateUI();
        ShowPrompt(false);

        // 初始状态设置为断电
        SetPowerOffState();
    }

    private void Update()
    {
        if (player == null) return;

        // 检查电源状态
        bool shouldBePowered = fuseBox != null && fuseBox.IsRepaired;

        if (shouldBePowered != isPoweredOn)
        {
            isPoweredOn = shouldBePowered;
            if (isPoweredOn)
            {
                OnPowerOn();
            }
            else
            {
                OnPowerOff();
            }
        
        }

        // 交互提示检测
        CheckPlayerProximity();

        if (!isPoweredOn || isGameOver) return;

        // 只有通电时才更新视觉效果
        if (isPoweredOn && !isGameOver)
        {
            UpdateVisualEffects();
            UpdateAmbientNoise();
        }

        // 发条消耗逻辑
        if (!isBeingWound)
        {
            currentWindUpValue -= drainRate * Time.deltaTime;
            currentWindUpValue = Mathf.Max(0, currentWindUpValue);

            if (currentWindUpValue < maxWindUpValue * 0.25f && !hasGivenWarning)
            {
                TriggerLowEnergyWarning();
            }

            if (currentWindUpValue <= 0)
            {
                emptyTimer += Time.deltaTime;
                if (emptyTimer >= gameOverDelay)
                {
                    StartCoroutine(GameOverSequence());
                }
            }
            else
            {
                emptyTimer = 0f;
            }
        }

        UpdateUI();
    }

    private void UpdateVisualEffects()
    {
        if (!isPoweredOn) return; // 未通电时不更新视觉效果
        // 检测是否进入低电量模式
        bool shouldBeLowPower = currentWindUpValue < maxWindUpValue * 0.25f;
        if (shouldBeLowPower != isLowPowerMode)
        {
            isLowPowerMode = shouldBeLowPower;
            if (isLowPowerMode) EnterLowPowerMode();
            else ExitLowPowerMode();
        }

        // 控制灯光频闪
        if (tvLight != null)
        {
            flickerTimer += Time.deltaTime * (isLowPowerMode ? lowPowerFlickerSpeed : normalFlickerSpeed);
            float flickerAmount = Mathf.PerlinNoise(flickerTimer, 0) *
                                (isLowPowerMode ? lowPowerFlickerIntensity : normalFlickerIntensity);

            float baseIntensity = isLowPowerMode ?
                Mathf.Lerp(minLightIntensity, maxLightIntensity * 0.5f, currentWindUpValue / (maxWindUpValue * 0.25f)) :
                maxLightIntensity;

            tvLight.intensity = baseIntensity * (1f - flickerAmount);
        }

        // 更新屏幕亮度
        if (tvScreenMaterial != null)
        {
            Color targetEmission = isLowPowerMode ?
                Color.Lerp(lowPowerEmission * 0.5f, lowPowerEmission, currentWindUpValue / (maxWindUpValue * 0.25f)) :
                normalEmission;

            tvScreenMaterial.SetColor(emissionColorProperty, targetEmission);
        }
    }

    private void EnterLowPowerMode()
    {
        // 可以在这里添加进入低电量模式的特殊效果
        Debug.Log("进入低电量模式");
    }

    private void ExitLowPowerMode()
    {
        // 可以在这里添加退出低电量模式的特殊效果
        Debug.Log("退出低电量模式");
    }

    private void CheckPlayerProximity()
    {
        if (!isPoweredOn || isGameOver)
        {
            ShowPrompt(false);
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool showPrompt = distance <= interactionDistance &&
                         IsPlayerFacingObject();

        ShowPrompt(showPrompt);
    }

    private bool IsPlayerFacingObject()
    {
        Vector3 directionToObject = transform.position - player.position;
        float angle = Vector3.Angle(player.forward, directionToObject.normalized);
        return angle < 45f;
    }

    private void ShowPrompt(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show);
        }
    }

    private void UpdateAmbientNoise()
    {
        float targetVolume = currentWindUpValue > 0 ? ambientVolume : 0f;
        ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVolume, 0.1f);

        if (!ambientSource.isPlaying && targetVolume > 0)
        {
            ambientSource.Play();
        }
    }

    public void StartWinding()
    {
        if (!isPoweredOn || isGameOver) return;

        isBeingWound = true;
        lastRefillAmount = currentWindUpValue;

        audioSource.loop = true;
        audioSource.clip = windUpSound;
        audioSource.volume = windUpVolume;
        audioSource.Play();
    }

    public void StopWinding()
    {
        isBeingWound = false;
        audioSource.Stop();
    }

    private void FixedUpdate()
    {
        if (isBeingWound && isPoweredOn && !isGameOver)
        {
            float refillAmount = refillRate * Time.fixedDeltaTime;
            currentWindUpValue += refillAmount;
            currentWindUpValue = Mathf.Min(maxWindUpValue, currentWindUpValue);

            if (currentWindUpValue - lastRefillAmount >= refillSoundThreshold)
            {
                lastRefillAmount = currentWindUpValue;
                TriggerRefillSound();
            }

            if (currentWindUpValue > maxWindUpValue * 0.3f && hasGivenWarning)
            {
                hasGivenWarning = false;
                warningText.gameObject.SetActive(false);
            }
        }
    }

    private void TriggerRefillSound()
    {
        if (soundTrigger != null)
        {
            soundTrigger.TriggerSoundEffect();
            StartCoroutine(TriggerScreenScare());
        }
    }

    private IEnumerator TriggerScreenScare()
    {
        if (tvScreenMaterial == null) yield break;

        // 触发跳吓效果
        if (scareSound != null)
        {
            audioSource.PlayOneShot(scareSound);
        }

        tvScreenMaterial.SetFloat(ifChangeProperty, 1f);
        yield return new WaitForSeconds(scareDuration);
        tvScreenMaterial.SetFloat(ifChangeProperty, 0f);
    }

    private void OnPowerOn()
    {
        Debug.Log("发条盒通电，开始运转");
        
        // 启用灯光并设置初始亮度
        if (tvLight != null)
        {
            tvLight.enabled = true;
            tvLight.intensity = maxLightIntensity;
        }

        // 恢复屏幕材质
        if (tvScreenMaterial != null)
        {
            tvScreenMaterial.SetColor(emissionColorProperty, normalEmission);
        }

        // 启动环境音效
        if (ambientNoise != null && ambientSource != null)
        {
            ambientSource.Play();
        }
    }

    private void OnPowerOff()
    {
        Debug.Log("发条盒断电");
        StopWinding();
        SetPowerOffState(); // 使用统一断电状态设置
    }

    private void TriggerLowEnergyWarning()
    {
        hasGivenWarning = true;
        warningText.text = warningMessage;
        warningText.color = warningColor;
        warningText.gameObject.SetActive(true);

        if (lowEnergySound != null)
        {
            audioSource.PlayOneShot(lowEnergySound);
        }
    }

    private IEnumerator GameOverSequence()
    {
        isGameOver = true;
        StopWinding();
        ShowPrompt(false);

        if (powerDownSound != null)
        {
            audioSource.PlayOneShot(powerDownSound);
        }

        // 关闭所有视觉效果
        if (tvLight != null) tvLight.enabled = false;
        if (ambientSource != null) ambientSource.Stop();

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        Debug.Log("游戏结束 - 发条能量耗尽");
        yield return null;
    }

    private void UpdateUI()
    {
        if (windUpSlider != null)
        {
            windUpSlider.value = currentWindUpValue / maxWindUpValue;
        }
    }

    private void OnDestroy()
    {
        // 恢复材质初始状态
        if (tvScreenMaterial != null)
        {
            tvScreenMaterial.SetColor(emissionColorProperty, normalEmission);
            tvScreenMaterial.SetFloat(ifChangeProperty, 0f);
        }

        // 清理材质实例
        if (Application.isEditor && !Application.isPlaying && tvScreenMaterial != null)
        {
            DestroyImmediate(tvScreenMaterial);
        }
    }

    private void SetPowerOffState()
    {
        // 关闭灯光
        if (tvLight != null)
        {
            tvLight.intensity = 0f;
            tvLight.enabled = false;
        }

        // 设置屏幕材质为全黑
        if (tvScreenMaterial != null)
        {
            tvScreenMaterial.SetColor(emissionColorProperty, Color.black);
            tvScreenMaterial.SetFloat(ifChangeProperty, 0f);
        }

        // 确保音效关闭
        if (ambientSource != null)
        {
            ambientSource.Stop();
        }
    }

}
