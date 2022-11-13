using UnityEngine;
using UnityEngine.UI;

public class UIPowerTank : MonoBehaviour
{
    [SerializeField] Color activeColor = Color.white;
    [SerializeField] Color inactiveColor = new Color(0.5f, 0.5f, 0.5f, 1f);

    [Space]
    [Space]

    [SerializeField] GameObject filled;
    [SerializeField] GameObject arrowTop;
    [SerializeField] GameObject arrowBottom;

    Image filledImg;

    bool isAcquired = false;
    bool isActive = false;

    public void SetActive(bool value)
    {
        isActive = value;
        RenderUI();
    }

    public void FlagAcquired()
    {
        isAcquired = true;
        RenderUI();
    }

    private void Awake()
    {
        filledImg = filled.GetComponent<Image>();
        RenderUI();
    }

    void RenderUI()
    {
        filledImg.color = isAcquired && isActive ? activeColor : inactiveColor;
        filled.SetActive(isAcquired);
        arrowTop.SetActive(isAcquired && isActive);
        arrowBottom.SetActive(isAcquired && isActive);
    }
}