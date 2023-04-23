#pragma once
#define WIN32_LEAN_AND_MEAN
#include <windows.h>

struct stConfiguration
{
	int resetValue;
	int languageType;
	bool isEnabled;
	bool isAutoEnabled;
	bool autoResize;
	bool autoMove;
	bool runRecenter;
	bool vLog;
	bool hmdPointing;
	bool forceFloatingScreen;
	bool forceFloatingInCutscene;
	bool horizontalLock;
	bool verticalLock;
	bool horizonLock;
	bool conloc;
	bool hmdloc;
	bool vertloc;
	bool motioncontrol;
	bool showWeaponInHand;
	float offsetAmountX;
	float offsetAmountY;
	float offsetAmountZ;
	float offsetAmountYFPS;
	float offsetAmountZFPS;
	float snapRotateAmountX;
	float snapRotateAmountY;
	bool uiDepth;
	float uiOffsetZ;
	float uiOffsetScale;
	float ipdOffset;
	bool swapEyes;
	bool swapEyesUI;
	int hmdWidth;
	int hmdHeight;
	int targetCursorSize;
	bool asymmetricProjection;
	bool mode2d;
	bool immersiveMovement;
	bool immersiveFull;
	bool ultrawideshadows;
};