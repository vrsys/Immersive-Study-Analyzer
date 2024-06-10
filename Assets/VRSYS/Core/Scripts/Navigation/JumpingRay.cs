// VRSYS plugin of Virtual Reality and Visualization Research Group (Bauhaus University Weimar)
//  _    ______  _______  _______
// | |  / / __ \/ ___/\ \/ / ___/
// | | / / /_/ /\__ \  \  /\__ \ 
// | |/ / _, _/___/ /  / /___/ / 
// |___/_/ |_|/____/  /_//____/  
//
//  __                            __                       __   __   __    ___ .  . ___
// |__)  /\  |  | |__|  /\  |  | /__`    |  | |\ | | \  / |__  |__) /__` |  |   /\   |  
// |__) /~~\ \__/ |  | /~~\ \__/ .__/    \__/ | \| |  \/  |___ |  \ .__/ |  |  /~~\  |  
//       ___               __                                                           
// |  | |__  |  |\/|  /\  |__)                                                          
// |/\| |___ |  |  | /~~\ |  \                                                                                                                                                                                     
//
// Copyright (c) 2022 Virtual Reality and Visualization Research Group
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:

// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.

// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.
//-----------------------------------------------------------------
//   Authors:        Ephraim Schott, Sebastian Muehlhaus
//   Date:           2022
//-----------------------------------------------------------------

using UnityEngine;
using UnityEngine.AI;

public class JumpingRay : MonoBehaviour
{
    //Setup Jumping Ray
    [Header("Configure Jumping Ray")]
    public float rayWidth = 0.01f;
    public float rayVelocity = 700;
    public bool validateAgainstNavMesh = false;
    public float navMeshTolerance = 0.1f;

    public LayerMask jumpingCollisions;
    public LineRenderer rayRenderer;

    public bool rayActive = false;
    private bool rayUpdate = true;
    private int segmentCount = 70;
    private float segmentScale = 0.3f;
    public Material rayMaterial;
    private Color validRayColor = Color.blue;
    private Color invalidRayColor = Color.red;
    //store hit information:
    private Collider _hitObject;
    public Collider hitObject { get { return _hitObject; } }
    //HitVector
    private Vector3 _hitVector;
    public Vector3 hitVector { get { return _hitVector; } }
    //hitNormal
    private Vector3 _hitNormal;
    public Vector3 hitNormal { get { return _hitNormal; } }

    public GameObject controller;

    void Start()
    {
        AddLineRenderer();
    }

    void FixedUpdate()
    {
        if (rayUpdate)
        {
            if (rayActive)
            {
                SimulateRayPath();
            }
            else
            {
                rayRenderer.positionCount = 0;
            }
        }
    }

    void SimulateRayPath()
    {
        Vector3[] segments = new Vector3[segmentCount];

        //start of the jumping ray at the position of the object this script is attached to
        segments[0] = controller.transform.position;

        // initial velocity
        Vector3 segVelocity = controller.transform.forward * rayVelocity * Time.deltaTime;
        _hitObject = null;   //reset hitobject
        bool skipRayTest = false;

        // calculate Raycast
        for (int i = 1; i < segmentCount; i++)
        {
            if (_hitObject != null)
            {
                segments[i] = _hitVector;
                continue;
            }
            // Time to traverse one segment of segScale; scale/length if length not 0; 0 else
            float segTime = (segVelocity.sqrMagnitude != 0) ? segmentScale / segVelocity.magnitude : 0;

            //add velocity for current segments timestep
            segVelocity = segVelocity + Physics.gravity * segTime;

            //Check for hit with a physics object
            RaycastHit hit;
            if (!skipRayTest && Physics.Raycast(segments[i - 1], segVelocity, out hit, segmentScale, jumpingCollisions))
            {
                if (validateAgainstNavMesh)
                {
                    NavMeshHit navMeshHit;
                    int walkableMask = 1 << NavMesh.GetAreaFromName("Walkable");
                    if (NavMesh.SamplePosition(hit.point, out navMeshHit, navMeshTolerance, walkableMask))
                    {
                        _hitObject = hit.collider;
                        // set next position to position where object hit occured
                        segments[i] = segments[i - 1] + segVelocity.normalized * hit.distance;
                        //correct ending velocity for interrupted path
                        segVelocity = segVelocity - Physics.gravity * (segmentScale - hit.distance) / segVelocity.magnitude;
                        //save Postion of Collision
                        _hitVector = segments[i];
                    }
                    else
                    {
                        segments[i] = segments[i - 1] + segVelocity * segTime;
                        skipRayTest = true;
                    }
                }
                else
                {
                    _hitObject = hit.collider;
                    // set next position to position where object hit occured
                    segments[i] = segments[i - 1] + segVelocity.normalized * hit.distance;
                    //correct ending velocity for interrupted path
                    segVelocity = segVelocity - Physics.gravity * (segmentScale - hit.distance) / segVelocity.magnitude;
                    //save Postion of Collision
                    _hitVector = segments[i];
                }
            }
            else
            {
                segments[i] = segments[i - 1] + segVelocity * segTime;
            }
        }
        rayRenderer.positionCount = segmentCount;
        for (int i = 0; i < segmentCount; i++)
        {
            rayRenderer.SetPosition(i, segments[i]);
        };
    }

    public void SetRayMaterial(Material material)
    {
        rayRenderer.material = material;

    }

    public void SetRayColor(Color color)
    {
        if(rayRenderer != null)
            rayRenderer.material.color = color;
    }

    public void RayValid(bool valid)
    {
        if (valid)
        {
            SetRayColor(validRayColor);
        }
        else
        {
            SetRayColor(invalidRayColor);
        }
    }

    void AddLineRenderer()
    {
        rayRenderer = gameObject.AddComponent<LineRenderer>();
        rayRenderer.startWidth = rayWidth;
        rayRenderer.material = rayMaterial;
    }


    private Vector3 GetMeshColliderNormal(RaycastHit hit)
    {
        if (hit.collider != null)
        {

            MeshCollider collider = hit.collider as MeshCollider;
            if (collider != null)
            {
                Mesh mesh = collider.sharedMesh;
                Vector3[] normals = mesh.normals;
                int[] triangles = mesh.triangles;

                Vector3 n0 = normals[triangles[hit.triangleIndex * 3 + 0]];
                Vector3 n1 = normals[triangles[hit.triangleIndex * 3 + 1]];
                Vector3 n2 = normals[triangles[hit.triangleIndex * 3 + 2]];
                Vector3 baryCenter = hit.barycentricCoordinate;
                Vector3 interpolatedNormal = n0 * baryCenter.x + n1 * baryCenter.y + n2 * baryCenter.z;
                interpolatedNormal.Normalize();
                interpolatedNormal = hit.transform.TransformDirection(interpolatedNormal);
                return interpolatedNormal;
            }
            else
            {
                return new Vector3();
            }
        }
        else
        {
            return new Vector3();
        }

    }
}
