using System.Collections;
using UnityEngine;
using DG.Tweening;
using TMPro;

public class AnimatedTitle : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI title;

    [Space]
    [Space]

    [SerializeField] float preFadeInTime = 0.5f;
    [SerializeField] float fadeInTime = 0.5f;
    [SerializeField] float holdTime = 2f;
    [SerializeField] float fadeOutTime = 0.5f;

    [Space]
    [Space]

    [SerializeField] float endTextOffset = 5f;
    [SerializeField] float textBlitTime = 2f;
    [SerializeField] AnimationCurve textBlitMod = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Space]
    [Space]

    [SerializeField] float spacingStart = 0f;
    [SerializeField] float spacingEnd = 0f;
    [SerializeField] float spacingTime = 2f;

    CanvasGroup canvasGroup;

    string _originalText = "";
    float _originalTextLengthQuotient = 1f;
    string originalText
    {
        get { return _originalText; }
        set
        {
            _originalText = value;
            _originalTextLengthQuotient = value.Length > 0 ? 1f / value.Length : float.PositiveInfinity;
        }
    }

    float offset = 0f;

    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
        offset = 0f;
        originalText = title.text;
        title.characterSpacing = spacingStart;
        StartCoroutine(IAnimate());
    }

    void Update()
    {
        title.text = GetModText();
    }

    string GetModText()
    {
        string newText = "";
        for (int i = 0; i < originalText.Length; i++)
        {
            newText += $"<voffset={GetModOffset(i)}em>{originalText[i]}</voffset>";
        }
        return newText;
    }

    float GetModOffset(int index)
    {
        float progress = Mathf.Clamp01((index + 1) * _originalTextLengthQuotient);
        float val = offset * (Utils.IsOdd(index) ? -1f : 1f) * textBlitMod.Evaluate(progress);
        return Mathf.Abs(val) < 0.001f ? 0f : val;
    }

    IEnumerator IAnimate()
    {
        yield return new WaitForSeconds(preFadeInTime);
        Tween fading = DOTween.To(() => canvasGroup.alpha, (float x) => canvasGroup.alpha = x, 1f, fadeInTime);
        Tween spacing = DOTween.To(() => title.characterSpacing, (float x) => title.characterSpacing = x, spacingEnd, spacingTime);
        if (fading.active) yield return fading.WaitForCompletion();
        if (spacing.active) yield return spacing.WaitForCompletion();
        yield return new WaitForSeconds(holdTime);
        DOTween.To(() => canvasGroup.alpha, (float x) => canvasGroup.alpha = x, 0f, fadeOutTime);
        DOTween.To(() => offset, (float x) => offset = x, endTextOffset, textBlitTime * 2f);
        DOTween.To(() => title.characterSpacing, (float x) => title.characterSpacing = x, spacingStart, textBlitTime);
    }
}
