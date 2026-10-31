# CaptureHost third-party notices

The Windows Graphics Capture/D3D11 interop implementation in `PoE2CaptureHost`
uses the public Windows Graphics Capture interfaces documented by Microsoft and
was informed by the MIT-licensed WgcSharp project:

- WgcSharp: https://github.com/Giltanas/WgcSharp
- Copyright (c) 2026 Giltanas
- License: MIT

WgcSharp's raw COM/vtable approach was used as a reference for creating an HWND
`GraphicsCaptureItem`, a free-threaded `Direct3D11CaptureFramePool`, and accessing
the underlying `ID3D11Texture2D`. PoE2CaptureHost implements a separate persistent
capture/session architecture and shared-memory transport for the autosplitter.

PoE2CaptureHost also depends on:

- Vortice.Direct3D11 3.8.1 (Vortice.Windows project), MIT licensed.

The MIT license text for WgcSharp is reproduced below as required by its license:

```
MIT License

Copyright (c) 2026 Giltanas

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
```
