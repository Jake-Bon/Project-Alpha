using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

public class ThirdPersonController : MonoBehaviour
{
    public MovementType moveType = MovementType.Normal;
    public float gravity = 10.0f;

    float speed;

    [Header("Movement Speed")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 4.5f;
    [SerializeField] private float backwardSpeed = 1.5f;
    [SerializeField] private float turningSpeed = 150f;
    [SerializeField] private float mouseSensitivity = 10f;
    [SerializeField] private Transform spawnpoint;

    float horizontalInput;
    float verticalInput;

    //States
    bool isRunning;
    bool isGrounded;
    
    Player player;
    CharacterController characterController;
    CrowdHandler crowdHandler;
    GameObject[] gameCameraPositionList;
    GameObject gameCamera;
    GameObject prevGameCameraPos;
    GameObject currGameCameraPos;
    ChildBehavior child;
    
    bool cameraChangeFlag;
    bool hNewCamFlag; bool vNewCamFlag; bool HVNewCamFlag;

    // Start is called before the first frame update
    void Start()
    {
        player = GetComponent<Player>();
        characterController = GetComponent<CharacterController>();

        gameCamera = GameObject.Find("Camera");
        gameCameraPositionList = GameObject.FindGameObjectsWithTag("Positions");
        CopyTransform(gameCamera,gameCameraPositionList[0]);
        prevGameCameraPos = gameCameraPositionList[0];
        currGameCameraPos = gameCameraPositionList[0];
        GameObject childTest = GameObject.Find("Child");
        if(childTest!=null){
            child = childTest.GetComponent<ChildBehavior>();   
        }  
        crowdHandler = GetComponent<CrowdHandler>();
        Cursor.lockState = CursorLockMode.Locked;
        cameraChangeFlag = false;
        hNewCamFlag = false; vNewCamFlag = false; HVNewCamFlag = false;
    }

    // Update is called once per frame
    void Update()
    {
        isGrounded = CheckIfGrounded();

        if (!isGrounded) {
            Fall();
        }

        if (!player.stopInput) {
            HandleMovementInput();

            if (moveType==MovementType.TankWASD)
                ApplyTankMovementWASD();
            else if(moveType==MovementType.TankMouse)
                ApplyTankMovementMouse();
            else 
                ApplyNormalMovement();
        }

        if(gameObject.transform.position.y<-3.0f){
            transform.position = spawnpoint.position;
            if(child!=null){
                child.ResetChild();
            }
        }

    }

    private void HandleMovementInput() {
        isRunning = Input.GetKey(KeyCode.LeftShift); // change key later?

        if (isRunning){
            speed = runSpeed;
        }else{
            speed = walkSpeed;
        }
        if(child!=null)
            child.SetSpeed(speed);

        if(moveType==MovementType.TankMouse){
            horizontalInput = Input.GetAxisRaw("Mouse X");
            if(horizontalInput<0)
                horizontalInput-=Math.Abs(Input.GetAxis("Mouse Y")); //Adds all mouse movement
            else
                horizontalInput+=Math.Abs(Input.GetAxis("Mouse Y")); //Adds all mouse movement
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
    }
    
    float prevH;
    float prevV; 

    private void ApplyNormalMovement() {
        float h = horizontalInput;
        float v = verticalInput;

        Vector3 moveX = Vector3.one;
        Vector3 moveZ = Vector3.one;
        HVFlagTrap(h, prevH, v, prevV);
        if (cameraChangeFlag) {
            if (!HVNewCamFlag) {
                moveX = new Vector3(prevGameCameraPos.transform.right.x * h, 0, prevGameCameraPos.transform.right.z * h);
                moveZ = new Vector3(prevGameCameraPos.transform.up.x * v, 0, prevGameCameraPos.transform.up.z * v);
            } else {
                moveX = new Vector3(gameCamera.transform.right.x * h, 0, gameCamera.transform.right.z * h);
                moveZ = new Vector3(gameCamera.transform.up.x * v, 0, gameCamera.transform.up.z * v);
                cameraChangeFlag = false;
            }
        }
        else {
            moveX = new Vector3(gameCamera.transform.right.x * h, 0, gameCamera.transform.right.z * h);
            moveZ = new Vector3(gameCamera.transform.up.x * v, 0, gameCamera.transform.up.z * v);
        }
        Vector3 move = moveX + moveZ;
        move = Vector3.Normalize(move) * Time.deltaTime * speed;

        Vector3 moveTarget = new Vector3(move.x, 0, move.z);
        Transform lastCamPos = gameCamera.transform;

        //rotate player model in direction of movement
        if (moveTarget != Vector3.zero) {
            if(lastCamPos)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveTarget),
                        turningSpeed * 2 * Time.deltaTime);
        }
        characterController.Move(move);
        prevV = v;
        prevH = h;

        //previous version

        /*
        float h = horizontalInput;
        float v = verticalInput;

        Vector3 moveX = Vector3.one;
        Vector3 moveZ = Vector3.one;
        HFlagTrap(h, prevH);
        VFlagTrap(v, prevV);
        if (cameraChangeFlag) {
            Debug.Log (hNewCamFlag + ", " + vNewCamFlag);
            if (!hNewCamFlag) {
                moveX = new Vector3(prevGameCameraPos.transform.right.x * h, 0, prevGameCameraPos.transform.right.z * h);
            } else {
                moveX = new Vector3(gameCamera.transform.right.x * h, 0, gameCamera.transform.right.z * h);
            }
            if(!vNewCamFlag) {
                moveZ = new Vector3(prevGameCameraPos.transform.up.x * v, 0, prevGameCameraPos.transform.up.z * v);
            } else {   
                moveZ = new Vector3(gameCamera.transform.up.x * v, 0, gameCamera.transform.up.z * v);
            }
            //if player is not moving
            if (hNewCamFlag && vNewCamFlag) {
                cameraChangeFlag = false;
            }
        }
        else {
            moveX = new Vector3(gameCamera.transform.right.x * h, 0, gameCamera.transform.right.z * h);
            moveZ = new Vector3(gameCamera.transform.up.x * v, 0, gameCamera.transform.up.z * v);
        }
        Vector3 move = moveX + moveZ;
        move = Vector3.Normalize(move) * Time.deltaTime * speed;

        Vector3 moveTarget = new Vector3(move.x, 0, move.z);
        Transform lastCamPos = gameCamera.transform;

        //rotate player model in direction of movement
        if (moveTarget != Vector3.zero) {
            if(lastCamPos)
                transform.rotation = Quaternion.RotateTowards(transform.rotation,
                        Quaternion.LookRotation(moveTarget),
                        turningSpeed * 2 * Time.deltaTime);
        }
        characterController.Move(move);
        prevV = v;
        prevH = h;
        */
    }

