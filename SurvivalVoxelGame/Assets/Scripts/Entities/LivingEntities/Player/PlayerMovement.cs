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

    ///Start function currently used for disabling the cursor
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

    //The main movement call.
    ///The main movement call currently made to make the player fly in order to view the world.
    public void Move()
    {
        ///Getting the inputs
        Vector3 movement = new Vector3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        movement.y = Input.GetButton("Jump") ? 1 : Input.GetButton("Fire1") ? -1 : 0;
        
        ///Setting the movement through a lerp for a smooth effect.
        activeMovement = Vector3.Lerp(activeMovement, movement, Time.deltaTime * lerpSpeed);

        ///Applying the movement to the object.
        transform.Translate(transform.TransformDirection(activeMovement) * Time.deltaTime * speed, Space.World);
    }

    //Rotating
    public void Rotate()
    {
        ///Rotates the player Horizontally
        transform.Rotate(Vector3.up * mouseSensitivity * Input.GetAxis("Mouse X"));
        ///Rotates the camera Vertically
        cam.transform.Rotate(Vector3.right * mouseSensitivity * -Input.GetAxis("Mouse Y"));
    }
}
