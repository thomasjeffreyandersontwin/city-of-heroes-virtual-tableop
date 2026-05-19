// HookCostume.cpp : Defines the initialization routines for the DLL.
//

#include "stdafx.h"
#include "power.h"

#ifdef _DEBUG
#define new DEBUG_NEW
#endif

//#include "HookCostumeDialog.h"
//extern HookCostumeDialog m_HookDLG;

///////////////////////////////////////////
//	DEFINITION of HOOK ADDRESS
///////////////////////////////////////////

//for mouse hovering NPC
#define HOOK_MOUSE_HOVER_XYZ_START	0x78A001
#define HOOK_MOUSE_HOVER_XYZ_RETURN 0x78A006

//for mouse hovering x,y,z,d
#define HOOK_MOUSE_HOVER_START	0x4CD242//0x5EE0A1
#define HOOK_MOUSE_HOVER_RETURN 0x4CD2DF//0x5EE0A7

//for power direction
#define HOOK_POWER_DIRECTION_START	0x593A4B
#define HOOK_POWER_DIRECTION_RETURN 0x593A51

//for command for process power direction(x,y,z)
#define HOOK_COMMAND_START	0x41BC4C
#define HOOK_COMMAND_RETURN 0x41BC53

//for POPMENU Command process
#define HOOK_COMMAND_MENU_START	0x41BC63
#define HOOK_COMMAND_MENU_RETURN 0x41BC68

#define HOOK_MENU_LOAD_FUNC 0x04D84F0

/////////////////////////////////////////
//		mouse hovering NPC 
/////////////////////////////////////////
int		NPC_no=-1;		//NPC no respect NPC table during Mouse hover
void	SendToUser(DWORD NPC_no);


void MyMOUSEHOVER_SHOW()
{
	_asm{
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		push	ebp;
		push	eax;
		push	ebx;
		push	ecx;
		push	edx;
		push	esi;
		push	edi;

		mov	ebx, ebp;
		mov NPC_no, ebx;
	}
	//
	SendToUser(NPC_no);
	//
	_asm{
		pop	edi;
		pop	esi;
		pop	edx;
		pop	ecx;
		pop	ebx;
		pop	eax;
		pop	ebp;

		//original code
		//add     ebp, 0x31C4;
		//return original
		mov		ebx, HOOK_MOUSE_HOVER_RETURN
		jmp		ebx; 
	}
	return;
}

////////////////////////////////////////////////////////////////
//
//		Analysis extended command to fire powers in any direction
//
//		input:
//				command x:??? y:??? z:???
//				e.g 
//					/loadcostume spy x:100 y:50 z:100
//
//		output: 
//				fx,fy,fz	direction
////////////////////////////////////////////////////////////////

FLOAT	fx = 0;		// direction x,y,x to fire powers
FLOAT	fy = 0;
FLOAT	fz = 0;

char	*command = NULL;	//command line string 
char	*str_x=NULL;		//pos there x:??? occurs in command line
char	*str_y = NULL;		//pos there y:??? occurs in command line
char	*str_z = NULL;		//pos there z:??? occurs in command line

char	*str_pos = NULL;	//temporary string variable
int		int_var = NULL;		//temporary int variable
int		i;					//temporary index variable
int		hook_flag=0;

char	*mstrstr(char *str,char *sub)
{
	char *res = strstr(str, sub);
	if (res)return res;
	int len = strlen(str);
	for (int i = 0; i < 0x100; i++){
		if (strncmp(str + len + i, sub, strlen(sub))==0)
		{
			return str + len + i;
		}
	}
	return NULL;
}

void MyCOMMAND_LINE_proc()
{

	if (strchr(command, '\"'))return;
/*
	while (true) {
		char *p = strstr(command, ":");
		if (p == NULL) {
			break;
		}
		else {
			p[0] = '=';
		}
	}
*/
	str_x = mstrstr((char*)command, "x=");
	str_y = mstrstr((char*)command, "y=");
	str_z = mstrstr((char*)command, "z=");

	//parse of x:???
	if (str_x != NULL){
		str_x--;
		str_x[0] = 0;
		str_x++;
		str_pos = strstr(str_x + 2, " ");
		if (str_pos != NULL){
			str_pos[0] = 0;
		}
		sscanf_s(str_x + 2, "%f", &fx);
		hook_flag = 1;
	}
	//parse of y:???
	if (str_y != NULL){
		str_y--;
		str_y[0] = 0;
		str_y++;
		str_pos = strstr(str_y + 2, " ");
		if (str_pos != NULL){
			str_pos[0] = 0;
		}
		sscanf_s(str_y + 2, "%f", &fy);
		hook_flag = 1;
	}
	//parse of z:???
	if (str_z != NULL){
		str_z--;
		str_z[0] = 0;
		str_z++;
		str_pos = strstr(str_z + 2, " ");
		if (str_pos != NULL){
			str_pos[0] = 0;
		}
		sscanf_s(str_z + 2, "%f", &fz);
		hook_flag = 1;
	}
	// trim of command string
	if (str_x != NULL || str_y != NULL || str_z != NULL){
		int_var = strlen(command);
		for (i = strlen(command) - 1; i >= 0; i--){
			if (command[i] != 0x20){	//space check
				int_var = i + 1;
				break;
			}
		}
		command[int_var] = 0;
	}
}

