// HookCostume.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "HookCostume.h"
#include "icon.h"
#include "power.h"
#include <windows.h>
#include <stdio.h>
#include <string.h>

#include "icon.h"
#include "code.h"
#include "data.h"
#include "strings.h"
#include "patch.h"
#include "util.h"
#include <string> 
#include <iostream>
#include <fstream>
using namespace std;
#ifdef _DEBUG
#define new DEBUG_NEW
#endif


// CHookCostumeApp

BEGIN_MESSAGE_MAP(CHookCostumeApp, CWinApp)
END_MESSAGE_MAP()

#pragma data_seg(".JOE")

static BOOL		GameStarted = false;

static int		ManagerWND = 0;							// Manager WND

//Mouse information
static DWORD	M_X = 0, M_Y = 0;						// mouse x,y
static float	B_X = 0, B_Y = 0, B_Z = 0, B_D = 0;				// game  x,y,z
static char		m_Mouse_Information[1024] = "";

//NPC  information
static int		NPC_NO = 0;
static int		NPC_Flag = 0;
static float	N_X = 0, N_Y = 0, N_Z = 0, N_D = 0;		// NPC info x,y,z
static char		m_NPC_Information[1024] = "";


// Command invoke
static DWORD    Export_CommandBuff_Realloc = 0;

// Collision detection
static DWORD	Export_Collision_Detection = 0; //Exported function
static float	SOURCEXYZ[3] = { 0, 0, 0 };
static float	DESTINATIONXYZ[3] = { 0, 0, 0 };
static int		COLLISION_RESULT = 0;
static char		m_Collision_Info[1024] = "";
static float	TEMPXYZ1[0x10] = { 0, 0, 0 };
static float	TEMPXYZ2[0x10] = { 0, 0, 0 };
#pragma data_seg()

#pragma comment(linker, "/section:.JOE,rws")

char		m_commandline[1024] = "";
char		t_NPC_Information[1024];
char		t_Mouse_Information[1024];

//export function//////////////////////////////////////////

__declspec(dllexport) int __cdecl InitGame(int mmhWnd, char *gamePath);
__declspec(dllexport) int __cdecl SetUserHWND(HWND hWnd);
__declspec(dllexport) int __cdecl CloseGame(HWND hWnd);
__declspec(dllexport) char * __cdecl GetHoveredNPCInfo();
__declspec(dllexport) char * __cdecl GetMouseXYZInGame();

__declspec(dllexport) int __cdecl ExecuteCommand(char *cmdstring);
//__declspec(dllexport) int __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d);
__declspec(dllexport) char * __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z);

void CollisionDetect()
{
	_asm {

		lea     ecx, TEMPXYZ2;
		push    ecx;

		lea     ecx, TEMPXYZ1;
		push    ecx;

		lea     ecx, SOURCEXYZ;
		push    ecx;

		lea		ecx, COLLISION_RESULT;
		lea		eax, DESTINATIONXYZ;

		mov     edx, 0x4CD300;
		call    edx;
		add		esp, 0x0C;

	}
}

void CommandBuff_Realloc()
{
	_asm {

		mov		ebx, 0x110;
		push	ebx;

		mov		ebx, 0xF0D7F4;	//command buff
		mov     ebx, [ebx];
		mov     ebx, [ebx + 0x18];

		SUB		ebx, 0xE;
		push	ebx; void *

			mov		ebx, 0xB24C98;
		mov		ebx, [ebx];
		call    ebx; //realloc
		mov		ebx, 0x100;
		mov[eax + 4], ebx;
		add     esp, 8;
		add     eax, 0Eh;

		mov		ebx, 0xF0D7F4;	//command buff
		mov     ebx, [ebx];
		mov[ebx + 0x18], eax;

		/////
		mov		ebx, 0x110;
		push	ebx;

		mov		ebx, 0xF0D7F4;	//command buff
		mov     ebx, [ebx];
		mov     ebx, [ebx + 0x1C];

		SUB		ebx, 0xE;
		push	ebx; void *

			mov		ebx, 0xB24C98;
		mov		ebx, [ebx];
		call    ebx; //realloc

		mov		ebx, 0x100;
		mov[eax + 4], ebx;
		add     esp, 8;
		add     eax, 0Eh;
		mov		ebx, 0xF0D7F4;	//command buff
		mov     ebx, [ebx];
		mov[ebx + 0x1C], eax;

	}
}

