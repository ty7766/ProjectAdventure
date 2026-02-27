//모든 오디오 명명 스크립트
public enum SoundType
{
    None = 0,
    
    //BackGroundMusic
    BGM_TitleSceneMusic = 100,
    BGM_BackGroundMusic = 101,

    //UI SFX
    SFX_ButtonClick = 200,
    SFX_ClearUI = 201,
    SFX_GameOverUI = 202,
    SFX_GameStartCountdown = 203,
    SFX_GameStart = 204,

    //InGame SFX
    SFX_MapSwitch = 300,
    SFX_MapChange = 301,
    SFX_GemCollect = 302,
    SFX_FlagCollect = 303,
    SFX_MapChangeAlert = 304,

    //Player SFX
    SFX_PlayerDamaged = 401,
    SFX_PlayerDead = 402,
    SFX_PlayerWalk = 403,

}