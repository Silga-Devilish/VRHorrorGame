using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("交互设置")]
    [SerializeField] private float interactionDistance = 2f;
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private LayerMask interactableLayer;
    
    private Camera playerCamera;
    private LightSwitchController currentSwitch;

    private void Awake()
    {
        playerCamera = Camera.main;
    }

    private void Update()
    {
        CheckLookingAtSwitch();
        
        if (Input.GetKeyDown(interactKey) && currentSwitch != null)
        {
            currentSwitch.InteractWithSwitch();
        }
    }

    private void CheckLookingAtSwitch()
    {
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        bool hitSwitch = Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableLayer);
        
        if (hitSwitch)
        {
            LightSwitchController newSwitch = hit.collider.GetComponent<LightSwitchController>();
            if (newSwitch != currentSwitch)
            {
                if (currentSwitch != null) currentSwitch.ShowPrompt(false);
                currentSwitch = newSwitch;
                currentSwitch.ShowPrompt(true);
            }
        }
        else if (currentSwitch != null)
        {
            currentSwitch.ShowPrompt(false);
            currentSwitch = null;
        }
    }
}
