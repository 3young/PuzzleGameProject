using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject donglePrefab;
    public Transform dongleGroup;
    public Dongle lastDongle;
    public int maxLevel;

    void Awake()
    {
        Application.targetFrameRate = 60;
    }

    void Start()
    {
        // 게임 시작 세팅
        GameStart();
    }

    void GameStart()
    {
        Invoke("NextDongle", 1.5f);
    }

    void NextDongle()
    {
        // 새로운 동글 생성 (생성 -> 레벨 설정 -> 활성화)
        GameObject instant = Instantiate(donglePrefab, dongleGroup);
        lastDongle = instant.GetComponent<Dongle>();
        lastDongle.manager = this;
        lastDongle.level = Random.Range(0, maxLevel); 
        instant.SetActive(true);

        // 다음 동글 생성을 기다리는 코루틴
        StartCoroutine(WaitNext());
        // StartCoroutine("WaitNext");

    }

    IEnumerator WaitNext()
    {
        // 현재 동글이 드랍될 때까지 대기
        while (lastDongle != null)
        {
            yield return null; // 한 프레임 쉼
        }
        
        yield return new WaitForSeconds(2f);

        // 다음 동글 생성 호출
        NextDongle();
    }

    public void TouchDown()
    {
        // 동글 드래그
        lastDongle.Drag();
    }

    public void TouchUp()
    {
        // 동글 드랍 (변수 비우기)
        lastDongle.Drop();
        lastDongle = null;
    }
}
