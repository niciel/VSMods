using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace SimpleRM
{
    public abstract class RadialMenuGui : GuiDialog
    {
        private RadialMenu menu;

        public RadialMenuGui(ICoreClientAPI capi, RadialMenu menu)
          : base(capi)
        {
            this.menu = menu;
        }

        protected bool CheckIfCanRebuild() => this.opened;

        public virtual bool TryOpen()
        {
            if (this.menu == null || this.IsOpened())
                return false;
            this.menu.Open();
            return base.TryOpen();
        }

        public virtual void OnMouseMove(MouseEvent e) => this.menu.MouseDeltaMove(e.DeltaX, e.DeltaY);

        public virtual void OnMouseDown(MouseEvent e)
        {
            if (e.Button == 0)
                this.TryClose(true);
            else
                this.TryClose(false);
        }

        public bool TryClose(bool select)
        {
            this.menu.Close(select);
            return base.TryClose();
        }

        public virtual bool TryClose() => this.TryClose(false);

        public virtual void OnRenderGUI(float deltaTime) => this.menu.OnRender(deltaTime);
    }
    public class SimpleRadialMenu : IRenderer, IDisposable
    {
        private static readonly int ESC_KEYBOARD_ID = 50;
        private ICoreClientAPI capi;
        private RadialMenu menu;

        public SimpleRadialMenu(ICoreClientAPI api, RadialMenu menu)
        {
            if (menu == null)
                throw new Exception("menu cannot be null");
            this.capi = api;
            this.menu = menu;
        }

        public double RenderOrder => 0.0;

        public int RenderRange => 10;

        public RadialMenu GetRadialMenu() => this.menu;

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (stage != EnumRenderStage.Ortho)
                return;
            this.menu.OnRender(deltaTime);
        }

        public bool Open()
        {
            if (this.menu.Opened)
                return false;
            // ISSUE: method pointer
            this.capi.Event.MouseMove += Event_MouseMove;
            // ISSUE: method pointer
            this.capi.Event.MouseDown += Event_MouseDown;
            // ISSUE: method pointer
            this.capi.Event.KeyDown += Event_KeyDown;
            this.capi.Event.RegisterRenderer((IRenderer)this, (EnumRenderStage)10, (string)null);
            this.menu.Open();
            return true;
        }

        public void Close(bool select)
        {
            this.RemoveEvents();
            this.menu.Close(select);
        }

        protected void RemoveEvents()
        {
            // ISSUE: method pointer
            this.capi.Event.MouseMove -= Event_MouseMove;
            // ISSUE: method pointer
            this.capi.Event.MouseDown -= Event_MouseDown;
            // ISSUE: method pointer
            this.capi.Event.KeyDown += Event_KeyDown;
            this.capi.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
        }

        private void Event_KeyDown(KeyEvent e)
        {
            if (e.KeyCode != SimpleRadialMenu.ESC_KEYBOARD_ID || !this.menu.Opened)
                return;
            this.Close(false);
        }

        private void Event_MouseDown(MouseEvent e)
        {
            EnumMouseButton button = e.Button;
            e.Handled = true;
            if (button == 0)
            {
                this.Close(true);
            }
            else
            {
                if (button == EnumMouseButton.None)
                    return;
                this.Close(false);
            }
        }

        private void Event_MouseMove(MouseEvent e)
        {
            e.Handled = true;
            this.menu.MouseDeltaMove(e.DeltaX, e.DeltaY);
        }

        public void Dispose()
        {
            if (this.menu.Opened)
                this.RemoveEvents();
            this.menu.Dispose();
        }
    }

}


