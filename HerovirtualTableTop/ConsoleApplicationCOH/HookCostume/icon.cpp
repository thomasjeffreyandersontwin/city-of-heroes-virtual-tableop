/* vim: set sts=4 sw=4 et: */

/* Titan Icon
 * Copyright (C) 2013 Titan Network
 * All Rights Reserved
 *
 * This code is for educational purposes only and is not licensed for
 * redistribution in source form.
 */
#include "stdafx.h"
#define WINVER 0x0501
#include <windows.h>
#include <stdio.h>
#include <string.h>

#include "icon.h"
#include "code.h"
#include "data.h"
#include "strings.h"
#include "patch.h"
#include "util.h"

int editnpc = 0;
int random = 0;

static void RunPatch();
static void PromptUserForCohLocation();

PROCESS_INFORMATION pinfo;
DWORD gamePID;

HANDLE m_hTargetProcess;
BOOL InjectDLL(TCHAR* szDllName)
{ 
	TCHAR szDirectoryPath[MAX_PATH];

	GetCurrentDirectory(MAX_PATH, szDirectoryPath);
	lstrcat(szDirectoryPath,_TEXT("\\"));
	lstrcat(szDirectoryPath, szDllName);

	TCHAR* szRemoteDllName = NULL;
	LPVOID lpLoadLibrary = NULL;
	HANDLE hRemoteThread = NULL;
	DWORD dwRemote = 0;

	DWORD A = _tcsclen(szDirectoryPath);

	szRemoteDllName = (TCHAR*)VirtualAllocEx(m_hTargetProcess,NULL, sizeof(TCHAR) * _tcsclen(szDirectoryPath), MEM_COMMIT, PAGE_READWRITE);
	if(NULL == szRemoteDllName) 
	{
		CloseHandle(m_hTargetProcess);
		return FALSE;
	}

	if(!WriteProcessMemory(m_hTargetProcess,szRemoteDllName,szDirectoryPath, sizeof(TCHAR) * _tcsclen(szDirectoryPath),NULL)) 
	{
		VirtualFreeEx(m_hTargetProcess,szRemoteDllName, 0, MEM_RELEASE);
		return FALSE;
	}

	lpLoadLibrary = GetProcAddress(GetModuleHandle(_TEXT("KERNEL32.dll")),"LoadLibraryW");
	hRemoteThread = CreateRemoteThread(m_hTargetProcess,NULL,0,(LPTHREAD_START_ROUTINE)lpLoadLibrary,szRemoteDllName,0,NULL);

	if(NULL == hRemoteThread) 
	{
		VirtualFreeEx(m_hTargetProcess,szRemoteDllName,0,MEM_RELEASE);
		CloseHandle(m_hTargetProcess);
		return FALSE;
	}

	WaitForSingleObject(hRemoteThread,INFINITE);
	GetExitCodeThread(hRemoteThread,&dwRemote);
	CloseHandle(hRemoteThread);
	VirtualFreeEx(m_hTargetProcess,szRemoteDllName,0,MEM_RELEASE);
	return TRUE;			
}
#include <shlwapi.h>
#include <TLHELP32.H>
#include <psapi.h>
typedef struct _ProcInfo
{
	DWORD	dwProcId;
	char	ProcName[32];
}PROCINFO;

DWORD	FindProcess(LPTSTR szName)
{
	DWORD			dwResult = 0;

	HANDLE			hSnapHandle;
	PROCESSENTRY32	pProcessEntry;

	hSnapHandle = CreateToolhelp32Snapshot(TH32CS_SNAPPROCESS, 0);
	if (hSnapHandle == INVALID_HANDLE_VALUE)
	{
		return	dwResult;
	}

	pProcessEntry.dwSize = sizeof(pProcessEntry);
	if (!Process32First(hSnapHandle, &pProcessEntry))
	{
		CloseHandle(hSnapHandle);
		return	dwResult;
	}

	do
	{
		if (pProcessEntry.th32ProcessID == 0)
			continue;

		if (lstrcmpi(szName, pProcessEntry.szExeFile) == 0)
		{
			dwResult = pProcessEntry.th32ProcessID;
			break;
		}
	} while (Process32Next(hSnapHandle, &pProcessEntry));

	CloseHandle(hSnapHandle);
	return	dwResult;
}

