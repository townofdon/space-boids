using UnityEngine;

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

    [SerializeField] ParticleSystem eatenFx;

    SpriteRenderer spriteRenderer;

    public bool isEaten = false;
    Boid currentBoid;

    public void GetChomped()
    {
        if (isEaten) return;
        hp--;
        if (hp <= 0f) Perish();
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

    // void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (!other.CompareTag("Boid")) return;
    //     currentBoid = other.GetComponent<Boid>();
    //     if (currentBoid == null) return;
    //     if (currentBoid.GetFoodType() != foodType) return;

    //     hp--;

    //     if (hp <= 0f) Perish();
    // }

    void Perish()
    {
        if (isEaten) return;
        isEaten = true;
        spriteRenderer.enabled = false;
        eatenFx.Play();
        Destroy(gameObject, 5f);
    }
}