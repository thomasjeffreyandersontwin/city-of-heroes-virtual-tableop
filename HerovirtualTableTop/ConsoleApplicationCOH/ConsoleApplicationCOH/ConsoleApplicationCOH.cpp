// ConsoleApplicationCOH.cpp : Defines the entry point for the console application.
//

#include "stdafx.h"

/*
int main()
{
return 0;
}
*/
#include "stdafx.h"
#include <iostream>
#include <limits>
//#include <synchapi.h>
////////////////////////////////
void LoadDLL();
void Close_Game();
bool Check_GameDone();
void ExecuteCommand(char *commandline);
char *GetHoveredNPCInfo();
char *GetMouseXYZInGame();
void SetHWND();
//int GetCollisionBetween(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d);
char* GetCollisionBetween(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z);

////////////////////////////////

using namespace std;

bool b_flag = false;//
int main(int argc, char **argv)
{
	char bb[1024];
	sprintf_s(bb, "%x", argc);
	if (argc > 1) {
		if (strstr(argv[1], "-d")) {
			b_flag = true;
		}
	}

	//	cout << argv[0] << endl;
	//	cout << argv[1] << endl;
	//	cout << bb << endl;


	//Load HookCostume.dll and run game
	LoadDLL();

	//Set user hwnd for NPC hovering;
	SetHWND();

	while (!Check_GameDone()) {
		cout << "Game is running." << endl;
		//		Sleep(1000);
	}

	cout << "Game is successfully run." << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	cout << "Press Enter to spawn a sample character" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* spawncommand = "/spawn_npc model_Statesman Sample Character";

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	ExecuteCommand(spawncommand);

	cout << "Get collison point and distance from (137.5, 8.25, -112.0) to (137.5, 8.25, -15000.0)" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');

	float s_x = 137.5, s_y = 8.25, s_z = -112.0;	//start position (137.5, 8.25, -112.0)
	float d_x = 137.5, d_y = 8.25, d_z = -150.0;	//destination position (137.5, 8.25, -150.0)
	float c_x, c_y, c_z, c_d;				//collison point (c_x,x_y,c_z) & distance(c_d)
	char  dispstr[1024];

	for (int i = 0; i < 400; i++) {

		//GetCollisionBetween(s_x, s_y+i, s_z, d_x, d_y+i, d_z, &c_x, &c_y, &c_z, &c_d);
		char* xyzd = GetCollisionBetween(s_x, s_y + i, s_z, d_x, d_y + i, d_z);
		sprintf_s(dispstr, xyzd);
		cout << dispstr << endl;
		//if (c_x == 0 && c_y == 0 && c_z == 0) {
		//	//No colission
		//	//sprintf_s(dispstr, "from (X:[%1.2f] Y:[%1.2f] Z:[%1.2f]) to (X:[%1.2f] Y:[%1.2f] Z:[%1.2f]) No collison distance D:[%1.2f]", s_x, s_y + i, s_z, d_x, d_y + i, d_z, c_d);
		//	cout << dispstr << endl;
		//}
		//else {
		//	sprintf_s(dispstr, "from (X:[%1.2f] Y:[%1.2f] Z:[%1.2f]) to (X:[%1.2f] Y:[%1.2f] Z:[%1.2f]) collison point X:[%1.2f] Y:[%1.2f] Z:[%1.2f] D:[%1.2f]", s_x, s_y + i, s_z, d_x, d_y + i, d_z, c_x, c_y, c_z, c_d);
		//	cout << dispstr << endl;
		//}
	}

	cout << "Get mouse hovering or target NPC information 3 time " << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* npcinfo;
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	npcinfo = GetHoveredNPCInfo();
	cout << npcinfo << endl;


	cout << "Get mouse hovering X, Y, Z information 3 time " << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* mouseinfo;
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	mouseinfo = GetMouseXYZInGame();
	cout << mouseinfo << endl;

	cout << "Press Enter to loadcostume" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* loadcostumecommand = "/load_costume spy";
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	ExecuteCommand(loadcostumecommand);

	/*HERE GOES YOUR CALL TO SEND "spawncommand" TO THE GAME*/

	cout << "Check if the characer is there. Press Enter to delete it" << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	char* clearcommand = "/clear_npc";
	ExecuteCommand(clearcommand);

	/*HERE GOES YOUR CALL TO SEND "clearcommand" TO THE GAME*/

	cout << "Check if the characer is gone. Press Enter to terminate..." << endl;
	cin.ignore(numeric_limits<streamsize>::max(), '\n');
	Close_Game();

	return 0;
}

