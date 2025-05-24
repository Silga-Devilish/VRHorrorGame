using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public bool HasFuse { get; private set; }
    
    public void AddFuse()
    {
        HasFuse = true;
        Debug.Log("获得保险丝");
    }
    
    public void UseFuse()
    {
        HasFuse = false;
    }
}
