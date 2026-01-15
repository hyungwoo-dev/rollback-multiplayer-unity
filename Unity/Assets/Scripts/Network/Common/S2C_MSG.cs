/// <summary>
/// [서버 -> 클라이언트] 메세지
/// </summary>
public enum S2C_MSG : short
{
    BEGIN = 0,

    /// <summary>
    /// 게임 시작
    /// </summary>
    S2C_START_GAME,

    /// <summary>
    /// 인풋 이벤트 전송
    /// </summary>
    S2C_FRAME_EVENT,

    /// <summary>
    /// 특정 프레임의 상태 검증 실패
    /// </summary>
    S2C_INVALIDATE_HASH,
}

public class S2C_MSG_GAME_START
{
    /// <summary>
    /// 게임을 시작할 Utc 기준 UnixTimeMillis
    /// </summary>
    public long GameStartUnixTimeMillis;

    /// <summary>
    /// 플레이어의 인덱스
    /// </summary>
    public byte PlayerIndex;

    /// <summary>
    /// 상대편 플레이어의 인덱스
    /// </summary>
    public byte OpponentPlayerIndex;
}

public class S2C_MSG_FRAME_EVENT
{
    /// <summary>
    /// 이벤트 유형, 이 유형이 None이라면 나머지 데이터는 파싱하지 않는다.
    /// </summary>
    public FrameEventType EventType;

    /// <summary>
    /// 이벤트가 작동할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 해당 프레임의 이벤트 실행 순서 (0 또는 1)
    /// </summary>
    public byte EventOrder;

    /// <summary>
    /// 이벤트를 실행할 유저 인덱스
    /// </summary>
    public byte UserIndex;
}

public class S2C_MSG_INVALIDATE_HASH
{
    public int Frame;
    public int PlayerHash;
    public int OpponentPlayerHash;
}