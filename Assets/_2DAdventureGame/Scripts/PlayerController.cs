using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    // Variables related to player character movement
    public InputAction MoveAction;
    Rigidbody2D rigidbody2d;
    Vector2 move;
    public float speed = 3.0f;

    // Variables related to the health system
    public int maxHealth = 5;
    int currentHealth;
    // get: 데이터를 요청받았을 때 실행되는 '입구(함수)'
    // return: 그 입구로 들어온 사람에게 들려보낼 '결과물'
    public int health { get { return currentHealth; } }

    // Variables related to temporary invincibility 무적
    public float timeInvincible = 2.0f;
    bool isInvincible;
    float damageCooldown; // 무적 쿨타임

    // Variables related to animation
    Animator animator;
    Vector2 moveDirection = new Vector2(1, 0); // (X, Y)

    // Variables related to projectiles
    public GameObject projectilePrefab;
    public InputAction LaunchAction;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MoveAction.Enable();
        LaunchAction.Enable();
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        move = MoveAction.ReadValue<Vector2>();

        // 플레이어가 움직이고 있다면 (0이 아니라면), 부동소수점문제 해결 위해 approximately 사용
        if (!Mathf.Approximately(move.x, 0.0f) || !Mathf.Approximately(move.y, 0.0f))
        {
            moveDirection.Set(move.x, move.y); // 현재 방향을 기억
            moveDirection.Normalize(); // 길이 1로 정규화
        }

        animator.SetFloat("Look X", moveDirection.x);
        animator.SetFloat("Look Y", moveDirection.y);
        animator.SetFloat("Speed", move.magnitude);

        if (isInvincible)
        {
            damageCooldown -= Time.deltaTime;
            if (damageCooldown < 0)
            {
                isInvincible = false;
            }
        }

        if (LaunchAction.WasPressedThisFrame()) // 발사 버튼 클릭 시
        {
            Launch();
        }

    }

    // FixedUpdate has the same call rate as the physics system
    void FixedUpdate()
    {
        Vector2 position = (Vector2)rigidbody2d.position + move * speed * Time.deltaTime;
        rigidbody2d.MovePosition(position);
    }

    // 외부에서 데미지를 주거나(amount가 음수), 힐을 줄 때(amount가 양수) 호출하는 함수
    public void ChangeHealth(int amount)
    {
        if (amount < 0) // 데미지 줄 때
        {
            if (isInvincible)
            {
                return;
            }
            isInvincible = true;
            damageCooldown = timeInvincible;
            animator.SetTrigger("Hit"); // Hit(피격) 애니메이션을 딱 한 번만 실행
        }

        /* Mathf.Clamp 설명
           현재 체력에 받은 양을 더하되, 그 결과가 0보다 작아지거나 maxHealth보다 커지지 않게 '고정'합니다.
           예: 체력이 5인데 힐을 100 받아도 최대치인 5로 유지됨! 
        */
        currentHealth = Mathf.Clamp(currentHealth + amount, 0, maxHealth);
        Debug.Log(currentHealth + "/" + maxHealth);

    }

    void Launch()
    {
        // projectilePrefab 복제, 현재 캐릭터 위치에서 위로 0.5만큼 살짝 위에서 총알이 나오게, Quaternion.identity는 회전 없음
        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300);
        animator.SetTrigger("Launch");
    }
}