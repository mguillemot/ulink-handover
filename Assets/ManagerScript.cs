using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using System.Linq;
using Random = UnityEngine.Random;


public class ManagerScript : uLink.MonoBehaviour
{

    public Rect Rect = new Rect(300, 0, 250, 250);
    public GameObject PlayerPrefab;

    public bool isServer { get; private set; }
    public bool isClient { get; private set; }

    void Start()
    {
        var serverPort = ApplicationUtils.GetCommandlineIntParameter("-server");
        if (serverPort.HasValue)
        {
            uLink.Network.InitializeServer(32, serverPort.Value);
        }

        InvokeRepeating("PeriodicUpdate", 1, 1);
    }

    void OnGUI()
    {
        GUILayout.BeginArea(Rect);
        if (uLink.Network.status == uLink.NetworkStatus.Disconnected)
        {
            if (GUILayout.Button("Start server 7000"))
            {
                uLink.Network.InitializeServer(32, 7000);
            }
            if (GUILayout.Button("Start server 7001"))
            {
                uLink.Network.InitializeServer(32, 7001);
            }
            if (GUILayout.Button("Start server 7002"))
            {
                uLink.Network.InitializeServer(32, 7002);
            }
            if (GUILayout.Button("Start client"))
            {
                uLink.Network.Connect("127.0.0.1", 7000);
            }
        }
        else if (uLink.Network.status == uLink.NetworkStatus.Connected)
        {
            if (isClient)
            {
                GUILayout.Label("I'm a client, connected on " + currentServer);

                var me = FindObjectOfType(typeof(Player)) as Player;
                if (me != null)
                {
                    GUILayout.Label("I'm at " + me.transform.position);
                    if (GUILayout.Button("MOVE!"))
                    {
                        me.networkView.RPC("RequestMove", uLink.RPCMode.Server);
                    }
                }

                GUILayout.Space(10);
            }
            if (isServer)
            {
                GUILayout.Label("I'm server " + uLink.Network.listenPort);
                GUILayout.Label("Peers: " + peers.Count);
                GUILayout.Label("Players: " + FindObjectsOfType(typeof(Player)).Length);

                var buttonLabel = emulateBadConditions ? "Disable bad conditions" : "Enable bad conditions";
                if (GUILayout.Button(buttonLabel))
                {
                    emulateBadConditions = !emulateBadConditions;
                }

                GUILayout.Space(10);
            }

            GUILayout.Label(string.Format("Latency: {0:0}-{1:0}", uLink.Network.emulation.minLatency * 1000, uLink.Network.emulation.maxLatency * 1000));
            GUILayout.Label(string.Format("Drop {0:0%} / Dupe {1:0%}", uLink.Network.emulation.chanceOfLoss, uLink.Network.emulation.chanceOfDuplicates));
        }
        GUILayout.EndArea();
    }

    void PeriodicUpdate()
    {
        //if (isClient)
        //{
        //    var me = FindObjectOfType(typeof(Player)) as Player;
        //    if (me != null)
        //    {
        //        me.networkView.RPC("RequestMove", uLink.RPCMode.Server);
        //    }
        //}
    }

    #region Server

    void uLink_OnServerInitialized()
    {
        Debug.LogError("Server started on port " + uLink.Network.listenPort);
        isServer = true;

        var p2pListener = gameObject.AddComponent<uLinkNetworkP2P>();
        p2pListener.listenPort = 10000 + uLink.Network.listenPort;

        for (int i = p2pListener.listenPort + 1; i < 17003; i++)
        {
            var p2pConnector = gameObject.AddComponent<uLinkP2PConnector>();
            p2pConnector.host = "127.0.0.1";
            p2pConnector.port = i;
        }

        emulateBadConditions = true;
    }

    private bool emulateBadConditions
    {
        get
        {
            return (uLink.Network.emulation.minLatency > 0);
        }
        set
        {
            if (value)
            {
                uLink.Network.emulation.minLatency = .4f;
                uLink.Network.emulation.maxLatency = .5f;
                uLink.Network.emulation.chanceOfLoss = .25f;
                uLink.Network.emulation.chanceOfDuplicates = .25f;
            }
            else
            {
                uLink.Network.emulation.minLatency = 0;
                uLink.Network.emulation.maxLatency = 0;
                uLink.Network.emulation.chanceOfLoss = 0;
                uLink.Network.emulation.chanceOfDuplicates = 0;
            }
        }
    }

