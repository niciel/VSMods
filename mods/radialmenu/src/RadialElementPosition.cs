using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using SimpleRM.utils;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Cairo;

namespace SimpleRM
{
    public class RadialElementPosition : IRadialElement, IDisposable
    {
        private static double ANGLE_OFFSET_FROM_SCREEN = 4.71238899230957;
        private ICoreClientAPI api;
        private IRenderAPI RendererApi;
        private IGuiAPI GuiApi;
        private int numericalPosition = -1;
        private int MiddleX;
        private int MiddleY;
        private int xOffset;
        private int yOffset;
        private float Angle;
        private float ElementAngle;
        private int _Gape = 5;
        private int MidRadius;
        private int Thickness;
        protected LoadedTexture BackGroundSelectedTexture;
        protected LoadedTexture BackGroundTexture;
        private float BackGroundTextureSize;
        private LoadedTexture Icon;
        private int2 IconSize;
        private bool AutoDisposeIcon;
        private float IconScale = 0.7f;
        public Action SelectEvent;
        public Action<bool> HoverEvent;
        private bool Hover = false;

        public RadialElementPosition(ICoreClientAPI api)
        {
            this.api = api;
            this.GuiApi = this.api.Gui;
            this.RendererApi = this.api.Render;
        }

        public RadialElementPosition(ICoreClientAPI api, LoadedTexture icon)
          : this(api)
        {
            this.Icon = icon;
        }

        public RadialElementPosition(ICoreClientAPI api, LoadedTexture icon, Action onSelect)
          : this(api, icon)
        {
            this.SelectEvent = onSelect;
        }

        public void UpdatePosition(
          int numericalPosition,
          int xOffset,
          int yOffset,
          float angle,
          float ElemenetAngle)
        {
            this.xOffset = xOffset;
            this.yOffset = yOffset;
            this.Angle = angle;
            this.ElementAngle = ElemenetAngle;
            this.numericalPosition = numericalPosition;
        }

        public void SetIcon(LoadedTexture texture, bool autoDispose)
        {
            if (this.Icon != null && this.AutoDisposeIcon && !this.Icon.Disposed)
                this.Icon.Dispose();
            this.Icon = texture;
            this.AutoDisposeIcon = true;
            this.UpdateIconSize(this.MaxIconSize());
        }

        public void OnHoverBegin()
        {
            this.Hover = true;
            if (this.HoverEvent == null)
                return;
            this.HoverEvent(this.Hover);
        }

        public void OnHoverEnd()
        {
            this.Hover = false;
            if (this.HoverEvent == null)
                return;
            this.HoverEvent(this.Hover);
        }

        public void OnSelect()
        {
            if (this.SelectEvent == null)
                return;
            this.SelectEvent();
        }

        public void UpdateRadius(int MidRadius, int Thickness)
        {
            this.MidRadius = MidRadius;
            this.Thickness = Thickness;
        }

        public void ReDrawElementToTexture()
        {
            if (this.BackGroundTexture != null && !this.BackGroundTexture.Disposed)
            {
                this.BackGroundTexture.Dispose();
                this.BackGroundSelectedTexture.Dispose();
            }
            this.BackGroundTexture = new LoadedTexture(this.api);
            this.BackGroundSelectedTexture = new LoadedTexture(this.api);
            this.BackGroundTextureSize = (float)((this.MidRadius + this.Thickness) * 2 + 10);
            ImageSurface surface = new ImageSurface((Format)0, (int)this.BackGroundTextureSize, (int)this.BackGroundTextureSize);
            Context ctx = new Context((Surface)surface);
            this.CreateTexture(surface, ctx);
            ctx.Dispose();
            ((Surface)surface).Dispose();
            this.UpdateIconSize(this.MaxIconSize());
        }

        protected virtual void CreateTexture(ImageSurface surface, Context ctx)
        {
            this.PushPath(ctx);
            double[] dialogLightBgColor = GuiStyle.DialogLightBgColor;
            ctx.SetSourceRGBA(dialogLightBgColor[0], dialogLightBgColor[1], dialogLightBgColor[2], 0.4);
            ctx.LineWidth = 6.0;
            ctx.StrokePreserve();
            ctx.SetSourceRGBA(dialogLightBgColor[0], dialogLightBgColor[1], dialogLightBgColor[2], 0.7);
            ctx.Fill();
            this.GuiApi.LoadOrUpdateCairoTexture(surface, true, ref this.BackGroundTexture);
            ctx.Dispose();
            ((Surface)surface).Dispose();
            surface = new ImageSurface((Format)0, (int)this.BackGroundTextureSize, (int)this.BackGroundTextureSize);
            ctx = new Context((Surface)surface);
            this.PushPath(ctx);
            ctx.SetSourceRGBA(dialogLightBgColor[0], dialogLightBgColor[1], dialogLightBgColor[2], 0.2);
            ctx.FillPreserve();
            ctx.SetSourceRGBA(dialogLightBgColor[0], dialogLightBgColor[1], dialogLightBgColor[2], 1.0);
            ctx.Stroke();
            this.GuiApi.LoadOrUpdateCairoTexture(surface, true, ref this.BackGroundSelectedTexture);
        }

