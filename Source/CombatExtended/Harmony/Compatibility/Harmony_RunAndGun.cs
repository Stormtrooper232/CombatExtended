using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using System.Reflection.Emit;
using System;


namespace CombatExtended.HarmonyCE.Compatibility
{
    [HarmonyPatch]
    class Harmony_Compat_RunAndGun
    {
        static readonly string logPrefix = Assembly.GetExecutingAssembly().GetName().Name + " :: ";
        static readonly Assembly asmbly = AppDomain.CurrentDomain.GetAssemblies().
                                       SingleOrDefault(assembly => assembly.GetName().Name == "Tacticowl");
        static MethodBase targetMethod = null;
        static Type Stance_RunAndGun = null;

        internal static bool Prepare()
        {
            if (asmbly != null)
            {
                foreach (Type modType in asmbly.GetTypes())
                {
                    Log.Error(($"{logPrefix}Checking RnG type "+modType.Name));
                    if (modType.Name == "Verb_TryStartCastOn")
                    {
                        targetMethod = AccessTools.Method(modType, "Prefix");
                    }
                    if (modType.Name == "Stance_RunAndGun")
                    {
                        Stance_RunAndGun = modType;
                        CompAmmoUser.rgStance = modType;
                    }
                }
                if (targetMethod == null || Stance_RunAndGun == null)
                {
                    Log.Error($"{logPrefix}Failed to find target method while attempting to patch Tacticowl run and gun.");
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        internal static MethodBase TargetMethod()
        {
            Log.Message($"{logPrefix}Applying compatibility patch for {asmbly.FullName}");
            return targetMethod;
        }

        internal static IEnumerable<CodeInstruction> Transpiler(ILGenerator gen, IEnumerable<CodeInstruction> instructions)
        {
            bool patched = false;
            bool ready = false;

            List<CodeInstruction> patch = new List<CodeInstruction>
            {
                new CodeInstruction(OpCodes.Ldarg_0),
                new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(Harmony_Compat_RunAndGun), nameof(CanBeFiredNow))),
                new CodeInstruction(OpCodes.Brfalse)
            };

            foreach (var code in instructions)
            {
                yield return code;
                if (!patched)
                {
                    if (code.opcode == OpCodes.Isinst && ReferenceEquals(code.operand, Stance_RunAndGun))
                    {
                        ready = true;
                    }
                    if (ready && code.opcode == OpCodes.Brtrue)
                    {
                        patch.Last().operand = code.operand;
                        foreach (var c in patch)
                        {
                            yield return c;
                        }
                        patched = true;
                    }
                }
            }
        }

        internal static bool CanBeFiredNow(Verb instance)
        {
            return instance.EquipmentSource.TryGetComp<CompAmmoUser>()?.CanBeFiredNow ?? true;
        }
    }
}
