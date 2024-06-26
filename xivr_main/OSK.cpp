#include "OSK.h"

typedef BOOL(WINAPI* DwmGetDxSharedSurface_td) (
	__in HWND hwnd,
	__out_opt HANDLE* p1,
	__out_opt LUID* p2,
	__out_opt ULONG* p3,
	__out_opt ULONG* p4,
	__out_opt ULONGLONG* p5);

DwmGetDxSharedSurface_td DwmGetSharedSurface = ((DwmGetDxSharedSurface_td)GetProcAddress(GetModuleHandle(TEXT("USER32")), "DwmGetDxSharedSurface"));


PROCESSENTRY32 OSK::FindProcess(std::wstring toFind)
{
	PROCESSENTRY32 entry;
	entry.dwSize = sizeof(PROCESSENTRY32);

	HANDLE snapshot = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
	if (snapshot == INVALID_HANDLE_VALUE)
		return PROCESSENTRY32();
	if (!Process32First(snapshot, &entry))
		return PROCESSENTRY32();

	do
	{
		std::wstring fileName(entry.szExeFile);
		if (fileName == toFind)
		{
			CloseHandle(snapshot);
			return entry;
		}
	} while (Process32Next(snapshot, &entry));
	CloseHandle(snapshot);
	return PROCESSENTRY32();
}

void OSK::ToggleOSK()
{
	INPUT input[6];
	ZeroMemory(input, sizeof(INPUT) * 6);
	input[0].type = INPUT_KEYBOARD;
	input[0].ki.wVk = VK_LCONTROL;
	input[0].ki.dwFlags = 0;
	input[1].type = INPUT_KEYBOARD;
	input[1].ki.wVk = VK_LWIN;
	input[1].ki.dwFlags = 0;
	input[2].type = INPUT_KEYBOARD;
	input[2].ki.wVk = VkKeyScan('O');
	input[2].ki.dwFlags = 0;

	input[3].type = INPUT_KEYBOARD;
	input[3].ki.wVk = VkKeyScan('O');
	input[3].ki.dwFlags = KEYEVENTF_KEYUP;
	input[4].type = INPUT_KEYBOARD;
	input[4].ki.wVk = VK_LWIN;
	input[4].ki.dwFlags = KEYEVENTF_KEYUP;
	input[5].type = INPUT_KEYBOARD;
	input[5].ki.wVk = VK_LCONTROL;
	input[5].ki.dwFlags = KEYEVENTF_KEYUP;
	SendInput(6, input, sizeof(INPUT));
}

void OSK::CreateOSKTexture(ID3D11Device* device, stBasicTexture* oskTexture)
{
	//----
	// Creates the given texture to be a copy of the onscreen keyboard
	//----
	if (oskSharedTexture.pTexture) { oskSharedTexture.Release(); }
	if (oskTexture->pTexture) { oskTexture->Release(); }

	if (oskLayout.hwnd)
	{
		//----
		// Get the position of the keyboard and get the very top left point of the keyboard window
		// to remove the top bar and border
		//----
		RECT rect;
		GetWindowRect(oskLayout.hwnd, &rect);

		POINT point = { rect.left, rect.top };
		ScreenToClient(oskLayout.hwnd, &point);
		displayRect.left = std::abs(point.x);
		displayRect.top = std::abs(point.y);

		//----
		// Get the actual client size of the keyboard
		//----
		GetClientRect(oskLayout.hwnd, &rect);
		displayRect.right = rect.right;
		displayRect.bottom = rect.bottom;
	}

	if (oskSharedTexture.Create(device, false, false, true))
	{
		oskSharedTexture.pTexture->GetDesc(&oskSharedTexture.textureDesc);

		oskTexture->textureDesc = oskSharedTexture.textureDesc;
		oskTexture->textureDesc.Width = displayRect.right;
		oskTexture->textureDesc.Height = displayRect.bottom;
		oskTexture->Create(device, false, true, false);

		oskLayout.width = oskTexture->textureDesc.Width;
		oskLayout.height = oskTexture->textureDesc.Height;
		oskLayout.haveLayout = true;
	}
}

