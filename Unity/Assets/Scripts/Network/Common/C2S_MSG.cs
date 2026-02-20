using System.Collections.Generic;

/// <summary>
/// [클라이언트 -> 서버] 메세지
/// </summary>
public enum C2S_MSG : short
{
    BEGIN = 0,

    /// <summary>
    /// 배틀 씬에 진입 완료
    /// </summary>
    ENTER_WORLD,

    /// <summary>
    /// 특정 프레임의 인풋 이벤트 전송
    /// </summary>
    FRAME_EVENT,

    /// <summary>
    /// 특정 프레임의 상태 검증을 위한 해시 전송
    /// </summary>
    FRAME_HASH,
}

public class C2S_MSG_ENTER_WORLD
{
    public long EnterUnixTimeMillis;
}

public class C2S_MSG_FRAME_EVENTS
{
    /// <summary>
    /// 이벤트가 작동할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 이 프레임에 적용할 이벤트 목록
    /// </summary>
    public List<C2S_MSG_FRAME_EVENT> Events;
}

public class C2S_MSG_FRAME_EVENT
{
    /// <summary>
    /// 이벤트 유형, 이 유형이 None이라면 나머지 데이터는 파싱하지 않는다.
    /// </summary>
    public FrameEventType EventType;

    /// <summary>
    /// 입력을 처리했던 시간 (Milliseconds)
    /// </summary>
    public int BattleTimeMillis;
}

public class C2S_MSG_FRAME_HASH
{
    /// <summary>
    /// 검증할 프레임
    /// </summary>
    public int Frame;

    /// <summary>
    /// 검증할 프레임의 상태 해시
    /// </summary>
    public int Hash;
}