        protected void PushPath(Context ctx)
        {
            float num1 = (float)this.Thickness / 2f;
            double num2 = (double)this.BackGroundTextureSize / 2.0;
            float num3 = this.ElementAngle / 2f;
            float radius1 = (float)this.MidRadius + num1;
            float num4 = this.AngleOffset(radius1);
            ctx.Arc(num2, num2, (double)radius1, (double)this.Angle - (double)num3 + RadialElementPosition.ANGLE_OFFSET_FROM_SCREEN + (double)num4, (double)this.Angle + (double)num3 + RadialElementPosition.ANGLE_OFFSET_FROM_SCREEN - (double)num4);
            float radius2 = (float)this.MidRadius - num1;
            float num5 = this.AngleOffset(radius2);
            double num6 = (double)radius2 * (double)GameMath.Sin(this.Angle + num3 - num5) + num2;
            double num7 = -(double)radius2 * (double)GameMath.Cos(this.Angle + num3 - num5) + num2;
            ctx.LineTo(num6, num7);
            ctx.ArcNegative(num2, num2, (double)radius2, (double)this.Angle + (double)num3 + RadialElementPosition.ANGLE_OFFSET_FROM_SCREEN - (double)num5, (double)this.Angle - (double)num3 + RadialElementPosition.ANGLE_OFFSET_FROM_SCREEN + (double)num5);
            ctx.ClosePath();
        }

        public void RenderMenuElement()
        {
            if (this.BackGroundTexture == null)
                return;
            int num = (int)((double)this.BackGroundTextureSize / 2.0);
            if (this.Hover)
                this.RendererApi.Render2DLoadedTexture(this.BackGroundSelectedTexture, (float)(this.MiddleX - num), (float)(this.MiddleY - num), 50f);
            else
                this.RendererApi.Render2DLoadedTexture(this.BackGroundTexture, (float)(this.MiddleX - num), (float)(this.MiddleY - num), 50f);
            if (this.Icon != null && !this.Icon.Disposed)
            {
                int2 int2 = this.IconSize / 2;
                this.RendererApi.Render2DTexture(this.Icon.TextureId, (float)(this.MiddleX + this.xOffset - int2.x), (float)(this.MiddleY + this.yOffset - int2.y), (float)this.IconSize.x, (float)this.IconSize.y, 50f, (Vec4f)null);
            }
        }

        protected int MaxIconSize()
        {
            int thickness = this.Thickness;
            float num1 = this.ElementAngle / 2f;
            float num2 = this.AngleOffset((float)this.MidRadius);
            float num3 = this.Angle - num1 + num2;
            int2 int2 = new int2((int)((double)this.MidRadius * (double)GameMath.Sin(num3)), (int)((double)-this.MidRadius * (double)GameMath.Cos(num3)));
            float num4 = this.Angle + num1 - num2;
            int2 second = new int2((int)((double)this.MidRadius * (double)GameMath.Sin(num4)), (int)((double)-this.MidRadius * (double)GameMath.Cos(num4)));
            int num5 = (int)int2.Distance(second);
            return thickness >= num5 ? num5 : thickness;
        }

        protected void UpdateIconSize(int maxIcon)
        {
            if (this.Icon == null || this.Icon.Disposed)
                return;
            float2 float2 = new float2((float)this.Icon.Width, (float)this.Icon.Height);
            if ((double)float2.Min <= 0.0)
            {
                this.IconSize = new int2(maxIcon, maxIcon);
            }
            else
            {
                float max = float2.Max;
                float num = (float)maxIcon / max * this.IconScale;
                this.IconSize = (int2)(float2 * num);
            }
        }

        protected float AngleOffset(float radius) => (float)this._Gape / radius;

        public void UpdateMiddlePosition(int x, int y)
        {
            this.MiddleX = x;
            this.MiddleY = y;
        }

        public int NumericID => this.numericalPosition;

        public void Dispose()
        {
            if (this.BackGroundTexture == null || this.BackGroundTexture.Disposed)
                return;
            this.BackGroundTexture.Dispose();
            this.BackGroundSelectedTexture.Dispose();
        }

        public int Gape
        {
            get => this._Gape;
            set => this._Gape = value;
        }

        public float2 GetOffset() => new float2((float)this.xOffset, (float)this.yOffset);
    }
}
