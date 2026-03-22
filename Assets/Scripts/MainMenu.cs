using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject GeneratorIconHover;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Load the intro scene, Pip's dialogues
    public void StartGame()
    {
        SceneManager.LoadScene(1);
    }

    // Load the level selecting scene
    public void SelectLevel()
    {
        SceneManager.LoadScene(2);
    }

    // Load the handbook scene
    public void OpenHandbook()
    {
        SceneManager.LoadScene(3);
    }

    // Quit the game
    public void QuitGame()
    {
        Application.Quit();
    }
}
