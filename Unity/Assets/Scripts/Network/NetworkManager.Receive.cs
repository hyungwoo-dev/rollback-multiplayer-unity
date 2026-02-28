public partial class NetworkManager
{
    public LiteNetClient.EnterWorldDelegate OnEnterWorld;
    public LiteNetClient.IntermidiateFrameEventDelegate OnIntermidiateFrameEvent;
    public LiteNetClient.FrameEventsDelegate OnFrameEvents;
    public LiteNetClient.FrameHashDelegate OnFrameHash;

    private void HandleOnEnterWorld(in P2P_ENTER_WORLD message)
    {
        OnEnterWorld?.Invoke(message);
    }

    private void HandleOnIntermidiateFrameEvent(in P2P_INTERMIDIATE_FRAME_EVENT message)
    {
        OnIntermidiateFrameEvent?.Invoke(message);
    }

    private void HandleOnFrameEvents(in P2P_FRAME_EVENTS message)
    {
        OnFrameEvents?.Invoke(message);
    }

    private void HandleOnFrameHash(in P2P_FRAME_HASH message)
    {
        OnFrameHash?.Invoke(message);
    }
}