using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameConfig : MonoBehaviour
{
     protected int GameTurns ;
     protected int TurnTime;
     protected int Score;
    public int score
    {
        get { return Score; }
        private set { Score = value; }
    }
    public int turntime
    {
        get { return TurnTime; }
        private set { TurnTime = value; }
    }
    public int gameturns
    {
        get { return GameTurns; }
        private set { GameTurns = value; }
    }

    public void getConfig()
    {
        GameTurns = PlayerPrefs.GetInt("Turn");
        TurnTime = PlayerPrefs.GetInt("Time");
        Score = 0;
    }

    public void UpdateScore(int value)
    {
        score += value;
    }

    public void DecreaseTurn()
    {
        gameturns -= 1;
    }

}
