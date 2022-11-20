using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using FMODUnity;

public class PowerTank : MonoBehaviour
{
    [SerializeField] Food.FoodType foodType;
    [SerializeField] ParticleSystem acquireFX;
    [SerializeField] ParticleSystemForceField particleAttractor;
    [SerializeField] Light2D light;
    [SerializeField] SpriteRenderer lightWebGL;

    [Space]
    [Space]

    [SerializeField][Range(0f, 1f)] float hoverRate = 1f;
    [SerializeField][Range(0f, 5f)] float hoverX = 0f;
    [SerializeField][Range(0f, 5f)] float hoverY = 1f;

    [Space]
    [Space]

    [SerializeField] StudioEventEmitter acquireSound;

    Vector2 initialPosition;
    float initialLightTransparency;
    float t = 0f;
    bool isAcquired = false;
    Tween intensify;
    float timeSinceLastAnimationEvent = 0f;

    // animation event
    public void SetLightIntensity(float value)
    {
        if (isAcquired) return;
        if (intensify != null) intensify.Kill();
        // intensify = DOTween.To(() => light.intensity, (x) => light.intensity = x, value, timeSinceLastAnimationEvent).SetEase(Ease.Linear);
        light.intensity = value;
        lightWebGL.color = lightWebGL.color.toAlpha(value * initialLightTransparency);
        timeSinceLastAnimationEvent = 0f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isAcquired) return;
        if (!other.CompareTag("Player")) return;

        switch (foodType)
        {
            case Food.FoodType.Pod:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_BLUE_POWER_TANK);
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_BLUE_POWER_TANK);
                break;
            case Food.FoodType.Xenon:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_RED_POWER_TANK);
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_RED_POWER_TANK);
                break;
            case Food.FoodType.Freighter:
                GlobalEvent.Invoke(GlobalEvent.type.ACQUIRE_YELLOW_POWER_TANK);
                GlobalEvent.Invoke(GlobalEvent.type.SELECT_YELLOW_POWER_TANK);
                break;
        }

        acquireSound.Play();
        isAcquired = true;
        GetComponent<SpriteRenderer>().enabled = false;
        if (intensify != null) intensify.Kill();
        light.enabled = false;
        lightWebGL.enabled = false;
        acquireFX.Play();
        particleAttractor.transform.SetParent(other.transform);
        particleAttractor.transform.position = other.transform.position;
        Destroy(gameObject, 6f);
        Destroy(particleAttractor.gameObject, 7f);
    }

    void Start()
    {
        initialPosition = transform.position;
        initialLightTransparency = lightWebGL.color.a;
        GetComponent<Rigidbody2D>().isKinematic = true;
    }

    void Update()
    {
        transform.position = initialPosition + new Vector2(
            Mathf.Sin(t * hoverRate) * hoverX,
            Mathf.Sin(t * hoverRate) * hoverY
        );
        t += Time.deltaTime;
        timeSinceLastAnimationEvent += Time.deltaTime;
    }
}