void MyCOMMAND_LINE()
{
	_asm{
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;

		push	eax;
		push	ebx;
		push	ecx;
		push	edx;
		push	esi;
		push	edi;

		mov	command, esi;
		cmp esi, 0;
		je	skip;
	}

	MyCOMMAND_LINE_proc();

skip:
	_asm{
		pop	edi;
		pop	esi;
		pop	edx;
		pop	ecx;
		pop	ebx;
		pop	eax;

		//original code
		test esi, esi;
		mov  eax, 1;
		//return original
		mov	ecx, HOOK_COMMAND_RETURN;
		jmp	ecx; 
	}
	return;
}

////////////////////////////////////////////////////////////////
//
//		Execute fire powers in any direction
//
//		input:
//			fx, fy, fz	direction
//				
////////////////////////////////////////////////////////////////

void MyPOWER_DIRECTION()
{
	_asm{
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		pusha;
	}
	if (hook_flag == 1){
		_asm{
			mov edx, [ebp + 0x24];
			cmp edx, 0;
			jne  skip;
			mov edx, [ebp + 0x28];
			cmp edx, 0;
			jne  skip;
			mov edx, [ebp + 0x2C];
			cmp edx, 0;
			jne  skip;

			//change direction to fire powers

			mov	edx, fx;
			mov[ebp + 0x24], edx;
			mov	edx, fy;
			mov[ebp + 0x28], edx;
			mov	edx, fz;
			mov[ebp + 0x2C], edx;
		}
	}
skip:
	_asm{
		popa;
		//original code
		mov [eax + 0xA8], ecx;
		//return original
		mov	edx, HOOK_POWER_DIRECTION_RETURN;
		jmp	edx;		
	}
	return;
}

/////////////////////////////////////////////
//
//		Dynamically Load popmenu
//
//////////////////////////////////////////////

void MyCOMMAND_MENU_proc()
{
	char *tmp = strstr((char*)command, "popmenu");
	if (tmp != NULL){
		_asm{
			mov eax, HOOK_MENU_LOAD_FUNC;
			call eax;
		}
	}
}
//
void MyCOMMAND_MENU()
{
	_asm{
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;

		push	eax;
		push	ebx;
		push	ecx;
		push	edx;
		push	esi;
		push	edi;

		mov	command, esi;
		cmp esi, 0;
		je	skip;
	}
	MyCOMMAND_MENU_proc();
skip:
	_asm{
		pop	edi;
		pop	esi;
		pop	edx;
		pop	ecx;
		pop	ebx;
		pop	eax;

		//original code
		mov  ebx, 0xA7C2CC;
		push ebx;
		//return original
		mov	ebx, HOOK_COMMAND_MENU_RETURN;
		jmp	ebx; 
	}
	return;
}

///////////////////////////////////////////
//  Mouse hovering getting x,y,z,d
///////////////////////////////////////////
typedef	struct tagNPC_XYZ
{
	float flR[9];
	float flX;
	float flY;
	float flZ;
}NPC_XYZ, *PNPC_XYZ;

DWORD	mouse_pos[2];	//[0]:x [1]:y
float   stage_pos[4];	//[0]:x [1]:y [2]:z [3]:distance from character pos
DWORD   character_INFO;	//character information
NPC_XYZ character_XYZ;	//character positon information
char	bstr[0x100];	//temporary buffer
char	buff[1024];		//temporary buffer

void SendToUserXYZ(DWORD m_x, DWORD m_y, float s_x, float s_y, float s_z, float m_d);