    private bool CheckIfGrounded() {
        // Create layer mask that includes everything but the player
        int layerMask = (1 << LayerMask.NameToLayer("Player"));
        layerMask = ~layerMask;

        Vector3 castPostition = gameObject.transform.position - new Vector3(0, 0.6f, 0.0f);

        return Physics.CheckSphere(castPostition, 0.5f, layerMask);
    }

    private void Fall() {
        float gravity = 5.0f;
        float y = characterController.velocity.y;

        y -= gravity * Time.deltaTime;

        Vector3 fall = new Vector3(0, y, 0);

        characterController.Move(fall);
    }

    public void ChangeCamera(int choice){
        cameraChangeFlag = true;
        hNewCamFlag = false;
        vNewCamFlag = false;
        HVNewCamFlag = false;
        prevGameCameraPos = currGameCameraPos;
        currGameCameraPos = gameCameraPositionList[choice];
        CopyTransform(gameCamera,currGameCameraPos);
        
    }

    public void CopyTransform(GameObject a, GameObject b){
        Vector3 posA = a.transform.position;
        Vector3 posB = b.transform.position;

        posA.x = posB.x;
        posA.y = posB.y;
        posA.z = posB.z;

        a.transform.position = posA;

        Vector3 rotB = new Vector3(b.transform.eulerAngles.x, b.transform.eulerAngles.y, b.transform.eulerAngles.z);
        a.transform.eulerAngles = rotB;
    }

    public enum MovementType {Normal,TankWASD,TankMouse};

    private void HFlagTrap(float curr, float prev) {
        //if player is not moving or changed directions
        if(curr == 0 || curr != prev)
            hNewCamFlag =  true;
    }

    private void VFlagTrap(float curr, float prev) {
        //if player is not moving or changed directions
        if(curr == 0 || curr != prev)
            vNewCamFlag = true;
    }

    private void HVFlagTrap(float hCurr, float hPrev, float vCurr, float vPrev) {
        //if player changes any directino, update now 
        if(hCurr != hPrev || vCurr != vPrev)
            HVNewCamFlag = true;
    }

    public float GetSpeed(){
        return speed;
    }

}
