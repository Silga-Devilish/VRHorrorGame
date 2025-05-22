using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Settings")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 10f;
    [SerializeField] private float crouchSpeed = 2.5f;
    [SerializeField] private float crouchYOffset = -0.5f; // 摄像头下移距离
    [SerializeField] private float crouchTransitionSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Look Settings")]
    [SerializeField] private float mouseSensitivity = 100f;
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform flashlightTransform; // 手电筒的Transform

    [Header("Flashlight Settings")]
    [SerializeField] private Light flashlight;
    [SerializeField] private KeyCode flashlightKey = KeyCode.Mouse1;

    private CharacterController controller;
    private Vector3 velocity;
    private float originalCameraY; // 摄像头原始Y位置
    private float targetCameraYOffset; // 目标摄像头Y偏移
    private float currentCameraYOffset; // 当前摄像头Y偏移
    private bool isCrouching = false;
    private bool isRunning = false;
    
    public bool IsRunning => isRunning;
    public bool IsCrouching => isCrouching;

        public bool IsMoving()
    {
        // 检查是否有输入或速度大于某个阈值
        return Mathf.Abs(Input.GetAxis("Horizontal")) > 0.1f || 
            Mathf.Abs(Input.GetAxis("Vertical")) > 0.1f ||
            controller.velocity.magnitude > 0.1f;
    }


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        originalCameraY = cameraTransform.localPosition.y;

        // 如果没指定手电筒Transform，尝试自动获取
        if (flashlightTransform == null && flashlight != null)
            flashlightTransform = flashlight.transform;

        // 初始化时将手电筒设为摄像头的子物体
        if (flashlightTransform != null && cameraTransform != null)
        {
            flashlightTransform.SetParent(cameraTransform);
            // 不再重置本地位置和旋转，保留编辑器中的设置
            // flashlightTransform.localPosition = Vector3.zero;
            // flashlightTransform.localRotation = Quaternion.identity;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

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
    }

    private void UpdateCrouchPosition()
    {
        // 平滑过渡摄像头位置
        currentCameraYOffset = Mathf.Lerp(
            currentCameraYOffset, 
            targetCameraYOffset, 
            crouchTransitionSpeed * Time.deltaTime
        );
        
        // 只更新摄像头位置，手电筒位置由父物体关系自动处理
        Vector3 cameraPos = cameraTransform.localPosition;
        cameraTransform.localPosition = new Vector3(
            cameraPos.x, 
            originalCameraY + currentCameraYOffset, 
            cameraPos.z
        );
        
        // 移除了对手电筒位置的直接控制
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

        // 左右旋转角色
        transform.Rotate(Vector3.up * mouseX);

        // 上下旋转相机
        Vector3 currentRotation = cameraTransform.localEulerAngles;
        float newXRotation = currentRotation.x - mouseY;
        
        // 限制上下视角范围
        newXRotation = Mathf.Clamp(newXRotation > 180 ? newXRotation - 360 : newXRotation, -90f, 90f);
        cameraTransform.localEulerAngles = new Vector3(newXRotation, 0f, 0f);
        
        // 手电筒现在会自动跟随摄像头旋转，因为它是摄像头的子物体
    }


    private void HandleFlashlight()
    {
        if (Input.GetKeyDown(flashlightKey) && flashlight != null)
        {
            flashlight.enabled = !flashlight.enabled;
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
        isRunning = Input.GetKey(KeyCode.LeftShift) && !isCrouching;
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
}