void DispXYZValue()
{
	double m_sum = 0;
	memcpy(stage_pos, (void *)0xCAF5BC, 16);
	memcpy(&character_XYZ, (VOID *)(character_INFO + 0x38), sizeof(NPC_XYZ));
	m_sum += (stage_pos[0] - character_XYZ.flX) * (stage_pos[0] - character_XYZ.flX);
	m_sum += (stage_pos[1] - character_XYZ.flY) * (stage_pos[1] - character_XYZ.flY);
	m_sum += (stage_pos[2] - character_XYZ.flZ) * (stage_pos[2] - character_XYZ.flZ);
	float dist = sqrt(m_sum);

	SendToUserXYZ(mouse_pos[0], mouse_pos[1], stage_pos[0], stage_pos[1], stage_pos[2], dist);

}

/////////////////////////////////////////
//
//	Mouse hovering x, y, z
//
/////////////////////////////////////////
float r = 150.00;
float n = 150000.0;
void MyMouseHoverXYZ()
{
	_asm {
		nop;
		nop;
		nop;
		nop;
		nop;
		nop;
		pusha;
	}
	//	if (flag == 1){
	_asm {
		mov ebx, 0xE37F2C;	//ClientY
		mov[ebx], eax;		//ClientY

		mov eax, 0x12F6C40;	//NPC_Table_ENT
		cmp eax, 0;
		je  skip;
		mov eax, [eax + 4];
		cmp eax, 0;
		je  skip;

		lea ebx, character_INFO;
		mov[ebx], eax;

		mov ebx, 0x167151C;
		cmp[ebx], 0x1;
		je  skip;

		push eax;// [character_INFO];
		 //			push 0xE37F28;		//client_X

		mov eax, n;				//15000.0
		mov ebx, 0xBA2D54;		
		mov[ebx], eax;


		lea  ebx, mouse_pos;
		mov  eax, 0xE37F28;
		mov  eax, [eax];
		mov	 [ebx], eax;	//X

		mov  eax, 0xE37F2C;
		mov  eax, [eax];
		mov  [ebx + 4], eax;	//Y

		////////////////////////////////
		lea  ebx, mouse_pos;
		push ebx;

		lea  eax, bstr;
		mov  ebx, 0x5B5A40;	//GetXYZ
		call ebx;
		add esp, 8;

		//drag
		//			mov  ecx, 0xE37F4C;		//mouse click
		//			mov  ecx, [ecx];		
		//			cmp  ecx, 1;
		//			je	 skip;

		mov eax, 0x553B90;	//Move_Stop
		call eax;
	}
	//	}
	DispXYZValue();
skip:
	_asm {
//		mov eax, r;
//		mov ebx, 0xBA2D54;
//		mov [ebx], eax;

		popa;
		mov	eax, HOOK_MOUSE_HOVER_XYZ_RETURN;
		jmp	eax;
	}
	return;
}

/////////////////////////////////////
//	Hook process function
/////////////////////////////////////

void *DetourFunc(BYTE *src, const BYTE *dst, const int len)
{
	BYTE *jmp = (BYTE*)malloc(len+5);
	DWORD dwback;

	VirtualProtect(src, len, PAGE_READWRITE, &dwback);

	memcpy(jmp, src, len);	jmp += len;

	jmp[0] = 0xE9;
	*(DWORD*)(jmp+1) = (DWORD)(src+len - jmp) - 5;

	src[0] = 0xE9;
	*(DWORD*)(src+1) = (DWORD)(dst - src) - 5;

	VirtualProtect(src, len, dwback, &dwback);

	return (jmp - len);
}

BOOL PowerHook()
{
	//MOUSE HOVER NPC
	DetourFunc(
		(BYTE*)HOOK_MOUSE_HOVER_START,
		(BYTE*)MyMOUSEHOVER_SHOW + 7, 
		5);

	//POWER FIRE in Direction fx,fy,fz
	DetourFunc(
		(BYTE*)HOOK_POWER_DIRECTION_START,
		(BYTE*)MyPOWER_DIRECTION + 7,
		5);

	//COMMAND LINE PROCESS EXETEND
	DetourFunc(
		(BYTE*)HOOK_COMMAND_START,
		(BYTE*)MyCOMMAND_LINE + 9,
		5);

	//POPMEMU COMMAND PROCESS EXETEND
	DetourFunc(
		(BYTE*)HOOK_COMMAND_MENU_START,
		(BYTE*)MyCOMMAND_MENU + 9,
		5);

	//Get x,y,z where mouse hover
	DetourFunc(
		(BYTE*)HOOK_MOUSE_HOVER_XYZ_START,
		(BYTE*)MyMouseHoverXYZ + 9,
		5);

	return TRUE;
}


