using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/*
    1/28: Player will not register ceiling as new ground if the ceiling is very close to the ground they jumped from. Also applies when jumping to a floor that's very close to the ceiling you jumped from.
 */

public class NewColliderControl : MonoBehaviour
{
    private Rigidbody2D rb;
    private CircleCollider2D cc;

    Vector3 down_vector;
    Vector3 forward_vector;

    public float move_speed;
    private bool in_control;
    public Vector3 current_gravity;
    public Vector3 gravity;
    public Vector3 gravity_gain;
    public Vector3 terminal_velocity;
    public float jump_decay;
    public float jump_multiplier;
    public float spin_speed;
    public float spin_potential;
    public float spin_actual;
    public Vector3 jump_force;
    public Vector3 current_jump_force;
    private bool just_landed;
    public Vector3 bottom_center;
    public Vector3 bottom_center_absolute;
    public Animation spinAnim;
    public RectTransform starRotate;
    private Vector3 prev_pos;
    private Vector3 last_dir;
    private float last_x_dir;
    public float last_y_dir;
    public float relative_dir;
    public float absolute_dir;
    public float relative_spin_dir;
    public Vector3 spin_momentum;
    public bool slow;
    public short number_of_water_colliders;
    public GameObject ripple;
    public GameObject canvas;
    private int layers_to_check;
    public Vector3 starting_pos;
    private bool lerp_to_goal;
    private Vector3 goal_pos;
    private int frame_count;
    public Image sprite;
    private Sprite starting_sprite;
    public Sprite white_sprite;
    public bool goal_touched;
    public growEntrance goal;
    private Vector3 final_move;
    private float debug_value;
    public float spin_decay_rate;
    private float spin_decay_rate_2;
    public Vector3 last_spin_move;
    private bool respawn;
    private Quaternion initial_rotation;
    public Vector3 pop_off;
    public Vector3 knockback;
    private bool v_override;
    private short flash_frames;
    public float spin_gain_when_holding_up;

    private bool isGrounded;
    private bool onIce;
    private bool grounded_last_frame;
    private Vector3 draw2point;
    public Collider2D current_ground;
    private Vector3 current_ground_point;


    // Start is called before the first frame update
    // For new movement system, try testing planned movement

    private void BackToStart()
    {
        current_jump_force = Vector3.zero;
        transform.position = starting_pos;
        goal_touched = false;
        respawn = true;
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // ENTERING WATER
        if (other.transform.gameObject.layer == 0)
        {
            number_of_water_colliders++;
        }
        else if (other.transform.gameObject.layer == 14)    // TOUCHING GOAL
        {
            sprite.sprite = white_sprite;
            goal_touched = true;
            in_control = false;
            lerp_to_goal = true;
            goal_pos = other.transform.position;
            
        }
        else if (other.transform.gameObject.layer == 16)    // TOUCHING HAZARD
        {
            if (flash_frames == 0)
            {
                // sent info to damage meters
            }
            if (flash_frames < 56)  // this ensures knockback will not be applied for multiple frames
            {
                knockback = (transform.position - other.transform.position).normalized * 125;
                current_gravity = Vector3.zero;
                gravity_gain = new Vector3(0, 5.6f, 0);
                spin_speed = 0;
                spin_potential = 0;
                spin_momentum = Vector3.zero;
                if (other.transform.position.y > transform.position.y)
                    current_jump_force = Vector3.zero;
            }
            flash_frames = 60;
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.transform.gameObject.layer == 16 && flash_frames == 0)    // TOUCHING HAZARD
        {
            knockback = (transform.position - collision.transform.position).normalized * 125;
            current_gravity = Vector3.zero;
            gravity_gain = new Vector3(0, 5.6f, 0);
            spin_speed = 0;
            spin_potential = 0;
            spin_momentum = Vector3.zero;
            flash_frames = 60;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.transform.gameObject.layer == 0)
        {
            if (number_of_water_colliders > 0)
                number_of_water_colliders--;
        }
    }
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        cc = GetComponent<CircleCollider2D>();

