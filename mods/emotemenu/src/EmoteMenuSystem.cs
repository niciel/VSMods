using emotemenu.helper;
using SimpleRM;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace emotemenu
{
    internal class EmoteMenuSystem : ModSystem, IRenderer, IDisposable
    {
        private static readonly int ESC_KEYBOARD_ID = 50;
        private static readonly string KeyCodeHandler = "radial-emote-menu";
        private static readonly string CONFIG_FILE_NAME = "emotemenu.json";
        private ICoreClientAPI capi;
        private RadialMenu CurrentlyOpened;
        private DefaulInnerCircleRenderer dicr;
        private LangConfigFile lang;
        private EMConfig config;
        private bool Keyboard;
        private int CurrnetKeyBind;
        private Dictionary<int, MenuItem> KeybordBinding = new Dictionary<int, MenuItem>();
        private List<MenuItem> MouseBinding = new List<MenuItem>();
        private long HoldThreshold;
        private DateTime time;
        private bool Clicked;
        private bool waitForRelease = false;
        private bool waitForBegin = false;

        public virtual bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public virtual void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.capi = api;
            this.LoadTranslations();
            this.ReloadConfig();
            this.HoldThreshold = (long)this.config.button_hold_milis;
            this.InitEmoteMenu();
            // ISSUE: method pointer
            this.capi.Event.KeyDown += Event_KeyDown;
            // ISSUE: method pointer
            this.capi.Event.KeyUp += Event_KeyUp;
            // ISSUE: method pointer
            this.capi.Event.MouseDown += Event_MouseDown;
            // ISSUE: method pointer
            this.capi.Event.MouseUp += Event_MouseUp;
            // ISSUE: method pointer
            this.capi.Event.MouseMove += Event_MouseMove;
            this.capi.Event.RegisterRenderer((IRenderer)this, (EnumRenderStage)10, (string)null);
        }

        public MenuItem SerchForMenuItem(string id)
        {
            foreach (MenuItem menuItem in this.MouseBinding)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                    return menuItem;
            }
            foreach (MenuItem menuItem in this.KeybordBinding.Values)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                    return menuItem;
            }
            return (MenuItem)null;
        }

        public bool CheckAddConflicts(MenuItem toAdd)
        {
            string id = toAdd.ID;
            foreach (MenuItem menuItem in this.KeybordBinding.Values)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                {
                    ((ICoreAPI)this.capi).Logger.Log((EnumLogType)8, "cannot register menuItem: " + id + " name id conflict");
                    return false;
                }
            }
            if (toAdd.MouseBinding)
            {
                int bindId = toAdd.BindID;
                foreach (MenuItem menuItem in this.MouseBinding)
                {
                    if (object.Equals((object)menuItem.BindID, (object)id))
                    {
                        ((ICoreAPI)this.capi).Logger.Log((EnumLogType)8, "cannot register menuItem: " + id + " name id conflict");
                        return false;
                    }
                    if (menuItem.BindID == bindId)
                    {
                        ((ICoreAPI)this.capi).Logger.Log((EnumLogType)8, "cannot register menuItem: " + id + " keybind id conflict");
                        return false;
                    }
                }
            }
            else
            {
                foreach (MenuItem menuItem in this.MouseBinding)
                {
                    if (object.Equals((object)menuItem.BindID, (object)id))
                    {
                        ((ICoreAPI)this.capi).Logger.Log((EnumLogType)8, "cannot register menuItem: " + id + " name id conflict");
                        return false;
                    }
                }
                if (this.KeybordBinding.ContainsKey(toAdd.BindID))
                {
                    ((ICoreAPI)this.capi).Logger.Log((EnumLogType)8, "cannot register menuItem: " + id + " name id conflict");
                    return false;
                }
            }
            return true;
        }

        public bool RegisterButtonRadialMenu(MenuItem mi)
        {
            if (!this.CheckAddConflicts(mi))
                return false;
            if (mi.MouseBinding)
                this.MouseBinding.Add(mi);
            else
                this.KeybordBinding.Add(mi.BindID, mi);
            ((ICoreAPI)this.capi).Logger.Log((EnumLogType)5, "registered emote menu of id: " + mi.ID + " mouse binding " + mi.MouseBinding.ToString() + " keycode " + mi.BindID.ToString());
            return true;
        }

        public bool RemoveMenuItem(string id)
        {
            MenuItem menuItem = this.SerchForMenuItem(id);
            if (menuItem == null)
                return false;
            if (menuItem.Menu.Opened)
                this.CloseActiveMenu();
            if (!menuItem.MouseBinding)
                return this.KeybordBinding.Remove(menuItem.BindID);
            for (int index = 0; index < this.MouseBinding.Count; ++index)
            {
                if (string.Equals(this.MouseBinding[index].ID, id))
                {
                    this.MouseBinding.RemoveAt(index);
                    return true;
                }
            }
            return false;
        }

        private void Event_KeyUp(KeyEvent e)
        {
            if (this.CurrentlyOpened == null || !this.Keyboard || this.CurrnetKeyBind != e.KeyCode)
                return;
            this.BindingUp();
        }

        private void Event_KeyDown(KeyEvent e)
        {
            if (e.KeyCode == EmoteMenuSystem.ESC_KEYBOARD_ID)
            {
                if (this.CurrentlyOpened != null)
                {
                    if (this.CurrentlyOpened.Opened)
                        this.CurrentlyOpened.Close(false);
                    this.CurrentlyOpened = (RadialMenu)null;
                }
                this.waitForBegin = false;
                this.waitForRelease = false;
            }
            else if (this.CurrentlyOpened != null)
            {
                if (!this.Keyboard || this.CurrnetKeyBind != e.KeyCode)
                    return;
                this.BindingDown();
            }
            else
            {
                MenuItem menuItem;
                if (!this.KeybordBinding.TryGetValue(e.KeyCode, out menuItem) || e.Handled || menuItem.RiseOnOpen != null && menuItem.RiseOnOpen(menuItem))
                    return;
                this.CurrentlyOpened = menuItem.Menu;
                this.CurrnetKeyBind = e.KeyCode;
                this.Keyboard = true;
                this.BindingDown();
            }
        }

        private void Event_MouseDown(MouseEvent e)
        {
            if (this.CurrentlyOpened == null && !e.Handled)
            {
                MenuItem mouseMenuItem = this.GetMouseMenuItem(e.Button);
                if (mouseMenuItem == null)
                    return;
                e.Handled = true;
                if (mouseMenuItem.RiseOnOpen == null || !mouseMenuItem.RiseOnOpen(mouseMenuItem))
                {
                    this.CurrentlyOpened = mouseMenuItem.Menu;
                    this.CurrnetKeyBind = (int)e.Button;
                    this.Keyboard = false;
                    this.BindingDown();
                }
            }
            else
            {
                e.Handled = true;
                EnumMouseButton button = e.Button;
                if (!this.Keyboard && this.CurrnetKeyBind == (int)button)
                {
                    this.BindingDown();
                }
                else
                {
                    if (button == 0)
                        this.CurrentlyOpened.Close();
                    else if (button != EnumMouseButton.None)
                        this.CurrentlyOpened.Close(false);
                    if (this.Clicked)
                    {
                        this.waitForRelease = true;
                    }
                    else
                    {
                        this.waitForRelease = false;
                        this.waitForBegin = false;
                        this.CurrentlyOpened = (RadialMenu)null;
                    }
                }
            }
        }

        private void Event_MouseUp(MouseEvent e)
        {
            if (this.CurrentlyOpened == null || this.Keyboard || this.CurrnetKeyBind != (int) e.Button)
                return;
            this.BindingUp();
        }

        private void Event_MouseMove(MouseEvent e)
        {
            if (this.CurrentlyOpened == null)
                return;
            this.CurrentlyOpened.MouseDeltaMove(e.DeltaX, e.DeltaY);
            e.Handled = true;
        }

        private void BindingDown()
        {
            this.Clicked = true;
            if (this.waitForRelease)
                return;
            if (this.waitForBegin)
            {
                this.waitForBegin = false;
                if (!this.CurrentlyOpened.Opened)
                    return;
                this.CurrentlyOpened.Close();
                this.waitForRelease = true;
            }
            else
            {
                if (this.CurrentlyOpened.Opened)
                    return;
                this.time = DateTime.Now.AddMilliseconds((double)this.HoldThreshold);
                this.CurrentlyOpened.Open();
            }
        }

        private void CloseActiveMenu(bool select = false)
        {
            if (this.CurrentlyOpened == null)
                return;
            this.CurrentlyOpened.Close(select);
            this.CurrentlyOpened = (RadialMenu)null;
        }

        private void BindingUp()
        {
            if (this.waitForRelease)
            {
                this.waitForRelease = false;
                if (this.CurrentlyOpened != null)
                    this.CurrentlyOpened = (RadialMenu)null;
                this.Clicked = false;
            }
            else
            {
                if (DateTime.Now <= this.time)
                    this.waitForBegin = true;
                else if (this.CurrentlyOpened.Opened)
                {
                    this.CurrentlyOpened.Close();
                    this.CurrentlyOpened = (RadialMenu)null;
                }
                this.Clicked = false;
            }
        }

        private MenuItem GetMouseMenuItem(EnumMouseButton emb)
        {
            foreach (MenuItem mouseMenuItem in this.MouseBinding)
            {
                if (mouseMenuItem.BindID == (int) emb)
                    return mouseMenuItem;
            }
            return (MenuItem)null;
        }

        private RadialElementPosition BuildElement(
          string command,
          Action hangeText)
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

        protected void LoadTranslations()
        {
            string str = this.capi.Settings.String["language"];
            try
            {
                this.lang = ((ICoreAPI)this.capi).Assets.Get<LangConfigFile>(new AssetLocation("emotemenu", "lang/" + str + ".json"));
                ((ICoreAPI)this.capi).Logger.DebugMod("loaded language file: " + str + ".json");
            }
            catch (Exception ex)
            {
                ((ICoreAPI)this.capi).Logger.DebugMod("cannot load language: '" + str);
                ((ICoreAPI)this.capi).Logger.DebugMod("create default one");
                this.lang = new LangConfigFile();
            }
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
                ((ICoreAPI)this.capi).Logger.DebugMod("cannot Load config, default instantiated");
                this.config = new EMConfig();
            }
        }

        protected void InitEmoteMenu()
        {
            float scale = this.config.scale;
            RadialMenu menu = new RadialMenu(this.capi, (int)(100.0 * (double)scale), (int)(200.0 * (double)scale));
            menu.Gape = (int)(5.0 * (double)scale);
            if (this.config.show_middle_circle)
            {
                menu.AddElement((IRadialElement)this.BuildElement("wave", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_wave)));
                menu.AddElement((IRadialElement)this.BuildElement("cheer", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_cheer)));
                menu.AddElement((IRadialElement)this.BuildElement("shrug", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_shrug)));
                menu.AddElement((IRadialElement)this.BuildElement("cry", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_cry)));
                menu.AddElement((IRadialElement)this.BuildElement("nod", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_nod)));
                menu.AddElement((IRadialElement)this.BuildElement("facepalm", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_facepalm)));
                menu.AddElement((IRadialElement)this.BuildElement("bow", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_bow)));
                menu.AddElement((IRadialElement)this.BuildElement("laugh", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_laugh)));
                menu.AddElement((IRadialElement)this.BuildElement("rage", (Action)(() => this.dicr.DisplayedText = this.lang.emote_menu_rage)), true);
                this.dicr = new DefaulInnerCircleRenderer(this.capi, 6);
                this.dicr.Gape = (int)(8.0 * (double)scale);
                menu.InnerRenderer = (InnerCircleRenderer)this.dicr;
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
                menu.AddElement((IRadialElement)this.BuildElement("rage", (Action)null), true);
            }
            this.capi.Input.RegisterHotKey(EmoteMenuSystem.KeyCodeHandler, this.lang.emote_properties_KeyBindDescription, (GlKeys)93, (HotkeyType)1, false, false, false);
            EnumMouseButton result;
            bool mouseBinding;
            int bindID;
            if (Enum.TryParse<EnumMouseButton>(this.config.button_mouse_binding, out result))
            {
                mouseBinding = true;
                bindID = (int)result;
            }
            else
            {
                mouseBinding = false;
                bindID = this.capi.Input.HotKeys[EmoteMenuSystem.KeyCodeHandler].CurrentMapping.KeyCode;
            }
            this.RegisterButtonRadialMenu(new MenuItem("emoteMenu", menu, mouseBinding, bindID));
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (this.CurrentlyOpened == null || !this.CurrentlyOpened.Opened)
                return;
            if (this.capi.Render.CurrentActiveShader == null)
                this.capi.Render.GetEngineShader((EnumShaderProgram)17).Use();
            this.CurrentlyOpened.OnRender(deltaTime);
        }

        public double RenderOrder => 0.0;

        public int RenderRange => 0;
    }
}
