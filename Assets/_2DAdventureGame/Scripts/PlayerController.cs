using UnityEngine;
using UnityEngine.InputSystem;
using System;

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

    // Variables related to NPC
    private NonPlayerCharacter lastNonPlayerCharacter;
    public InputAction TalkAction; // 대화 키

    // Variables related to audio
    AudioSource audioSource;

    // 액션 이벤트
    public event Action OnTalkedToNPC;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        MoveAction.Enable();
        LaunchAction.Enable();
        TalkAction.Enable(); // 대화키 가능
        rigidbody2d = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        currentHealth = maxHealth;
        audioSource = GetComponent<AudioSource>();

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

        // NPC 레이캐스트 감지 로직
        // Physics2D.Raycast(시작위치, 방향, 거리, 감지할 레이어)
        RaycastHit2D hit = Physics2D.Raycast(
            rigidbody2d.position + Vector2.up * 0.2f, // 시작점: 캐릭터 위치에서 위로 0.2 유닛 (발밑 감지 방지)
            moveDirection,                             // 방향: 현재 캐릭터가 움직이는(바라보는) 방향
            1.5f,                                      // 거리: 앞방향으로 1.5 유닛만큼만 레이저를 쏨
            LayerMask.GetMask("NPC")                   // 필터: "NPC" 레이어가 설정된 오브젝트만 충돌 처리
        );

        // 레이캐스트에 무언가 감지되었다면
        if (hit.collider != null)
        {
            // 충돌한 오브젝트에서 NonPlayerCharacter 스크립트 컴포넌트를 가져옴
            NonPlayerCharacter npc = hit.collider.GetComponent<NonPlayerCharacter>();

            npc.dialogueBubble.SetActive(true); // 해당 NPC의 대화 키 표시 말풍선을 화면에 표시
            lastNonPlayerCharacter = npc;       // 나중에 말풍선을 끄기 위해 현재 NPC 정보를 변수에 저장
            FindFriend(); // 친구를 찾는 추가 로직 실행
        }
        // 레이캐스트에 아무것도 감지되지 않았다면 (NPC 앞을 벗어났다면)
        else
        {
            // 이전에 감지했던 NPC 정보가 변수에 남아있는지 확인
            if (lastNonPlayerCharacter != null)
            {
                lastNonPlayerCharacter.dialogueBubble.SetActive(false); // 켜져 있던 대화 키 말풍선을 다시 끔
                lastNonPlayerCharacter = null; // NPC 저장 변수를 비움
            }
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
        UIHandler.instance.SetHealthValue(currentHealth / (float)maxHealth);

    }

    void Launch()
    {
        // projectilePrefab 복제, 현재 캐릭터 위치에서 위로 0.5만큼 살짝 위에서 총알이 나오게, Quaternion.identity는 회전 없음
        GameObject projectileObject = Instantiate(projectilePrefab, rigidbody2d.position + Vector2.up * 0.5f, Quaternion.identity);
        Projectile projectile = projectileObject.GetComponent<Projectile>();
        projectile.Launch(moveDirection, 300);
        animator.SetTrigger("Launch");
    }

    void FindFriend()
    {
        if (TalkAction.WasPressedThisFrame()) // 대화 키 누르면
        {
            OnTalkedToNPC?.Invoke(); // 알림
        }
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip); // 한 번만 소리 재생하는 함수
    }

}