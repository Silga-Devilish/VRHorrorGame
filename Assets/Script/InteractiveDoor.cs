using UnityEngine;
using System.Collections;

[RequireComponent(typeof(BoxCollider))]
[RequireComponent(typeof(AudioSource))] // 添加音频组件需求
public class InteractiveDoor : MonoBehaviour
{
    [Header("门设置")]
    [SerializeField] private Transform doorPivot;
    [SerializeField] private float openAngle = 90f;
    [SerializeField] private float openSpeed = 2f;    // 开门速度
    [SerializeField] private float closeSpeed = 1.5f;  // 关门速度
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactionKey = KeyCode.Mouse0;

    [Header("音效设置")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] [Range(0,1)] private float soundVolume = 0.8f;
    [SerializeField] private float openSoundDelay = 0.1f;  // 开门音效延迟
    [SerializeField] private float closeSoundDelay = 0.05f; // 关门音效延迟

    [Header("UI设置")]
    [SerializeField] private GameObject interactionUI;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 initialRotation;
    private Vector3 targetRotation;
    private Transform player;
    private AudioSource audioSource;

    private void Awake()
    {
        if (doorPivot == null && transform.childCount > 0)
        {
            doorPivot = transform.GetChild(0);
            Debug.LogWarning("自动分配门扉Transform: " + doorPivot.name);
        }

        initialRotation = doorPivot.localEulerAngles;
        interactionUI.SetActive(false);
        
        // 获取或添加AudioSource组件
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        audioSource.spatialBlend = 1f; // 设置为3D音效
        audioSource.playOnAwake = false;

        // 设置触发碰撞体
        BoxCollider collider = GetComponent<BoxCollider>();
        collider.isTrigger = true;
        collider.size = new Vector3(2f, 2f, interactionDistance * 2f);
        collider.center = new Vector3(0, 1f, interactionDistance);
    }

    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        targetRotation = initialRotation + new Vector3(0, openAngle, 0);
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool isFacingDoor = IsPlayerFacingDoor();

        if (distance <= interactionDistance && isFacingDoor)
        {
            interactionUI.SetActive(true);
            
            if (Input.GetKeyDown(interactionKey))
            {
                TryInteractWithDoor();
            }
        }
        else
        {
            interactionUI.SetActive(false);
        }
    }

    private bool IsPlayerFacingDoor()
    {
        Vector3 toDoor = (transform.position - player.position).normalized;
        return Vector3.Dot(player.forward, toDoor) > 0.5f;
    }

    private void TryInteractWithDoor()
    {
        if (!isAnimating)
        {
            ToggleDoor();
        }
    }

    private void ToggleDoor()
    {
        if (isOpen)
        {
            StartCoroutine(RotateDoor(initialRotation, closeSpeed));
            PlayDoorSound(closeSound, closeSoundDelay);
        }
        else
        {
            StartCoroutine(RotateDoor(targetRotation, openSpeed));
            PlayDoorSound(openSound, openSoundDelay);
        }
        isOpen = !isOpen;
    }

    private IEnumerator RotateDoor(Vector3 targetRot, float speed)
    {
        isAnimating = true;
        
        while (Vector3.Distance(doorPivot.localEulerAngles, targetRot) > 0.1f)
        {
            doorPivot.localEulerAngles = Vector3.Lerp(
                doorPivot.localEulerAngles,
                targetRot,
                Time.deltaTime * speed
            );
            yield return null;
        }
        
        doorPivot.localEulerAngles = targetRot;
        isAnimating = false;
    }

    private void PlayDoorSound(AudioClip clip, float delay)
    {
        if (audioSource == null || clip == null) return;

        audioSource.clip = clip;
        audioSource.volume = soundVolume;
        audioSource.PlayDelayed(delay);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawWireCube(
            new Vector3(0, 1f, interactionDistance),
            new Vector3(2f, 2f, interactionDistance * 2f)
        );
    }
}
