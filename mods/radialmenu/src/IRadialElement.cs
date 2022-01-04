using SimpleRM.utils;
using System;

namespace SimpleRM
{
    public interface IRadialElement : IDisposable
    {
        void UpdatePosition(
          int numericalposition,
          int xOffset,
          int yOffset,
          float angle,
          float ElemenetAngle);

        void UpdateRadius(int MidRadius, int Thickness);

        void ReDrawElementToTexture();

        void RenderMenuElement();

        void UpdateMiddlePosition(int x, int y);

        int NumericID { get; }

        float2 GetOffset();

        void OnHoverBegin();

        void OnHoverEnd();

        void OnSelect();
    }
}
