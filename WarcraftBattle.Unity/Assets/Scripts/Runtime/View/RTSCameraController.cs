using UnityEngine;

namespace WarcraftBattle3D
{
    public class RTSCameraController : MonoBehaviour
    {
        [SerializeField]
        private float panSpeed = 20f;
        [SerializeField]
        private float panBorderThickness = 10f;
        [SerializeField]
        private Vector2 panLimitX = new Vector2(-100, 1000);
        [SerializeField]
        private Vector2 panLimitZ = new Vector2(-100, 1000);
        [SerializeField]
        private float scrollSpeed = 2000f;
        [SerializeField]
        private float minY = 10f;
        [SerializeField]
        private float maxY = 80f;

        private void Update()
        {
            Vector3 pos = transform.position;

            if (Input.GetKey("w") || Input.mousePosition.y >= Screen.height - panBorderThickness)
            {
                pos.z += panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("s") || Input.mousePosition.y <= panBorderThickness)
            {
                pos.z -= panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("d") || Input.mousePosition.x >= Screen.width - panBorderThickness)
            {
                pos.x += panSpeed * Time.deltaTime;
            }
            if (Input.GetKey("a") || Input.mousePosition.x <= panBorderThickness)
            {
                pos.x -= panSpeed * Time.deltaTime;
            }

            float scroll = Input.GetAxis("Mouse ScrollWheel");
            pos.y -= scroll * scrollSpeed * 100f * Time.deltaTime;

            pos.x = Mathf.Clamp(pos.x, panLimitX.x, panLimitX.y);
            pos.y = Mathf.Clamp(pos.y, minY, maxY);
            pos.z = Mathf.Clamp(pos.z, panLimitZ.x, panLimitZ.y);

            transform.position = pos;
        }
    }
}
