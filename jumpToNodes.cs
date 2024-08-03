using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// -------------------------------------------------------------
/// DESCRIPTION
/// -------------------------------------------------------------
/// sin(n) mapped from n-values 0 to pi draws an arc that's perfect for illustrating jumps.
/// However, this only draws an arc between two points on a flat plane.
/// Here, we want to move in an arc motion between two points at different elevations.
/// We have one object that linearly interpolates a straight line movement between the two points,
/// ...and a child of that object that moves in a 0-to-pi sine wave pattern while also copying its parent's movements.
/// This allows the child to follow an arc path between nodes at different elevations.
/// -------------------------------------------------------------
/// This also works in reverse, decided by a "reverse" variable that is toggled by outside scripts.
/// -------------------------------------------------------------

public class jumpToNodes : MonoBehaviour
{
    public Transform[] nodes = new Transform[5];
    public Transform arc;
    private float percentage;
    private Vector3 start;
    private Vector3 end;
    private byte wait;
    private byte index;
    public bool reverse;
    // Start is called before the first frame update
    void Start()
    {
        Init_Set();
    }

    private void OnEnable()
    {
        Init_Set();
    }

    // Update is called once per frame
    void Update()
    {
        if (wait == 0)
        {
            percentage += 0.05f;
            if (percentage > 1)
                percentage = 1;

            transform.position = Vector3.Lerp(start, end, percentage);
            arc.localPosition = new Vector3(0, (Mathf.Sin(percentage * Mathf.PI)) * 4, 0);  
            // tying the sine wave to percentage ensures that the linear trajectory and the arc reach their end points at the exact same time

            if (percentage == 1)
            {
                // jump is complete, wait and prepare new start and end positions
                percentage = 0;
                wait = 15;

                if (reverse)
                {
                    index--;
                    end = nodes[index - 1].transform.position;
                }
                else
                {
                    index++;
                    end = nodes[index + 1].transform.position;
                }

                start = nodes[index].transform.position;
                
            }
        }
        else
        {
            wait--;
        }

        // check for end of script
        if ((!reverse & index == nodes.Length - 1) || (reverse & index == 0))
        {
            enabled = false;
        }
    }

    private void Init_Set()
    {
        if (reverse)
        {
            index = 4;
            end = nodes[index - 1].transform.position;
        }
        else
        {
            index = 0;
            end = nodes[index + 1].transform.position;
        }
        start = nodes[index].transform.position;
        transform.position = start;
        percentage = 0;
    }

}
