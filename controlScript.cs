using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class controlScript : MonoBehaviour
{

    // SCRIPT IS NOT FINAL: dummy variables exist for possible future implementation

    public CharacterController controller;
    public Camera mainCam;
    public Sprite[] sprites = new Sprite[3];
    public SpriteRenderer heroSprite;
    public float speed = 6f;
    public float jumpForce = 5f;
    public float gravity = 0.01f;
    public float _directionY;
    public int truDir;
    public Vector3 trueForward = new Vector3(0, 0, 0);
    public Vector3 cameraForward;
    public Vector3 frontDir;
    public Vector3 rightDir;
    private Animator animator;
    float horizontal;
    float vertical;
    public int fpsSet;
    public bool slow;
    private string currentAnimation;
    private int number_of_slow_colliders;
    private bool small;
    public bool auto_move;
    public Vector3 auto_dir;
    public bool fade_to_black;
    public Vector3 snap_pos;
    public byte transition_frames;
    public Transform migrate_point;
    public bool transition;
    private bool makeParent;
    public Vector3 force_move;

    public GameObject textBox;
    public GameObject text;
    public typeWriter yeah;
    public GameObject descrBox;
    public GameObject descr;
    public DescriptionBox descrType;
    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = fpsSet;
        animator = GetComponent<Animator>();
        yeah = text.GetComponent<typeWriter>();
        auto_move = true;
        fade_to_black = false;
        snap_pos = Vector3.zero;
        transition_frames = 0;
        transition = false;
        makeParent = false;
    }

    // Update is called once per frame

    private void OnTriggerEnter(Collider other)
    {
        if (other.transform.gameObject.layer == 10)
        {
            number_of_slow_colliders += 1;
            slow = true;
        }
        else if (other.transform.gameObject.layer == 6)
        {
            small = true;
            transform.localScale = new Vector3(0.1027944f, 0.09123003f, 0.1027944f);
            heroSprite.color = new Color(0.3453399f, 0.3660251f, 0.6377357f);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.transform.gameObject.layer == 10)
        {
            number_of_slow_colliders -= 1;
            if (number_of_slow_colliders == 0)
                slow = false;
        }
    }

    void Update()
    {
        cameraForward = mainCam.transform.forward;

        // BASIC MOVEMENT
        horizontal = Input.GetAxisRaw("Horizontal");  //returns -1 if you press A/left, 0 if neutral, 1 if D/right
        vertical = Input.GetAxisRaw("Vertical");

        var camF = mainCam.transform.forward;
        var camR = mainCam.transform.right;
        camF.y = 0;
        camR.y = 0;
        camF.Normalize();
        camR.Normalize();

        var direction = camF * vertical + camR * horizontal;

        if (auto_move)
        {
            direction = auto_dir;
        }
        frontDir = camF;
        rightDir = camR;
        //Debug.Log(direction);

        Debug.DrawRay(transform.position, trueForward * 10, Color.white);

        Physics.SphereCast(transform.position, controller.bounds.extents.x, Vector3.down, out RaycastHit hitInfo);

        if (hitInfo.collider)
        {
            // This fixes the overly-precise normal returns given by the spherecast
            Physics.Raycast(transform.position, hitInfo.point - transform.position, out hitInfo);
            Debug.DrawLine(transform.position, hitInfo.point, Color.white);
        }

        direction = Quaternion.FromToRotation(Vector3.up, hitInfo.normal) * direction;

        // update direction character is facing based on their last input-driven movement
        if (direction.magnitude >= 0.1f)
        {
            trueForward = direction;
            controller.Move(direction * speed * Time.deltaTime);
        }

        // JUMPING

        if (Input.GetButtonDown("Jump"))
        {
            _directionY = jumpForce;
        }

        _directionY -= gravity * Time.deltaTime;
        direction.y = _directionY;

        controller.Move(direction * speed * Time.deltaTime);    // the actual move

        // SPRITE RENDERING

        // get angle character is facing relative to direction camera is facing
        float testAngle = Vector3.SignedAngle(trueForward, camF, Vector3.up);

        if (testAngle >= -22.5 && testAngle < 22.5)
        {  
            // front
            truDir = 0;
            if (animator.GetInteger("direction") != 0)
                animator.SetInteger("direction", 0);
            currentAnimation = "somRun 0";
           
        } else if (testAngle >= 22.5 && testAngle < 67.5)
        {
            //  front-left
            truDir = 4;
            if (animator.GetInteger("direction") != 4)
                animator.SetInteger("direction", 4);
            currentAnimation = "backLeft";
        }
        else if (testAngle >= 67.5 && testAngle < 112.5)
        {
            // left
            truDir = 3;
            if (animator.GetInteger("direction") != 3)
                animator.SetInteger("direction", 3);
            currentAnimation = "somRunLeft";
        }
        else if (testAngle >= 112.5 && testAngle < 157.5)
        {
            // back-left
            truDir = 5;
            if (animator.GetInteger("direction") != 8)
                animator.SetInteger("direction", 8);
            currentAnimation = "frontLeft";
        }
        else if ((testAngle >= 157.5 && testAngle <= 180) || (testAngle >= -180 && testAngle < -157.5))
        {
            // back
            truDir = 1;
            if (animator.GetInteger("direction") != 1)
                animator.SetInteger("direction", 1);
            currentAnimation = "backRun";
        } else if (testAngle >= -157.5 && testAngle < -112.5)
        {
            //back-right
            truDir = 6;
            if (animator.GetInteger("direction") != 7)
                animator.SetInteger("direction", 7);
            currentAnimation = "frontRight";
        }
        else if (testAngle >= -112.5 && testAngle < -67.5)
        {
            //right
            truDir = 2;
            if (animator.GetInteger("direction") != 2)
                animator.SetInteger("direction", 2);
            currentAnimation = "somRunSide";
        } else if (testAngle >= -67.5 && testAngle < -22.5)
        {
            //right-front
            truDir = 7;
            if (animator.GetInteger("direction") != 6)
                animator.SetInteger("direction", 6);
            currentAnimation = "backRight";
        }

        // play walking animation if input is given or controller is set to move automatically
        if (horizontal != 0 || vertical != 0 || auto_move)
                animator.speed = 1.0f;
        else
        {
            animator.Play(currentAnimation, 0, 0.0f);
            animator.speed = 0.0f;
            
        }

        // adjust animation playback speed depending on if player is walking or running
        if (Input.GetKey("x") || auto_move)
        {
            animator.speed = 1.0f;
            speed = 0.8f;
        }
        else
        {
            animator.speed = 0.6f;
            speed = 0.4f;
        }

        // niche cases
        if (slow)
            speed /= 3f;

        if (small)
            speed /= 4;

        if (fade_to_black)
        {
            heroSprite.color -= new Color(0.05f, 0.05f, 0.05f, 0f);
        }

        if (auto_move)
        {
            if (transition_frames < 31)
                transition_frames++;
        }

        Physics.BoxCast(transform.position, Vector3.one, trueForward);

        // PUSHING
        Physics.BoxCast(transform.position, new Vector3(0.01f, 0.01f, 0.01f), trueForward, out RaycastHit pushInfo, transform.rotation, 0.36f);
        if (pushInfo.collider != null)
        {
            var pushObj = pushInfo.collider.GetComponent<pushable>();
            if (pushObj != null)
            {
                pushInfo.collider.transform.Translate(trueForward * .01f);
            }
        }

        if (makeParent)
        {
            if (transition_frames > 4)
            {
                mainCam.transform.parent.gameObject.transform.parent = this.gameObject.transform;
                mainCam.transform.parent.gameObject.GetComponent<goToParent>().enabled = true;
                makeParent = false;
            }
            else
            {
                transition_frames++;
            }
        }

        // SNAPPING BACK TO PLAYER AFTER ROOM TRANSITION
        if (transition_frames > 30)
        {
            if (transition)
            {
                shadowCast shadow = gameObject.GetComponent<shadowCast>();
                shadow.shadow.SetActive(false);
                shadow.enabled = false;
                transform.position = snap_pos;
                heroSprite.color = new Color(1, 1, 1, 0);
                transform.position = migrate_point.position;
                makeParent = true;
            }
            
            
            auto_move = false;
            transition_frames = 0;
            makeParent = true;
        }



        // TEXT
        if (Input.GetKeyDown("z"))
        {

            // check point directly in front of player
            Physics.BoxCast(transform.position, new Vector3(0.01f, 0.01f, 0.01f), trueForward, out RaycastHit interactInfo, transform.rotation, 1.0f);
            if (interactInfo.collider != null && (textBox.activeSelf == false && descrBox.activeSelf == false))
            {
                var textInfo = interactInfo.collider.GetComponent<message>();
                if (textInfo != null)
                {
                    if (textInfo.mainTex)
                    {
                        yeah.currentText = "";
                        yeah.fullText = textInfo.text;
                        textBox.SetActive(true);
                    }
                    else
                    {
                        descrType.currentText = "";
                        descrType.fullText = textInfo.text;
                        descrBox.SetActive(true);
                    }
                }

                var actionInfo = interactInfo.collider.GetComponent<open>();
                if (actionInfo != null)
                {
                    actionInfo.enabled = true;
                }
            }
            
        }
    }

    }
