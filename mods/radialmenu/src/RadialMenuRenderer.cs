using System;

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
            if (this.Disposed)
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
            if (select)
                element.OnSelect();
            element.OnHoverEnd();
        }

        public void Dispose()
        {
            this.Disposed = true;
            if (this._opened)
                this.Close(false);
            foreach (IDisposable element in this._Elements)
                element.Dispose();
            if (this._InnerCircle == null)
                return;
            this._InnerCircle.Dispose();
            this._InnerCircle = (InnerCircleRenderer)null;
        }
    }


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
                    float xadvance = (float)((TextExtents)ref textExtents).XAdvance;
                    context.MoveTo(((double)textureSize - (double)xadvance) / 2.0, (double)num2 + (double)index * (double)num1);
                    context.ShowText(str);
                }
                this.api.Gui.LoadOrUpdateCairoTexture(imageSurface, true, ref this.TextTexture);
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
            ((Surface)imageSurface).Dispose();
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

