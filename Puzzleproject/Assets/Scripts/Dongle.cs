using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Dongle : MonoBehaviour
{
    public GameManager manager;
    public int level;

    Rigidbody2D rigid;
    Animator anim;
    CircleCollider2D circleCollider;

    bool isDrag;
    bool isMerge;

    private void Awake()
    {
        rigid = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
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
            mousePos.y = 8;
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

    private void OnCollisionStay2D(Collision2D collision)
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

        yield return new WaitForSeconds(0.3f);
        level++;
        // 최대 레벨 갱신
        manager.maxLevel = Mathf.Max(manager.maxLevel, level);
        // 잠금 OFF
        isMerge = false;
    }

    void Hide(Vector3 targetPos)
    {
        // 잠금 ON
        isMerge = true;
        // 물리 효과 OFF
        rigid.simulated = false;
        // 충돌 OFF
        circleCollider.enabled = false;
        // 숨기기 코루틴 실행
        StartCoroutine("HideRoutine", targetPos);
    }

    IEnumerator HideRoutine(Vector3 targetPos)
    {
        int timeCount = 0;
        while (timeCount < 20)
        {
            timeCount++;
            transform.position = Vector3.Lerp(transform.position, targetPos, 0.5f);
            yield return null;
        }
        // 비활성화
        gameObject.SetActive(false);
        // 잠금 OFF
        isMerge = false;
    }
}
