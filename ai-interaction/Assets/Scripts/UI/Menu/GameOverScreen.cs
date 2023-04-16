using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class GameOverScreen : MonoBehaviour
{
    public TMP_Text pointsText;
    public EnvController m_EnvController;

    private void Awake()
    {
        this.gameObject.SetActive(false);
    }

    public void GameOver(int score)
    {
        // this.gameObject.SetActive(true);
        pointsText.text = score.ToString() + " POINTS";
    }

    public void Restart()
    {
        this.gameObject.SetActive(false);
        m_EnvController.ResetScene();
        m_EnvController.activePiece.board.Reset();
    }

    public void Exit()
    {
        SceneManager.LoadScene(0);
    } 


}
