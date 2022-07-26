using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using SimpleRM.utils;
using Vintagestory.API.MathTools;
using Cairo;
namespace SimpleRM
{

    public class RadialMenu : IDisposable
    {
        private static float VECTOR_LENGHT_THRESHOLD = 0.5f;
        private List<IRadialElement> _Elements = new List<IRadialElement>();
        private float _elementAngle;
        private int MiddleScreenX;
        private int MiddleScreenY;
        private int innerCircleRadius;
        private int outerCircleRadius;
        private float VectorSensiticity;
        private float2 MouseDirection = float2.ZERO;
        private int LastSelectedElement = -1;
        private InnerCircleRenderer _InnerCircle;
        public int Gape = 5;
        protected ICoreClientAPI capi;
        private bool _opened = false;
        private bool Disposed = false;

        public RadialMenu(ICoreClientAPI capi, int innerCircleRadius, int outerCircleRadius)
        {
            this.capi = capi;
            this.innerCircleRadius = innerCircleRadius;
            this.outerCircleRadius = outerCircleRadius;
            this.UpdateScreenMidPoint();
        }

        protected virtual void UpdateScreenMidPoint()
        {
            int y;
            int x = y = 0;
            this.capi.GetScreenResolution(ref x, ref y);
            this.VectorSensiticity = (float)(y / 9);
            this.MiddleScreenX = x / 2;
            this.MiddleScreenY = y / 2;
            foreach (IRadialElement element in this._Elements)
                element.UpdateMiddlePosition(this.MiddleScreenX, this.MiddleScreenY);
        }

        public virtual void OnRender(float deltaTime)
        {
            for (int index = 0; index < this._Elements.Count; ++index)
                this._Elements[index].RenderMenuElement();
            if (this._InnerCircle == null)
                return;
            this._InnerCircle.Render(this.MiddleScreenX, this.MiddleScreenY);
        }

        public InnerCircleRenderer InnerRenderer
        {
            get => this._InnerCircle;
            set
            {
                if (this._InnerCircle != null)
                {
                    this._InnerCircle.Dispose();
                    this._InnerCircle = (InnerCircleRenderer)null;
                }
                this._InnerCircle = value;
                if (value == null)
                    return;
                value.Radius = this.innerCircleRadius;
                if (value.Gape < 0)
                    value.Gape = this.Gape;
                value.Rebuild();
            }
        }

        public virtual void MouseDeltaMove(int x, int y)
        {
            this.MouseDirection += new float2((float)x, (float)y);
            float magnitude = this.MouseDirection.magnitude;
            if ((double)magnitude > (double)this.VectorSensiticity)
                this.MouseDirection = this.MouseDirection / magnitude * this.VectorSensiticity;
            IRadialElement closest = this.SimpleFindClosest(this.MouseDirection);
            if (closest == null || closest.NumericID == this.LastSelectedElement)
                return;
            if (this.LastSelectedElement >= 0)
                this._Elements[this.LastSelectedElement].OnHoverEnd();
            closest.OnHoverBegin();
            this.LastSelectedElement = closest.NumericID;
        }

        protected IRadialElement SimpleFindClosest(float2 val)
        {
            IRadialElement closest = (IRadialElement)null;
            float num = float.MaxValue;
            foreach (IRadialElement element in this._Elements)
            {
                float2 offset = element.GetOffset();
                float magnitude = (offset / offset.magnitude * this.VectorSensiticity - val).magnitude;
                if ((double)magnitude < (double)this.VectorSensiticity * (double)RadialMenu.VECTOR_LENGHT_THRESHOLD && (double)magnitude <= (double)num)
                {
                    closest = element;
                    num = magnitude;
                }
            }
            return closest;
        }

        public bool AddElement(IRadialElement element, bool rebuild = false)
        {
            if (this._opened)
                return false;
            int Thickness = this.outerCircleRadius - this.innerCircleRadius;
            int MidRadius = this.innerCircleRadius + Thickness / 2;
            element.UpdateRadius(MidRadius, Thickness);
            element.UpdateMiddlePosition(this.MiddleScreenX, this.MiddleScreenY);
            this._Elements.Add(element);
            if (rebuild)
                this.Rebuild();
            return true;
        }

        public bool RemoveElement(int id, bool rebuild = false)
        {
            if (id < 0 || id >= this.ElementsCount())
                return false;
            this._Elements.RemoveAt(id);
            if (rebuild)
                this.Rebuild();
            return true;
        }

        public virtual bool Rebuild()
        {
            if (this.Disposed)
                return false;
            this._elementAngle = 6.283185f / (float)this._Elements.Count;
            int num = this.innerCircleRadius + (this.outerCircleRadius - this.innerCircleRadius) / 2;
            for (int index = 0; index < this._Elements.Count; ++index)
            {
                IRadialElement element = this._Elements[index];
                if (element == null)
                    return false;
                float angle = (float)index * this._elementAngle;
                int xOffset = (int)((double)num * (double)GameMath.Sin(angle));
                int yOffset = (int)((double)-num * (double)GameMath.Cos(angle));
                element.UpdatePosition(index, xOffset, yOffset, angle, this._elementAngle);
                element.ReDrawElementToTexture();
            }
            if (this._InnerCircle != null)
                this._InnerCircle.Rebuild();
            return true;
        }

        public int ElementsCount() => this._Elements != null ? this._Elements.Count : -1;

        public bool Opened => this._opened;

        public virtual void Open()
        {
            if (this.Disposed || this._opened)
                return;
            this.UpdateScreenMidPoint();
            this.LastSelectedElement = -1;
            this._opened = true;
        }

        public virtual void Close(bool select = true)
        {
            this._opened = false;
            if (this.LastSelectedElement <= -1)
                return;
            IRadialElement element = this._Elements[this.LastSelectedElement];
            element.OnHoverEnd();
            if (select)
                element.OnSelect();
        }

        public void Dispose()
        {
            this.Disposed = true;
            if (this._opened)
                this.Close(false);
            foreach (IDisposable element in this._Elements)
                element.Dispose();
            if (this._InnerCircle != null)
            {
                this._InnerCircle.Dispose();
                this._InnerCircle = (InnerCircleRenderer)null;
            }
        }
    }



}

