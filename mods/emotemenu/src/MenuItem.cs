﻿using SimpleRM;
using System;
using Vintagestory.API.Client;
using Vintagestory.API.Common;

namespace emotemenu
{
    public class MenuItem
    {
        private readonly string _ID;
        private RadialMenu _menu;
        public Predicate<MenuItem> RiseOnOpen;
        private bool _MouseBinding;
        private int _BindID;

        public MenuItem(string iD, RadialMenu menu, GlKeys key)
          : this(iD, menu, false, (int)key)
        {
        }

        public MenuItem(string iD, RadialMenu menu, EnumMouseButton key)
          : this(iD, menu, true, (int)key)
        {
        }

        public MenuItem(string iD, RadialMenu menu, bool mouseBinding, int bindID)
        {
            this._ID = iD;
            this._menu = menu;
            this._MouseBinding = mouseBinding;
            this._BindID = bindID;
        }

        public bool MouseBinding => this._MouseBinding;

        public int BindID => this._BindID;

        public RadialMenu Menu => this._menu;

        public string ID => this._ID;
    }
}

