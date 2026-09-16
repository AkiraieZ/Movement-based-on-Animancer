using System;
using UnityEngine;
using Unity.Netcode;

public class NetworkGameManager : MonoBehaviour
{
    [Header("出生点（场景对象，按 OwnerClientId % 数量 分配）")]
    [SerializeField] private Transform[] spawnPoints;

    /// <summary>
    /// 供 prefab 内的 NetworkPlayerController 读取：prefab 无法序列化场景对象引用，
    /// 所以出生点登记在场景里的本组件上，再由静态入口暴露。
    /// </summary>
    public static Transform[] SpawnPoints { get; private set; }

    private bool _started;

    private void Awake()
    {
        SpawnPoints = spawnPoints;
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("[NetworkGameManager] NetworkManager.Singleton is null");
            return;
        }

        NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        string[] args = Environment.GetCommandLineArgs();
        if (ContainsArg(args, "-net-host"))
        {
            StartHost();
        }
        else if (ContainsArg(args, "-net-client"))
        {
            StartClient();
        }
    }

    private void Update()
    {
        if (_started) return;

        if (Input.GetKeyDown(KeyCode.H))
        {
            StartHost();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            StartClient();
        }
    }

    /// <summary>
    /// 命令行参数检测。与 Awake 无关的纯静态入口，prefab 内的组件可安全调用（阶段 3 起用于 -net-debug）。
    /// </summary>
    public static bool HasCommandLineArg(string arg)
        => ContainsArg(Environment.GetCommandLineArgs(), arg);

    private static bool ContainsArg(string[] args, string arg)
    {
        for (int i = 0; i < args.Length; i++)
        {
            if (string.Equals(args[i], arg, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }
        return false;
    }

    private void StartHost()
    {
        if (_started) return;
        _started = true;
        NetworkManager.Singleton.StartHost();
        Debug.Log($"[NetworkGameManager] StartHost, LocalClientId={NetworkManager.Singleton.LocalClientId}");
    }

    private void StartClient()
    {
        if (_started) return;
        _started = true;
        NetworkManager.Singleton.StartClient();
        Debug.Log($"[NetworkGameManager] StartClient, LocalClientId={NetworkManager.Singleton.LocalClientId}");
    }

    private void OnClientConnected(ulong clientId)
    {
        if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log($"[NetworkGameManager] ClientConnected clientId={clientId}, IsServer=True, ConnectedClients={NetworkManager.Singleton.ConnectedClientsList.Count}");
        }
        else
        {
            Debug.Log($"[NetworkGameManager] ClientConnected clientId={clientId}, IsServer=False");
        }
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"[NetworkGameManager] ClientDisconnected clientId={clientId}, IsServer={NetworkManager.Singleton.IsServer}");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }
}
