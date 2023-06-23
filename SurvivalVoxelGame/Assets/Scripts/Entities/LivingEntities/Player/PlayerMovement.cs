using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float speed;
    public float lerpSpeed;
    public float mouseSensitivity;
    public Transform cam;

    private Vector3 activeMovement = Vector3.zero;

    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void Update()
    {
        Rotate();
        Move();
    }

    public void Move()
    {
        Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        movement.y = Input.GetButton("Jump") ? 1 : Input.GetButton("Fire1") ? -1 : 0;

        activeMovement = Vector3.Lerp(activeMovement, movement, Time.deltaTime * lerpSpeed);

        transform.Translate(transform.TransformDirection(activeMovement) * Time.deltaTime * speed, Space.World);
    }

    public void Rotate()
    {
        transform.Rotate(Vector3.up * mouseSensitivity * Input.GetAxis("Mouse X"));
        cam.transform.Rotate(Vector3.right * mouseSensitivity * -Input.GetAxis("Mouse Y"));
    }
}
