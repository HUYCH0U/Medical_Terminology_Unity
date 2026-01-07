using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class StartMenu : MonoBehaviour
{
    private TMP_InputField time;
    private TMP_InputField turn;

    void Awake()
    {
        time = GameObject.Find("Canvas/Time").GetComponent<TMP_InputField>();
        turn = GameObject.Find("Canvas/Turn").GetComponent<TMP_InputField>();
    }

    public void StartGame()
    {
        PlayerPrefs.SetInt("Time", int.TryParse(time.text, out int Time) ? Time : 60);
        PlayerPrefs.SetInt("Turn", int.TryParse(turn.text, out int Turn) ? Turn : 3);
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainGame");
    }

    public void Exit()
    {
        Application.Quit();
    }

}
