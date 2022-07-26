
using Vintagestory.API.Common;

namespace SimpleRM.debugS
{
    public static class DebugSimplify
    {
        public static void DebugMod(this ILogger logger, string message)
        {
            logger.Log(EnumLogType.Debug, "[SimpleEmoteMenu] " + message);
        }
    }
}
