using UnityEngine;

public class BulletTrailEffect : MonoBehaviour
{
    [SerializeField] private TrailRenderer Trail;

    [SerializeField] private AnimationCurve WidthCurve;
    [SerializeField] private float Time = 0.5f;
    [SerializeField] private float MinVertexDistance = 0.1f;
    [SerializeField] private Gradient ColorGradient;
    [SerializeField] private Material Material;
    [SerializeField] private int CornerVertices;
    [SerializeField] private int EndCapVertices;

    [SerializeField]
    private Renderer Renderer;
    [SerializeField] private float autoDestroyTime = 1.0f;
    private bool IsDisabling = false;

    protected const string DISABLE_METHOD_NAME = "Disable";
    protected const string DO_DISABLE_METHOD_NAME = "DoDisable";

    private void Awake()
    {
        if (Trail == null)
            Trail = GetComponent<TrailRenderer>();
    }

    private void OnEnable()
    {
        Renderer.enabled = true;
        IsDisabling = false;
        CancelInvoke(DISABLE_METHOD_NAME);
        ConfigureTrail();

        Invoke(DISABLE_METHOD_NAME, autoDestroyTime);
    }
    public void SetupTrail(TrailRenderer TrailRenderer)
    {
        TrailRenderer.widthCurve = WidthCurve;
        TrailRenderer.time = Time;
        TrailRenderer.minVertexDistance = MinVertexDistance;
        TrailRenderer.colorGradient = ColorGradient;
        TrailRenderer.sharedMaterial = Material;
        TrailRenderer.numCornerVertices = CornerVertices;
        TrailRenderer.numCapVertices = EndCapVertices;
    }

    private void ConfigureTrail()
    {
        if (Trail != null)
        {
            SetupTrail(Trail);
        }
    }

    protected void Disable()
    {
        CancelInvoke(DISABLE_METHOD_NAME);
        CancelInvoke(DO_DISABLE_METHOD_NAME);


        if (Trail != null)
        {
            IsDisabling = true;
            Invoke(DO_DISABLE_METHOD_NAME, Time);
        }
        else
        {
            DoDisable();
        }
    }

    protected void DoDisable()
    {
        if (Trail != null)
        {
            Trail.Clear();
        }

        gameObject.SetActive(false);
    }
}
