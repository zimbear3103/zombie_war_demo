using UnityEngine;

public class InputHandler : MonoBehaviour
{
    private void Awake()
    {
        Input.multiTouchEnabled = true;
    }

    private void Update()
    {
        var controller = GamePlayController.Instance;
        if (controller == null || !controller.IsGamePlaying || TileSpawner.Instance == null)
            return;

        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);
            if (touch.phase == TouchPhase.Began)
                TileSpawner.Instance.TapAtScreenPosition(touch.position);
        }

        if (Input.touchCount == 0 && Input.GetMouseButtonDown(0))
            TileSpawner.Instance.TapAtScreenPosition(Input.mousePosition);
    }
}
