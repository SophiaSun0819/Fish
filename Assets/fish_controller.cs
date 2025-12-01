using UnityEngine;

public class fish_controller : MonoBehaviour
{
    public float speed = 5f;
    public float turnSpeed = 90f;
    public float diveHeight = 10f;
    public Rigidbody rigidbody;
    public float jumpower = 10f;

    private Vector2 input;

    void Update()
    {
        input = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));

        // move forward&backward
        transform.position += transform.forward * input.y * speed * Time.deltaTime;

        // change direction
        transform.Rotate(Vector3.up, input.x * turnSpeed * Time.deltaTime);

        //dive&jump

        if (Input.GetKeyDown(KeyCode.Space))
        {
            transform.position -= transform.up * Time.deltaTime * diveHeight;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            transform.position += transform.up * Time.deltaTime * diveHeight;

            rigidbody.AddForce(Vector3.up * jumpower);
        }

    }
}
