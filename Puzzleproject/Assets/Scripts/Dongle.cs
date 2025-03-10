using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager;
    public ParticleSystem effect;
    public int level;

    Rigidbody2D rigid;
    Animator anim;
    SpriteRenderer spriteRenderer;
    CircleCollider2D circleCollider;

    bool isDrag;
    bool isMerge;
    bool isAttach;
    float deadTime;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        circleCollider = GetComponent<CircleCollider2D>();
    }

    private void OnEnable()
    {
        anim.SetInteger("Level", level);

        if (!anim.GetBool("IsPlaying"))
        {
            anim.SetBool("IsPlaying", false);
            StartCoroutine(ResetAnimation());
        }
    }
    IEnumerator ResetAnimation()
    {
        yield return new WaitForSeconds(0.2f); // 애니메이션 길이에 맞춰 조정
        anim.SetBool("IsPlaying", true);  // 다시 실행되지 않도록 설정
    }

    void Update()
    {
        // 드래그 상태일 때만 마우스 X축 따라가기
        if (isDrag)
        {
            Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            // x축 최대값 적용
            float LeftBorder = -4.2f + transform.localScale.x / 2;
            float RightBorder = 4.2f - transform.localScale.x / 2;

            if (mousePos.x < LeftBorder)
            {
                mousePos.x = LeftBorder;
            }
            else if (mousePos.x > RightBorder)
            {
                mousePos.x = RightBorder;
            }

            mousePos.y = 7;
            mousePos.z = 0;
            transform.position = Vector3.Lerp(transform.position, mousePos, 0.1f);
        }

    }

    public void Drag()
    {
        // 드래그 플래그 ON
        isDrag = true;
    }

    public void Drop()
    {
        // 드래그 플래그 OFF + 물리 효과 ON
        isDrag = false;
        rigid.simulated = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        isAttach = true;

        StartCoroutine(AttachRoutine());
    }

    IEnumerator AttachRoutine()
    {
        if (!isAttach)
            yield break;

        isAttach = true;
        manager.PlaySfx(GameManager.Sfx.Attach);

        yield return new WaitForSeconds(0.2f);
        isAttach = false;
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        // 충돌 상대편이 동글일 때
        if (collision.gameObject.tag == "Dongle")
        {
            Dongle other = collision.gameObject.GetComponent<Dongle>();
            // 조건 비교 (같은 레벨인지 + 지금 합쳐지는 중이 아닌지 + 만렙이 아닌지)
            if (level == other.level && !isMerge && !other.isMerge && level < 7)
            {
                // 나와 상대편 위치 가져오기
                float meX = transform.position.x;
                float meY = transform.position.y;
                float otherX = other.transform.position.x;
                float otherY = other.transform.position.y;

                // 내가 상대편보다 위에 있거나, 같은 높이에서 오른쪽에 있을 때
                if (meY < otherY || (meY == otherY && meX > otherX))
                {
                    other.Hide(transform.position);
                    LevelUp();
                }
            }
        }
    }

    void LevelUp()
    {
        isMerge = true;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0f;

        StartCoroutine("LevelUpRoutine");
    }

    IEnumerator LevelUpRoutine()
    {
        yield return new WaitForSeconds(0.2f);
        anim.SetInteger("Level", level+1);
        anim.SetBool("IsPlaying", false);
        StartCoroutine(ResetAnimation());
        manager.PlaySfx(GameManager.Sfx.LevelUp);
        EffectPlay();

        yield return new WaitForSeconds(0.3f);
        level++;
        // 최대 레벨 갱신
        manager.maxLevel = Mathf.Max(manager.maxLevel, level);
        // 잠금 OFF
        isMerge = false;
    }

    public void Hide(Vector3 targetPos)
    {
        // 잠금 ON
        isMerge = true;
        // 물리 효과 OFF
        rigid.simulated = false;
        // 충돌 OFF
        circleCollider.enabled = false;
        // 숨기기 코루틴 실행
        StartCoroutine("HideRoutine", targetPos);

        // 게임 오버 시 이펙트 플레이
        if (targetPos == Vector3.up * 100)
        {
            EffectPlay();
        }
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int timeCount = 0;
        while (timeCount < 20)
        {
            timeCount++;
            // 상대가 있을 시
            if (targetPos != Vector3.up * 100)
            {
                transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            }
            // 게임 오버 시
            else if (targetPos == Vector3.up * 100)
            {
                transform.localScale = Vector3.Lerp(transform.localScale, Vector3.zero, 0.2f);
            }
            yield return null;

        }
        // 점수 증가
        manager.score += (int)Mathf.Pow(2, level);
        // 비활성화
        gameObject.SetActive(false);
        // 잠금 OFF
        isMerge = false;
    }

    void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            // 데드타임 증가
            deadTime += Time.deltaTime;
            // 2초 이상 경과 시 빨간색으로 변경하여 경고
            if (deadTime > 2)
            {
                spriteRenderer.color = new Color(0.5f, 0.2f, 0.2f);
            }
            // 5초 이상 경과 시 게임 오버
            if (deadTime > 5)
            {
                manager.Result();
            }
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.tag == "Finish")
        {
            // 데드타임 및 색상 초기화
            deadTime = 0;
            spriteRenderer.color = Color.white;
        }
    }

    void OnDisable()
    {
        // 동글 속성 초기화
        level = 0;
        deadTime = 0;

        // 동글 위치, 크기, 회전값 초기화
        transform.localPosition = Vector3.zero;
        transform.localScale = Vector3.zero;
        transform.localRotation = Quaternion.identity;

        // 동글 물리 초기화
        rigid.simulated = false;
        rigid.velocity = Vector2.zero;
        rigid.angularVelocity = 0f;
        circleCollider.enabled = true;
    }

    void EffectPlay()
    {
        // 파티클 위치와 크기 설정
        effect.transform.position = transform.position;
        effect.transform.localScale = transform.localScale;
        // 파티클 플레이
        effect.Play();
    }
}
