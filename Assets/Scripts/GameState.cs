using System;

namespace Assets.Scripts
{
    [Flags]
    public enum GameState
    {
        Init = 0,

        Loading = 1 << 0,

        MainMenu = 1 << 1,
        Match = 1 << 2
    }
}