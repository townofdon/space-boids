using System.Collections;
using UnityEngine;

public class FadeIn : MonoBehaviour
{
    [SerializeField] float preFadeInTime = 0.3f;
    [SerializeField] float fadeInTime = 2f;
    [SerializeField] AnimationCurve fadeInCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

    CanvasGroup canvasGroup;

    float currentTime = float.MaxValue;

    // Start is called before the first frame update
    void Start()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        currentTime = preFadeInTime + fadeInTime;
        SetAlpha(1f);
        StartCoroutine(IFadeIn());
    }

    void SetAlpha(float val)
    {
        canvasGroup.alpha = val;
    }

    IEnumerator IFadeIn()
    {
        currentTime = preFadeInTime;
        SetAlpha(1f);

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            yield return null;
        }

        currentTime = fadeInTime;

        while (currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            canvasGroup.alpha = Mathf.Clamp01(fadeInCurve.Evaluate(currentTime / fadeInTime));
            Simulation.SetSimulationSpeed(1f - canvasGroup.alpha);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        Simulation.SetSimulationSpeed(1f);

        GlobalEvent.Invoke(GlobalEvent.type.SIMULATION_START);
    }
}
