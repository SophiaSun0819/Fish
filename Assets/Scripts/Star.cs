using UnityEngine;

public class Star : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public int pointValue = 1;
    public float rotationSpeed = 50f;
    public float floatSpeed = 1f;
    public float floatAmount = 0.5f;

    private Vector3 startPos;

    void Start()
    {
        startPos = transform.position;
    }

    void Update()
    {
        // 旋轉效果
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        // 上下浮動效果
        float newY = startPos.y + Mathf.Sin(Time.time * floatSpeed) * floatAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
    }
}