extern char t_gamepath[1024];
char old_gamepath[1024];
int WINAPI mWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance,
	PSTR szCmdParam, int iCmdShow)
{
	//if (!stricmp(szCmdParam, "-n")) //phc
	//editnpc = 1;
	//if (!stricmp(szCmdParam, "-r")) //phc
	random = 1;					  //phc	

	memset(&pinfo, 0, sizeof(pinfo));
	HWND hwnd = ::FindWindowA("CrypticWindow", NULL);
	if (hwnd == NULL) {

		// First check to see if the file exists.
		//
		//		while (GetFileAttributesA("cityofheroes.exe") == INVALID_FILE_ATTRIBUTES) {
		//			PromptUserForCohLocation();
		//		}
		GetCurrentDirectoryA(1024, old_gamepath);
		SetCurrentDirectoryA(t_gamepath);
		STARTUPINFO startup;
		memset(&startup, 0, sizeof(startup));
		startup.cb = sizeof(STARTUPINFO); 
		//if (!CreateProcessA("cityofheroes.exe", "cityofheroes.exe -project coh", NULL, NULL, FALSE, CREATE_NEW_PROCESS_GROUP | CREATE_SUSPENDED | DETACHED_PROCESS, NULL, NULL, (LPSTARTUPINFOA)&startup, &pinfo)) {
		if (!CreateProcessA("cityofheroes.exe", "cityofheroes.exe -project coh -noverify", NULL, NULL, FALSE, CREATE_NEW_PROCESS_GROUP | CREATE_SUSPENDED | DETACHED_PROCESS, NULL, NULL, (LPSTARTUPINFOA)&startup, &pinfo)) {
			MessageBoxA(NULL, "Failed to launch process!", "Error", MB_OK | MB_ICONEXCLAMATION);
			return 0;
		}


		// delete old crap previous version used
		BOOL blAtr = GetFileAttributesA("data\\charcreate.txt");
		if (blAtr)
			DeleteFileA("data\\charcreate.txt");

		RunPatch();

		//Inject my Dll
		//SetCurrentDirectoryA(old_gamepath); // This line was totally unncecessary 

		gamePID = pinfo.dwProcessId;
		m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, gamePID);

		// Again, the following line of code is unnecessary and causing bugs. The current directory should be where the game is, i.e. t_gamepath. 
		//Not sure why this was ever done, as it caused bugs when application output directory and game directory were different (as would be the case most of the time).
		//SetCurrentDirectoryA(old_gamepath); 

		InjectDLL(_T("HookCostume.dll"));

		CloseHandle(m_hTargetProcess);

		ResumeThread(pinfo.hThread);


	}
	else {
		gamePID = FindProcess(_T("cityofheroes.exe"));
		pinfo.hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, gamePID);
		SuspendThread(pinfo.hProcess);

		char buff[10];
		//check memory
		ReadProcessMemory(pinfo.hProcess, (void *)0x004165BD, &buff, 5, NULL);
		if (memcmp(buff, "\xE8\x4E\x81\x5C\x00", 5) == 0) {
			RunPatch();
		}

		m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, gamePID);
		SuspendThread(m_hTargetProcess);

		InjectDLL(_T("HookCostume.dll"));

		ResumeThread(m_hTargetProcess);
		CloseHandle(m_hTargetProcess);

	}
	return 0;
}

static void RunPatch() {
    int vers = 0;

	ULONG gIntVal = GetInt(0x00BE15D4);
    if (gIntVal == 0xa77f40)
		vers = 23;
    else if (GetInt(0x00BE38BC) == 0xa76044)
		vers = 24;
    else
		Bailout("Sorry, your cityofheroes.exe file is not a supported version.");

    InitCoh(vers);

    WriteStrings();
    WriteData();
    RelocateCode();
    FixupCode(vers);
    WriteCode();

    if (vers == 23)
		PatchI23();
    else if (vers == 24)
		PatchI24();
}
//#include <dlgs.h>       // for standard control IDs for commdlg
//#include "afxglobals.h"
static void PromptUserForCohLocation() {
    OPENFILENAMEA ofn;
    char szFile[1024];

    MessageBoxA(NULL, "We couldn't find a cityofheroes.exe file in the current directory.\n\nPlease select the game installation that you wish to use.", "Titan Icon", MB_OK);

    ZeroMemory(&ofn, sizeof(ofn));
    ofn.lStructSize = sizeof(ofn);
    ofn.lpstrFile = szFile;
    ofn.lpstrFile[0] = 0;
    ofn.nMaxFile = sizeof(szFile);
    ofn.lpstrFilter = "Cityofheroes.exe\0cityofheroes.exe\0";
    ofn.nFilterIndex = 0;
    ofn.lpstrFileTitle = NULL;
    ofn.nMaxFileTitle = 0;
    ofn.lpstrInitialDir = NULL;
    ofn.Flags = OFN_PATHMUSTEXIST | OFN_FILEMUSTEXIST | OFN_DONTADDTORECENT;

    if (GetOpenFileNameA(&ofn) == FALSE)
	exit(0);
}
