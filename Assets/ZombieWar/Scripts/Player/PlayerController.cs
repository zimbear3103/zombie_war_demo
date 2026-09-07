using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    private Vector3 m_moveAmt = Vector3.zero;
    [SerializeField] private float m_moveSpeed = 5f;
    //private InputSystem inputSystem;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnMove(InputAction.CallbackContext ctx)
    {
        m_moveAmt = ctx.ReadValue<Vector2>();
    }

    private void Update()
    {
        onPlayerMove();
    }

    public void onPlayerMove()
    {
        Vector3 movement = new Vector3(m_moveAmt.x, 0f, m_moveAmt.y);

        transform.Translate(movement * m_moveSpeed * Time.deltaTime, Space.World);
    }
}
