using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectLevel : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void StartLevel1()
    {
        SceneManager.LoadScene(4);
    }

    public void StartLevel2()
    {
        SceneManager.LoadScene(5);
    }

    public void StartLevel3()
    {
        SceneManager.LoadScene(6);
    }

    // Back to Menu scene
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }

}
