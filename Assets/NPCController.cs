using UnityEngine;

public class NPCController : MonoBehaviour
{
    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float detectionRadius = 2f;
    public float avoidanceRadius = 2f;

    private Vector3 targetPoint;
    private bool hasTargetPoint = false;
    private float stoppingDistance = 0.5f;
    public Animator animator;
    private Rigidbody rb;

    private float nextStateChangeTime = 0f;
    private bool isIdle = true;
    private bool isRunningAway = false; 

    void Start()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();

        if (animator == null)
        {
            Debug.LogError("Animator component is missing on " + gameObject.name);
            return;
        }

        SetRandomState();
    }

    void Update()
    {
        if (isRunningAway)
        {
            
            RunAwayFromCar();
        }
        else
        {
            if (Time.time >= nextStateChangeTime && isIdle)
            {
                SetRandomState();
            }

            if (!isIdle && !hasTargetPoint)
            {
                GenerateTargetPoint();
            }

            if (hasTargetPoint)
            {
                MoveTowardsTarget();
            }

            if (hasTargetPoint && Vector3.Distance(transform.position, targetPoint) < stoppingDistance)
            {
                hasTargetPoint = false;
                SetRandomState();
            }
        }
    }

    void RunAwayFromCar()
    {
        
        Vector3 directionAway = (transform.position - targetPoint).normalized; 
        Vector3 newPosition = transform.position + directionAway * runSpeed * Time.deltaTime;

        if (IsWithinBounds(newPosition, 0f))
        {
            rb.MovePosition(newPosition);

            
            if (directionAway != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(directionAway);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
    }

    void GenerateTargetPoint()
    {
        float minDistance = 10f;

        do
        {
            Vector3 randomPoint = new Vector3(
                transform.position.x + Random.Range(-10f, 10f),
                transform.position.y,
                transform.position.z + Random.Range(-10f, 10f)
            );

            if (IsWithinBounds(randomPoint, minDistance))
            {
                targetPoint = randomPoint;
                hasTargetPoint = true;
                break;
            }
        } while (!hasTargetPoint);
    }

    void MoveTowardsTarget()
    {
        float currentSpeed = animator.GetCurrentAnimatorStateInfo(0).IsName("locom_m_jogging_30f") ? runSpeed : walkSpeed;

        Vector3 direction = (targetPoint - transform.position).normalized;

        Vector3 avoidanceDirection = AvoidObstacles();

        if (avoidanceDirection != Vector3.zero)
        {
            direction = avoidanceDirection;
        }

        direction.Normalize();

        Vector3 newPosition = transform.position + direction * currentSpeed * Time.deltaTime;

        if (IsWithinBounds(newPosition, 0f))
        {
            rb.MovePosition(newPosition);

            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
            }
        }
        else
        {
            ChangeDirection();
        }
    }

    private bool IsWithinBounds(Vector3 position, float minDistance)
    {
        Collider[] colliders = Physics.OverlapSphere(position, 0.1f);
        foreach (var collider in colliders)
        {
            if (collider.CompareTag("Asphalt"))
            {
                return true;
            }
        }
        return false;
    }

    void ChangeDirection()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 newDirection = Quaternion.Euler(0, Random.Range(-90f, 90f), 0) * (targetPoint - transform.position).normalized;
            Vector3 newPosition = transform.position + newDirection * walkSpeed * Time.deltaTime;

            if (IsWithinBounds(newPosition, 0f))
            {
                rb.MovePosition(newPosition);
                return;
            }
        }

        hasTargetPoint = false;
    }

    Vector3 AvoidObstacles()
    {
        Vector3 obstacleAvoidance = Vector3.zero;
        RaycastHit hit;

        if (Physics.Raycast(transform.position, transform.forward, out hit, detectionRadius))
        {
            if (hit.collider != null && hit.collider.gameObject != gameObject)
            {
                Vector3 hitNormal = hit.normal;
                Vector3 avoidDirection = Vector3.Cross(hitNormal, Vector3.up);
                obstacleAvoidance = avoidDirection * walkSpeed;
            }
        }

        return obstacleAvoidance;
    }

    void SetRandomState()
    {
        nextStateChangeTime = Time.time + Random.Range(5f, 10f);

        int randomState = Random.Range(0, 3);

        if (randomState == 0)
        {
            animator.Play("idle_m_1_200f");
            isIdle = true;
            hasTargetPoint = false;
        }
        else if (randomState == 1)
        {
            animator.Play("locom_m_basicWalk_30f");
            isIdle = false;
        }
        else if (randomState == 2)
        {
            animator.Play("locom_m_jogging_30f");
            isIdle = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            isRunningAway = true; 
            targetPoint = other.transform.position; 
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Car"))
        {
            isRunningAway = false; 
        }
    }
}
