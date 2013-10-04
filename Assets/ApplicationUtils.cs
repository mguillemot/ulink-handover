using System;
using System.Linq;


public static class ApplicationUtils
{

    public static bool HasCommandlineFlag(string argument)
    {
        argument = argument.ToLower();
        return Environment.GetCommandLineArgs().Any(arg => arg.ToLower() == argument);
    }

    public static string GetCommandlineStringParameter(string argument)
    {
        argument = argument.ToLower();
        var args = Environment.GetCommandLineArgs();
        for (int i = 0, n = args.Length - 1 /* we skip the last one on purpose! */; i < n; i++)
        {
            var arg = args[i];
            if (arg.ToLower() == argument)
            {
                return args[i + 1];
            }
        }
        return null;
    }

    public static int? GetCommandlineIntParameter(string argument)
    {
        argument = argument.ToLower();
        var args = Environment.GetCommandLineArgs();
        for (int i = 0, n = args.Length - 1 /* we skip the last one on purpose! */; i < n; i++)
        {
            var arg = args[i];
            if (arg.ToLower() == argument)
            {
                int result;
                if (int.TryParse(args[i + 1], out result))
                {
                    return result;
                }
            }
        }
        return null;
    }

    public static long? GetCommandlineLongParameter(string argument)
    {
        argument = argument.ToLower();
        var args = Environment.GetCommandLineArgs();
        for (int i = 0, n = args.Length - 1 /* we skip the last one on purpose! */; i < n; i++)
        {
            var arg = args[i];
            if (arg.ToLower() == argument)
            {
                long result;
                if (long.TryParse(args[i + 1], out result))
                {
                    return result;
                }
            }
        }
        return null;
    }

}
