using System;


[Serializable]
public class PlayerData
{

    public string name;
    public float[] stuff;

    public override string ToString()
    {
        return string.Format("[PlayerData name={0} stuff={1}]", name, stuff.Length);
    }

    static void Write(uLink.BitStream stream, object value, params object[] codecOptions)
    {
        var data = (PlayerData)value;
        stream.Write(data.name);
        stream.Write(data.stuff.Length);
        for (int i = 0; i < data.stuff.Length; i++)
        {
            stream.Write(data.stuff[i]);
        }
    }

    static object Read(uLink.BitStream stream, params object[] codecOptions)
    {
        var data = new PlayerData();
        data.name = stream.Read<string>();
        data.stuff = new float[stream.Read<int>()];
        for (int i = 0; i < data.stuff.Length; i++)
        {
            data.stuff[i] = stream.Read<float>();
        }
        return data;
    }

    static PlayerData()
    {
        uLink.BitStreamCodec.AddAndMakeArray<PlayerData>(Read, Write);
    }

}
