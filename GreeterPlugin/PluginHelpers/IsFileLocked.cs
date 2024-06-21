using System.Runtime.InteropServices;

namespace GreeterPlugin.PluginHelpers;

public static class IsFileLocked
{
    public static bool Check(string filePath, int secondsToWait)
    {
        var isLocked = true;
        var i = 0;

        while (isLocked &&  ((i < secondsToWait) || (secondsToWait == 0)))
        {
            try
            {
                using (File.Open(filePath, FileMode.Open)) { }
                return false;
            }
            catch (IOException e)
            {
                var errorCode = Marshal.GetHRForException(e) & ((1 << 16) - 1);
                isLocked = errorCode == 32 || errorCode == 33;
                i++;

                if (secondsToWait !=0)
                    new System.Threading.ManualResetEvent(false).WaitOne(1000);
            }
        }

        return isLocked;
    }
}