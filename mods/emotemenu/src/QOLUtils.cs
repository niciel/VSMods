using Vintagestory.API.Common;

namespace emotemenu.helper
{
    public static class QOLUtils
    {
        private static string LOGGER_PREFIX = "[EmoteMenu] ";

        public static void DebugMod(this ILogger log, string message) => log.Debug(QOLUtils.LOGGER_PREFIX + " " + message);
    }
}
