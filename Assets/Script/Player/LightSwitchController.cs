using UnityEngine;
using TMPro;
using System.Collections;

public class LightSwitchController : MonoBehaviour
{
    [Header("灯光设置")]
    [SerializeField] private Light[] controlledLights;
    [SerializeField] private float switchCooldown = 0.5f;
    
    [Header("音效设置")]
    [SerializeField] private AudioClip switchSound;
    [SerializeField] private AudioClip deniedSound;
    [SerializeField] private float switchVolume = 0.7f;
    
    [Header("UI提示")]
    [SerializeField] private GameObject interactionPrompt;
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private string switchOnText = "按 E 开灯";
    [SerializeField] private string switchOffText = "按 E 关灯";
    [SerializeField] private string needFuseText = "请先修复保险丝";
    
    [Header("调试设置")]
    [SerializeField] private bool startWithFuseBroken = true;
    [SerializeField] private bool debugRepairFuse = false; // 调试用开关

    private AudioSource audioSource;
    private bool isOn = true;
    private bool canInteract = true;
    private bool needsFuseRepair;
    private bool playerInRange;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f;
        }

        needsFuseRepair = startWithFuseBroken;
        UpdatePromptVisibility(false);
    }

    private void Update()
    {
        if (debugRepairFuse)
        {
            debugRepairFuse = false;
            RepairFuse();
        }
    }

    public void InteractWithSwitch()
    {
        if (!canInteract) return;
        
        if (needsFuseRepair)
        {
            audioSource.PlayOneShot(deniedSound, switchVolume);
            return;
        }

        StartCoroutine(SwitchCooldown());
        ToggleLights();
    }

    private void ToggleLights()
    {
        isOn = !isOn;
        
        foreach (Light light in controlledLights)
        {
            light.enabled = isOn;
        }
        
        audioSource.PlayOneShot(switchSound, switchVolume);
        UpdatePromptText();
    }

    private IEnumerator SwitchCooldown()
    {
        canInteract = false;
        yield return new WaitForSeconds(switchCooldown);
        canInteract = true;
    }

    public void RepairFuse()
    {
        needsFuseRepair = false;
        audioSource.PlayOneShot(switchSound, switchVolume);
        UpdatePromptText();
    }

    public void ShowPrompt(bool show)
    {
        playerInRange = show;
        UpdatePromptVisibility(show);
    }

    private void UpdatePromptVisibility(bool show)
    {
        if (interactionPrompt != null)
        {
            interactionPrompt.SetActive(show && (playerInRange || needsFuseRepair));
        }
        if (show) UpdatePromptText();
    }

    private void UpdatePromptText()
    {
        if (promptText == null) return;
        
        promptText.text = needsFuseRepair ? needFuseText : 
                         (isOn ? switchOffText : switchOnText);
    }
}
