using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoundSystem : MonoBehaviour
{
    private int currentRound = 0;
    public float timeBetweenRounds = 5f;
    private float timer;
    private bool isCountDown =false;


    public Action<int> OnRoundBegin;
    public Action OnRoundEnd;

    public int GetCurrentRound() { return currentRound; }
    public void StartRound()
    {
        currentRound++;
        OnRoundBegin?.Invoke(currentRound);
        Debug.Log("Round Started");
    }
    public void RoundFinished(EnemyManager enemyManager)
    {
        if (enemyManager.GetEnemiesCountLeft() <= 0 && enemyManager.GetEnemiesAlive() <= 0 && !isCountDown)
        {
            Debug.Log("Round finished");
            OnRoundEnd?.Invoke();
            StartCountDown();
        }
    }


    private void Update()
    {
        if (isCountDown)
        {
            timer -= Time.deltaTime;

            if (timer <= 0)
            {
                StartRound();
                isCountDown = false;
            }
        }
    }

    private void StartCountDown()
    {
        isCountDown = true;
        timer = timeBetweenRounds;
        Debug.Log("Count down Started");
    }
}