using System.Collections.Generic;
using Farplane.Memory;

namespace Farplane.FFX.Data;

public static class Skills
{
    static int _offsetSkillTable = 0;
    static KernelTable _skillTable;

    static void UpdateOffset()
    {
        var skillTablePointer = OffsetScanner.GetOffset(GameOffset.FFX_SkillTablePointer);
        _offsetSkillTable = GameMemory.Read<int>(skillTablePointer, false);
    }

    static void LoadTable() => _skillTable = new KernelTable(_offsetSkillTable); //_skillTable = new KernelTable("D:\\Games\\Steam\\SteamApps\\common\\FINAL FANTASY FFX&FFX-2 HD Remaster\\data\\ffx_data_VBF\\ffx_ps2\\ffx\\master\\new_uspc\\battle\\kernel\\sphere.bin");

    public static List<string> GetSkillNames()
    {
        if (_offsetSkillTable == 0)
        {
            UpdateOffset();
        }

        LoadTable();

        var names = new List<string>();
        for (var i = 0; i < _skillTable.BlockCount; i++)
        {
            var skillName = _skillTable.GetString1(i);
            names.Add(skillName);
        }

        return names;
    }

    public static string GetSkillName(int skillIndex) => _skillTable.GetString1(skillIndex);

    public static string GetSkillDescription(int skillIndex) => _skillTable.GetString2(skillIndex);
}

public struct SkillData
{
    public ushort unknown_1;
    public ushort unknown_2;
    public ushort unknown_3;
    public ushort Animation1;
    public ushort Animation2;
}
