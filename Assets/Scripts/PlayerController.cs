using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    public const int COLLISION_MAX_ITERATIONS = 4; // 4 is a good number for this, one for each side
    public const float TERMINAL_VELOCITY = 40f;

    public float jumpSpeed;
    public float midairStopVelocity;
    public float moveSpeed;


    public bool jumping;
    public bool moving;
    public bool mainCameraFollowing;

    public Vector2 cameraOffset;

    public LayerMask collisionLayers;

    Vector2 velocity, acceleration;

    Rigidbody2D rb; // Why do you make my life miserable
    BoxCollider2D ourBox;
    
    bool grounded;
    
    void Awake() {
        rb = GetComponent<Rigidbody2D>();
        ourBox = GetComponent<BoxCollider2D>();

        acceleration = Physics2D.gravity;
        
        GameManager.instance.SetPlayerController(this);
        
    }

    void Update() {                          // That should work...
        if (Bluetooth.connectedToAndroid && GameManager.instance.playerType== PlayerType.Runner) {
            // Get tap
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began) {
                DoJump();
                GameManager.instance.SendPlayerJump(true, transform.position);
            } else if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended) {
                // Midair jump stop (it's wrong NOT to have it)
                GameManager.instance.SendPlayerJump(false, transform.position);
                if (velocity.y > midairStopVelocity) {
                    velocity.y = midairStopVelocity;
                }
            }
        } 
        else if(Bluetooth.connectedToAndroid && GameManager.instance.playerType== PlayerType.Blocker)
        {
            //if(jumping)
            //{
            //    DoJump();
            //}
            //else
            //{
            //    if (velocity.y > midairStopVelocity) {
            //        velocity.y = midairStopVelocity;
            //    }
            //}
        }
        else {
            // Get spacebar
            if (Input.GetKeyDown(KeyCode.Space)) {
                DoJump();
            } else if (Input.GetKeyUp(KeyCode.Space)) {
                // Midair jump stop (it's wrong NOT to have it)
                if (velocity.y > midairStopVelocity) {
                    velocity.y = midairStopVelocity;
                }
            }
        }

        // mooooooove bitch, get out da way
        if (moving)
            velocity.x = moveSpeed;
        else {
            velocity.x = 0f;
        }
    }

    void FixedUpdate() {
        // Do the physics and check the resulting collisions
        CheckCollisions();
    }

    void LateUpdate() {
        if (mainCameraFollowing) {
            Camera.main.transform.position = new Vector3(transform.position.x + cameraOffset.x, transform.position.y + cameraOffset.y, Camera.main.transform.position.z);
        }
    }

    public void RunnerDoJump(Vector3 pos) {
        grounded = false;
        velocity.y = jumpSpeed;
        //transform.position = pos;
    }
    public void RunnerStopJump(Vector3 pos) {
        if (velocity.y > midairStopVelocity) {
            velocity.y = midairStopVelocity;
        }
        //transform.position = pos;
    }
    void DoJump() {
        grounded = false;
        velocity.y = jumpSpeed;
    }

    void CheckCollisions() {

        // Apply gravity (vf = vi + a*t)
        if (!grounded)
            velocity.y += acceleration.y*Time.fixedDeltaTime;
        
        velocity.y = Mathf.Clamp(velocity.y, -TERMINAL_VELOCITY, TERMINAL_VELOCITY);

        // IMPORTANT: THIS is the main position update before collisions happen
        transform.position = new Vector3(
            transform.position.x + velocity.x*Time.fixedDeltaTime,
            transform.position.y + velocity.y*Time.fixedDeltaTime,
            transform.position.z);

        // TODO optimize later
        int collisionIteration = 0;
        Collider2D col;

        bool checkBottom = true, checkTop = true, checkLeft = true, checkRight = true; // y'know, the egyptians worshipped ugliness... just sayin

        do {
            Collider2D[] cols = Physics2D.OverlapBoxAll(transform.position, ourBox.bounds.size, 0f, collisionLayers.value);

            // Right on first iteration, check if we walked off a ledge and get out soon if so
            if (collisionIteration == 0 && cols.Length == 0 && grounded) {
                grounded = false;
                velocity.y = 0f;
                return;
            }
            
            if (cols.Length == 0)
                break;

            // We want only the closest block done first, OverlapBox is stinky and doesn't do this (I think it just returns a random collider? Furthest away? They never explained it in the docs)
            System.Array.Sort(cols,
            delegate (Collider2D hit1, Collider2D hit2) {
                return Vector2.Distance(transform.position, hit1.transform.position).CompareTo(
                    Vector2.Distance(transform.position, hit2.transform.position));
            });

            col = cols[0];
            // Now time for collision resolving (I should really do a write-up about this):

            // Picture a rectangle with two diagonal lines going through it to make an X. Each section represents the direction of resolution.
            // The side with the smallest interpenetration amount is going to be the side we resolve on this pass.
            // This method is NOT perfect (what if we're falling down and land on an edge? It should be a ground collision,
            // but the smallest interpenetration is on the side!), but the edge cases are easy to address.

            // We're game programmers, not physicists. 

            Vector2 ourMin = ourBox.bounds.min;
            Vector2 ourMax = ourBox.bounds.max;
            Vector2 theirMin = col.bounds.min;
            Vector2 theirMax = col.bounds.max;

            // Get displacements (the amount of overlap on each side between us and them) - direction is relative to us
            float displacementBottom = checkBottom ? theirMax.y - ourMin.y : Mathf.Infinity;
            float displacementTop = checkTop ? ourMax.y - theirMin.y : Mathf.Infinity;
            float displacementLeft = checkLeft ? theirMax.x - ourMin.x : Mathf.Infinity;
            float displacementRight = checkRight ? ourMax.x - theirMin.x : Mathf.Infinity;

            // Check to see if we're actually done here or not -- either all sides are 0 or past half-size (ie this side is irrelevant)
            if ((!checkBottom || displacementBottom > col.bounds.extents.y) &&
                (!checkTop || displacementTop > col.bounds.extents.y) &&
                (!checkLeft || displacementLeft > col.bounds.extents.x) &&
                (!checkRight || displacementRight > col.bounds.extents.x)) {
                break;
            }
            // Min each side's displacement. The smallest one is main collision side on this pass.
            float min = Mathf.Min(displacementBottom, displacementTop, displacementLeft, displacementRight);
            
            string message = Time.timeSinceLevelLoad + " - " + col.transform.position + ": "
                + displacementBottom + " "
                + displacementLeft + " "
                + displacementRight + " "
                + displacementTop + "  - " + min;

            //Debug.Log(message);

            if (min == displacementLeft) {
                // PLAYER COL - LEFT

                // Certain-distanced falls will result in what SHOULD be a bottom collision actually looking like a
                // side collision when landing on a block edge.

                // Check if we can even land here
                Collider2D wall = Physics2D.OverlapPoint(new Vector2(col.bounds.min.x+0.01f, col.bounds.max.y+0.01f), collisionLayers.value);

                if (displacementBottom + velocity.y*Time.deltaTime < displacementLeft && displacementLeft != 0f && !wall) {
                    // Welp, this should be a ground collision
                    velocity.y = 0f;
                    grounded = true;
                    checkBottom = false;
                    transform.position = new Vector3(transform.position.x, transform.position.y + displacementBottom, transform.position.z);
                } else {
                    // Handle side collision normally
                    checkLeft = false;
                    if (wall || !(displacementBottom == 0 || !checkBottom)) { // shit gets stuck when moving along the top of a block - prevent this
                        transform.position = new Vector3(transform.position.x + displacementLeft, transform.position.y, transform.position.z);
                    }
                }

            } else if (min == displacementRight) {
                // PLAYER COL - RIGHT

                // Same stuff as displacementLeft

                Collider2D wall = Physics2D.OverlapPoint(new Vector2(col.bounds.min.x+0.01f, col.bounds.max.y+0.01f), collisionLayers.value);

                if (displacementBottom + velocity.y*Time.deltaTime < displacementRight && displacementRight != 0f && !wall) {
                    velocity.y = 0f;
                    grounded = true;
                    checkBottom = false;
                    transform.position = new Vector3(transform.position.x, transform.position.y + displacementBottom, transform.position.z);
                } else {
                    // Handle side collision normally
                    checkRight = false;
                    if (wall || !(displacementBottom == 0 || !checkBottom)) { // shit gets stuck when moving along the top of a block - prevent this
                        transform.position = new Vector3(transform.position.x - displacementRight, transform.position.y, transform.position.z);
                    }
                }

            } else if (min == displacementTop) {
                // PLAYER COL - TOP
                checkTop = false;
                if (!grounded && velocity.y > 0f && checkLeft && checkRight) {
                    velocity.y = 0f;
                    transform.position = new Vector3(transform.position.x, transform.position.y - displacementTop, transform.position.z);
                }

            } else if (min == displacementBottom) {
                // PLAYER COL - BOTTOM
                checkBottom = false;
                Collider2D wall = Physics2D.OverlapPoint(new Vector2(col.bounds.min.x+0.01f, col.bounds.max.y+0.01f), collisionLayers.value);

                if (!grounded && velocity.y < 0f && !wall) {
                    velocity.y = 0f;
                    grounded = true;
                    transform.position = new Vector3(transform.position.x, transform.position.y + displacementBottom, transform.position.z);
                }
            }

        } while (++collisionIteration < COLLISION_MAX_ITERATIONS);

        if (collisionIteration >= COLLISION_MAX_ITERATIONS) {
            Debug.Log("DONE WITH COLLISION - EXCEEDED ITERATIONS!");
        }
    }
}