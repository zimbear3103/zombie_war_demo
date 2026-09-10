using UnityEngine;

public class BombController : MonoBehaviour
{
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionForce = 700f;
    [SerializeField] private float explosionDelay = 3f;

    [SerializeField] private GameObject explosionEffectPrefab;
    private float countdown;
    private bool hasExploded = false;
    private void Start()
    {
        // Initialize bomb properties here
    }

    private void Update()
    {
        if (hasExploded) return;
        if (countdown > 0)
        {
            countdown -= Time.deltaTime;
            if (countdown <= 0)
            {
                Explode();
                hasExploded = true;
            }
        }
    }
    public void Explode()
    {
        // Implement explosion logic here
        Instantiate(explosionEffectPrefab, transform.position, transform.rotation);

        KnockBack();

        Debug.Log("Bomb exploded!");
        Destroy(gameObject);
    }   
    private void KnockBack()
    {

        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);

        foreach (Collider nearbyObject in colliders)
        {
            Rigidbody rb = nearbyObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius);
            }
        }
    }
}