        starting_sprite = sprite.sprite;
        relative_spin_dir = 1;
        starting_pos = transform.position;
        frame_count = 0;
        in_control = true;
        down_vector = Vector3.down;
        forward_vector = Vector3.right;
        just_landed = false;
        bottom_center = transform.position + (down_vector * cc.bounds.extents.y);
        bottom_center_absolute = bottom_center;
        spinAnim["spin"].speed = 0.2f;
        spin_potential = 0f;
        prev_pos = transform.position;
        spin_momentum = Vector3.zero;
        relative_dir = 1;
        absolute_dir = 1;
        number_of_water_colliders = 0;
        last_x_dir = 1;
        last_y_dir = 1;
        layers_to_check = LayerMask.GetMask("ground", "Water", "ice", "oneway");
        goal_touched = false;
        final_move = Vector3.zero;
        debug_value = 0;
        last_spin_move = Vector3.zero;
        respawn = false;
        pop_off = Vector3.zero;
        flash_frames = 0;

        isGrounded = false;
        grounded_last_frame = false;
        spin_decay_rate_2 = spin_decay_rate / 2.3f;
        initial_rotation = sprite.transform.localRotation;
        draw2point = Vector3.zero;
        current_ground = null;
        current_ground_point = Vector3.zero;
        knockback = Vector3.zero;
    }

    private void FixedUpdate()
    {
        bool just_went_airborne = false;
        current_ground = null;
        bool schedule_noground = false;

        if (in_control)
        {
            // ----------- GET INPUTS ------------
            float vInput = Input.GetAxisRaw("Vertical");
            float hInput = Input.GetAxisRaw("Horizontal");
            bool jInput = Input.GetButtonDown("Jump");
            bool jRelease = Input.GetButtonUp("Jump");
            

            if (v_override)
            {
                vInput = 0;
            }

            if (Input.GetKeyDown(KeyCode.K))
            {
                BackToStart();
            }

            if (Input.GetKeyDown(KeyCode.L))
            {
                Debug.Break();
            }



            // ----------- CHECK GROUNDING, GRAVITY ---------------

            //isGrounded = false;
            //just_landed = false;

            RaycastHit2D ground_info = Physics2D.CircleCast(transform.position, cc.bounds.extents.x - 0.01f, down_vector, 1.0f, layers_to_check);

            if (ground_info.collider)
            {
                ground_info = Physics2D.Raycast(transform.position, ground_info.point - new Vector2(transform.position.x, transform.position.y), 5.0f, layers_to_check);
                if (ground_info.collider)
                {
                    if ((ground_info.normal.y >= 0.08f || (spin_speed > 0.3f || spin_potential > 0.3f)))
                    {
                        GroundSet(ground_info);
                        draw2point = ground_info.point;

                    }
                    else
                    {
                        //NoGround();
                        schedule_noground = true;
                        if (ground_info.normal.y <= 0.08f)      // pop player off wall/ceiling if their spin speed isn't enough, stop them from inputting more spin speed during fall
                        {
                            current_jump_force += new Vector3(ground_info.normal.x, ground_info.normal.y, 0) * 20;
                            // disabled for testing, was acting up when it wasn't supposed to
                            //Debug.Break();
                            v_override = true;
                        }
                    }
                }
                else
                {
                    //NoGround();
                    schedule_noground = true;
                }
            }
            else
            {
                // Check for a backcast if spinning controller still needs to adhere to wall
                if ((spin_speed > 0.3f || spin_potential > 0.3f) && current_jump_force == Vector3.zero && isGrounded)
                {
                    RaycastHit2D back_info = Physics2D.Raycast(bottom_center + down_vector, -forward_vector * relative_spin_dir, 8.0f, LayerMask.GetMask("ground", "Water", "ice"));
                    //1/31: shortened raycast length to solve issue of snapping to opposite wall
                    if (back_info.collider)
                    {
                        GroundSet(back_info);
                        schedule_noground = false;

                        transform.position = new Vector3(back_info.point.x, back_info.point.y, transform.position.z) - (down_vector * cc.bounds.extents.y) - (forward_vector * relative_spin_dir * (cc.bounds.extents.y - 1));
                        Debug.DrawRay(transform.position, down_vector * 4, Color.white, 2);
                    }
                    else
                    {
                        schedule_noground = true;
                    }
                }
                else
                {
                    // Reset direction vectors and grounding status
                    schedule_noground = true;
                }
            }

            

            // GRAVITY AND JUMP FORCE DECAY
            if (isGrounded || just_went_airborne)
            {
                current_gravity = Vector3.zero;
                if (!just_went_airborne)
                    current_jump_force = Vector3.zero;

                gravity_gain = new Vector3(0, 5.6f, 0);
            }
            else
            {
                // This part determines jump height based on how long the button was held for
                if (jRelease)
                {
                    gravity_gain = new Vector3(0, 13, 0);
                }
                
                if (!just_went_airborne)
                    current_gravity -= gravity_gain;

                if (hInput != 0)
                {
                    // Let player counteract their current jump force
                    if (hInput == 1 && current_jump_force.x < 0)
                        current_jump_force.x += 2;
                    else if (hInput == -1 && current_jump_force.x > 0)
                        current_jump_force.x -= 2;
                }
            }

            // NORMAL MOVEMENT
            Vector3 normal_move = Vector3.zero;
            if (down_vector.y < 0)
            {
                normal_move = forward_vector * hInput * move_speed;

                RaycastHit2D normal_move_test = Physics2D.CircleCast(transform.position, cc.bounds.extents.x - 0.05f, normal_move, 1.0f, layers_to_check);

                if (normal_move_test.collider)
                {
                    normal_move_test = Physics2D.Raycast(transform.position, normal_move_test.point - new Vector2(transform.position.x, transform.position.y), 10.0f, layers_to_check);
                    if (normal_move_test.normal.y <= 0)
                    {
                        // Player's movement is blocked
                        //normal_move = Vector3.zero;
                    }
                    else
                    {
                        // Player has hit a new slope, make that their new ground
                        GroundSet(normal_move_test);
                        schedule_noground = false;
                        normal_move = forward_vector * hInput * move_speed;
                    }
                }

                // Adjust speed based on slope and whether or not you're working with or against gravity
                if (normal_move.y > 0)
                {
                    // Slow down when climbing up
                    normal_move *= Mathf.Abs(down_vector.y) * 0.5f;
                }
                else if (normal_move.y < 0)
                {
                    // Speed up when sliding down
                    normal_move *= 1.8f + Mathf.Abs(down_vector.x);
                }
            }

            if (flash_frames > 0 && normal_move != Vector3.zero)   // reduce control when player is in hitsun
            {
                normal_move *= 0.6f;
            }

            // SPINNING

            if (isGrounded)
            {

                // let other movement decide spin direction is spin speed is negligible
                if (spin_speed < 0.1f && spin_potential < 0.1f)
                {
                    relative_spin_dir = last_x_dir;
                }

                if (vInput > 0 && spin_speed <= 0.3f)
                {
                    // Drag if trying to spin while grounded
                    spin_potential += 0.01f;
                }
                else
                {
                    // Spin fades out
                    // let player offset spin speed by holding opposite direction
                    if (hInput != 0 && hInput == -Mathf.Sign(forward_vector.x * relative_spin_dir))
                    {
                        spin_potential -= spin_decay_rate;
                    }
                    else if (last_dir.y >= 0.0f)
                    {
                        spin_potential -= spin_decay_rate_2;
                    }
                }

                if (spin_potential < 0.01f)
                    spin_potential = 0;

                // add spin potential to actual speed if applicable
                spin_speed = spin_potential;

            }
            else
            {
                if (vInput > 0 && spin_speed < 2.0f)
                {
                    // Increase spin potential if player is in air
                    spin_potential += spin_gain_when_holding_up;
                }
            }

            // If spin speed is over the limit, cap it
            if (spin_potential > 2.0f)
            {
                spin_potential = 2.0f;
            }
            if (spin_speed > 2.0f)
            {
                spin_speed = 2.0f;
            }

            spinAnim["spin"].speed = spin_potential * -relative_spin_dir;
            Vector3 spin_move = spin_speed * forward_vector * relative_spin_dir * 50;


            // test if spin move is valid
            Vector3 spin_move_test_trajectory = spin_move / 50 + current_gravity + current_jump_force + spin_momentum;
            if (spin_move == Vector3.zero && spin_potential != 0)
            {
                spin_move_test_trajectory = spin_potential * forward_vector * relative_spin_dir + current_gravity + current_jump_force + spin_momentum;
            }
            RaycastHit2D spin_move_test = Physics2D.CircleCast(transform.position, cc.bounds.extents.x - 0.05f, spin_move_test_trajectory, 1.0f, LayerMask.GetMask("ground", "Water", "ice", "oneway", "ceiling"));

            if (spin_move_test.collider)
            {
                spin_move_test = Physics2D.Raycast(transform.position, spin_move_test.point - new Vector2(transform.position.x, transform.position.y), 6.0f, LayerMask.GetMask("ground", "Water", "ice", "oneway", "ceiling"));

                if (spin_speed < 0.35f && spin_potential < 0.35f)
                {
                    // Player's movement is blocked
                    //normal_move = Vector3.zero;
                    spin_move = Vector3.zero;
                }
                else
                {
                    // Player has hit a new slope, make that their new ground
                    // failsafe if player hits side of one-way platform collider
                    if ((spin_move_test.collider.gameObject.layer == 12 || spin_move_test.collider.gameObject.layer == 13) && spin_move_test.normal.y == 0)
                    {
                        // do nothing, may need something here in the future
                    }
                    else
                    {
                        GroundSet(spin_move_test);
                        schedule_noground = false;
                        spin_speed = spin_potential;    // THIS IS EXPERIMENTAL, 1/23
                        spin_move = spin_speed * forward_vector * relative_spin_dir * 50;
                    }
                    

                }
            }

            if (schedule_noground)
            {
                NoGround();
            }

            if (just_landed)
            {
                if (Mathf.Abs(down_vector.y) > Mathf.Abs(down_vector.x))
                {
                    if (Mathf.Sign(forward_vector.x) * relative_spin_dir != last_x_dir)
                    {
                        //Debug.Log("Forward vector is " + Mathf.Sign(forward_vector.x) + ", " + "Relative spin dir is " + relative_spin_dir);
                        relative_spin_dir *= -1;
                    }

                }
                else
                {
                    if (Mathf.Sign(forward_vector.y) * relative_spin_dir != last_y_dir)
                    {
                        relative_spin_dir *= -1;
                        //Debug.Break();
                    }
                }
                spin_move = spin_speed * forward_vector * relative_spin_dir * 50;
            }


            // JUMPING
            //1/28: moved this to before spinning section to fix problem where character would immediately detach from ceilings that were close to the ground.
            //1/29: moved it back because of problems jumping on first frame after landing
            // This is because spin_test_trajectory needed to account for jump force applied on the first frame
            if (jInput)
            {
                if (isGrounded)
                {
                    current_jump_force = -down_vector * jump_multiplier;
                    //IDEA: When jumps, add spin momentum and transfer actual spin speed to potential
                    spin_momentum = spin_speed * forward_vector * relative_spin_dir * 50;
                    debug_value = spin_speed;
                    // bandaid fix for abruptly applying small amounts of spin momentum in wrong direction when jumping while the opposite horizontal direction is pressed
                    if (spin_speed < 0.04f && spin_potential < 0.04f)
                    {
                        spin_momentum = Vector3.zero;
                    }
                    spin_potential = spin_speed;
                    spin_speed = 0;
                    NoGround();
                }
            }

            // SEE IF GROUND BELOW PLAYER IS MOVING
            Vector3 ground_move = Vector3.zero;

            if (current_ground != null)
            {
                movingPlatform move_info = current_ground.GetComponent<movingPlatform>();
                if (move_info != null)
                {
                    //This part is questionable, maybe just delete it entirely
                    if (bottom_center.y > current_ground_point.y)
                    {
                        //transform.position = new Vector3(current_ground_point.x, current_ground_point.y, transform.position.z) + new Vector3(0, transform.position.y - bottom_center.y, 0);
                    }
                    ground_move = move_info.move_delta;
                    if (jInput)
                    {
                        current_jump_force += ground_move * 50;
                    }
                }
                
            }

            // decrease knockback if player is grounded;
            if (isGrounded)
            {
                knockback *= 0.9f;
                knockback.y = 0;
                if (Mathf.Abs(knockback.x) < 1)
                {
                    knockback = Vector3.zero;
                }
            }
            else
            {
                knockback *= 0.9f;
            }


            final_move = normal_move + current_gravity + current_jump_force + spin_move + spin_momentum + knockback;
            transform.position += ground_move;      // consider just adding this on to rb.velocity for a potentially less janky solution
            rb.velocity = final_move;
            
            Debug.DrawRay(transform.position, spin_momentum * 10000, Color.green);

            if (isGrounded)
                Debug.DrawRay(bottom_center, forward_vector * last_x_dir * 10, Color.red);
            else
                Debug.DrawRay(bottom_center, forward_vector * last_x_dir * 10, Color.blue);

            if (isGrounded)
                grounded_last_frame = true;
            else
                grounded_last_frame = false;

            Debug.DrawLine(transform.position, new Vector3(draw2point.x, draw2point.y, transform.position.z), Color.green);
            Debug.DrawRay(bottom_center, current_jump_force * 10, Color.red);

        }
        else
        {
            // player touched goal and is in "transition" or "lerp_to_goal" state
            if (lerp_to_goal)
            {
                goal.transform.localScale = Vector3.zero;
                //transform.position = new Vector3((transform.position.x + goal_pos.x) / 2, (transform.position.y + goal_pos.y) / 2, transform.position.z);
                transform.position = new Vector3(Mathf.Lerp(transform.position.x, goal_pos.x, 0.1f), Mathf.Lerp(transform.position.y, goal_pos.y, 0.1f), transform.position.z);
                frame_count += 1;

                if (sprite.color == Color.white)
                {
                    sprite.color = new Color(0.5754717f, 0.4336022f, 0, 1);
                }
                else
                {
                    sprite.color = Color.white;
                }

                // end transition state
                if (frame_count >= 50)
                {
                    //in_control = true;
                    frame_count = 0;
                    lerp_to_goal = false;
                    BackToStart();
                    sprite.sprite = white_sprite;
                    sprite.color = Color.white;
                    starRotate.localScale = new Vector3(0.01f, 8f, 1f);
                    starRotate.localPosition -= new Vector3(0, 8f, 0);
                    rb.velocity = Vector3.zero;


                }
            }

            if (respawn)
            {
                spin_momentum = Vector3.zero;
                relative_spin_dir = last_x_dir;
                starRotate.pivot = new Vector2(0.5f, 0);
                starRotate.localScale += new Vector3(0.07f, -0.61f, 0f);
                if (starRotate.localScale.y <= 1)
                {
                    starRotate.localScale = new Vector3(1, 1, 1);
                    in_control = true;
                    sprite.sprite = starting_sprite;
                    respawn = false;
                    starRotate.pivot = new Vector2(0.5f, 0.5f);
                    starRotate.localPosition += new Vector3(0, 8f, 0);
                    down_vector = Vector3.down;
                    forward_vector = Vector3.right;
                }
            }
        }

        bottom_center = transform.position + (down_vector * cc.bounds.extents.y) + (down_vector);
        bottom_center_absolute = transform.position + (Vector3.down * cc.bounds.extents.y);

        // APPARENT MOVEMENT DIRECTION
        last_dir = transform.position - prev_pos;
        if (final_move.x != 0)
        {
            last_x_dir = Mathf.Sign(final_move.x);
        }
        if (final_move.y != 0)
        {
            //bandaid fix for taking a single, neglibile frame of gravity in to account for surface adherance calculations
            if (final_move.y != -5.6f)
                last_y_dir = Mathf.Sign(final_move.y);
        }

        // DAMAGE INVULNERABILITY FLICKER
        if (flash_frames > 0)
        {
            if (sprite.color.a != 0)
            {
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 0);
            }
            else
            {
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1);
            }
            flash_frames -= 1;
            if (flash_frames == 0)
            {
                sprite.color = new Color(sprite.color.r, sprite.color.g, sprite.color.b, 1);
            }
        }

        prev_pos = transform.position;





        // ---------------------------- FUNCTIONS -----------------------------------------

        void GroundSet(RaycastHit2D ground_info)
        {
            down_vector = -ground_info.normal;
            forward_vector.x = down_vector.y;
            forward_vector.y = -down_vector.x;
            forward_vector *= -1;

            // Test if player was grounded last frame. If not, they just landed.
            if (!grounded_last_frame)
                just_landed = true;
            else
                just_landed = false;

            spin_momentum = Vector3.zero;
            isGrounded = true;
            v_override = false;
            current_ground = ground_info.collider;
            current_ground_point = ground_info.point;
            current_gravity = Vector3.zero;
            current_jump_force = Vector3.zero;
            bottom_center = transform.position + (down_vector * cc.bounds.extents.y) + (down_vector);
            last_spin_move = spin_speed * forward_vector * relative_spin_dir * 50;

            //if (just_landed)
                //Debug.DrawRay(transform.position, down_vector * 4, Color.white, 10);

            //Debug.DrawRay(bottom_center, ground_info.normal * 100, Color.white, 0.3f);
        }

        void NoGround()
        {
            // Flip relative direction if character is detaching from ceiling
            if (down_vector.y > 0)
            {
                // This has returned true in some cases where it shouldn't, keep an eye out on this block if something goes wrong
                relative_spin_dir *= -1;

            }

            // Reset direction vectors and grounding status
            down_vector = Vector3.down;
            forward_vector = Vector3.right;

            if (grounded_last_frame)
                just_went_airborne = true;
            else
                just_went_airborne = false;

            isGrounded = false;
            just_landed = false;    // this may be causing air momentum to cancel out for some reason?

            //EXPERIMENTAL: 1/9 taken out 1/29, was causing problems with spin momentum when jumping on first frame after landing.
            //spin_momentum = last_spin_move;
        }


    }


}
