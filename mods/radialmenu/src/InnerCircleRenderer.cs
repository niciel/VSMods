using System;

namespace SimpleRM
{
    public interface InnerCircleRenderer : IDisposable
    {
        int Radius { get; set; }
        int Gape { get; set; }

        void Rebuild();

        void Render(int x, int y);
    }
}
