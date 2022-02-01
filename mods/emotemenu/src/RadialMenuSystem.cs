using emotemenu.helper;
using SimpleRM;
using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace emotemenu
{
    internal class RadialMenuSystem : ModSystem, IRenderer, IDisposable
    {
        private static readonly int ESC_KEYBOARD_ID = 50;
        private static readonly string CONFIG_FILE_NAME = "radialmenu.json";

        private ICoreClientAPI capi;
        private IGuiAPI guiApi;
        private RadialMenuCFG config;
        private RadialMenu CurrentlyOpened;


        private bool Keyboard;
        private int CurrnetKeyBind;
        private Dictionary<int, RadialItemMenu> KeybordBinding = new Dictionary<int, RadialItemMenu>();
        private List<RadialItemMenu> MouseBinding = new List<RadialItemMenu>();
        private long HoldThreshold;
        private DateTime time;
        private bool Clicked;
        private bool waitForRelease = false;
        private bool waitForBegin = false;


        public override bool ShouldLoad(EnumAppSide forSide)
        {
            return forSide == EnumAppSide.Client;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            base.StartClientSide(api);
            this.capi = api;
            this.guiApi = api.Gui;
            this.ReloadConfig();
            this.HoldThreshold = (long)this.config.button_hold_milis;
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

        public RadialItemMenu SerchForMenuItem(string id)
        {
            foreach (RadialItemMenu menuItem in this.MouseBinding)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                    return menuItem;
            }
            foreach (RadialItemMenu menuItem in this.KeybordBinding.Values)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                    return menuItem;
            }
            return (RadialItemMenu)null;
        }

        public bool CheckAddConflicts(RadialItemMenu toAdd)
        {
            string id = toAdd.ID;
            foreach (RadialItemMenu menuItem in this.KeybordBinding.Values)
            {
                if (object.Equals((object)menuItem.BindID, (object)id))
                {
                    capi.Logger.DebugMod("cannot register menuItem: " + id + " name id conflict");
                    return false;
                }
            }
            if (toAdd.MouseBinding)
            {
                int bindId = toAdd.BindID;
                foreach (RadialItemMenu menuItem in this.MouseBinding)
                {
                    if (object.Equals((object)menuItem.BindID, (object)id))
                    {
                        capi.Logger.DebugMod("cannot register menuItem: " + id + " name id conflict");
                        return false;
                    }
                    if (menuItem.BindID == bindId)
                    {
                        capi.Logger.DebugMod( "cannot register menuItem: " + id + " keybind id conflict");
                        return false;
                    }
                }
            }
            else
            {
                foreach (RadialItemMenu menuItem in this.MouseBinding)
                {
                    if (object.Equals((object)menuItem.BindID, (object)id))
                    {
                        capi.Logger.DebugMod("cannot register menuItem: " + id + " name id conflict");
                        return false;
                    }
                }
                if (this.KeybordBinding.ContainsKey(toAdd.BindID))
                {
                    capi.Logger.DebugMod("cannot register menuItem: " + id + " name id conflict");
                    return false;
                }
            }
            return true;
        }

        public bool RegisterButtonRadialMenu(RadialItemMenu mi)
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
            RadialItemMenu menuItem = this.SerchForMenuItem(id);
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
            if (this.capi.IsGamePaused)
                return;
            if (e.Handled)
                return;
            if (this.HashOpenedTextInput())
                return;
            if (e.KeyCode == RadialMenuSystem.ESC_KEYBOARD_ID)
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
                RadialItemMenu menuItem;
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
            if (this.capi.IsGamePaused)
                return;
            if (this.CurrentlyOpened == null && !e.Handled)
            {
                RadialItemMenu mouseMenuItem = this.GetMouseMenuItem(e.Button);
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

        public bool HashOpenedTextInput()
        {
            List<GuiDialog> list = this.guiApi.OpenedGuis;

            foreach (GuiDialog g in list)
            {
                if (string.Equals(g.DebugName, "HudDialogChat") && g.Focused)
                    return true;
                if (string.Equals(g.DebugName, "GuiDialogBlockEntityTextInput"))
                    return true;
            }
            return false;
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

        private RadialItemMenu GetMouseMenuItem(EnumMouseButton emb)
        {
            foreach (RadialItemMenu mouseMenuItem in this.MouseBinding)
            {
                if (mouseMenuItem.BindID == (int) emb)
                    return mouseMenuItem;
            }
            return (RadialItemMenu)null;
        }





        protected void ReloadConfig()
        {
            try
            {
                this.config = ((ICoreAPICommon)this.capi).LoadModConfig<RadialMenuCFG>(RadialMenuSystem.CONFIG_FILE_NAME);
                if (this.config != null)
                    return;
                this.config = new RadialMenuCFG();
                ((ICoreAPICommon)this.capi).StoreModConfig<RadialMenuCFG>(this.config, RadialMenuSystem.CONFIG_FILE_NAME);
            }
            catch (Exception ex)
            {
                ((ICoreAPI)this.capi).Logger.DebugMod("cannot Load radialmenu config, default instantiated");
                this.config = new RadialMenuCFG();
            }
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