void OSK::CopyOSKTexture(ID3D11Device* device, ID3D11DeviceContext* devCon, stBasicTexture* oskTexture)
{
	//----
	// Copy the on screen keyboard to the given texture
	//----
	if (oskLayout.hwnd)
	{
		LUID adapter = LUID();
		ULONG pFmtWindow = 0;
		ULONG pPresentFlags = 0;
		ULONGLONG pWin32kUpdateId = 0;
		HANDLE toskSurface;

		DwmGetSharedSurface(oskLayout.hwnd, &toskSurface, &adapter, &pFmtWindow, &pPresentFlags, &pWin32kUpdateId);
		if (toskSurface != oskSharedTexture.pSharedHandle)
		{
			oskSharedTexture.pSharedHandle = toskSurface;
			CreateOSKTexture(device, oskTexture);
		}

		//devCon->CopyResource(oskTexture->pTexture, oskSharedTexture.pTexture);
		D3D11_BOX oskTextureCutout = { displayRect.left, displayRect.top, 0, displayRect.left + displayRect.right, displayRect.top + displayRect.bottom, 1 };
		devCon->CopySubresourceRegion(oskTexture->pTexture, 0, 0, 0, 0, oskSharedTexture.pTexture, 0, &oskTextureCutout);
	}
}

bool OSK::LoadOSK(ID3D11Device* device, stBasicTexture* oskTexture, RECT position)
{
	//----
	// Find the on screen keyboard, if it isnt loaded, load it and wait for it to be found
	// otherwise resize it and create the texture
	//----

	bool haveFoundKeyboard = false;
	int haveFoundKeyboardCount = 100;

	PROCESSENTRY32 osk = FindProcess(L"osk.exe");
	if (osk.th32ProcessID != 0)
	{
		haveFoundKeyboard = true;
	}
	else if (osk.th32ProcessID == 0)
	{
		ToggleOSK();
		while (!haveFoundKeyboard && haveFoundKeyboardCount > 0)
		{
			osk = FindProcess(L"osk.exe");
			if (osk.th32ProcessID != 0)
				haveFoundKeyboard = true;
			haveFoundKeyboardCount--;
		}
	}

	if (haveFoundKeyboard)
	{
		oskLayout.hwnd = FindWindow(L"OSKMainClass", NULL);
		if (oskLayout.hwnd) {
			LUID adapter = LUID();
			ULONG pFmtWindow = 0;
			ULONG pPresentFlags = 0;
			ULONGLONG pWin32kUpdateId = 0;

			DwmGetSharedSurface(oskLayout.hwnd, &oskSharedTexture.pSharedHandle, &adapter, &pFmtWindow, &pPresentFlags, &pWin32kUpdateId);

			if (oskSharedTexture.pSharedHandle != 0)
			{
				SetWindowPos(oskLayout.hwnd, 0, position.left, position.top, position.right, position.bottom, SWP_NOACTIVATE | SWP_NOZORDER | SWP_FRAMECHANGED);
				SendMessageA(oskLayout.hwnd, WM_EXITSIZEMOVE, WPARAM(0), LPARAM(0));
				CreateOSKTexture(device, oskTexture);
			}
		}
	}
	return haveFoundKeyboard;
}

stScreenLayout* OSK::GetOSKLayout()
{
	return &oskLayout;
}

void OSK::ShowHide(bool show)
{
	HWND curActiveWin = GetActiveWindow();
	SetActiveWindow(oskLayout.hwnd);
	if (show)
		ShowWindow(oskLayout.hwnd, SW_SHOW);
	else
		ShowWindow(oskLayout.hwnd, SW_HIDE);
	SetActiveWindow(curActiveWin);
}

void OSK::UnloadOSK()
{
	if (oskLayout.hwnd != 0)
	{
		PROCESSENTRY32 osk = FindProcess(L"osk.exe");
		if (osk.th32ProcessID != 0)
			ToggleOSK();
		oskLayout = stScreenLayout();
		oskSharedTexture.Release();
	}
}