extern PROCESS_INFORMATION pinfo;
extern DWORD gamePID;
char strbuff[1024];

// CHookCostumeApp construction

CHookCostumeApp::CHookCostumeApp()
{
}


// The one and only CHookCostumeApp object

CHookCostumeApp theApp;

// CHookCostumeApp initialization
HINSTANCE	hInst;								// current instance
HINSTANCE	hInstance;
BOOL		PowerHook();

#include "COHDialog.h"
HANDLE		m_HookDLGThread = NULL;
DWORD		m_HookDLGThreadID;
COHDialog	m_HookDLG;

int HookDLGThread()
{
	TCHAR bb[1024];
	wsprintf(bb, _T("%x"), ManagerWND);
	//	AfxMessageBox(bb);

	/*TCHAR buf[100];
	_stprintf_s(buf, _T("%d"), ManagerWND);
	MessageBox(0, buf, _T("HookDLG"), MB_OK);*/
	if (ManagerWND == 0) {
		Sleep(1000);
		m_HookDLG.DoModal();
	}
	else {
		while (1) {
			Sleep(50);
			NPC_Flag = 0;
		}
	}
	return 0;
}


BOOL CHookCostumeApp::InitInstance()
{
	CWinApp::InitInstance();

	//	hInstance = hModule;
	DWORD pid = GetCurrentProcessId();
	HINSTANCE handle;

	char fname[1024];
	handle = GetModuleHandle(NULL);
	::GetModuleFileNameA(handle, fname, 1024);

	if (strstr(fname, "cityofheroes.exe") != NULL) {
		//MessageBox(0, _T("found"), _T("HookDLG"), MB_OK);
		PowerHook();
		Export_CommandBuff_Realloc = (DWORD)CommandBuff_Realloc;
		Export_Collision_Detection = (DWORD)CollisionDetect;
		return true;

	}
	else {
		// Commenting the following line as, when loaded from an external application, the HookDLGThread is initialized before ManagerWND variable is initialized, 
		// which causes a problem and always shows the hook dialog because it finds the ManagerWND = 0
		// Therefore, we should rather initialize this thread from InitGame method after ManagerWND variable has been initialized.
		//m_HookDLGThread = ::CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)HookDLGThread, (LPVOID)0, 0, &m_HookDLGThreadID);
		::Sleep(1000);
	}

	return TRUE;
}

void SendToUser(DWORD NPC_no)
{
	NPC_NO = NPC_no;

	NPC_Info mNpcInfo;
	//	mNpcInfo = getHoverNPCMemInfo(NPC_NO);
	DWORD NPCSelected;
	memcpy(&NPCSelected, (char *)0xF14FB0, 4);

	memcpy(mNpcInfo.npcName, (char *)NPC_no + 0x31C4, 128);
	memcpy(&mNpcInfo.m_NpcXYZ.flX, (char *)NPC_no + 0x5C, 4);
	memcpy(&mNpcInfo.m_NpcXYZ.flY, (char *)NPC_no + 0x60, 4);
	memcpy(&mNpcInfo.m_NpcXYZ.flZ, (char *)NPC_no + 0x64, 4);

	N_X = mNpcInfo.m_NpcXYZ.flX;
	N_Y = mNpcInfo.m_NpcXYZ.flY;
	N_Z = mNpcInfo.m_NpcXYZ.flZ;

	NPC_Flag = 1;

	sprintf_s(m_NPC_Information,
		"Name: [%s] X:[%1.2f] Y:[%1.2f] Z:[%1.2f]",
		mNpcInfo.npcName, mNpcInfo.m_NpcXYZ.flX, mNpcInfo.m_NpcXYZ.flY, mNpcInfo.m_NpcXYZ.flZ);

}

