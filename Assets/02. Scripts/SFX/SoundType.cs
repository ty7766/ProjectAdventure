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
    SFX_GameStartCountdown = 203,
    SFX_GameStart = 204,

    //InGame SFX
    SFX_MapSwitch = 300,
    SFX_MapChange = 301,
    SFX_GemCollect = 302,
    SFX_MapChangeAlert = 304,

    //Player SFX
    SFX_PlayerDamaged = 401,
    SFX_PlayerDead = 402,
    SFX_PlayerWalk = 403,

    //Map SFX
    SFX_VolcanoRock = 500,
    SFX_SphinxEye = 501,
    SFX_SphinxFallingRock = 502,
    SFX_Teleport = 503,
    SFX_Rune = 504,
    SFX_RuneDoor = 505,
    SFX_SnowBall = 506,

}