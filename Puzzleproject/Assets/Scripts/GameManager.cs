using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("-------[ Core ]")]
    public int maxLevel;
    public bool isOver;
    public int score;

    [Header("-------[ Object Pooling ]")]
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public GameObject effectPrefab;
    public Transform effectGroup;
    [Range(1, 20)]
    public int poolSize;
    public List<Dongle> donglePool;
    public List<ParticleSystem> effectPool;
    Dongle lastDongle;
    int poolCursor;

    [Header("-------[ Audio ]")]
    public AudioSource bgmPlayer;
    public AudioSource[] sfxPlayers; // Change from AudioSource to AudioSource[]
    public AudioClip[] sfxClips;
    int sfxCursor;

    [Header("-------[ UI ]")]
    public GameObject line;
    public GameObject floor; 
    public GameObject startGroup;
    public GameObject endGroup;
    public Text scoreText;
    public Text maxScoreText;
    public Text subScoreText;

    public enum Sfx { LevelUp, Next, Attach, Button, GameOver };


    void Awake()
    {
        // 프레임 설정 (FPS 60)
        Application.targetFrameRate = 60;

        // 오브젝트 풀 시작
        donglePool = new List<Dongle>();
        effectPool = new List<ParticleSystem>();
        for(int index = 0; index < poolSize; index++)
        {
            MakeDongle(index);
        }

        // 최대 점수 설정
        if(PlayerPrefs.HasKey("MaxScore"))
        {
            PlayerPrefs.SetInt("MaxScore", 0);
        }

        maxScoreText.text = PlayerPrefs.GetInt("MaxScore").ToString();
    }

    Dongle MakeDongle(int id)
    {
        // 새로운 이펙트 생성 + 풀에 추가
        GameObject instantEffect = Instantiate(effectPrefab, effectGroup);
        instantEffect.name = "Effect_" + id;
        ParticleSystem instantEffectParticle = instantEffect.GetComponent<ParticleSystem>();
        effectPool.Add(instantEffectParticle);

        // 새로운 동글 생성 (생성 -> 레벨 설정 -> 활성화) + 풀에 추가
        GameObject instantDongle = Instantiate(donglePrefab, dongleGroup);
        Dongle instantDongleLogic = instantDongle.GetComponent<Dongle>();
        instantDongle.name = "Dongle_" + id;
        instantDongleLogic.manager = this;
        instantDongleLogic.effect = instantEffectParticle;
        donglePool.Add(instantDongleLogic);

        return instantDongleLogic;
    }

    Dongle GetDongle()
    {
        for(int index = 0; index < donglePool.Count; index++)
        {
            poolCursor = (poolCursor + 1) % donglePool.Count;
            if (!donglePool[poolCursor].gameObject.activeSelf)
            {
                return donglePool[poolCursor];
            }
        }

        return MakeDongle(donglePool.Count);
    }

    public void GameStart()
    {
        // UI 컨트롤
        startGroup.SetActive(false);
        line.SetActive(true);
        floor.SetActive(true);
        scoreText.gameObject.SetActive(true);
        maxScoreText.gameObject.SetActive(true);

        // 버튼 효과음
        PlaySfx(Sfx.Button);

        // BGM 시작
        bgmPlayer.Play();

        // 동글 생성 시작
        Invoke("NextDongle", 1.5f);
    }

    void NextDongle()
    {
        if (isOver)
            return;

        // 다음 동글 가져오기
        lastDongle = GetDongle();
        lastDongle.level = Random.Range(0, maxLevel);
        lastDongle.gameObject.SetActive(true);

        // 다음 동글 생성을 기다리는 코루틴
        StartCoroutine(WaitNext());
        // StartCoroutine("WaitNext");

        // 효과음 재생
        PlaySfx(Sfx.Next);
    }

    IEnumerator WaitNext()
    {
        // 현재 동글이 드랍될 때까지 대기
        while (lastDongle != null)
        {
            yield return null; // 한 프레임 쉼
        }
        
        yield return new WaitForSeconds(2.5f);

        // 다음 동글 생성 호출
        NextDongle();
    }

    public void TouchDown()
    {
        if (lastDongle == null)
            return;

        // 동글 드래그
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        if (lastDongle == null)
            return;

        // 동글 드랍 (변수 비우기)
        lastDongle.Drop();
        lastDongle = null;
    }

    public void Result()
    {
        // 게임 오버 및 결산
        isOver = true;
        bgmPlayer.Stop();

        StartCoroutine("ResultRoutine");
    }

    IEnumerator ResultRoutine()
    {
        // 남아있는 동글들을 순차적으로 숨김
        for (int index = 0; index < donglePool.Count; index++)
        {
            if (donglePool[index].gameObject.activeSelf)
            {
                donglePool[index].Hide(Vector3.up * 100);
                yield return new WaitForSeconds(0.05f); 
            }
        }

        yield return new WaitForSeconds(1f);
        // 점수 적용
        subScoreText.text = "점수 : " + scoreText.text;
        // 최대 점수 갱신
        int maxScore = Mathf.Max(PlayerPrefs.GetInt("MaxScore"), score);
        PlayerPrefs.SetInt("MaxScore", maxScore);
        // UI 띄우기
        endGroup.SetActive(true);
        // 효과음 재생
        PlaySfx(Sfx.GameOver);
    }

    public void Reset()
    {
        // 효과음 재생
        PlaySfx(Sfx.Button);
        StartCoroutine(ResetRoutine());
    }

    IEnumerator ResetRoutine()
    {
        yield return new WaitForSeconds(1.0f);
        // 장면 다시 불러오기
        SceneManager.LoadScene(0);
    }

    public void PlaySfx(Sfx type)
    {
        // SFX 플레이어 커서 이동
        sfxCursor = (sfxCursor + 1) % sfxPlayers.Length;

        // 효과음 종류에 따라 클립 설정
        switch (type)
        {
            case Sfx.LevelUp:
                sfxPlayers[sfxCursor].clip = sfxClips[Random.Range(0, 3)];
                break;
            case Sfx.Next:
                sfxPlayers[sfxCursor].clip = sfxClips[3];
                break;
            case Sfx.Attach:
                sfxPlayers[sfxCursor].clip = sfxClips[4];
                break;
            case Sfx.Button:
                sfxPlayers[sfxCursor].clip = sfxClips[5];
                break;
            case Sfx.GameOver:
                sfxPlayers[sfxCursor].clip = sfxClips[6];
                break;
        }

        // 효과음 재생
        sfxPlayers[sfxCursor].Play();
    }

    void LateUpdate()
    {
        scoreText.text = score.ToString();  
    }
}
