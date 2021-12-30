using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using System.Threading.Tasks;
using System.Threading;

public class EnvironmentScript : MonoBehaviour
{
    public Rigidbody envCube;

    public Environment env;
    // Start is called before the first frame update
    void Start()
    {
        env = new Environment(500, 500, 500, 200);
        envCube.position = env.Volume.center;
        envCube.transform.localScale = env.Volume.size;
        envCube.GetComponent<Collider>().enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        envCube.position = env.Volume.center;
        envCube.transform.localScale = env.Volume.size;
        env.Run();
    }

    public static Mesh ConeMesh(int subdivisions, float radius, float height)
    {
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[subdivisions + 2];
        Vector2[] uv = new Vector2[vertices.Length];
        int[] triangles = new int[(subdivisions * 2) * 3];

        vertices[0] = Vector3.zero;
        uv[0] = new Vector2(0.5f, 0f);
        for (int i = 0, n = subdivisions - 1; i < subdivisions; i++)
        {
            float ratio = (float)i / n;
            float r = ratio * (Mathf.PI * 2f);
            float x = Mathf.Cos(r) * radius;
            float z = Mathf.Sin(r) * radius;
            vertices[i + 1] = new Vector3(x, 0f, z);

            Debug.Log(ratio);
            uv[i + 1] = new Vector2(ratio, 0f);
        }
        vertices[subdivisions + 1] = new Vector3(0f, height, 0f);
        uv[subdivisions + 1] = new Vector2(0.5f, 1f);

        // construct bottom

        for (int i = 0, n = subdivisions - 1; i < n; i++)
        {
            int offset = i * 3;
            triangles[offset] = 0;
            triangles[offset + 1] = i + 1;
            triangles[offset + 2] = i + 2;
        }

        // construct sides

        int bottomOffset = subdivisions * 3;
        for (int i = 0, n = subdivisions - 1; i < n; i++)
        {
            int offset = i * 3 + bottomOffset;
            triangles[offset] = i + 1;
            triangles[offset + 1] = subdivisions + 1;
            triangles[offset + 2] = i + 2;
        }

        mesh.vertices = vertices;
        mesh.uv = uv;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();


        Quaternion qAngle = Quaternion.AngleAxis(90, Vector3.right);
        Vector3[] rotatedVerts = new Vector3[subdivisions + 2];
        for (int i = 0; i < mesh.vertices.Length; i++)
        {
            rotatedVerts[i] = qAngle * mesh.vertices[i];
        }

        mesh.vertices = rotatedVerts;

        return mesh;
    }
}
public class Environment
{
    #region Public Methods
    public Environment(float x, float y, float z, int boidCount)
    {
        Volume = new Bounds(new Vector3(0, 0, 0), new Vector3(x, y, z));
        GenerateBoids(boidCount);
    }
    public void Run()
    {
        for (int i = 0; i < Boids.Count; i++)
        {
            Boid boid = Boids[i];
            boid.Advance();
        }
    }
    #endregion

    #region Private Methods
    private void GenerateBoids(int boidCount)
    {
        int minX = Convert.ToInt32(Volume.min.x);
        int maxX = Convert.ToInt32(Volume.max.x);

        int minY = Convert.ToInt32(Volume.min.y);
        int maxY = Convert.ToInt32(Volume.max.y);

        int minZ = Convert.ToInt32(Volume.min.z);
        int maxZ = Convert.ToInt32(Volume.max.z);

        bool playable = false;
        for (int i = 0; i < boidCount; i++)
        {
            GameObject obj;
            if (i == 0)
            {
                obj = GameObject.Find("Main Camera");
                playable = true;
            }
            else
            {
                obj = new GameObject("Boid " + i);
                playable = false;
            }

            obj.AddComponent<Rigidbody>();
            obj.AddComponent<MeshFilter>().sharedMesh = EnvironmentScript.ConeMesh(10, 1.0f, 3f);
            obj.AddComponent<MeshRenderer>();
            obj.GetComponent<Rigidbody>().useGravity = false;
            obj.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotationX;

            int px = rnd.Next(minX, maxX);
            int py = rnd.Next(minY, maxY);
            int pz = rnd.Next(minZ, maxZ);

            int dx = rnd.Next(minX, maxX);
            int dy = rnd.Next(minY, maxY);
            int dz = rnd.Next(minZ, maxZ);

            Vector3 dir = new Vector3(dx, dy, dz);

            (obj.GetComponent<Rigidbody>()).position = new Vector3(px, py, pz);

            dir.Normalize();
            Boid boid = new Boid(dir, this, obj);
            boid.Playable = playable;
            boid.Speed = (float)(rnd.NextDouble() * (2 - 0.5) + 0.5);
            boid.UnityBody.transform.localScale = new Vector3(boid.Radius, boid.Radius, boid.Radius);

            Boids.Add(boid);
        }
    }
    #endregion

