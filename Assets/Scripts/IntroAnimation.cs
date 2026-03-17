using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class IntroAnimation : MonoBehaviour
{
    public float pageTimer;
    private float getPageTimer;
    public List<GameObject> IntroAnimationImgs = new List<GameObject>();
    private int pageIndex;

    [SerializeField] TextMeshProUGUI _textMeshPro;
    public string[] stringArray;
    [SerializeField] float timeBetweenChars;
    [SerializeField] float timeBetweenWords;
    private int textIndex = 0;
    private bool isTyping = false;
    private bool waitingForTimer = false;

    void Start()
    {
        // Auto-populate list if Inspector references are missing
        if (IntroAnimationImgs == null || IntroAnimationImgs.Count == 0 || IntroAnimationImgs[0] == null)
        {
            IntroAnimationImgs = new List<GameObject>();
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
            {
                if (sr.gameObject != this.gameObject && sr.gameObject.name.StartsWith("prologue"))
                    IntroAnimationImgs.Add(sr.gameObject);
            }
            IntroAnimationImgs.Sort((a, b) => a.name.CompareTo(b.name));
        }

        if (IntroAnimationImgs.Count == 0)
        {
            Debug.LogError("No intro images found!");
            SceneManager.LoadScene(2);
            return;
        }

        pageIndex = 0;
        getPageTimer = pageTimer;

        // Ensure only the first image is active
        for (int idx = 0; idx < IntroAnimationImgs.Count; idx++)
        {
            IntroAnimationImgs[idx].SetActive(idx == 0);
        }

        StartTyping();
    }

    void Update()
    {
        if (IntroAnimationImgs == null || IntroAnimationImgs.Count == 0) return;
        if (isTyping) return;

        // Text finished typing — wait for the page timer before advancing
        if (!waitingForTimer)
        {
            waitingForTimer = true;
            pageTimer = getPageTimer;
        }

        if (pageTimer > 0)
        {
            pageTimer -= Time.deltaTime;
            return;
        }

        // Timer expired — advance to next page
        waitingForTimer = false;

        if (pageIndex < IntroAnimationImgs.Count - 1)
        {
            IntroAnimationImgs[pageIndex].SetActive(false);
            pageIndex++;
            IntroAnimationImgs[pageIndex].SetActive(true);
            StartTyping();
        }
        else
        {
            SceneManager.LoadScene(2);
        }
    }

    private void StartTyping()
    {
        if (textIndex < stringArray.Length)
        {
            _textMeshPro.text = stringArray[textIndex];
            StartCoroutine(TextVisible());
        }
    }

    private IEnumerator TextVisible()
    {
        isTyping = true;
        _textMeshPro.ForceMeshUpdate();
        int totalVisibleCharacters = _textMeshPro.textInfo.characterCount;
        int counter = 0;

        while (true)
        {
            int visibleCount = counter % (totalVisibleCharacters + 1);
            _textMeshPro.maxVisibleCharacters = visibleCount;

            if (visibleCount >= totalVisibleCharacters)
            {
                textIndex++;
                break;
            }

            counter += 1;
            yield return new WaitForSeconds(timeBetweenChars);
        }

        isTyping = false;
    }

    public void SkipIntroScene()
    {
        // Load the level selecting scene
        SceneManager.LoadScene(2);
    }
}
