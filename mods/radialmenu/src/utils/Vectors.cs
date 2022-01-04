using Vintagestory.API.MathTools;

namespace SimpleRM.utils
{
    public struct int2
    {
        public int x;
        public int y;

        public int2(int x, int y)
        {
            this.x = x;
            this.y = y;
        }

        public float magnitude => GameMath.Sqrt((float)(this.x * this.x + this.y * this.y));

        public override string ToString() => "{x:" + this.x.ToString() + ",y:" + this.y.ToString() + "}";

        public static int2 operator +(int2 a, int2 b) => new int2()
        {
            x = a.x + b.x,
            y = a.y + b.y
        };

        public static int2 operator -(int2 a, int2 b) => new int2()
        {
            x = a.x - b.x,
            y = a.y - b.y
        };

        public static int2 operator /(int2 a, int b) => new int2(a.x / b, a.y / b);

        public static int2 operator *(int2 a, int b) => new int2(a.x * b, a.y * b);

        public float Distance(int2 second)
        {
            int num1 = second.x - this.x;
            int num2 = second.y - this.y;
            return GameMath.Sqrt((float)(num1 * num1 + num2 * num2));
        }

        public int Max => this.x >= this.y ? this.x : this.y;

        public int Min => this.x <= this.y ? this.x : this.y;

        public static int2 ZERO => new int2(0, 0);
    }


    public struct float2
    {
        public float x;
        public float y;

        public float2(float x, float y)
        {
            this.x = x;
            this.y = y;
        }

        public float magnitude => GameMath.Sqrt((float)((double)this.x * (double)this.x + (double)this.y * (double)this.y));

        public override string ToString() => "{x:" + this.x.ToString() + ",y:" + this.y.ToString() + "}";

        public static float2 operator +(float2 a, float2 b) => new float2()
        {
            x = a.x + b.x,
            y = a.y + b.y
        };

        public static float2 operator -(float2 a, float2 b) => new float2()
        {
            x = a.x - b.x,
            y = a.y - b.y
        };

        public static float2 operator /(float2 a, float b) => new float2(a.x / b, a.y / b);

        public static float2 operator *(float2 a, float b) => new float2(a.x * b, a.y * b);

        public static explicit operator int2(float2 f) => new int2((int)f.x, (int)f.y);

        public float Distance(float2 second)
        {
            float num1 = second.x - this.x;
            float num2 = second.y - this.y;
            return GameMath.Sqrt((float)((double)num1 * (double)num1 + (double)num2 * (double)num2));
        }

        public float Max => (double)this.x >= (double)this.y ? this.x : this.y;

        public float Min => (double)this.x <= (double)this.y ? this.x : this.y;

        public static float2 ZERO => new float2(0.0f, 0.0f);
    }
}
