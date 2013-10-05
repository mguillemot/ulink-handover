using System;


[Serializable]
public class TestData
{

    public int a;
    public long b;
    public ushort c;

    public static void Write(uLink.BitStream stream, object value, params object[] codecOptions)
    {
        var data = (TestData)value;
        stream.Write(data.a);
        stream.Write(data.b);
        stream.Write(data.c);
    }

    public static object Read(uLink.BitStream stream, params object[] codecOptions)
    {
        var data = new TestData();
        data.a = stream.Read<int>();
        data.b = stream.Read<long>();
        data.c = stream.Read<ushort>();
        return data;
    }

}
