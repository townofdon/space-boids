using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using FMODUnity;

public class Food : MonoBehaviour
{
    public enum FoodType
    {
        Pod,
        Xenon,
        Freighter,
    }

    [SerializeField] public FoodType foodType;
    [SerializeField] float hp = 3f;
    [SerializeField] float lifetime = 30f;

    [Space]
    [Space]

    [SerializeField] float explosionIntensity = 3f;
    [SerializeField] float explosionTime = 1f;

    [Space]
    [Space]

    [SerializeField] float timeBetweenChomps = 0.2f;
    [SerializeField] ParticleSystem eatenFx;
    [SerializeField] ParticleSystem chompedFX;
    [SerializeField] Light2D light;
    [SerializeField] SpriteRenderer lightWebGL;

    [Space]
    [Space]

    [SerializeField] StudioEventEmitter chompSound;
    [SerializeField] StudioEventEmitter perishSound;

    SpriteRenderer spriteRenderer;
    GameManager gameManager;

    public bool isEaten = false;
    float initialLightIntensity = 1f;
    float initialLightTransparency = 1f;
    Boid currentBoid;

    float timeSinceLastAnimationEvent = 0f;
    float timeSinceLastChomped = float.MaxValue;
    Tween intensify;
    Tween transparencify;

    float t;

    // animation event
    public void SetLightIntensity(float value)
    {
        if (isEaten) return;
        if (intensify != null) intensify.Kill();
        // intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, value * initialLightIntensity, timeSinceLastAnimationEvent).SetEase(Ease.Linear);
        light.intensity = value * initialLightIntensity;
        lightWebGL.color = lightWebGL.color.toAlpha(value * initialLightTransparency);
        timeSinceLastAnimationEvent = 0f;
    }

    public void GetChomped()
    {
        if (isEaten) return;
        if (timeSinceLastChomped < timeBetweenChomps) return;

        timeSinceLastChomped = 0f;
        hp--;
        if (hp <= 0f)
        {
            Perish();
        }
        else
        {
            chompedFX.Play();
            chompSound.Play();
        }
    }

    void OnEnable()
    {
        Simulation.RegisterFood(this);
    }

    void OnDisable()
    {
        Simulation.DeregisterFood(this);
        intensify.Kill();
        transparencify.Kill();
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = true;
        gameManager = GameObject.FindWithTag("GameManager").GetComponent<GameManager>();
    }

    void Start()
    {
        initialLightIntensity = light.intensity;
        initialLightTransparency = lightWebGL.color.a;
        if (gameManager.UseJobs)
        {
            light.SetActiveAndEnable(true);
            lightWebGL.SetActiveAndEnable(false);
        }
        else
        {
            light.SetActiveAndEnable(false);
            lightWebGL.SetActiveAndEnable(true);
        }
    }

    void Update()
    {
        timeSinceLastAnimationEvent += Time.deltaTime;
        timeSinceLastChomped += Time.deltaTime;
        t += Time.deltaTime * Simulation.speed;
        if (t >= lifetime) Perish();
    }

    void Perish()
    {
        if (isEaten) return;

        perishSound.Play();
        isEaten = true;
        spriteRenderer.enabled = false;
        eatenFx.Play();
        Destroy(gameObject, 5f);
        StartCoroutine(ExplosionFlash());
    }

    IEnumerator ExplosionFlash()
    {
        if (intensify != null) intensify.Kill();
        intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, explosionIntensity, 0.1f);
        transparencify = DOTween.To(() => lightWebGL.color.a, (x) => lightWebGL.color = lightWebGL.color.toAlpha(x), Mathf.Clamp01(explosionIntensity), 0.1f);
        yield return intensify.WaitForCompletion();
        yield return transparencify.WaitForCompletion();
        intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, 0f, explosionTime);
        transparencify = DOTween.To(() => lightWebGL.color.a, (x) => lightWebGL.color = lightWebGL.color.toAlpha(x), 0f, 0.1f);
        yield return intensify.WaitForCompletion();
        yield return transparencify.WaitForCompletion();
    }
}
