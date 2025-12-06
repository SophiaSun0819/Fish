using UnityEngine;

public class camera_controller : MonoBehaviour
{


    public Transform target;        // 玩家（小魚）
    public Vector3 offset = new Vector3(0, 5, -10);
    public float smoothTime = 0.25f;

    private Vector3 velocity = Vector3.zero;

    void LateUpdate()
    {
        if (target == null) return;

        // 目標位置 = 玩家＋偏移量
        Vector3 targetPosition = target.position + offset;

        // 平滑移動攝影機到目標位置
        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref velocity,
            smoothTime
        );

        // 攝影機保持看向玩家
        transform.LookAt(target);
    }
}
