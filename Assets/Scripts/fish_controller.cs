using UnityEngine;

public class fish_controller : MonoBehaviour
{
    public float speed = 5f;
    public float turnSpeed = 90f;
    public float diveHeight = 10f;
    public Rigidbody rigidbody;
    public float jumpower = 10f;

    private Vector2 input;

    public float diveSpeed = 3f;          // 潛水速度
    public float floatUpSpeed = 2f;       // 平常上浮速度
    public float waterSurfaceY = 0f;      // 水面高度
    public float maxDiveDepth = -10f;     // 最大深度

    [Header("Jump Out Settings")]
    public float jumpOutHeight = 1.5f;    // 跳出水面高度
    public float jumpOutSpeed = 6f;       // 往上彈出的速度
    public float gravity = 10f;           // 回落速度（重力感）

    private bool wasDiving = false;       // 用來偵測 "剛放開空白鍵"
    private float verticalVelocity = 0f;


    public AudioSource jumpSFX;


    public Transform body;

    public float stretchIntensity = 0.15f; // 速度影響拉伸程度
    public float turnSquashIntensity = 0.1f; // 轉彎壓扁程度
    public float smooth = 10f; // 平滑程度

    Vector3 baseScale;



    private void Start()
    {
        baseScale = body.localScale;
    }

    void Update()
    {

        //ApplySquashStretch();

        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // move forward&backward
        transform.position += transform.forward * input.y * speed * Time.deltaTime;

        // change direction
        transform.Rotate(Vector3.up, input.x * turnSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.LeftShift))
        {
            speed = 10f;
        }
        else speed = 5f;

        //dive&jump
        /*
        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position -= transform.up * Time.deltaTime * diveHeight;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            transform.position += transform.up * Time.deltaTime * diveHeight;

            rigidbody.AddForce(Vector3.up * jumpower);
        }
        */

        

  


        //潛水+跳出功能
        bool isDiving = Input.GetKey(KeyCode.Space);
        Vector3 pos = transform.position;

        // 1. ➤ 潛水階段
        if (isDiving)
        {
            pos.y -= diveSpeed * Time.deltaTime;

            if (pos.y < maxDiveDepth)
                pos.y = maxDiveDepth;

            wasDiving = true;
            verticalVelocity = 0; // 潛水時不累積往上的速度
        }
        else
        {
            // 2. ➤ 剛放開空白鍵 → 給一個往上衝的速度
            if (wasDiving)
            {
                verticalVelocity = jumpOutSpeed; // 往上衝
                wasDiving = false;

                //跳出水面音效
                jumpSFX.Play();
            }

            // 3. ➤ 若有跳出速度 → 執行跳出 + 重力
            if (verticalVelocity > 0)
            {
                pos.y += verticalVelocity * Time.deltaTime;
                verticalVelocity -= gravity * Time.deltaTime;

                // 限制最高跳出位置
                if (pos.y >= waterSurfaceY + jumpOutHeight)
                {
                    pos.y = waterSurfaceY + jumpOutHeight;
                    verticalVelocity = 0; // 到頂就往下掉
                }
            }
            else
            {
                // 4. ➤ 已經跳完、慢慢回到水面
                pos.y -= gravity * Time.deltaTime;

                if (pos.y <= waterSurfaceY)
                {
                    pos.y = waterSurfaceY;
                    verticalVelocity = 0;
                }
            }
        }



        void ApplySquashStretch()
        {
            float forwardAmount = Mathf.Abs(input.y);
            float turnAmount = Mathf.Abs(input.x);

            // 速度造成前後拉長
            float stretch = 1 + forwardAmount * stretchIntensity;

            // 轉彎造成左右壓扁
            float squash = 1 - turnAmount * turnSquashIntensity;

            // 合成目標 scale
            Vector3 targetScale = new Vector3(
                baseScale.x * squash,
                baseScale.y,
                baseScale.z * stretch
            );



            // 平滑插值，讓魚有「果凍 / QQ」的動態
            body.localScale = Vector3.Lerp(body.localScale, targetScale, Time.deltaTime * smooth);
        }

        transform.position = pos;

    }
}
