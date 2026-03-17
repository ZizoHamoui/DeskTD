using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Handbook : MonoBehaviour
{
    public List<GameObject> StationarySkillPages = new List<GameObject>();

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
    public void ShowSparkTowerDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "SparktowerIMG");
        temp.SetActive(true);
    }

    public void ShowCompassDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "CompassIMG");
        temp.SetActive(true);
    }

    public void ShowPencilDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "PencilIMG");
        temp.SetActive(true);
    }
    public void ShowEraserDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "EraserIMG");
        temp.SetActive(true);
    }
    public void ShowStickyNoteDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "StickyNoteIMG");
        temp.SetActive(true);
    }
    public void ShowRulerDetail()
    {
        CloseAllPages();
        // Find the Spark Tower detail img
        GameObject temp = StationarySkillPages.Find(obj => obj.name == "RulerIMG");
        temp.SetActive(true);
    }

    // Close all the detail pages
    private void CloseAllPages()
    {
        for (int i = 0; i < StationarySkillPages.Count; i++)
        {
            StationarySkillPages[i].SetActive(false);
        }
    }

    // Back to Menu scene
    public void BackToMenu()
    {
        SceneManager.LoadScene(0);
    }
}
