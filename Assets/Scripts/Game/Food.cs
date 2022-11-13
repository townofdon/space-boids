using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;

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

    [Space]
    [Space]

    [SerializeField] float explosionIntensity = 3f;
    [SerializeField] float explosionTime = 1f;

    [Space]
    [Space]

    [SerializeField] float timeBetweenChomps = 0.2f;
    [SerializeField] ParticleSystem eatenFx;
    [SerializeField] ParticleSystem chompedFX;
    [SerializeField] new Light2D light;

    SpriteRenderer spriteRenderer;

    public bool isEaten = false;
    float initialLightIntensity = 1f;
    Boid currentBoid;

    float timeSinceLastAnimationEvent = 0f;
    float timeSinceLastChomped = float.MaxValue;
    Tween intensify;

    // animation event
    public void SetLightIntensity(float value)
    {
        if (isEaten) return;
        intensify.Kill();
        intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, value, timeSinceLastAnimationEvent).SetEase(Ease.Linear);
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

            // TODO: PLAY SOUND
        }
    }

    private void OnEnable()
    {
        Simulation.RegisterFood(this);
    }

    private void OnDisable()
    {
        Simulation.DeregisterFood(this);
    }

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = true;
    }

    void Start()
    {
        initialLightIntensity = light.intensity;
    }

    void Update()
    {
        timeSinceLastAnimationEvent += Time.deltaTime;
        timeSinceLastChomped += Time.deltaTime;
    }

    void Perish()
    {
        if (isEaten) return;

        // TODO: PLAY SOUND

        isEaten = true;
        spriteRenderer.enabled = false;
        eatenFx.Play();
        Destroy(gameObject, 5f);
        StartCoroutine(ExplosionFlash());
    }

    IEnumerator ExplosionFlash()
    {
        intensify.Kill();
        intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, explosionIntensity, 0.1f);
        yield return intensify.WaitForCompletion();
        intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, 0f, explosionTime);
        yield return intensify.WaitForCompletion();
    }
}
