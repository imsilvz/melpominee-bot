using NetCord.Gateway.Voice;

namespace Melpominee.Models;

public class VoiceInstance : IDisposable
{
    private VoiceClient _voiceClient;
    public VoiceInstance(VoiceClient voiceClient)
    {
        _voiceClient = voiceClient;
    }

    public void Dispose()
    {
        _voiceClient.Dispose();
    }
}
