using UnityEngine;
using UnityEngine.InputSystem;

namespace Script.CameraSystem
{
    public class CameraController : MonoBehaviour
    {
        [Header("Grid Settings")] public int width = 100;
        public int height = 100;
        public float cellSize = 1f;

        [Header("Zoom")] public float zoomSpeed = 5f;
        public float minZoom = 1f;
        public float maxZoom = 30f;
        public float initialZoom = 5f;

        [Header("Drag")] public float dragSpeed = 1f;
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _pointAction;
        [SerializeField] private InputActionReference _panHoldAction;
        [SerializeField] private InputActionReference _zoomAction;

        private Vector3 dragOrigin;
        private Camera cam;

        //Confini camera
        private float minX;
        private float maxX;
        private float minY;
        private float maxY;

        private Matrix4x4 isoMatrix;

        void Start()
        {
            cam = GetComponent<Camera>();
            InitIsoMatrix();
            CenterCamera();
            SetUpCamera();
        }

        void Update()
        {
            HandleZoom();
            HandleDrag();
        }

        private void OnEnable()
        {
            _pointAction?.action?.Enable();
            _panHoldAction?.action?.Enable();
            _zoomAction?.action?.Enable();
        }

        private void OnDisable()
        {
            _pointAction?.action?.Disable();
            _panHoldAction?.action?.Disable();
            _zoomAction?.action?.Disable();
        }

        private void OnDestroy()
        {
            OnDisable();
        }

        void InitIsoMatrix()
        {
            isoMatrix = Matrix4x4.identity;
            isoMatrix.SetColumn(0, new Vector4(1f, 0.5f, 0f, 0f));
            isoMatrix.SetColumn(1, new Vector4(-1f, 0.5f, 0f, 0f));
            isoMatrix.SetColumn(2, new Vector4(0f, 0f, 1f, 0f));
        }

        void CenterCamera()
        {
            Vector3 centerGrid = new Vector3(width / 2f, height / 2f) * cellSize;
            Vector3 isoCenter = isoMatrix.MultiplyPoint3x4(centerGrid);
            cam.transform.position = new Vector3(isoCenter.x, isoCenter.y, -10f);

            cam.orthographicSize = initialZoom;
        }

        void SetUpCamera()
        {
            float padding = 2f;
            minX = -width - padding;
            maxX = width + padding;
            minY = 0 - padding;
            maxY = height + padding;
        }


        void HandleZoom()
        {
            float scroll = 0f;
            if (_zoomAction != null && _zoomAction.action != null)
            {
                scroll = _zoomAction.action.ReadValue<float>();
            }

            if (scroll != 0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }

        void HandleDrag()
        {
            if (_panHoldAction != null && _panHoldAction.action != null && _panHoldAction.action.WasPressedThisFrame())
            {
                dragOrigin = cam.ScreenToWorldPoint(ReadMousePosition());
            }

            if (_panHoldAction != null && _panHoldAction.action != null && _panHoldAction.action.IsPressed())
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(ReadMousePosition());
                Vector3 delta = dragOrigin - currentPos;

                transform.position += delta;

                // Clamping con margini
                float marginX = 1f;
                float marginY = 1f;

                transform.position = new Vector3(
                    Mathf.Clamp(transform.position.x, minX - marginX, maxX + marginX),
                    Mathf.Clamp(transform.position.y, minY - marginY, maxY + marginY),
                    transform.position.z
                );
            }
        }

        private Vector3 ReadMousePosition()
        {
            if (_pointAction != null && _pointAction.action != null)
            {
                Vector2 screenPos = _pointAction.action.ReadValue<Vector2>();
                return new Vector3(screenPos.x, screenPos.y, 0f);
            }

            if (Mouse.current != null)
            {
                Vector2 screenPos = Mouse.current.position.ReadValue();
                return new Vector3(screenPos.x, screenPos.y, 0f);
            }

            return Vector3.zero;
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(minX, maxY, 0));
            Gizmos.DrawLine(new Vector3(maxX, minY, 0), new Vector3(maxX, maxY, 0));
            Gizmos.DrawLine(new Vector3(minX, minY, 0), new Vector3(maxX, minY, 0));
            Gizmos.DrawLine(new Vector3(minX, maxY, 0), new Vector3(maxX, maxY, 0));
        }
    }
}