void SendToUserXYZ(DWORD m_x, DWORD m_y, float s_x, float s_y, float s_z, float s_d)
{
	M_X = m_x;
	M_Y = m_y;

	B_X = s_x;
	B_Y = s_y;
	B_Z = s_z;
	B_D = s_d;				// 

	if (NPC_Flag == 1) {
		B_X = N_X;
		B_Y = N_Y;
		B_Z = N_Z;
	}
	GameStarted = true;

	sprintf_s(m_Mouse_Information, "X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]", B_X, B_Y, B_Z, B_D);

	//	PostMessage(ManagerWND, WM_USER + 102, m_x, m_y);
}

void SetXYZ()	// DWORD x, DWORD y)
{
	char buff[1024];
	USES_CONVERSION;

	sprintf_s(buff, "X:[%d] Y:[%d]", M_X, M_Y);
	m_HookDLG.m_mousepos.SetWindowText(A2W(buff));

	sprintf_s(buff, "X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]", B_X, B_Y, B_Z, B_D);
	m_HookDLG.m_stagepos.SetWindowText(A2W(buff));
}

void SetNPC()
{
	if (NPC_Flag == 1){// && NPC_NO!=-1) {
		strcpy_s(t_NPC_Information, m_NPC_Information);
		USES_CONVERSION;
		m_HookDLG.m_NPCINFO.SetWindowText(A2W(t_NPC_Information));
	}
	else {
		m_HookDLG.m_NPCINFO.SetWindowText(_T(""));
	}
	NPC_Flag = 0;
}

///////////////////////////////////////////////////////////
char t_gamepath[1024];
__declspec(dllexport) int __cdecl InitGame(int mmhWnd, char *gamePath)
{
	ManagerWND = mmhWnd;
	/*TCHAR buf[100];
	_stprintf_s(buf, _T("%d"), ManagerWND);
	MessageBox(0, buf, _T("InitGame"), MB_OK);*/
	m_HookDLGThread = ::CreateThread(NULL, 0, (LPTHREAD_START_ROUTINE)HookDLGThread, (LPVOID)0, 0, &m_HookDLGThreadID);
	strcpy_s(t_gamepath, gamePath);
	mWinMain(NULL, NULL, ::GetCommandLineA(), SW_SHOW);

	return FALSE;
}

//CityOfHeores Close

extern PROCESS_INFORMATION pinfo;
extern DWORD gamePID;

__declspec(dllexport) int __cdecl CloseGame(HWND hWnd)
{
	//	if (m_HookDLGThread != NULL) {
	//		TerminateThread(m_HookDLGThread, 0);
	//	}
	m_HookDLG.PostMessage(WM_CLOSE, 0, 0);
	Sleep(1000);

	//	TerminateProcess(OpenProcess(PROCESS_ALL_ACCESS, FALSE, gamePID), 0);
	return FALSE;
}

__declspec(dllexport) BOOL __cdecl CheckGameDone()
{
	return GameStarted;
}

//SetUserHWND
__declspec(dllexport) int __cdecl SetUserHWND(HWND hWnd)
{
	/*
	if (hWnd == NULL) {
	ManagerWND = m_HookDLG.m_hWnd;
	}else{
	ManagerWND = hWnd;
	}
	*/
	return FALSE;
}