    #region Public Properties
    public List<Boid> Boids { get; set; } = new List<Boid>();
    public Bounds Volume { get; set; }
    #endregion

    #region Private Fields
    private System.Random rnd = new System.Random();
    #endregion
}
public class Boid
{
    #region Public Methods
    public Boid(Vector3 direction, Environment env, GameObject unityObject)
    {
        Debug.Log("MoI");
        UnityBody = unityObject;
        Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();
        thisBody.velocity = direction;

        Env = env;
    }


    public void Advance()
    {
        Vector3 manualForce = UnityBody.transform.forward;
        float multiplier = 1.5f;

        if (Playable)
        {
            if (Input.GetKey("w"))
            {
                manualForce += UnityBody.transform.up * Time.deltaTime * 2000;
            }
            if (Input.GetKey("s"))
            {
                manualForce += UnityBody.transform.up * Time.deltaTime * -2000;
            }
            if (Input.GetKey("d"))
            {
                manualForce += UnityBody.transform.right * Time.deltaTime * 2000;
            }
            if (Input.GetKey("a"))
            {
                manualForce += UnityBody.transform.right * Time.deltaTime * -2000;
            }
            if (Input.GetKey("space"))
            {
                multiplier = 5f;
            }
            Vector3 f3 = Move(7f);
            Vector3 resultant = f3 + manualForce;
            AddForce(resultant.normalized * multiplier);
        }
        else
        {
            CalculateNeighbors();

            Vector3 f1 = SwarmCenterDirectionSteer(3f);
            Vector3 f3 = Move(7f);
            Vector3 f2 = CommonDirectionSteer(2f);
            Vector3 f4 = CollisionDirectionSteer(1f);
            Vector3 f5 = EnvCollisionSteer(50f);
            Vector3 f6 = EnemyCollisionStreer(-100f);

            Vector3 resultant = f1 + f2 + f3 + f4 + f5;
            AddForce(resultant.normalized + f6.normalized * 20);
        }

        SlowingForce();

        Rotate();
    }

    public double Distance(Boid other)
    {
        Rigidbody otherBody = other.UnityBody.GetComponent<Rigidbody>();
        Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();

        double juu = Math.Sqrt(Math.Pow((otherBody.position.x - thisBody.position.x), 2)
                             + Math.Pow((otherBody.position.y - thisBody.position.y), 2)
                             + Math.Pow((otherBody.position.z - thisBody.position.z), 2));
        return juu;
    }
    #endregion

    #region Private Methods
    private Vector3 Move(float factor)
    {
        Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();
        Vector3 velocityDir = thisBody.velocity;

        return velocityDir * factor;
    }

    private Vector3 EnvCollisionSteer(float factor)
    {
        Bounds smallerBounds = new Bounds(Env.Volume.center, Env.Volume.size - Env.Volume.extents * (9 / 10));
        Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();

        if (!smallerBounds.Contains(thisBody.position))
        {
            Vector3 awayDirComponent = Env.Volume.center - thisBody.position;
            return awayDirComponent.normalized * factor;
        }
        return new Vector3(0, 0, 0);
    }

