using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Module.Shared.Enumerations
{
    public enum ErrorDisplayType
    {
        Information = 1,
        Warning = 2,
        Error = 3
    }

    public enum ModuleEnum
    { 
        HeroVirtualTabletop
    }

    public enum GameEvent
    {
        TargetName,
        PrevSpawn,
        NextSpawn,
        RandomSpawn,
        Fly,
        EditPos,
        DetachCamera,
        NoClip,
        AccessLevel,
        Command,
        SpawnNpc,
        Rename,
        LoadCostume,
        MoveNPC,
        DeleteNPC,
        ClearNPC,
        Move,
        TargetEnemyNear,
        LoadBind,
        BeNPC,
        SaveBind,
        GetPos,
        CamDist,
        Follow,
        LoadMap,
        BindLoadFile,
        Macro,
        NOP,
        PopMenu
    }

}