    private PlayerData approvedData;

    void uLink_OnPlayerApproval(uLink.NetworkPlayerApproval approval)
    {
        if (approval.handoverData != null)
        {
            var handoverBytes = approval.handoverData.bytesRemaining;
            Debug.LogError("Handover incoming! " + handoverBytes + " bytes and " + approval.handoverInstances.Length + " instances");
            if (handoverBytes == 0)
            {
                Debug.LogError("!!!!!!!!!!!!!!!!!!!! THIS IS THE BUG !!!!!!!!!!!!!!!!!!!!!");
            }

            approvedData = approval.handoverData.Read<PlayerData>();
            Debug.LogError("Handover data was " + approvedData);
        }
        else
        {
            approvedData = new PlayerData
                               {
                                   name = "Player " + Random.Range(1, 1000000),
                                   stuff = new float[1234]
                               };
            for (int i = 0; i < approvedData.stuff.Length; i++)
            {
                approvedData.stuff[i] = Random.value;
            }
            Debug.LogError("New connection from " + approval + ", generating PlayerData " + approvedData);
        }

        approval.Approve();
    }

    void uLink_OnPlayerConnected(uLink.NetworkPlayer player)
    {
        Debug.LogError(player + " connected");

        var pos = Random.insideUnitSphere * 1000;
        var playerObject = uLink.Network.Instantiate(player, PlayerPrefab, pos, Quaternion.identity, 0);
        playerObject.GetComponent<Player>().data = approvedData;
        approvedData = null;
    }

    void uLink_OnPlayerDisconnected(uLink.NetworkPlayer player)
    {
        Debug.LogError(player + " disconnecting");

        uLink.Network.DestroyPlayerObjects(player);
    }

    // Sent by Player
    public void RequestMove(uLink.NetworkPlayer player)
    {
        var playerObjects = (Player[])FindObjectsOfType(typeof(Player));
        var playerObject = playerObjects.FirstOrDefault(p => p.networkView.owner == player);
        if (playerObject == null)
        {
            Debug.LogError("ERROR: Cannot find player object for " + player);
            return;
        }

        var peerPort = uLink.Network.listenPort + 10001;
        if (peerPort == 17003)
        {
            peerPort = 17000;
        }
        Debug.LogError("Trying to move " + playerObject.data + " to server " + peerPort);
        var peer = peers.FirstOrDefault(p => p.port == peerPort);
        if (peer == default(uLink.NetworkPeer))
        {
            Debug.LogError("ERROR: Server not found");
        }
        else
        {
            networkP2P.Handover(player, peer, /* handoverData */ playerObject.data, "turlututu", 42);
        }
    }

    public readonly List<uLink.NetworkPeer> peers = new List<uLink.NetworkPeer>();

    void uLink_OnPeerConnected(uLink.NetworkPeer peer)
    {
        peers.Add(peer);
    }

    void uLink_OnPeerDisconnected(uLink.NetworkPeer peer)
    {
        peers.Remove(peer);
    }

    #endregion

    #region Client

    public IPEndPoint currentServer { get; private set; }

    void uLink_OnConnectedToServer(IPEndPoint server)
    {
        Debug.LogError("Connected to server on " + server);
        isClient = true;
        currentServer = server;

        StartCoroutine(RedirectIn(2));
    }

    IEnumerator RedirectIn(float delay)
    {
        yield return new WaitForSeconds(delay);

        var me = FindObjectOfType(typeof(Player)) as Player;
        if (me != null)
        {
            Debug.LogError("Requesting to move on the next server...");
            me.networkView.RPC("RequestMove", uLink.RPCMode.Server);
        }
    }

    void uLink_OnRedirectingToServer(IPEndPoint newServer)
    {
        Debug.LogError("Redirecting to " + newServer);
    }

    #endregion

}
