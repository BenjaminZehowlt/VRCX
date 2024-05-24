// Copyright(c) 2019-2022 pypy, Natsumi and individual contributors.
// All rights reserved.
//
// This work is licensed under the terms of the MIT license.
// For a copy, see <https://opensource.org/licenses/MIT>.

using CefSharp;
using CefSharp.Enums;
using CefSharp.OffScreen;
using CefSharp.Structs;
using System;
using CefSharp.Core;
using Silk.NET.Core.Native;
using Silk.NET.Direct3D11;
using BrowserSettings = CefSharp.BrowserSettings;
using Range = CefSharp.Structs.Range;

namespace VRCX
{
    public class OffScreenBrowser : ChromiumWebBrowser, IRenderHandler
    {
        public ComPtr<ID3D11Device1> RenderDevice { get; set; }
        public ComPtr<ID3D11DeviceContext1> RenderContext { get; set; }
        public ComPtr<ID3D11Texture2D> RenderOutput { get; set; }

        public OffScreenBrowser(string address, int width, int height)
            : base(
                address,
                automaticallyCreateBrowser: false
            )
        {
            IWindowInfo info = ObjectFactory.CreateWindowInfo();
            info.SetAsWindowless(IntPtr.Zero);
            // Allows us to use OnAcceleratedPaint
            info.SharedTextureEnabled = true;
            
            CreateBrowser(
                info,
                new BrowserSettings()
                {
                    DefaultEncoding = "UTF-8"
                }
            );
            
            Size = new System.Drawing.Size(width, height);
            RenderHandler = this;

            JavascriptBindings.ApplyVrJavascriptBindings(JavascriptObjectRepository);
        }

        public new void Dispose()
        {
            RenderHandler = null;
            base.Dispose();
        }

        ScreenInfo? IRenderHandler.GetScreenInfo()
        {
            return null;
        }

        bool IRenderHandler.GetScreenPoint(int viewX, int viewY, out int screenX, out int screenY)
        {
            screenX = viewX;
            screenY = viewY;
            return false;
        }

        Rect IRenderHandler.GetViewRect()
        {
            return new Rect(0, 0, Size.Width, Size.Height);
        }

        unsafe void IRenderHandler.OnAcceleratedPaint(PaintElementType type, Rect dirtyRect, AcceleratedPaintInfo paintInfo)
        {
            if (type != PaintElementType.View)
                return;
            
            if (RenderDevice.Handle == IntPtr.Zero.ToPointer())
                return;
            
            if (RenderDevice.Handle == IntPtr.Zero.ToPointer())
                return;
            
            IntPtr sharedHandle = paintInfo.SharedTextureHandle;
            ComPtr<ID3D11Texture2D> sharedResource = default;
            Guid iid = ID3D11Texture2D.Guid;
            SilkMarshal.ThrowHResult(RenderDevice.OpenSharedResource1(ref sharedHandle, &iid, (void**)&sharedResource));
            RenderContext.CopyResource(RenderOutput, sharedResource);
        }

        void IRenderHandler.OnCursorChange(IntPtr cursor, CursorType type, CursorInfo customCursorInfo)
        {
        }

        void IRenderHandler.OnImeCompositionRangeChanged(Range selectedRange, Rect[] characterBounds)
        {
        }

        void IRenderHandler.OnPaint(PaintElementType type, Rect dirtyRect, IntPtr buffer, int width, int height)
        {
        }

        void IRenderHandler.OnPopupShow(bool show)
        {
        }

        void IRenderHandler.OnPopupSize(Rect rect)
        {
        }

        void IRenderHandler.OnVirtualKeyboardRequested(IBrowser browser, TextInputMode inputMode)
        {
        }

        bool IRenderHandler.StartDragging(IDragData dragData, DragOperationsMask mask, int x, int y)
        {
            return false;
        }

        void IRenderHandler.UpdateDragCursor(DragOperationsMask operation)
        {
        }
    }
}