    private Vector3 CollisionDirectionSteer(float factor)
    {
        if (NearestNeighbor != null)
        {
            double dist = Distance(NearestNeighbor);

            if (dist <= VisionRange)
            {
                Rigidbody nearestBody = NearestNeighbor.UnityBody.GetComponent<Rigidbody>();
                Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();

                Vector3 awayDirComponent = thisBody.position - nearestBody.position;
                return awayDirComponent * factor;
            }
        }
        return new Vector3(0, 0, 0);
    }

    private Vector3 CommonDirectionSteer(float factor)
    {
        Vector3 commonDirComponent = new Vector3();
        for (int i = 0; i < Neighbors.Count; i++)
        {
            Boid neighbor = Neighbors[i];
            Rigidbody neighborBody = neighbor.UnityBody.GetComponent<Rigidbody>();
            commonDirComponent += neighborBody.velocity.normalized;
        }

        return commonDirComponent * factor;
    }

    private Vector3 SwarmCenterDirectionSteer(float factor)
    {
        if (Neighbors.Any())
        {
            Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();

            List<Vector3> neighborCenters = Neighbors.Select(n => n.UnityBody.GetComponent<Rigidbody>().position).ToList();
            Vector3 swarmCenterPoint = new Vector3(neighborCenters.Average(p => p.x),
                                                   neighborCenters.Average(p => p.y),
                                                   neighborCenters.Average(p => p.z));

            Vector3 centerComponent = swarmCenterPoint - thisBody.position;

            return centerComponent * factor;
        }

        return new Vector3(0, 0, 0);
    }

    private Vector3 EnemyCollisionStreer(float factor)
    {
        IEnumerable<Boid> players = Neighbors.Where(n => n.Playable);
        if (players.Any())
        {
            Rigidbody thisBody = this.UnityBody.GetComponent<Rigidbody>();

            List<Vector3> playersCenters = players.Select(n => n.UnityBody.GetComponent<Rigidbody>().position).ToList();
            Vector3 playerSwarmCenterPoint = new Vector3(playersCenters.Average(p => p.x),
                                                   playersCenters.Average(p => p.y),
                                                   playersCenters.Average(p => p.z));

            Vector3 centerComponent = playerSwarmCenterPoint - thisBody.position;

            return centerComponent * factor;
        }

        return new Vector3(0, 0, 0);
    }

    private void AddForce(Vector3 force)
    {
        Rigidbody body = UnityBody.GetComponent<Rigidbody>();
        body.AddForceAtPosition(force * Speed * 20f, body.position);
    }
    private void SlowingForce(float multiplier = 1.5f)
    {
        Rigidbody body = UnityBody.GetComponent<Rigidbody>();
        body.AddForceAtPosition(body.velocity * -multiplier, body.position);
    }

    private void Rotate()
    {
        Rigidbody body = UnityBody.GetComponent<Rigidbody>();
        Quaternion rot = body.transform.rotation;
        Vector3 direction = body.velocity;

        rot.SetLookRotation(direction, Vector3.up);
        body.transform.SetPositionAndRotation(body.transform.position, rot);
    }

    private void CalculateNeighbors()
    {
        Neighbors.Clear();
        NearestNeighbor = null;
        double nearestDist = double.MaxValue;

        for (int i = 0; i < Env.Boids.Count; i++)
        {
            Boid boid = Env.Boids[i];
            double dist = Distance(boid);
            if (boid != this && dist <= VisionRange)
            {
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    NearestNeighbor = boid;
                }

                Neighbors.Add(boid);
            }
        }
    }
    #endregion

    #region Public Properties
    public bool Playable { get; set; }
    public float Speed { get; set; } = 1f;
    public float VisionRange { get; set; } = 50f;
    public float Radius { get; set; } = 5f;
    public List<Boid> Neighbors { get; set; } = new List<Boid>();
    public Boid NearestNeighbor { get; set; } = null;
    public GameObject UnityBody { get; set; } = null;
    #endregion

    #region Private Fields
    private Environment Env;
    #endregion
}