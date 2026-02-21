using Rage;
using Rage.Native;
using System;

/// <summary>
/// Applies and removes the Bolingbroke prison outfit.
/// Ported from prison-mod's clothes_changer() (ScriptHookVDotNet) to RPH natives.
/// </summary>
public static class BolingbrookeOutfit
{
    // SET_PED_COMPONENT_VARIATION args: (ped, componentId, drawableId, textureId, paletteId)
    // Component 3 = legs/lower body, Component 4 = feet/shoes

    public static void Apply(Ped player)
    {
        if (!player.Exists()) return;

        uint model   = NativeFunction.Natives.GET_ENTITY_MODEL<uint>(player);
        uint michael = NativeFunction.Natives.GET_HASH_KEY<uint>("player_zero");
        uint franklin= NativeFunction.Natives.GET_HASH_KEY<uint>("player_one");
        uint trevor  = NativeFunction.Natives.GET_HASH_KEY<uint>("player_two");

        if (model == michael)
        {
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 3, 12, 4, 2);
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 4, 11, 4, 2);
        }
        else if (model == franklin)
        {
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 3, 1, 5, 2);
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 4, 1, 5, 2);
        }
        else if (model == trevor)
        {
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 3, 5, 2, 1);
            NativeFunction.Natives.SET_PED_COMPONENT_VARIATION(player, 4, 5, 2, 1);
        }
        // FreeMode/custom models: no change — LSR handles clothing separately
    }

    public static void Remove(Ped player)
    {
        if (!player.Exists()) return;
        NativeFunction.Natives.SET_PED_DEFAULT_COMPONENT_VARIATION(player);
    }
}