//this function is do load HookCostume.dll

#include <windows.h>

//Export function from HookCostume.dll
HMODULE dllhandle = NULL;
typedef BOOL InitGame(int hWnd, char *gamepath);
typedef BOOL CloseGame(HWND hWnd);
typedef BOOL SetUserHWND(HWND hWnd);
typedef int  Execute_CMD(char *commandline);
typedef char *TGetHoveredNPCInfo();
typedef char *TGetMouseXYZInGame();
//typedef int TCollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d);
typedef char *TCollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z);
typedef BOOL CheckGameDone();

//////////////////////////////////////
void LoadDLL()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	InitGame *Func_InitGame;
	if (dllhandle != NULL) {
		Func_InitGame = (InitGame *)GetProcAddress(dllhandle, "InitGame");
		if (Func_InitGame != NULL) {
			(Func_InitGame)(0, "D:\Work\Freelancing\Hero Virtual Table Top\Game");
			//if b_flag == false, Dialog Display
			//if true, non Dialog
		}
	}

}

bool Check_GameDone()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	CheckGameDone *Func_CheckGameDone;
	if (dllhandle != NULL) {
		Func_CheckGameDone = (CheckGameDone *)GetProcAddress(dllhandle, "CheckGameDone");
		if (Func_CheckGameDone != NULL) {
			return (Func_CheckGameDone)();
		}
	}
	return true;
	//	FreeLibrary(dllhandle);
}

void Close_Game()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	CloseGame *Func_CloseGame;
	if (dllhandle != NULL) {
		Func_CloseGame = (CloseGame *)GetProcAddress(dllhandle, "CloseGame");
		if (Func_CloseGame != NULL) {
			(Func_CloseGame)(NULL);
		}
	}
	//	FreeLibrary(dllhandle);
}

void ExecuteCommand(char *commandline)
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	Execute_CMD *Func_Execute_CMD;
	if (dllhandle != NULL) {
		Func_Execute_CMD = (Execute_CMD *)GetProcAddress(dllhandle, "ExecuteCommand");
		if (Func_Execute_CMD != NULL) {
			(Func_Execute_CMD)(commandline);
		}
	}

}

void SetHWND()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	SetUserHWND *Func_SetUserHWND;
	if (dllhandle != NULL) {
		Func_SetUserHWND = (SetUserHWND *)GetProcAddress(dllhandle, "SetUserHWND");
		if (Func_SetUserHWND != NULL) {
			(Func_SetUserHWND)(NULL);
		}
	}

}
char *GetHoveredNPCInfo()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	TGetHoveredNPCInfo *Func_GetHoveredNPCInfo;
	if (dllhandle != NULL) {
		Func_GetHoveredNPCInfo = (TGetHoveredNPCInfo *)GetProcAddress(dllhandle, "GetHoveredNPCInfo");
		if (Func_GetHoveredNPCInfo != NULL) {
			return (Func_GetHoveredNPCInfo)();
		}
	}
	return "";
}

char *GetMouseXYZInGame()
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	TGetMouseXYZInGame *Func_GetMouseXYZInGame;
	if (dllhandle != NULL) {
		Func_GetMouseXYZInGame = (TGetHoveredNPCInfo *)GetProcAddress(dllhandle, "GetMouseXYZInGame");
		if (Func_GetMouseXYZInGame != NULL) {
			return (Func_GetMouseXYZInGame)();
		}
	}
	return "";
}

//int GetCollisionBetween(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d)
//{
//	dllhandle = ::LoadLibraryA("HookCostume.dll");
//
//	TCollisionDetection *Func_CollisionDetection;
//	if (dllhandle != NULL) {
//		Func_CollisionDetection = (TCollisionDetection *)GetProcAddress(dllhandle, "CollisionDetection");
//		if (Func_CollisionDetection != NULL) {
//			return (Func_CollisionDetection)(s_x, s_y, s_z, d_x, d_y, d_z, c_x, c_y, c_z, c_d);
//		}
//	}
//	return 0;
//}

char* GetCollisionBetween(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z)
{
	dllhandle = ::LoadLibraryA("HookCostume.dll");

	TCollisionDetection *Func_CollisionDetection;
	if (dllhandle != NULL) {
		Func_CollisionDetection = (TCollisionDetection *)GetProcAddress(dllhandle, "CollisionDetection");
		if (Func_CollisionDetection != NULL) {
			return (Func_CollisionDetection)(s_x, s_y, s_z, d_x, d_y, d_z);
		}
	}
	return 0;
}

