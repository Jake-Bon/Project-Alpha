using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public MovementType moveType = MovementType.TankMouse;
    public Animator anim;

    float speed;

    [Header("Movement Speed")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float backwardSpeed = 1.5f;
    [SerializeField] private float turningSpeed = 1f;
    [SerializeField] private float mouseSensitivity = 10f;
    [SerializeField] private float gravity = 20.0f;

    float horizontalInput;
    float verticalInput;

    //States
    bool isWalking;
    bool isRunning;
    bool isGrounded;
    
    CharacterController characterController;
    [SerializeField] private GameObject gameCamera;

    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        HandleMovementInput();
        if (moveType==MovementType.TankWASD)
            ApplyTankMovementWASD();
        else if(moveType==MovementType.TankMouse)
            ApplyTankMovementMouse();
        else 
            ApplyNormalMovement();
    }

    private void HandleMovementInput() {
        isRunning = Input.GetKey(KeyCode.LeftShift); // change key later?

        if (isRunning){
            speed = runSpeed;
        }else{
            speed = walkSpeed;
        }

        if(moveType==MovementType.TankMouse){
            horizontalInput = Input.GetAxisRaw("Mouse X");
            if(horizontalInput<0)
                horizontalInput-=Mathf.Abs(Input.GetAxis("Mouse Y")); //Adds all mouse movement
            else
                horizontalInput+=Mathf.Abs(Input.GetAxis("Mouse Y")); //Adds all mouse movement
        }else{
            horizontalInput = Input.GetAxisRaw("Horizontal");
        }
        verticalInput = Input.GetAxisRaw("Vertical");

        horizontalInput = Mathf.Clamp(horizontalInput, -1, 1);
        verticalInput = Mathf.Clamp(verticalInput, -1, 1);

        
    }

    private void ApplyTankMovementMouse(){
        if (verticalInput < 0)
            speed = backwardSpeed;
        float h = horizontalInput * Time.deltaTime * turningSpeed * mouseSensitivity;
        float v = verticalInput * Time.deltaTime * speed;
        
        Vector3 move = new Vector3(0,0,v);
        move = transform.TransformDirection(move);
        characterController.Move(move);

        Vector3 turn = new Vector3(0,h,0);
        transform.Rotate(turn);
    }

    private void ApplyTankMovementWASD() {
        if (verticalInput < 0)
            speed = backwardSpeed;
        float h = horizontalInput * Time.deltaTime * turningSpeed;
        float v = verticalInput * Time.deltaTime * speed;
        
        Vector3 move = new Vector3(0,0,v);
        move = transform.TransformDirection(move);
        characterController.Move(move);

        Vector3 turn = new Vector3(0,h,0);
        transform.Rotate(turn);
        
        if (anim != null) {
            anim.SetFloat("speed", move.magnitude);
        }
        //Debug.Log(move.magnitude);
    }

    Vector3 move;

    private void ApplyNormalMovement() {
        float h = horizontalInput;
        float v = verticalInput;

        float gravityValue = move.y; // dont include gravity in normalization
        Vector3 yDir = new Vector3(
            Mathf.Cos(gameCamera.transform.rotation.x) * gameCamera.transform.forward.x
                + Mathf.Sin(gameCamera.transform.rotation.x) * gameCamera.transform.up.x,
            0,
            Mathf.Cos(gameCamera.transform.rotation.x) * gameCamera.transform.forward.z
                + Mathf.Sin(gameCamera.transform.rotation.x) * gameCamera.transform.up.z);

        Vector3 moveX = new Vector3(gameCamera.transform.right.x * h, 0, gameCamera.transform.right.z * h);
        Vector3 moveZ = new Vector3(yDir.x * v, 0, yDir.z * v);
        //Vector3 moveZ = new Vector3(gameCamera.transform.forward.x * v, 0, gameCamera.transform.forward.z * v); //.up
        move = Vector3.Normalize(moveX + moveZ) * speed;

        move.y = gravityValue; // reapply previous gravity value

        Vector3 moveTarget = new Vector3(move.x, 0, move.z);
        Transform lastCamPos = gameCamera.transform;

        if(!characterController.isGrounded) { move.y -= gravity * Time.deltaTime; }
        else                                { move.y = -.5f; }

        //rotate player model in direction of movement
        if (moveTarget != Vector3.zero) {
            if(lastCamPos)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveTarget),
                        turningSpeed * 2 * Time.deltaTime);
        }
        characterController.Move(move * Time.deltaTime);

        if (anim != null) {
            anim.SetFloat("speed", move.magnitude);
        }
        //Debug.Log(move.magnitude);
    }

    public enum MovementType {Normal,TankWASD,TankMouse};

}
