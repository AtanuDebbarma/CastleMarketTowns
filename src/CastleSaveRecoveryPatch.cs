using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace CastleMarketTowns
{
    // Vanilla writes each save entry length as a signed 16-bit value, so entries of 32768-65535 bytes
    // come back negative and the whole save refuses to load.
    [HarmonyPatch]
    internal static class CastleSaveRecoveryPatch
    {
        static List<MethodBase> Targets()
        {
            Type deserializer = AccessTools.TypeByName("TaleWorlds.SaveSystem.ArchiveDeserializer");
            return SettlementGetter.Existing(deserializer == null ? null : AccessTools.Method(deserializer, "LoadFrom"));
        }

        static bool Prepare()
        {
            return Targets().Count > 0 && ReadBytes() != null;
        }

        static IEnumerable<MethodBase> TargetMethods()
        {
            return Targets();
        }

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo readBytes = ReadBytes();
            MethodInfo helper = AccessTools.Method(typeof(CastleSaveRecoveryPatch), "ReadEntryBytes");
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.Calls(readBytes))
                {
                    instruction.opcode = OpCodes.Call;
                    instruction.operand = helper;
                }

                yield return instruction;
            }
        }

        public static byte[] ReadEntryBytes(TaleWorlds.Library.BinaryReader reader, int length)
        {
            return reader.ReadBytes(length & 0xFFFF);
        }

        private static MethodInfo ReadBytes()
        {
            return AccessTools.Method(typeof(TaleWorlds.Library.BinaryReader), "ReadBytes", new Type[] { typeof(int) });
        }
    }
}
