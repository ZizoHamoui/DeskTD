using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonSelectIconDisplay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject GeneratorIconHover;
    public void OnPointerEnter(PointerEventData eventData)
    {
        GeneratorIconHover.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        GeneratorIconHover.SetActive(false);
    }
}
