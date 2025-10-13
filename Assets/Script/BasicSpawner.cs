using Fusion;
using Fusion.Addons.Physics;
using Fusion.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;


public struct NetworkInputData : INetworkInput //플레이어의 입력 데이터를 담는 용도
{
    public const byte JUMP = 1; //스페이스바 입력을 숫자 1로 부르기로 약속


    public NetworkButtons buttons; //스페이스바 클릭 버튼 입력 저장
    public Vector3 direction; //wasd키 입력에 따른 이동 방향(벡터)을 저장
}

public class BasicSpawner : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner; //네트워크 관리

    //UI요소와 연결할 변수
    public TMP_InputField playerNameField; //플레이어 이름 입력창
    public TMP_InputField roomNameField; //방 이름 입력창
    public Button hostButton; //호스트 버튼
    public Button joinButton;
    public GameObject connectionMenu; //메뉴 전체를 담는 패널
    public void OnConnectedToServer(NetworkRunner runner)
    {

    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }

    void Start()
    {
        //버튼이 클릭되었을 때 어떤 함수를 실행할지 연결
        hostButton.onClick.AddListener(OnHostButtonClicked);
        joinButton.onClick.AddListener(OnJoinButtonClicked);
    }
    //버튼 클릭시 실행될 함수
    public void OnHostButtonClicked()
    {
        //입력창의 텍스트를 가져와서 이름으로 설정
        MyPlayerName = playerNameField.text;
        //startgame 함수 호출시 입력창의 텍스트를 방 이름으로 사용
        StartGame(GameMode.Host, roomNameField.text);

    }
    public void OnJoinButtonClicked()
    {
        //입력창의 텍스트를 가져와서 이름으로 설정
        MyPlayerName = playerNameField.text;
        //startgame 함수 호출시 입력창의 텍스트를 방 이름으로 사용
        StartGame(GameMode.Client, roomNameField.text);
    }

    //로컬 플레이어의 이름을 다른 스크립트에서 참조할 수 있도록 static 변수 추가
    public static string MyPlayerName { get; private set; }

    //startgame 함수가 방 이름을 파라미터로 받도록 수정
    async void StartGame(GameMode mode, string roomName)
    {
        //메뉴 UI를 비활성화해서 숨김
        connectionMenu.SetActive(false);
        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.ProvideInput = true;

        var runnerSimulatePhyscics3D = gameObject.AddComponent<RunnerSimulatePhysics3D>();
        runnerSimulatePhyscics3D.ClientPhysicsSimulation = ClientPhysicsSimulation.SimulateAlways;

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = mode,
            SessionName = roomName,
            Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()

        });


    }
    [SerializeField]
    private NetworkPrefabRef _playerPrefab;
    private Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // Create a unique position for the player
            Vector3 spawnPosition = new Vector3((player.RawEncoded % runner.Config.Simulation.PlayerCount) * 1, 1, 0);
            NetworkObject networkPlayerObject = runner.Spawn(_playerPrefab, spawnPosition, Quaternion.identity, player);
            // Keep track of the player avatars for easy access
            _spawnedCharacters.Add(player, networkPlayerObject);
        }

    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            _spawnedCharacters.Remove(player);
        }

    }
    private bool _jumpButton;

    private void Update()
    {
        _jumpButton = _jumpButton || Input.GetKeyDown(KeyCode.Space);
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var data = new NetworkInputData();

        if (Input.GetKey(KeyCode.W))
            data.direction += Vector3.forward;

        if (Input.GetKey(KeyCode.S))
            data.direction += Vector3.back;

        if (Input.GetKey(KeyCode.A))
            data.direction += Vector3.left;

        if (Input.GetKey(KeyCode.D))
            data.direction += Vector3.right;

       data.buttons.Set(NetworkInputData.JUMP, _jumpButton);
        _jumpButton = false;

        input.Set(data);
    }
}
