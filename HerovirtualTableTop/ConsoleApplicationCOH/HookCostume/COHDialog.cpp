// OHDialog.cpp : implementation file
//
#include "stdafx.h"
#include "HookCostume.h"
#include "COHDialog.h"
#include "afxdialogex.h"


// COHDialog dialog

IMPLEMENT_DYNAMIC(COHDialog, CDialogEx)

COHDialog::COHDialog(CWnd* pParent /*=NULL*/)
: CDialogEx(IDD_DIALOG1, pParent)
{
	//	WM_MOUSEMOVE
}

COHDialog::~COHDialog()
{
}

void COHDialog::DoDataExchange(CDataExchange* pDX)
{
	CDialogEx::DoDataExchange(pDX);
	DDX_Control(pDX, IDC_EDIT1, m_NPCINFO);
	DDX_Control(pDX, IDC_EDIT2, m_command);
	DDX_Control(pDX, IDC_EDIT3, m_mousepos);
	DDX_Control(pDX, IDC_EDIT4, m_stagepos);
	DDX_Control(pDX, IDC_EDIT5, m_SourceXYZ);
	DDX_Control(pDX, IDC_EDIT6, m_DestXYZ);
	DDX_Control(pDX, IDC_EDIT7, m_CollisionXYZ);
}


BEGIN_MESSAGE_MAP(COHDialog, CDialogEx)
	ON_BN_CLICKED(IDOK, &COHDialog::OnBnClickedOk)
	ON_WM_TIMER()
	ON_BN_CLICKED(IDC_BUTTON1, &COHDialog::OnBnClickedButton1)
	ON_BN_CLICKED(IDC_BUTTON2, &COHDialog::OnBnClickedButton2)
END_MESSAGE_MAP()

// COHDialog message handlers
__declspec(dllexport) int __cdecl ExecuteCommand(char *cmdstring);

char strcommand[1024];
void COHDialog::OnBnClickedOk()
{
	TCHAR buff[1024];
	m_command.GetWindowTextW(buff, 1024);

	USES_CONVERSION;
	sprintf_s(strcommand, "%s", W2A(buff));

	ExecuteCommand(strcommand);

	//CDialogEx::OnOK();
}


BOOL COHDialog::OnInitDialog()
{
	CDialogEx::OnInitDialog();

	CRect cRC;
	GetClientRect(&cRC);
	INT	nSWidth = GetSystemMetrics(SM_CXFULLSCREEN);
	INT	nSHeight = GetSystemMetrics(SM_CYFULLSCREEN);
	::SetWindowPos(m_hWnd, HWND_TOPMOST, nSWidth - cRC.Width(), 0, cRC.Width(), cRC.Height(), SWP_SHOWWINDOW | SWP_NOSIZE);

	m_command.SetWindowText(_T("/loadcostume Agents of Orisha 1\\Agents of Orisha 1_BeamRifle_PenetratingRay.fx x=0 y=0.5 z=10"));

	SetTimer(1, 50, NULL); //timer for NPC hovering

	SetTimer(2, 50, NULL);	//timer for mouse x,y,z

	m_SourceXYZ.SetWindowText(_T("X:[137.5] Y:[8.5] Z:[-112.0]"));
	m_DestXYZ.SetWindowText(_T("X:[137.5] Y:[8.5] Z:[-150.0]"));

	//	char buff[1024];
	//	sprintf_s(buff, "%x", gamePID);
	//	USES_CONVERSION;
	//	SetWindowText(A2W(buff));
	//	ManagerWND = this.m_hWnd;
	return TRUE;  // return TRUE unless you set the focus to a control
	// EXCEPTION: OCX Property Pages should return FALSE
}

void SetNPC();
void SetXYZ();
LRESULT COHDialog::WindowProc(UINT message, WPARAM wParam, LPARAM lParam)
{
	switch (message) {
	case WM_USER + 101:
		//			KillTimer(1);
		//NPC hovering display
		//			SetNPCNo(wParam);
		//			SetTimer(1, 100, NULL);
		break;
	case WM_USER + 102:
		//Mouse Pos X,Y,Z
		SetXYZ();// wParam, lParam);
		break;
	}

	return CDialogEx::WindowProc(message, wParam, lParam);
}


