using UnityEngine;

[CreateAssetMenu(fileName = "WeaponScriptableObject", menuName = "WeaponScriptableObjects/Weapon")]
public class WeaponScriptableObject : MonoBehaviour
{
    [SerializeField] private GameObject m_weaponPrefab;
    [SerializeField] private float m_fireRate = 0.5f;
    [SerializeField] private float m_damage = 10f;
    [SerializeField] private float m_range = 100f;
    
    public GameObject WeaponPrefab => m_weaponPrefab;
    public float FireRate => m_fireRate;
    public float Damage => m_damage;
    public float Range => m_range;

}
