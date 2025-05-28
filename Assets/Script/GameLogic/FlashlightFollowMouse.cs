using UnityEngine;

public class FlashlightFollowMouse : MonoBehaviour
{
    [Header("手电设置")]
    [SerializeField] private Light spotlight;  // 绑定的Spotlight组件
    [SerializeField] private float maxDistance = 10f;  // 最大照射距离
    [SerializeField] private float smoothSpeed = 5f;   // 跟随平滑度

    [Header("闪烁设置")] 
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 1.5f;
    [SerializeField] private float flickerSpeed = 10f;
    [SerializeField] private float flickerVariance = 0.2f; // 闪烁随机性

    private Vector3 targetPoint;
    private float baseIntensity;

    private void Start()
    {
        Cursor.visible = false; // 隐藏系统鼠标
        baseIntensity = spotlight.intensity;
    }

    private void Update()
    {
        FollowMouse();
        FlickerLight();
    }

    private void FollowMouse()
    {
        // 获取鼠标在屏幕上的位置
        Vector3 mousePos = Input.mousePosition;
        mousePos.z = maxDistance; // 设置Z坐标为照射距离
        
        // 转换为世界坐标
        targetPoint = Camera.main.ScreenToWorldPoint(mousePos);
        
        // 平滑旋转指向目标点
        Vector3 direction = (targetPoint - transform.position).normalized;
        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation, 
            targetRotation, 
            smoothSpeed * Time.deltaTime
        );
    }

    private void FlickerLight()
    {
        // 基础闪烁（正弦波）
        float flicker = Mathf.Sin(Time.time * flickerSpeed);
        
        // 添加随机性
        flicker += Random.Range(-flickerVariance, flickerVariance);
        
        // 计算最终强度
        float intensity = baseIntensity * Mathf.Lerp(
            minIntensity, 
            maxIntensity, 
            (flicker + 1f) * 0.5f  // 将-1~1映射到0~1
        );
        
        spotlight.intensity = intensity;
    }

    // 可视化调试（在Scene视图显示照射方向）
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, targetPoint);
        Gizmos.DrawWireSphere(targetPoint, 0.1f);
    }
}