void COHDialog::OnTimer(UINT_PTR nIDEvent)
{
	switch (nIDEvent){
	case 1:
		//NPC hovering display
		SetNPC();
		break;
	case 2:
		//Mouse Pos X,Y,Z
		SetXYZ();// wParam, lParam);
		break;

	}

	CDialogEx::OnTimer(nIDEvent);
}

void GetStringXYZ(char *buff, float *x, float *y, float *z)
{
	char *xstr = strstr(buff, "X:[");
	char *ystr = strstr(buff, "Y:[");
	char *zstr = strstr(buff, "Z:[");
	if (xstr != NULL) {
		char *t = strstr(xstr, "]");
		if (t != NULL) {
			t[0] = 0;
		}
		xstr += 3;
		sscanf_s(xstr, "%f", x);
	}
	if (ystr != NULL) {
		char *t = strstr(ystr, "]");
		if (t != NULL) {
			t[0] = 0;
		}
		ystr += 3;
		sscanf_s(ystr, "%f", y);
	}
	if (zstr != NULL) {
		char *t = strstr(zstr, "]");
		if (t != NULL) {
			t[0] = 0;
		}
		zstr += 3;
		sscanf_s(zstr, "%f", z);
	}

}
//__declspec(dllexport) int __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z, float *c_x, float *c_y, float *c_z, float *c_d);
__declspec(dllexport) char* __cdecl CollisionDetection(float s_x, float s_y, float s_z, float d_x, float d_y, float d_z);
float c_x = 0, c_y = 0, c_z = 0, c_d = 0;
void COHDialog::OnBnClickedButton1()
{
	//Collision detection from source x,y,z to destination x,y,z
	float s_x = 0, s_y = 0, s_z = 0;
	float d_x = 0, d_y = 0, d_z = 0;

	TCHAR sourceXYZ[1024];
	TCHAR destXYZ[1024];
	char buff[1024];

	m_SourceXYZ.GetWindowText(sourceXYZ, 1024);
	USES_CONVERSION;
	sprintf_s(buff, "%s", W2A(sourceXYZ));
	GetStringXYZ(buff, &s_x, &s_y, &s_z);

	m_DestXYZ.GetWindowText(destXYZ, 1024);
	sprintf_s(buff, "%s", W2A(destXYZ));
	GetStringXYZ(buff, &d_x, &d_y, &d_z);

	char* res = CollisionDetection(s_x, s_y, s_z, d_x, d_y, d_z);
	buff[0] = 0;
	//	if (res != 0) {
	sprintf_s(buff, res);
	//	} else {
	//		sprintf_s(buff, "No collision");
	//	}
	m_CollisionXYZ.SetWindowText(A2W(buff));

}

extern DWORD gamePID;
void COHDialog::OnBnClickedButton2()
{
	if (c_x == 0 && c_y == 0 && c_z == 0)return;

	//Contirm collision
	DWORD dwPID = gamePID;// GetCurrentProcessId();
	HANDLE m_hTargetProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, dwPID);

	//DisableThread
	SuspendThread(m_hTargetProcess);
	DWORD buff;
	ReadProcessMemory(m_hTargetProcess, (void *)(0x012F6C44), &buff, 4, NULL);
	if (buff != NULL) {
		DWORD oldprotect;
		VirtualProtectEx(GetCurrentProcessId, (LPVOID*)(buff + 0x5C), 8, PAGE_READWRITE, &oldprotect);

		WriteProcessMemory(m_hTargetProcess, (void *)(buff + 0x5C), &c_x, 4, NULL);
		WriteProcessMemory(m_hTargetProcess, (void *)(buff + 0x60), &c_y, 4, NULL);
		WriteProcessMemory(m_hTargetProcess, (void *)(buff + 0x64), &c_z, 4, NULL);

		VirtualProtectEx(m_hTargetProcess, (LPVOID*)(buff + 0x5C), 8, oldprotect, &oldprotect);
	}
	while (ResumeThread(m_hTargetProcess) != -1);
	CloseHandle(m_hTargetProcess);

}