//__declspec(dllexport) int __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d)
//{
//	SOURCEXYZ[0] = s_x;	SOURCEXYZ[1] = s_y;	SOURCEXYZ[2] = s_z;
//	DESTINATIONXYZ[0] = d_x; DESTINATIONXYZ[1] = d_y; DESTINATIONXYZ[2] = d_z;
//	COLLISION_RESULT = 0;
//	memset(TEMPXYZ1, 0, 0x40);
//	memset(TEMPXYZ2, 0, 0x40);
//	TEMPXYZ1[0] = s_x; TEMPXYZ1[1] = s_y; TEMPXYZ1[2] = s_z;
//	TEMPXYZ2[0] = s_x; TEMPXYZ2[1] = s_y; TEMPXYZ2[2] = s_z;
//
//	if (Export_Collision_Detection == NULL)return 0;
//	DWORD dwPID = gamePID;// GetCurrentProcessId();
//	HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
//
//	//Disable Thread
//	SuspendThread(m_hTargetProcess);
//	{
//		HANDLE hRemoteThread = NULL;
//		hRemoteThread = CreateRemoteThread(m_hTargetProcess, NULL, 0, (LPTHREAD_START_ROUTINE)Export_Collision_Detection, NULL, 0, NULL);
//		WaitForSingleObject(hRemoteThread, INFINITE);
//		CloseHandle(hRemoteThread);
//		*c_x = TEMPXYZ2[0x9];
//		*c_y = TEMPXYZ2[0xA];
//		*c_z = TEMPXYZ2[0xB];
//		float sum = 0;
//		if (*c_x == 0 && *c_y == 0 && *c_z == 0) {
//			sum= (s_x - d_x) * (s_x - d_x) + (s_y - d_y) * (s_y - d_y) + (s_z - d_z) * (s_z - d_z);
//			*c_d = sqrt(sum);
//		} else {
//			sum = (s_x - *c_x) * (s_x - *c_x) + (s_y - *c_y) * (s_y - *c_y) + (s_z - *c_z) * (s_z - *c_z);
//			*c_d = sqrt(sum);
//		}
//		sprintf_s(m_Mouse_Information, "X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]", B_X, B_Y, B_Z, B_D);
//	}
//	//Resume Thread
//	while (ResumeThread(m_hTargetProcess) != -1);
//
//	//Result process
//
//	return COLLISION_RESULT;
//}

__declspec(dllexport) char * __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z)
{
	SOURCEXYZ[0] = s_x;	SOURCEXYZ[1] = s_y;	SOURCEXYZ[2] = s_z;
	DESTINATIONXYZ[0] = d_x; DESTINATIONXYZ[1] = d_y; DESTINATIONXYZ[2] = d_z;
	COLLISION_RESULT = 0;
	float c_x, c_y, c_z, c_d;
	memset(TEMPXYZ1, 0, 0x40);
	memset(TEMPXYZ2, 0, 0x40);
	TEMPXYZ1[0] = s_x; TEMPXYZ1[1] = s_y; TEMPXYZ1[2] = s_z;
	TEMPXYZ2[0] = s_x; TEMPXYZ2[1] = s_y; TEMPXYZ2[2] = s_z;

	if (Export_Collision_Detection == NULL)return 0;
	DWORD dwPID = gamePID;// GetCurrentProcessId();
	HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);

	//Disable Thread
	SuspendThread(m_hTargetProcess);
	{
		HANDLE hRemoteThread = NULL;

		char olds[6];
		ReadProcessMemory(m_hTargetProcess, olds, (void *)0x04CD32F, 5, NULL);
		char nops[6] = "\x90\x90\x90\x90\x90";
		WriteProcessMemory(m_hTargetProcess, (void *)0x04CD32F, nops, 5, NULL);

		hRemoteThread = CreateRemoteThread(m_hTargetProcess, NULL, 0, (LPTHREAD_START_ROUTINE)Export_Collision_Detection, NULL, 0, NULL);
		WaitForSingleObject(hRemoteThread, INFINITE);
		CloseHandle(hRemoteThread);
		WriteProcessMemory(m_hTargetProcess, (void *)0x04CD32F, olds, 5, NULL);
		c_x = TEMPXYZ2[0x9];
		c_y = TEMPXYZ2[0xA];
		c_z = TEMPXYZ2[0xB];
		float sum = 0;
		if (c_x == 0 && c_y == 0 && c_z == 0) {
			sum = (s_x - d_x) * (s_x - d_x) + (s_y - d_y) * (s_y - d_y) + (s_z - d_z) * (s_z - d_z);
			c_d = sqrt(sum);
		}
		else {
			sum = (s_x - c_x) * (s_x - c_x) + (s_y - c_y) * (s_y - c_y) + (s_z - c_z) * (s_z - c_z);
			c_d = sqrt(sum);
		}
		sprintf_s(m_Mouse_Information, "X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]", c_x, c_y, c_z, c_d);
	}
	//Resume Thread
	while (ResumeThread(m_hTargetProcess) != -1);

	//Result process

	return m_Mouse_Information;
}

