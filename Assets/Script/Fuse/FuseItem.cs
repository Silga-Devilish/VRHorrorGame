using UnityEngine;

public class FuseItem : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        // 添加多重安全检查
        if (other == null) return;
        if (!other.CompareTag("Player")) return;
        
        PlayerInventory inventory = other.GetComponent<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("玩家对象缺少PlayerInventory组件");
            return;
        }

        inventory.AddFuse();
        Destroy(gameObject);
        
        Debug.Log("保险丝已拾取");
    }
}
