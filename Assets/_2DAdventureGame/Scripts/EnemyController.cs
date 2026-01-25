using UnityEngine;

public class EnemyController : MonoBehaviour
{
    // Public variables
    public float speed;
    public bool vertical;
    public float changeTime = 3.0f;

    // Private variables
    Rigidbody2D rigidbody2d;
    Animator animator;
    float timer;
    int direction = 1;
    bool broken = true;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        timer = changeTime;
    }

    // Update is called every frame
    void Update()
    {
        timer -= Time.deltaTime;

        if (timer < 0)
        {
            direction = -direction;
            timer = changeTime;
        }
    }

    // FixedUpdate has the same call rate as the physics system
    void FixedUpdate()
    {
        if (!broken)
        {
            return;
        }

        Vector2 position = rigidbody2d.position;

        if (vertical)
        {
            position.y = position.y + speed * direction * Time.deltaTime;
            animator.SetFloat("Move X", 0); // "Move X"라는 변수 값을 0으로 만듦
            animator.SetFloat("Move Y", direction);
        }
        else
        {
            position.x = position.x + speed * direction * Time.deltaTime;
            animator.SetFloat("Move X", direction);
            animator.SetFloat("Move Y", 0);
        }
        
        rigidbody2d.MovePosition(position);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        PlayerController player = other.gameObject.GetComponent<PlayerController>();

        if (player != null)
        {
            player.ChangeHealth(-1);
        }
    }

    public void Fix()
    {
        broken = false;
        // 적 게임 오브젝트를 물리 시스템의 충돌 시뮬레이션에서 제외.
        // 투사체가 더 이상 적과 충돌하지 않으며 적 캐릭터에게 피해를 못 주게 됨.
        rigidbody2d.simulated = false;
        animator.SetTrigger("Fixed");
    }
}