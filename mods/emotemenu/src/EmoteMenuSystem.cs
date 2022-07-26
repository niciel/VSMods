using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using emotemenu.helper;
using SimpleRM;

namespace emotemenu
{
    public class EmoteMenuSystem : ModSystem
    {
        private static readonly string KeyCodeHandler = "radial-emote-menu";
        private static readonly string CONFIG_FILE_NAME = "emotemenu.json";

        private ICoreClientAPI capi;
        private LangConfigFile lang;
        private EMConfig config;


        public override void StartClientSide(ICoreClientAPI api)
        {
            this.capi = api;
            this.LoadTranslations();
            this.ReloadConfig();
            this.InitEmoteMenu();


        }

        protected void LoadTranslations()
        {
            string str = this.capi.Settings.String["language"];
            try
            {
                this.lang = ((ICoreAPI)this.capi).Assets.Get<LangConfigFile>(new AssetLocation("emotemenu", "lang/" + str + ".json"));
                this.capi.Logger.DebugMod("loaded language file: " + str + ".json");
            }
            catch (Exception ex)
            {
                ((ICoreAPI)this.capi).Logger.DebugMod("cannot load language: '" + str);
                ((ICoreAPI)this.capi).Logger.DebugMod("create default one");
                this.lang = new LangConfigFile();
            }
        }

        protected void InitEmoteMenu()
        {

            float scale = this.config.scale;
            RadialMenu menu = new RadialMenu(this.capi, (int)(100.0 * (double)scale), (int)(200.0 * (double)scale));
            DefaulInnerCircleRenderer dicr = new DefaulInnerCircleRenderer(this.capi , (int) (100*scale));
            menu.Gape = (int)(5.0 * (double)scale);
            if (this.config.show_middle_circle)
            {
                menu.AddElement((IRadialElement)this.BuildElement("wave", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_wave)));
                menu.AddElement((IRadialElement)this.BuildElement("cheer", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_cheer)));
                menu.AddElement((IRadialElement)this.BuildElement("shrug", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_shrug)));
                menu.AddElement((IRadialElement)this.BuildElement("cry", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_cry)));
                menu.AddElement((IRadialElement)this.BuildElement("nod", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_nod)));
                menu.AddElement((IRadialElement)this.BuildElement("facepalm", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_facepalm)));
                menu.AddElement((IRadialElement)this.BuildElement("bow", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_bow)));
                menu.AddElement((IRadialElement)this.BuildElement("laugh", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_laugh)));
                menu.AddElement((IRadialElement)this.BuildElement("rage", (Action)(() => dicr.DisplayedText = this.lang.emote_menu_rage)));
                dicr = new DefaulInnerCircleRenderer(this.capi, 6);
                dicr.Gape = (int)(8.0 * (double)scale);
                menu.InnerRenderer = (InnerCircleRenderer) dicr;
            }
            else
            {
                menu.AddElement((IRadialElement)this.BuildElement("wave", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("cheer", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("shrug", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("cry", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("nod", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("facepalm", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("bow", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("laugh", (Action)null));
                menu.AddElement((IRadialElement)this.BuildElement("rage", (Action)null));
            }
            menu.Rebuild();
            bool mouseBinding;
            int bindID;
            if (Enum.TryParse<EnumMouseButton>(this.config.button_mouse_binding, out EnumMouseButton result))
            {
                mouseBinding = true;
                bindID = (int)result;
            }
            else
            {
                mouseBinding = false;
                this.capi.Input.RegisterHotKey(EmoteMenuSystem.KeyCodeHandler, 
                    this.lang.emote_properties_KeyBindDescription, (GlKeys)93, (HotkeyType)1, false, false, false);
                bindID = this.capi.Input.HotKeys[EmoteMenuSystem.KeyCodeHandler].CurrentMapping.KeyCode;
            }
            RadialMenuSystem radialmenu = capi.ModLoader.GetModSystem<RadialMenuSystem>();
            radialmenu.RegisterButtonRadialMenu(new RadialItemMenu("emoteMenu", menu, mouseBinding, bindID));
        }

        protected void ReloadConfig()
        {
            try
            {
                this.config = ((ICoreAPICommon)this.capi).LoadModConfig<EMConfig>(EmoteMenuSystem.CONFIG_FILE_NAME);
                if (this.config != null)
                    return;
                this.config = new EMConfig();
                ((ICoreAPICommon)this.capi).StoreModConfig<EMConfig>(this.config, EmoteMenuSystem.CONFIG_FILE_NAME);
            }
            catch (Exception ex)
            {
                ((ICoreAPI)this.capi).Logger.DebugMod("cannot Load emotemenu config, default instantiated");
                this.config = new EMConfig();
            }
        }

        private RadialElementPosition BuildElement(string command, Action hangeText)
        {
            AssetLocation assetLocation = new AssetLocation("emotemenu", "textures/" + command + ".png");
            LoadedTexture icon = new LoadedTexture(this.capi);
            this.capi.Render.GetOrLoadTexture(assetLocation, ref icon);
            RadialElementPosition radialElementPosition = new RadialElementPosition(this.capi, icon, (Action)(() => this.capi.SendChatMessage("/emote " + command, (string)null)));
            if (hangeText != null)
                radialElementPosition.HoverEvent = (Action<bool>)(hover =>
                {
                    if (!hover)
                        return;
                    hangeText();
                });
            return radialElementPosition;
        }

    }
}
