using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    //따라갈 타겟(플레이어)
    private Transform _target;

    //타켓으로부터 얼마나 뒤쪽을 바라볼지 결정하는 거리
    public float backDistance = 2f;

    //타겟과의 거리 및 높이 오프셋
    [SerializeField] private Vector3 _offset = new Vector3(0, 2, -4);

    //카메라 이동의 부드러움 정도 (값이 작을수록 부드럽게 따라감)
    [SerializeField] private float _smoothSpeed = 0.125f;

    private void LateUpdate()
    {
        //아직 타겟을 찾지 못했다면, 로컬 플레이어를 찾아서 타겟으로 설정
        if( _target == null )
        {
            //Player.Local은 입력 권한이 있는 '내 캐릭터'를 가르킨다
            if(Player.Local != null)
            {
                _target = Player.Local.transform;
                Debug.Log("Camera target found: " +  _target.name); 
            }
            else
            {
                //아직 로컬 플레이어가 스폰되지 않았으면 아무것도 하지 않음
                return;
            }

        }
        //목표위기 계산
        //타겟의 현재 위치에 우리가 설정한 오프셋(거리, 높이)를 더함
        Vector3 desiredPodsion = _target.position + _offset;

        //부드러운 이동
        //현재 카메라 위치에서 목표 위치까지 부드럽게 이동
        Vector3 smoothPosion = Vector3.Lerp(transform.position, desiredPodsion, _smoothSpeed);

        // 1. 타겟의 뒷쪽 위치를 계산합니다.
        //    (타겟의 현재 위치 - 타겟의 앞쪽 방향 * 거리)
        Vector3 targetBackPosition = _target.position - _target.forward * backDistance;

        // 2. 계산된 타겟의 뒷쪽 위치를 바라보게 합니다.
        transform.LookAt(targetBackPosition);
    }
}


