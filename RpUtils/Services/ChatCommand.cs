using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.FFXIV.Client.UI;

namespace RpUtils.Services;

internal static class ChatCommand
{
    public static unsafe void Send(string command)
    {
        var cmd = Utf8String.FromString(command);
        try
        {
            UIModule.Instance()->ProcessChatBoxEntry(cmd);
        }
        finally
        {
            cmd->Dtor(true);
        }
    }
}
