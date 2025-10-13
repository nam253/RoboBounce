using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static Fusion.NetworkBehaviour;

public class Player : NetworkBehaviour
{
    public static Player Local { get; private set; }

    [SerializeField]
   // private Ball _prefabBall;

    private NetworkCharacterController _cc;
    private Vector3 _forward = Vector3.forward;

    [Networked]
    private TickTimer delay { get; set; }

    [Networked]
    public bool spawnedProjectile { get; set; }


    // --- (핵심 1) 이름표 TextMeshPro 컴포넌트를 연결할 필드 추가 ---
    [SerializeField] private TextMeshPro _nameText;

    // --- (핵심 2) 플레이어 이름을 동기화하기 위한 Networked 변수 추가 ---
    [Networked]
    public NetworkString<_32> PlayerName { get; set; }

    private ChangeDetector _changeDetector;

    private Animator _animator;

    [Networked]
    public float Health { get; set; } = 100f; // 체력 추가 (네트워크 동기화)

    [Networked]
    public NetworkBool IsDead { get; set; }

    private void Awake()
    {
        _cc = GetComponent<NetworkCharacterController>();
        _animator = GetComponentInChildren<Animator>();
    }

    public override void FixedUpdateNetwork()
    {
        if (IsDead) return;

        if (GetInput(out NetworkInputData data))
        {
            data.direction.Normalize();
            _cc.Move(5 * data.direction * Runner.DeltaTime);
            if (data.direction.sqrMagnitude > 0)
                _forward = data.direction;

            if (HasStateAuthority && delay.ExpiredOrNotRunning(Runner))
            {
                // --- (핵심 수정 1) 총알이 생성될 위치를 미리 계산 ---
                // 캐릭터의 위치에서 y축으로 1.0f 만큼 위로 올립니다. (캐릭터 가슴 높이 정도)
                // 이 값(1.0f)을 조절하여 원하는 높이를 맞출 수 있습니다.
                Vector3 spawnPosition = transform.position + new Vector3(0, 1.0f, 0);

                if (data.buttons.IsSet(NetworkInputData.JUMP))
                {
                    delay = TickTimer.CreateFromSeconds(Runner, 0.5f);
                    //Runner.Spawn(_prefabBall, spawnPosition + _forward, Quaternion.LookRotation(_forward), Object.InputAuthority, (runner, o) => { o.GetComponent<Ball>().Init(Object.InputAuthority); });
                    spawnedProjectile = !spawnedProjectile;
                }
            }
        }

    }
    public void TakeDamage(float damage)
    {
        if (!HasStateAuthority || IsDead) return;

        Health -= damage;
        if (Health <= 0)
        {
            Health = 0;
            IsDead = true;
            Debug.Log($"Player {Object.Id} died!");
        }
    }

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (HasStateAuthority)
        {
            IsDead = false;
            Health = 100f;
        }

        // --- (핵심 수정 2) 스폰될 때 자신이 로컬 플레이어인지 확인하고 static 변수에 등록 ---
        if (Object.HasInputAuthority)
        {
            Local = this;
            Debug.Log("Local Player spawned and registered.");
        }


        // --- (핵심 3) 로컬 플레이어라면, 서버에 자신의 이름을 알리는 RPC 호출 ---
        if (Object.HasInputAuthority)
        {
            RPC_SetPlayerName(BasicSpawner.MyPlayerName);
        }

        UpdateNameText();
    }
   /* public void RPC_PlayHitEffect(Vector3 position, Quaternion rotation)
    {
        GameObject.Find("EffectManager").GetComponent<EffectManager>()
                   .PlayEffect(position, rotation);
    }*/

    // --- (핵심 4) 이름 변경을 서버에 요청하는 RPC ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetPlayerName(string name)
    {
        // 서버에서만 이 코드가 실행되어 PlayerName 프로퍼티를 변경합니다.
        // 이 변경은 네트워크를 통해 모든 클라이언트에게 전파됩니다.
        PlayerName = name;
    }

    // --- 이름표 텍스트를 업데이트하는 별도의 함수 ---
    private void UpdateNameText()
    {
        if (_nameText != null)
        {
            // NetworkString은 .Value로 실제 string 값을 가져옵니다.
            _nameText.text = PlayerName.Value;
        }
    }

    public override void Render()
    {
        if (_animator != null && !IsDead)
        {
            // NetworkCharacterController의 현재 속력을 기반으로 Speed 파라미터 설정
            float speed = _cc.Velocity.magnitude;
            _animator.SetFloat("Speed", speed);
        }
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsDead):
                    // IsDead 프로퍼티에 변경이 감지되면 죽음 처리 함수 호출
                    HandleDeathState(IsDead);
                    break;

                case nameof(PlayerName):
                    // PlayerName 프로퍼티에 변경이 감지되면 이름표 업데이트 함수 호출
                    UpdateNameText();
                    break;
            }


        }
    }
    private void HandleDeathState(bool isDead)
    {
        if (isDead)
        {
            _animator?.SetTrigger("Die");
            if (TryGetComponent<NetworkCharacterController>(out var cc))
            {
                cc.enabled = false;
            }
        }
    }
}

