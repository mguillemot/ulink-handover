using UnityEngine;


public class Player : uLink.MonoBehaviour
{

    public PlayerData data { get; set; }
    public TestData test { get; set; }

    void uLink_OnNetworkInstantiate(uLink.NetworkMessageInfo info)
    {
        Debug.Log("Instantiated player " + networkView.viewID);
    }

    // On: Server
    [RPC]
    private void RequestMove(uLink.NetworkMessageInfo info)
    {
        Debug.Log(info.sender + " requests move");

        var manager = (ManagerScript) FindObjectOfType(typeof (ManagerScript));
        manager.RequestMove(info.sender);
    }

}
