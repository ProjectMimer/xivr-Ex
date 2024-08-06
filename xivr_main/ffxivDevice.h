#pragma once
#include <d3d11_4.h>
#include "ffxivSwapChain.h"

struct stDevice
{
	/* 0x000 */ byte spcr1[0x8];
	/* 0x008 */ unsigned long long ContextArray;
	/* 0x010 */ unsigned long long RenderThread;
	/* 0x018 */ byte spcr2[0x70 - 0x18];
	/* 0x070 */ stSwapChain* SwapChain;
	/* 0x078 */ byte spcr3[0x2];
	/* 0x07A */ byte RequestResolutionChange;
	/* 0x07B */ byte spcr4[0x8C - 0x7B];
	/* 0x08C */ unsigned int width;
	/* 0x090 */ unsigned int height;
	/* 0x094 */ byte spcr5[0x830 - 0x94];
	/* 0x830 */ unsigned int width1;
	/* 0x834 */ unsigned int height1;
	/* 0x838 */ byte spcr8[0x890 - 0x838];
	/* 0x890 */ unsigned int D3DFeatureLevel;
	/* 0x898 */ IDXGIFactory4* IDXGIFactory;
	/* 0x8A0 */ IDXGIOutput4* IDXGIOutput;
	/* 0x8A8 */ ID3D11Device4* Device;
	/* 0x8B0 */ ID3D11DeviceContext4* DeviceContext;
	/* 0x8B8 */ unsigned long long uk2;
	/* 0x8C0 */ unsigned long long ImmediateContext;
};