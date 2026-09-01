using UnityEngine;

namespace ChalkMaze
{
    public sealed class CameraRig : MonoBehaviour
    {
        public Transform Target;
        public float CellsOnScreen = 6.2f;   // 취향 값. 크게 하면 넓게, 작게 하면 크게 보인다.

        Camera _cam;
        float _shake;
        Vector3 _pos;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            _cam.orthographic = true;
            _cam.backgroundColor = Palette.Void;
            _cam.clearFlags = CameraClearFlags.SolidColor;
            _cam.transform.position = new Vector3(0, 0, -10);
        }

        public void Kick(float amount) => _shake = Mathf.Max(_shake, amount);

        public void SnapTo(Vector3 worldPos)
        {
            _pos = new Vector3(worldPos.x, worldPos.y, -10);
            transform.position = _pos;
        }

        void LateUpdate()
        {
            // 화면의 짧은 변에 항상 CellsOnScreen 칸이 들어오게 한다.
            // 세로든 가로든 체감 배율이 같아진다.
            float aspect = Mathf.Max(0.3f, _cam.aspect);
            _cam.orthographicSize = CellsOnScreen * 0.5f / Mathf.Min(1f, aspect);

            if (Target != null)
            {
                var want = new Vector3(Target.position.x, Target.position.y, -10);
                _pos = Vector3.Lerp(_pos, want, 1f - Mathf.Exp(-14f * Time.deltaTime));
            }

            Vector3 jitter = Vector3.zero;
            if (_shake > 0.001f)
            {
                _shake *= Mathf.Exp(-7f * Time.deltaTime);
                jitter = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0) * _shake * 0.16f;
            }
            transform.position = _pos + jitter;
        }
    }
}
