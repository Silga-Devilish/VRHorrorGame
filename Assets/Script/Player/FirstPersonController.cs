using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(AudioSource))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchYOffset = -0.5f;
    [SerializeField] private float crouchTransitionSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform flashlightTransform;

    [Header("Head Bobbing Settings")]
    [SerializeField] private float walkBobSpeed = 14f;
    [SerializeField] private float walkBobAmount = 0.05f;
    [SerializeField] private float runBobSpeed = 18f;
    [SerializeField] private float runBobAmount = 0.1f;
    [SerializeField] private float crouchBobSpeed = 10f;
    [SerializeField] private float crouchBobAmount = 0.025f;
    [SerializeField] private float bobResetSpeed = 3f;

    [Header("Footstep Sounds")]
    [SerializeField] private AudioClip[] walkFootsteps;
    [SerializeField] private AudioClip[] runFootsteps;
    [SerializeField] private AudioClip[] crouchFootsteps;
    [SerializeField] private float walkStepInterval = 0.5f;
    [SerializeField] private float runStepInterval = 0.3f;
    [SerializeField] private float crouchStepInterval = 0.7f;
    [SerializeField] private float walkVolume = 0.5f;
    [SerializeField] private float runVolume = 0.7f;
    [SerializeField] private float crouchVolume = 0.3f;

    [Header("体力设置")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainRate = 20f; // 跑步时每秒消耗的体力
    [SerializeField] private float staminaRegenRate = 15f; // 恢复时每秒恢复的体力
    [SerializeField] private float staminaRegenDelay = 2f; // 停止跑步后开始恢复的延迟时间
    [SerializeField] private Image staminaBar; // 体力条UI

    [Header("手电筒设置")]
    [SerializeField] private Light flashlight;
    [SerializeField] private KeyCode flashlightKey = KeyCode.Mouse1;
    [SerializeField] private AudioClip flashlightSound; // 新增：手电筒开关音效
    [SerializeField] private float flashlightVolume = 0.5f; // 新增：手电筒音效音量

    private CharacterController controller;
    private AudioSource audioSource;
    private Vector3 velocity;
    private float originalCameraY;
    private float targetCameraYOffset;
    private float currentCameraYOffset;
    private bool isCrouching = false;
    private bool isRunning = false;
    private float defaultYPos;
    private float timer;
    private float stepCycle;
    private float nextStep;
    private float currentStamina;
    private float lastRunTime;
    private bool canRun = true;
    private AudioSource flashlightAudioSource;

    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;
    public bool IsMoving => (Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f ||
                           Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f) &&
                           controller.isGrounded;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        audioSource = GetComponent<AudioSource>();
        originalCameraY = cameraTransform.localPosition.y;
        defaultYPos = cameraTransform.localPosition.y;

        if (flashlightTransform == null && flashlight != null)
            flashlightTransform = flashlight.transform;

        if (flashlightTransform != null && cameraTransform != null)
        {
            flashlightTransform.SetParent(cameraTransform);
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        currentStamina = maxStamina;
        UpdateStaminaUI();

            // 新增：初始化手电筒音效源
        flashlightAudioSource = gameObject.AddComponent<AudioSource>();
        flashlightAudioSource.spatialBlend = 0f; // 2D音效
        flashlightAudioSource.playOnAwake = false;
        flashlightAudioSource.volume = flashlightVolume;

        if (flashlight != null)
            flashlight.enabled = false;


    }

    private void Update()
    {
        HandleMovement();
        HandleMouseLook();
        HandleFlashlight();
        HandleCrouch();
        HandleRun();
        ApplyGravity();
        UpdateCrouchPosition();
        HandleStamina();

        if (IsMoving)
        {
            HandleHeadBob();
            ProgressStepCycle();
        }
        else
        {
            ResetHeadPosition();
        }
    }

    private void HandleHeadBob()
    {
        float speed = isCrouching ? crouchBobSpeed : (isRunning ? runBobSpeed : walkBobSpeed);
        float amount = isCrouching ? crouchBobAmount : (isRunning ? runBobAmount : walkBobAmount);

        timer += Time.deltaTime * speed;
        Vector3 cameraPos = cameraTransform.localPosition;
        cameraTransform.localPosition = new Vector3(
            cameraPos.x,
            defaultYPos + currentCameraYOffset + Mathf.Sin(timer) * amount,
            cameraPos.z
        );
    }

    private void ResetHeadPosition()
    {
        Vector3 cameraPos = cameraTransform.localPosition;
        float newY = Mathf.Lerp(cameraPos.y, defaultYPos + currentCameraYOffset, bobResetSpeed * Time.deltaTime);
        cameraTransform.localPosition = new Vector3(
            cameraPos.x,
            newY,
            cameraPos.z
        );
    }

    private void ProgressStepCycle()
    {
        float speed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);
        stepCycle += (controller.velocity.magnitude + speed) * Time.deltaTime;

        if (stepCycle > nextStep)
        {
            nextStep = stepCycle + (isCrouching ? crouchStepInterval :
                                  (isRunning ? runStepInterval : walkStepInterval));
            PlayFootstepAudio();
        }
    }

    private void PlayFootstepAudio()
    {
        if (!controller.isGrounded) return;

        AudioClip[] clips = isCrouching ? crouchFootsteps :
                           (isRunning ? runFootsteps : walkFootsteps);
        float volume = isCrouching ? crouchVolume :
                      (isRunning ? runVolume : walkVolume);

        if (clips.Length > 0)
        {
            int index = Random.Range(0, clips.Length);
            audioSource.clip = clips[index];
            audioSource.volume = volume;
            audioSource.Play();
        }
    }

    private void UpdateCrouchPosition()
    {
        currentCameraYOffset = Mathf.Lerp(
            currentCameraYOffset,
            targetCameraYOffset,
            crouchTransitionSpeed * Time.deltaTime
        );

        Vector3 cameraPos = cameraTransform.localPosition;
        cameraTransform.localPosition = new Vector3(
            cameraPos.x,
            originalCameraY + currentCameraYOffset,
            cameraPos.z
        );
    }

    private void HandleMovement()
    {
        float currentSpeed = isCrouching ? crouchSpeed : (isRunning ? runSpeed : walkSpeed);

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        Vector3 move = transform.right * x + transform.forward * z;
        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        Vector3 currentRotation = cameraTransform.localEulerAngles;
        float newXRotation = currentRotation.x - mouseY;

        newXRotation = Mathf.Clamp(newXRotation > 180 ? newXRotation - 360 : newXRotation, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(newXRotation, 0f, 0f);
    }

    private void HandleFlashlight()
    {
        if (Input.GetKeyDown(flashlightKey))
        {
            if (flashlight != null)
            {
                flashlight.enabled = !flashlight.enabled;
                PlayFlashlightSound();
            }
        }
    }

        // 新增：播放手电筒音效方法
    private void PlayFlashlightSound()
    {
        if (flashlightSound != null && flashlightAudioSource != null)
        {
            flashlightAudioSource.PlayOneShot(flashlightSound);
        }
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            targetCameraYOffset = crouchYOffset;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            targetCameraYOffset = 0f;
        }
    }

    private void HandleRun()
    {
        bool wantToRun = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
        
        // 只有在有体力时才能跑步
        isRunning = wantToRun && canRun;
    }


    private void ApplyGravity()
    {
        if (controller.isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void HandleStamina()
    {
        // 跑步时消耗体力
        if (isRunning && canRun)
        {
            currentStamina -= staminaDrainRate * Time.deltaTime;
            lastRunTime = Time.time;

            // 体力耗尽
            if (currentStamina <= 0)
            {
                currentStamina = 0;
                canRun = false;
                isRunning = false; // 强制停止跑步
            }
        }
        // 恢复体力
        else if (Time.time - lastRunTime > staminaRegenDelay && currentStamina < maxStamina)
        {
            currentStamina += staminaRegenRate * Time.deltaTime;
            if (currentStamina >= maxStamina)
            {
                currentStamina = maxStamina;
                canRun = true;
            }
        }

        UpdateStaminaUI();
    }

    private void UpdateStaminaUI()
    {
        if (staminaBar != null)
        {
            staminaBar.fillAmount = currentStamina / maxStamina;
        }
    }
    
    
}
