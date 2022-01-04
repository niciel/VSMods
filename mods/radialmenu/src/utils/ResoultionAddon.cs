using System.Reflection;
using Vintagestory.API.Client;

namespace SimpleRM.utils
{
    public static class ResoultionAddon
    {
        public static FieldInfo _GET_FIELD_PLATFORM;
        public static FieldInfo _GET_FIELD_WINDOW;
        public static PropertyInfo _GET_Property_HEIGHT;
        public static PropertyInfo _GET_Property_WIDTH;
        public static bool firstUse = true;

        public static void GetScreenResolution(this ICoreClientAPI capi, ref int x, ref int y)
        {
            if (ResoultionAddon.firstUse)
                ResoultionAddon.LOAD(capi);
            object obj1 = ResoultionAddon._GET_FIELD_PLATFORM.GetValue((object)capi.World);
            object obj2 = ResoultionAddon._GET_FIELD_WINDOW.GetValue(obj1);
            x = (int)ResoultionAddon._GET_Property_WIDTH.GetValue(obj2);
            y = (int)ResoultionAddon._GET_Property_HEIGHT.GetValue(obj2);
        }

        public static void LOAD(ICoreClientAPI capi)
        {
            object world = (object)capi.World;
            ResoultionAddon._GET_FIELD_PLATFORM = world.GetType().GetField("Platform", BindingFlags.Instance | BindingFlags.NonPublic);
            object obj1 = ResoultionAddon._GET_FIELD_PLATFORM.GetValue(world);
            ResoultionAddon._GET_FIELD_WINDOW = obj1.GetType().GetRuntimeField("window");
            object obj2 = ResoultionAddon._GET_FIELD_WINDOW.GetValue(obj1);
            ResoultionAddon._GET_Property_HEIGHT = obj2.GetType().GetRuntimeProperty("Height");
            ResoultionAddon._GET_Property_WIDTH = obj2.GetType().GetRuntimeProperty("Width");
        }
    }
}
