using UnityEngine;

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


        void SetUpCamera_test()
        {
            Vector3[] corners =
            {
                isoMatrix.MultiplyPoint3x4(Vector3.zero * cellSize), // Bottom-left
                isoMatrix.MultiplyPoint3x4(new Vector3(width, 0) * cellSize), // Bottom-right
                isoMatrix.MultiplyPoint3x4(new Vector3(0, height) * cellSize), // Top-left
                isoMatrix.MultiplyPoint3x4(new Vector3(width, height) * cellSize) // Top-right
            };

            Vector3 min = corners[0];
            Vector3 max = corners[0];

            foreach (var corner in corners)
            {
                min = Vector3.Min(min, corner);
                max = Vector3.Max(max, corner);
            }

            float padding = 1f;
            minX = min.x - padding;
            maxX = max.x + padding;
            minY = min.y - padding;
            maxY = max.y + padding;
            
            minX = -width - padding;
            maxX = width + padding;
            minY = 0 - padding;
            maxY = height + padding;
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
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0f)
            {
                cam.orthographicSize -= scroll * zoomSpeed;
                cam.orthographicSize = Mathf.Clamp(cam.orthographicSize, minZoom, maxZoom);
            }
        }

        void HandleDrag()
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragOrigin = cam.ScreenToWorldPoint(Input.mousePosition);
            }

            if (Input.GetMouseButton(0))
            {
                Vector3 currentPos = cam.ScreenToWorldPoint(Input.mousePosition);
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