__declspec(dllexport) int __cdecl ExecuteCommand(char *m_commandline)
{
	TCHAR buft[1000];
	ofstream myfile;
	myfile.open("hooklog.txt");
	if (Export_CommandBuff_Realloc == NULL)return 0;

	int bufflen = strlen(m_commandline);
	myfile << "CommandLine: " << m_commandline << endl;
	myfile << "Bufflen: " << bufflen << endl;
	if (bufflen >= 0x100)return 0;

	DWORD dwPID = gamePID;// GetCurrentProcessId();
	HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);
	myfile << "PID: " << dwPID << endl;
	//DisableThread
	SuspendThread(m_hTargetProcess);
	DWORD commandoption = 2;

	//Command line show
	WriteProcessMemory(m_hTargetProcess, (void *)0x0DFC65C, &commandoption, 4, NULL);
	_stprintf_s(buft, _T("%d"), commandoption);
	myfile << "Command option: " << commandoption << endl;
	DWORD buff = 0;
	WriteProcessMemory(m_hTargetProcess, (void *)0xF0D8FF, &buff, 1, NULL);
	_stprintf_s(buft, _T("%d"), buff);
	myfile << "Buff initial after write 0xF0D8FF : " << buff << endl;
	buff = 0;
	ReadProcessMemory(m_hTargetProcess, (void *)0xD175EC, &buff, 4, NULL);
	_stprintf_s(buft, _T("%d"), buff);
	myfile << "Buff after read 0xD175EC : " << buff << endl;
	if (buff != 0) {
		WriteProcessMemory(m_hTargetProcess, (void *)0xD175EC, &buff, 4, NULL);
	}

	ReadProcessMemory(m_hTargetProcess, (void *)0xF0D7F4, &buff, 4, NULL); //command buffer
	_stprintf_s(buft, _T("%d"), buff);
	myfile << "Buff after read 0xF0D7F4 : " << buff << endl;
	if (buff != 0) {
		DWORD buff1 = 0;
		DWORD buff2 = 0;
		ReadProcessMemory(m_hTargetProcess, (void *)(buff + 0x1c), &buff1, 4, NULL);
		ReadProcessMemory(m_hTargetProcess, (void *)(buff + 0x18), &buff2, 4, NULL);
		_stprintf_s(buft, _T("%d"), buff1);
		myfile << "Buff1 after read (buff + 0x1c) : " << buff1 << endl;
		_stprintf_s(buft, _T("%d"), buff2);
		myfile << "Buff2 after read (buff + 0x18) : " << buff2 << endl;
		DWORD templen = 0;
		ReadProcessMemory(m_hTargetProcess, (void *)(buff2 - 0xA), &templen, 4, NULL); //buffer size
		_stprintf_s(buft, _T("%d"), templen);
		myfile << "Templen : " << templen << endl;
		if (templen < bufflen) {
			HANDLE hRemoteThread = NULL;
			hRemoteThread = CreateRemoteThread(m_hTargetProcess, NULL, 0, (LPTHREAD_START_ROUTINE)Export_CommandBuff_Realloc, NULL, 0, NULL);
			WaitForSingleObject(hRemoteThread, INFINITE);
			CloseHandle(hRemoteThread);
		}
		ReadProcessMemory(m_hTargetProcess, (void *)(buff + 0x18), &buff2, 4, NULL);
		_stprintf_s(buft, _T("%d"), buff2);
		myfile << "Buff2 after read (buff + 0x18): " << buff2 << endl;
		if (buff1 != 0) {
			int bufflen = strlen(m_commandline);

			//process of char ":"
			memset(strbuff, 0, 0x100);
			memcpy(strbuff, m_commandline, bufflen + 1);
			while (true) {
				char *p = strstr(strbuff, ":");
				if (p == NULL) {
					break;
				}
				else {
					p[0] = ' ';
				}
			}
			_stprintf_s(buft, _T("%d"), buff2);
			//change buffer length of command string
			WriteProcessMemory(m_hTargetProcess, (void *)(buff + 0x20), &bufflen, 4, NULL);
			
			_stprintf_s(buft, _T("%d"), bufflen);
			//MessageBox(0, buft, _T("inwhileloop2"), MB_OK);
			myfile << "Bufflen after write (buff + 0x20): " << bufflen << endl;

			WriteProcessMemory(m_hTargetProcess, (void *)(buff2 - 6), &bufflen, 4, NULL);
			WriteProcessMemory(m_hTargetProcess, (void *)buff2, strbuff, 0x100, NULL);

			WriteProcessMemory(m_hTargetProcess, (void *)(buff1 - 6), &bufflen, 4, NULL);
			WriteProcessMemory(m_hTargetProcess, (void *)buff1, strbuff, 0x100, NULL);

			//mov     byte_F0D8FF, 1
			char bone[1];
			bone[0] = 1;
			WriteProcessMemory(m_hTargetProcess, (void *)0xF0D8FF, bone, 1, NULL);

			//			HANDLE hRemoteThread = NULL;
			//			hRemoteThread = CreateRemoteThread(m_hTargetProcess, NULL, 0, (LPTHREAD_START_ROUTINE)0x6373D0, NULL, 0, NULL);
			//			WaitForSingleObject(hRemoteThread, INFINITE);
			//			CloseHandle(hRemoteThread);

		}
	}
	//invoke
	buff = 1;
	WriteProcessMemory(m_hTargetProcess, (void *)0xF0D7F0, &buff, 1, NULL);

	char b[2] = { 0x74, 0x0c };	//jz      short loc_63CFF3;
	char r[2] = { 0x90, 0x90 };	//nop;nop;

	WriteProcessMemory(m_hTargetProcess, (void *)0x063CFE5, &r, 2, NULL);

	while (ResumeThread(m_hTargetProcess) != -1);

	while (true) {
		ReadProcessMemory(m_hTargetProcess, (void *)0xF0D7F4, &buff, 4, NULL);
		if (buff != 0) {
			DWORD buff1 = 0;
			ReadProcessMemory(m_hTargetProcess, (void *)(buff + 0x20), &buff1, 4, NULL);
			if (buff1 == 0)break;
		}
		else {
			break;
		}
	}

	//DisableThread
	SuspendThread(m_hTargetProcess);

	WriteProcessMemory(m_hTargetProcess, (void *)0x063CFE5, &b, 2, NULL);
	buff = 0;
	WriteProcessMemory(m_hTargetProcess, (void *)0xF0D7F0, &buff, 1, NULL);

	while (ResumeThread(m_hTargetProcess) != -1);

	CloseHandle(m_hTargetProcess);

	return 1;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//	Getting NPC hovering information
//	return string type
//		"Name: [%s] X:[%1.2f] Y:[%1.2f] Z:[%1.2f]"
/////////////////////////////////////////////////////////////////////////////////////////////////////

__declspec(dllexport) char * __cdecl GetHoveredNPCInfo()
{

	t_NPC_Information[0] = 0;
	if (NPC_Flag != 0) {
		strcpy_s(t_NPC_Information, m_NPC_Information);
	}
	return t_NPC_Information;
}

/////////////////////////////////////////////////////////////////////////////////////////////////////
//	Getting GameXYZ hovering information
//  return string type
//		"X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]" , where D is the distance from player character
/////////////////////////////////////////////////////////////////////////////////////////////////////
__declspec(dllexport) char * __cdecl GetMouseXYZInGame()
{
	t_Mouse_Information[0] = 0;
	strcpy_s(t_Mouse_Information, m_Mouse_Information);
	return t_Mouse_Information;
}

int CHookCostumeApp::ExitInstance()
{
	if (m_HookDLGThread != NULL) {
		m_HookDLG.SendMessage(WM_CLOSE, 0, 0);
		Sleep(1000);
		//		TerminateThread(m_HookDLGThread, 0);
	}
	return CWinApp::ExitInstance();
}
