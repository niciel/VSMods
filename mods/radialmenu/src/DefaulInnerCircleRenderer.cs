using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using SimpleRM.utils;
using Vintagestory.API.MathTools;
using Cairo;

namespace SimpleRM
{
    public class DefaulInnerCircleRenderer : InnerCircleRenderer, IDisposable
    {
        private int _Radius = 1;
        private int _Gape = -1;
        private ICoreClientAPI api;
        private double[] FillColor;
        private LoadedTexture Texture;
        private double[] CircleColor;
        private int LineWidth = 6;
        private IRenderAPI Renderer;
        private int HalfTextureSize = -1;
        protected TextTextureUtil TTU;
        private int TextureSize;
        private string text;
        private LoadedTexture TextTexture;
        private CairoFont font;

        public DefaulInnerCircleRenderer(ICoreClientAPI api, int lineWidth)
        {
            this.TTU = new TextTextureUtil(api);
            this.api = api;
            this.Renderer = api.Render;
            this.FillColor = GuiStyle.DialogLightBgColor;
            this.CircleColor = GuiStyle.DialogLightBgColor;
            this.Texture = new LoadedTexture(api);
            this.font = new CairoFont(GuiStyle.NormalFontSize, GuiStyle.StandardFontName);
            this.font.Orientation = (EnumTextOrientation)2;
            ((FontConfig)this.font).Color = GuiStyle.DialogDefaultTextColor;
        }

        public int Radius
        {
            get => this._Radius;
            set => this._Radius = value;
        }

        public int Gape
        {
            get => this._Gape;
            set => this._Gape = value;
        }

        public string DisplayedText
        {
            set
            {
                this.text = value;
                this.RebuildText();
            }
            get => this.text;
        }

        public void Rebuild()
        {
            this.RebuildMiddleCircle();
        }

        public void RebuildText()
        {
            if (this.text == null)
            {
                if (this.TextTexture == null && this.TextTexture.Disposed)
                    return;
                this.TextTexture.Dispose();
                this.TextTexture = null;
            }
            else
            {
                if (this.TextTexture == null)
                    this.TextTexture = new LoadedTexture(this.api);
                string[] strArray = this.text.Split('\n');
                int length = strArray.Length;
                float num1 = (float)(((FontConfig)this.font).UnscaledFontsize * 1.05);
                int textureSize = this.TextureSize;
                float num2 = (float)(((double)textureSize - (double)length * (double)num1) / 2.0 + (double)length * (double)num1 / 2.0);
                ImageSurface imageSurface = new ImageSurface((Format)0, textureSize, textureSize);
                Context context = new Context((Surface)imageSurface);
                this.font.SetupContext(context);
                for (int index = 0; index < length; ++index)
                {
                    string str = strArray[index];
                    TextExtents textExtents = this.font.GetTextExtents(str);
                    float xadvance = (float)textExtents.XAdvance;
                    context.MoveTo(((double)textureSize - (double)xadvance) / 2.0, (double)num2 + (double)index * (double)num1);
                    context.ShowText(str);
                }
                this.api.Gui.LoadOrUpdateCairoTexture(imageSurface, true, ref this.TextTexture);
                context.Dispose();
                imageSurface.Dispose();
            }
        }

        public void RebuildMiddleCircle()
        {
            int num1 = (this._Radius - this.Gape) * 2;
            this.TextureSize = num1;
            this.HalfTextureSize = num1 / 2;
            ImageSurface imageSurface = new ImageSurface((Format)0, num1, num1);
            Context context = new Context((Surface)imageSurface);
            context.SetSourceRGBA(this.FillColor[0], this.FillColor[1], this.FillColor[2], this.FillColor.Length > 3 ? this.FillColor[3] : 0.3);
            context.LineWidth = 6.0;
            double num2 = (double)(num1 / 2);
            context.Arc(num2, num2, (double)(this._Radius - this.Gape), 0.0, 6.28318548202515);
            context.ClosePath();
            context.Fill();
            context.LineWidth = (double)this.LineWidth;
            context.SetSourceRGBA(this.CircleColor[0], this.CircleColor[1], this.CircleColor[2], this.FillColor.Length > 3 ? this.CircleColor[3] : 1.0);
            context.Stroke();
            this.api.Gui.LoadOrUpdateCairoTexture(imageSurface, true, ref this.Texture);
            context.Dispose();
            imageSurface.Dispose();
        }

        public void Render(int x, int y)
        {
            if (this.Texture != null && !this.Texture.Disposed)
                this.Renderer.Render2DLoadedTexture(this.Texture, (float)(x - this.HalfTextureSize), (float)(y - this.HalfTextureSize), 50f);
            if (this.TextTexture == null || this.TextTexture.Disposed)
                return;
            this.Renderer.Render2DTexture(this.TextTexture.TextureId, (float)(x - this.HalfTextureSize), (float)(y - this.HalfTextureSize), (float)this.TextureSize, (float)this.TextureSize, 50f, (Vec4f)null);
        }

        public void Dispose()
        {
            if (this.Texture != null && !this.Texture.Disposed)
            {
                this.Texture.Dispose();
                this.Texture = null;
            }
            if (this.TextTexture == null || this.TextTexture.Disposed)
                return;
            this.TextTexture.Dispose();
            this.TextTexture = null;
        }
    }
}
