// HookCostume.h : main header file for the HookCostume DLL
//

#pragma once
#include "define.h"
#ifndef __AFXWIN_H__
	#error "include 'stdafx.h' before including this file for PCH"
#endif

#include "resource.h"		// main symbols


// CHookCostumeApp
// See HookCostume.cpp for the implementation of this class
//

class CHookCostumeApp : public CWinApp
{
public:
	CHookCostumeApp();

// Overrides
public:
	virtual BOOL InitInstance();

	DECLARE_MESSAGE_MAP()
	virtual int ExitInstance();
};
