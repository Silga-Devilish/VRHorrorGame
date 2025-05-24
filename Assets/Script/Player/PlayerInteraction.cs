using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private float repairTime = 3f;
    
    [Header("UI设置")]
    [SerializeField] private GameObject repairProgressUI;
    [SerializeField] private Image repairProgressBar;
    [SerializeField] private TextMeshProUGUI repairPromptText;
    
    private Camera playerCamera;
    private LightSwitchController currentSwitch;
    private FuseBox currentFuseBox;
    private float repairTimer;
    private bool isRepairing;
    private PlayerInventory inventory;

    private void Awake()
    {
        playerCamera = Camera.main;
        inventory = GetComponent<PlayerInventory>();
        repairProgressUI.SetActive(false);
    }

    private void Update()
    {
        CheckLookingAtInteractable();
        
        HandleRepairInteraction();
        
        HandleSwitchInteraction();
    }

    private void CheckLookingAtInteractable()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer))
        {
            LightSwitchController switchCtrl = hit.collider.GetComponent<LightSwitchController>();
            FuseBox fuseBox = hit.collider.GetComponent<FuseBox>();
            
            if (switchCtrl != currentSwitch || fuseBox != currentFuseBox)
            {
                ClearCurrentInteractable();
                
                if (switchCtrl != null)
                {
                    currentSwitch = switchCtrl;
                    currentSwitch.ShowPrompt(true);
                }
                else if (fuseBox != null)
                {
                    currentFuseBox = fuseBox;
                    currentFuseBox.ShowPrompt(true);
                }
            }
        }
        else
        {
            ClearCurrentInteractable();
        }
    }

    private void ClearCurrentInteractable()
    {
        if (currentSwitch != null) currentSwitch.ShowPrompt(false);
        if (currentFuseBox != null) currentFuseBox.ShowPrompt(false);
        
        currentSwitch = null;
        currentFuseBox = null;
        ResetRepair();
    }

    private void HandleRepairInteraction()
    {
        if (currentFuseBox == null) return;
        
        if (Input.GetKeyDown(interactKey) && inventory.HasFuse && !currentFuseBox.IsRepaired)
        {
            currentFuseBox.StartRepairingSound(); // 新增：开始修理音效
            StartRepair();
        }
        
        if (isRepairing)
        {
            if (Input.GetKey(interactKey))
            {
                repairTimer += Time.deltaTime;
                repairProgressBar.fillAmount = repairTimer / repairTime;
                
                if (repairTimer >= repairTime)
                {
                    CompleteRepair();
                }
            }
            else
            {
                currentFuseBox.StopRepairingSound(); // 新增：停止修理音效
                ResetRepair();
            }
        }
    }

    private void StartRepair()
    {
        isRepairing = true;
        repairProgressUI.SetActive(true);
        repairPromptText.text = "修理中...";
    }

    private void CompleteRepair()
    {
        currentFuseBox.Repair();
        inventory.UseFuse();
        ResetRepair();
    }

    private void ResetRepair()
    {
        isRepairing = false;
        repairTimer = 0f;
        repairProgressBar.fillAmount = 0f;
        repairProgressUI.SetActive(false);
    }

    private void HandleSwitchInteraction()
    {
        if (currentSwitch != null && Input.GetKeyDown(interactKey))
        {
            currentSwitch.InteractWithSwitch();
        }
    }
}
