using UnityEngine;

public class CrateAmmo : MonoBehaviour
{
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") == true)
        {
            var player = collision.gameObject.GetComponent<PlayerController>();
            if (player != null) {
                Debug.Log("Refill Ammo");
                player.OnRefillAllAmmo();
            }
        }
    }
}
